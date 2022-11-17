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
using System.Net.Http;
using System.Net.Http.Json;
#if __WASM__
using Uno.Foundation;
#endif

namespace MatesovaPrace
{
    public sealed partial class LoginPage : Page
    {
        const string configName = "appsettings.json";

        internal static LoginModel _data = new();
        UserCredential? credential;
        private DriveService? drive;
        private XPlatformCodeReceiver receiver = new XPlatformCodeReceiver();
        internal Action<ConnectionModel>? OnConnected { get; set; }

        public Visibility SelectedFileInfoVisible => _data.SelectedFile == null ? Visibility.Collapsed : Visibility.Visible;
        public Visibility NextPageVisible => string.IsNullOrEmpty(_data.NextPageToken) ? Visibility.Collapsed : Visibility.Visible;
        public LoginPage()
        {
            InitializeComponent();
            DataContext = _data;
#if WINDOWS
            var app = (Application.Current as App);
            app?.MainWindow.SetTitleBar((UIElement)((AppBarElementContainer)AppBar.PrimaryCommands[1]).Content);
#endif
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is Tuple<Action<ConnectionModel>, string> connectedAndAuth)
            {
                OnConnected = connectedAndAuth.Item1;
                // Generate custom user credential object from auth code passed in the URL
                var client = new HttpClient();
                var sec = GetClient().Secrets;
                var redirectUri = "http://127.0.0.1";
#if __WASM__
                redirectUri = WebAssemblyRuntime.InvokeJS("location.origin");
#endif
                client.GetFromJsonAsync<dynamic>("https://oauth2.googleapis.com/token?code=" + connectedAndAuth.Item2 +
                    "&client_id=" + sec.ClientId +
                    "&client_secret=" + sec.ClientSecret +
                    "&grant_type=authorization_code" +
                    "&redirect_uri=" + redirectUri
                    ).ContinueWith(access =>
                            {
                                credential = new UserCredential(new Google.Apis.Auth.OAuth2.Flows.GoogleAuthorizationCodeFlow(
                            new Google.Apis.Auth.OAuth2.Flows.GoogleAuthorizationCodeFlow.Initializer
                            {
                                ClientSecrets = sec
                            }
                            ), "user", new Google.Apis.Auth.OAuth2.Responses.TokenResponse
                            {
                                AccessToken = access.Result.access_token,
                                ExpiresInSeconds = access.Result.expire_in,
                                IssuedUtc = DateTime.UtcNow,
                                Scope = access.Result.scope,
                                TokenType = "Bearer"
                            }
                        );
                            });

                try
                {
                    Console.WriteLine("Creating drive service #2");
                    CreateDriveService();
                    LoadSettings();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to create drive service because {ex.Message} {ex.StackTrace}");
                }
            }
            else
            {
                if (e.Parameter is Action<ConnectionModel> connected)
                {
                    OnConnected = connected;
                }

                // Load cached settings
                LoadSettings();
                // Show log in page or load offline login
                GDriveAuth();
            }
            base.OnNavigatedTo(e);
        }

        async void LoadSettings()
        {
            Console.WriteLine("Loading settings");
            try
            {
                var cFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(configName, CreationCollisionOption.OpenIfExists);
                using var stream = await cFile.OpenStreamForReadAsync();
                try
                {
                    Console.WriteLine("Deserializer");
                    using var sReader = new StreamReader(stream);
                    var reader = new JsonTextReader(sReader);
                    var serializer = new Newtonsoft.Json.JsonSerializer();
                    Console.WriteLine("Deserializing");
                    dynamic settings = serializer.Deserialize(reader);
                    Console.WriteLine("D 2");
                    if (settings.DataSource != null)
                    {
                        Console.WriteLine("D 3");
                        if (settings.DataSource.SheetId != null)
                        {
                            Console.WriteLine("D 4");
                            _data.SheetId = settings.DataSource.SheetId;
                        }
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine($"Could not read {configName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to config file {ex.Message} {ex.StackTrace}");
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
                Console.WriteLine("Already authenticated");
                return;
            }
#if __WASM__
            Console.WriteLine("Opening web authentication");
            WebAssemblyRuntime.InvokeJS("open(\"https://accounts.google.com/o/oauth2/v2/auth?scope=https%3A//www.googleapis.com/auth/drive&include_granted_scopes=true&redirect_uri=\"" +
                "+encodeURI(location)+"
#if DEBUG
                + "\"&client_id=859872582086-aej576ehl3r10lgljrc0an8m44jj4io7.apps.googleusercontent.com&response_type=code\")");
#else
                + "\"&client_id=859872582086-3gge5prcrrmo08navcfbajddiar5earp.apps.googleusercontent.com&response_type=token\")");
#endif
            Frame.Navigate(typeof(WebLoginMessage));
            return;
#else
            Console.WriteLine("Authenticating for offline use");
            GoogleClientSecrets secretContainer = GetClient();
            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secretContainer.Secrets,
                new[] {
                        SheetsService.Scope.Spreadsheets,
                        DriveService.Scope.Drive
                },
                "user", CancellationToken.None,null, receiver);
#endif
            Console.WriteLine("Creating drive service #1");
            CreateDriveService();
        }

        private void CreateDriveService()
        {
            drive = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "MatesovaPrace",
            });
            Console.WriteLine("GDrive service created");
        }

        private static GoogleClientSecrets GetClient()
        {
            var resourceLoader = ResourceLoader.GetForViewIndependentUse();
            var clientSecretString = resourceLoader.GetString("client");
            return NewtonsoftJsonSerializer.Instance.Deserialize<GoogleClientSecrets>(clientSecretString);
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

        async void SearchFiles(bool morePages = false)
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
            var fileList = await listRequest.ExecuteAsync();
            _data.FoundFiles.Clear();
            foreach (var file in fileList.Files)
            {
                var foundFile = FileToListModel(file);
                _data.FoundFiles.Add(foundFile);
            }
            _data.NextPageToken = fileList.NextPageToken;
        }

        RelayCommand<FileListModel> MarkFileAsSelected { get; set; } = new RelayCommand<FileListModel>((FileListModel file) =>
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