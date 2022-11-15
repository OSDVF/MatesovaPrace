using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Json;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
using MatesovaPrace.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.Storage;
using Newtonsoft.Json;
using CommunityToolkit.Mvvm.Input;
#if __WASM__
using Uno.Foundation;
#endif

namespace MatesovaPrace
{
    public sealed partial class LoginPage : Page
    {
        const string configName = "appsettings.json";

        internal static LoginModel _data = new LoginModel();
        UserCredential? credential;
        private DriveService? drive;

        internal Action<ConnectionModel>? OnConnected { get; set; }

        public Visibility SelectedFileInfoVisible => _data.SelectedFile == null ? Visibility.Collapsed : Visibility.Visible;
        public Visibility NextPageVisible => string.IsNullOrEmpty(_data.NextPageToken) ? Visibility.Collapsed : Visibility.Visible;
        public LoginPage()
        {
            InitializeComponent();
            DataContext = _data;

            // Log in
            GDriveAuth();

            // Load cached settings
            LoadSettings();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is Action<ConnectionModel> connected)
            {
                OnConnected = connected;
            }
            base.OnNavigatedTo(e);
        }

        async void LoadSettings()
        {
            var cFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(configName, CreationCollisionOption.OpenIfExists);
            using (var stream = await cFile.OpenStreamForReadAsync())
            {
                try
                {
                    using var sReader = new StreamReader(stream);
                    var reader = new JsonTextReader(sReader);
                    var serializer = new Newtonsoft.Json.JsonSerializer();
                    dynamic settings = serializer.Deserialize(reader);

                    if (settings.DataSource != null)
                    {
                        if (settings.DataSource.SheetId != null)
                        {
                            _data.SheetId = settings.DataSource.SheetId;
                        }
                    }
                }
                catch(Exception)
                {
                    Console.WriteLine($"Could not read {configName}");
                }
            }
        }

        private async void Manual_Click(object sender, RoutedEventArgs e)
        {
            await GDriveAuth();
            if (drive != null)
            {
                var fileInfo = await drive.Files.Get(_data.SheetId).ExecuteAsync();
                if (fileInfo == null)
                {
                    await (new ContentDialog
                    {
                        Title = "Not found",
                        Content = $"File {_data.SheetId} not found"
                    }).ShowAsync();
                }
                else
                {
                    _data.SelectedFile = FileToListModel(fileInfo);
                }
            }
            else
            {
                await (new ContentDialog
                {
                    Title = "Not connected",
                    Content = "Connect to Google Drive first"
                }).ShowAsync();
            }
        }

        public async Task GDriveAuth()
        {
            if (credential != null)
            {
                return;
            }
#if __WASM__
            WebAssemblyRuntime.InvokeJS("open(\"https://accounts.google.com/o/oauth2/v2/auth?scope=https%3A//www.googleapis.com/auth/drive&include_granted_scopes=true&response_type=token&redirect_uri=\""+
                "+encodeURI(location)+"
#if DEBUG
                + "\"&client_id=859872582086-aej576ehl3r10lgljrc0an8m44jj4io7.apps.googleusercontent.com\")");
#else
                + "\"&client_id=859872582086-3gge5prcrrmo08navcfbajddiar5earp.apps.googleusercontent.com\")");
#endif
#else
            var resourceLoader = ResourceLoader.GetForCurrentView();
            var clientSecretString = resourceLoader.GetString("client");
            var secretContainer = NewtonsoftJsonSerializer.Instance.Deserialize<GoogleClientSecrets>(clientSecretString);
            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secretContainer.Secrets,
                new[] {
                        SheetsService.Scope.Spreadsheets,
                        DriveService.Scope.Drive
                },
                "user", CancellationToken.None);
#endif
            drive = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "MatesovaPrace",
            });
        }

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            await GDriveAuth();
            SearchFiles();
        }

        FileListModel FileToListModel(Google.Apis.Drive.v3.Data.File file)
        {
            return new FileListModel
            {
                Id = file.Id,
                Name = file.Name,
                Info = file
            };
        }

        void SearchFiles(bool morePages = false)
        {
            var listRequest = drive.Files.List();
            listRequest.IncludeItemsFromAllDrives = _data.IncludeSharedFiles;
            listRequest.Q = _data.FindString;
            listRequest.Corpora = "allDrives";
            listRequest.SupportsAllDrives = true;
            if (morePages)
            {
                listRequest.PageToken = _data.NextPageToken;
            }
            var fileList = listRequest.Execute();
            _data.FoundFiles.Clear();
            foreach (var file in fileList.Files)
            {
                var foundFile = FileToListModel(file);
                _data.FoundFiles.Add(foundFile);
            }
            _data.NextPageToken = fileList.NextPageToken;
        }

        RelayCommand<FileListModel> MarkFileAsSelected = new RelayCommand<FileListModel>((FileListModel file) =>
        {
            _data.SelectedFile = file;
        });

        void NextPage_Click(object sender, RoutedEventArgs e)
        {
            SearchFiles(true);
        }

        void Back_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }
    }
}