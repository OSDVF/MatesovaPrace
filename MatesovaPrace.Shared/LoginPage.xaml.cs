using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Json;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
using MatesovaPrace.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Newtonsoft.Json;
using CommunityToolkit.Mvvm.Input;
using System.Net.Http;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Uno.Extensions;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Auth.OAuth2.Responses;
using System.Net.Sockets;
using Windows.Graphics.Imaging;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.ComponentModel;
#if __WASM__
using Uno.Foundation;
#endif

namespace MatesovaPrace
{
    public sealed partial class LoginPage : Page
    {
        const string configName = "appsettings.json";

        internal static LoginModel _data = new();
        private DriveService? drive;
        private App? app;
        public FileDetailView fileDetailView;
        public LoginPage()
        {
            InitializeComponent();
            DataContext = _data;
#if WINDOWS
            app = (Application.Current as App);
            SetTitleBar();
            SizeChanged += ResetTitlebar;
#endif
            ShowFileSelectDialog = new RelayCommand<FileListModel>(SelectFileDialog);
            Loaded += LoginPage_Loaded;
        }

        public RelayCommand<FileListModel> ShowFileSelectDialog { get; set; }
        internal Action<IDataSource?>? OnConnected { get; set; }
        public Visibility NextPageVisible => string.IsNullOrEmpty(_data.NextPageToken) ? Visibility.Collapsed : Visibility.Visible;

        private async void LoginPage_Loaded(object sender, RoutedEventArgs e)
        {
            fileDetailView = new FileDetailView()
            {
                DataContext = _data,
                XamlRoot = XamlRoot
            };

            // Inject client secret into configuration
            try
            {
                var _ = GDriveSource.Client;
            }
            catch (Exception)
            {
                await new ContentDialog
                {
                    Content = "Application was compiled without Google Client token",
                    Title = "Error",
                    XamlRoot = XamlRoot,
                    CloseButtonText = "Dismiss"
                }.ShowAsync();
            }
        }

        private void SetTitleBar()
        {
            app?.MainWindow.SetTitleBar(AppBar);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            SizeChanged -= ResetTitlebar;
        }

        void ResetTitlebar(object sender, SizeChangedEventArgs e)
        {
            SetTitleBar();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is Tuple<Action<IDataSource>, string> connectedAndAuth)
            {
                OnConnected = connectedAndAuth.Item1;
                // Exchange code for an access token
                var client = new HttpClient();
                var redirectUri = "http://127.0.0.1/";//Default fallback
#if __WASM__
                // Redirect back to where we are right now
                redirectUri = WebAssemblyRuntime.InvokeJS("location.origin") + "/";
#endif
                GDriveSource.FromAuthCode(connectedAndAuth.Item2, redirectUri).ContinueWith(s => _data.Source = s.Result);
                CreateDriveService();
                LoadSettings();
            }
            else
            {
                if (e.Parameter is Action<IDataSource> connected)
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

        private async void Manual_Click(object sender, RoutedEventArgs _e)
        {
            await GDriveAuth();
            if (drive != null)
            {
                var request = drive.Files.Get(_data.SheetId);
                request.SupportsAllDrives = true;
                Google.Apis.Drive.v3.Data.File? fileInfo = null;
                Exception? ex = null;
                try
                {
                    fileInfo = await request.ExecuteAsync();
                }
                catch (Exception e)
                {
                    ex = e;
                }
                if (fileInfo == null)
                {
                    var dialog = new ContentDialog
                    {
                        Title = "Not found",
                        Content = $"File {_data.SheetId} not found.",
                        XamlRoot = XamlRoot
                    };
                    if (ex != null)
                    {
                        (dialog.Content) += $"\nAn error occured: ${ex}";
                    }
                }
                else
                {
                    SelectFileDialog(FileToListModel(fileInfo));
                }
            }
            else
            {
                await (new ContentDialog
                {
                    Title = "Not connected",
                    Content = "Connect to Google Drive first",
                    XamlRoot = XamlRoot
                }).ShowAsync();
            }
        }

        public async Task GDriveAuth()
        {
            if (_data.Source?.Credential != null)
            {
                Console.WriteLine("Already authenticated");
                return;
            }
            var token = await GDriveSource.ObjectStorage.GetAsync<TokenResponse>("user");
            if (token == null)
            {
#if __WASM__
            
                Console.WriteLine("Opening web authentication");
                WebAssemblyRuntime.InvokeJS("open(\"https://accounts.google.com/o/oauth2/v2/auth?scope=https%3A//www.googleapis.com/auth/drive&redirect_uri=\"" +
                    "+encodeURI(location)+"
#if DEBUG
                + "\"&client_id=859872582086-aej576ehl3r10lgljrc0an8m44jj4io7.apps.googleusercontent.com&response_type=code&access_type=offline\")");
                //+ "\"&client_id=859872582086-3gge5prcrrmo08navcfbajddiar5earp.apps.googleusercontent.com&response_type=code\")");
#else
                    + "\"&client_id=859872582086-3gge5prcrrmo08navcfbajddiar5earp.apps.googleusercontent.com&response_type=token\")");
            
#endif
                Frame.Navigate(typeof(WebLoginMessage));
                return;
#else
                Console.WriteLine("Authenticating for offline use");
                _data.Source = await GDriveSource.AuthorizeAsync();
#endif
                Console.WriteLine("Creating drive service #1");
            }
            else
            {
                _data.Source = new GDriveSource(new UserCredential(GDriveSource.GetFlow(), "user", token));
                Console.WriteLine("Loaded credentials from cache");
            }
            CreateDriveService();
        }

        private void CreateDriveService()
        {
            drive = new DriveService(new DriveService.Initializer
            {
                HttpClientInitializer = _data.Source.Credential,
                ApplicationName = "MatesovaPrace"
            });
            Console.WriteLine("GDrive service created");
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
                Mime = file.MimeType
            };
        }

        async void SearchFiles(bool morePages = false)
        {
            _data.Searching = true;
            var listRequest = drive.Files.List();
            listRequest.IncludeItemsFromAllDrives = _data.IncludeSharedFiles;
            listRequest.Q = _data.FindString;
            listRequest.Corpora = "allDrives";
            listRequest.SupportsAllDrives = true;
            if (morePages)
            {
                listRequest.PageToken = _data.NextPageToken;
            }
            try
            {
                var fileList = await listRequest.ExecuteAsync();
                _data.FoundFiles.Clear();
                foreach (var file in fileList.Files)
                {
                    var foundFile = FileToListModel(file);
                    _data.FoundFiles.Add(foundFile);
                }
                _data.NextPageToken = fileList.NextPageToken;
                _data.Searching = false;
            }
            catch (HttpRequestException e)
            {
                await new ContentDialog
                {
                    Content = e.Message,
                    Title = "Network Error",
                    XamlRoot = XamlRoot,
                    CloseButtonText = "Dismiss"
                }.ShowAsync();
                _data.Searching = false;
            }
            catch (Exception e)
            {
                await new ContentDialog
                {
                    Content = e.Message,
                    Title = "General Error",
                    XamlRoot = XamlRoot,
                    CloseButtonText = "Dismiss"
                }.ShowAsync();
                _data.Searching = false;
            }
        }

        async void SelectFileDialog(FileListModel? file)
        {
            if (file == null)
            {
                _data.SelectedFile = null;
                _data.DetailLoading = false;
                return;
            }
            _data.DetailLoading = true;
            _data.SelectedFile = null;
            try
            {
                var request = drive.Files.Get(file.Id);
                BitmapImage img = new BitmapImage();
                request.SupportsAllDrives = true;
                request.Fields = "thumbnailLink";
                var futureDialog = fileDetailView.ShowAsync();
                var fileInfo = await request.ExecuteAsync();
                var image = await drive.HttpClient.GetByteArrayAsync(fileInfo.ThumbnailLink);
                _data.SelectedFile = new FileDetailModel(file, image);
                _data.DetailLoading = false;
                if (await futureDialog == ContentDialogResult.Primary)
                {
                    if (OnConnected == null)
                    {
                        fileDetailView.Hide();
                        await new ContentDialog
                        {
                            Title = "Not Connected",
                            Content = "Google Drive Service is not connected",
                            XamlRoot = XamlRoot,
                            CloseButtonText = "Dismiss"
                        }.ShowAsync();
                    }
                    else
                    {
                        GDriveSource.ObjectStorage.StoreAsync<string>("sheetId", file.Id);
                        _data.Source!.SheetId = file.Id;
                        OnConnected(_data.Source);
                        Frame.GoBack();
                    }
                }
            }
            catch (Exception e)
            {
                _data.DetailLoading = false;
                fileDetailView.Hide();
                await new ContentDialog
                {
                    Title = "Error Getting File Detail",
                    Content = e.InnerException is null ? e.Message : e.Message + e.InnerException.Message,
                    XamlRoot = XamlRoot,
                    CloseButtonText = "Dismiss"
                }.ShowAsync();
            }

        }

        void NextPage_Click(object sender, RoutedEventArgs e)
        {
            SearchFiles(true);
        }

        void Back_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }

        public void Logout_Click(object sender, RoutedEventArgs e)
        {
            GDriveSource.ObjectStorage.DeleteAsync<TokenResponse>("user");
            _data.Source.Credential = null;
            OnConnected?.Invoke(null);
        }

        private void CommandBar_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var cb = (sender as CommandBar);
            cb.IsOpen = !cb.IsOpen;
        }
    }
}