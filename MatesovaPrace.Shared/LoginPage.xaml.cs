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
#if __WASM__
using Uno.Foundation;
#endif

namespace MatesovaPrace
{
    public sealed partial class LoginPage : Page
    {
        const string configName = "appsettings.json";

        internal static LoginModel _data = new();

        public UserCredential? Credential
        {
            get { return (UserCredential?)GetValue(CredentialProperty); }
            set { SetValue(CredentialProperty, value); }
        }

        public static readonly DependencyProperty CredentialProperty =
            DependencyProperty.Register("Credential", typeof(UserCredential), typeof(LoginPage), new PropertyMetadata(null));

        private DriveService? drive;
        private XPlatformCodeReceiver receiver = new XPlatformCodeReceiver();
        private UWPObjectStorage objectStorage = new();
        private App? app;
        private GoogleClientSecrets gClient;
        internal Action<ConnectionModel>? OnConnected { get; set; }
        public Visibility NextPageVisible => string.IsNullOrEmpty(_data.NextPageToken) ? Visibility.Collapsed : Visibility.Visible;
        public LoginPage()
        {
            InitializeComponent();
            DataContext = _data;
            // Inject client secret into configuration
            GetClient();

#if WINDOWS
            app = (Application.Current as App);
            SetTitleBar();
            SizeChanged += ResetTitlebar;
#endif
        }

        private void SetTitleBar()
        {
            app?.MainWindow.SetTitleBar((UIElement)((AppBarElementContainer)AppBar.PrimaryCommands[2]).Content);
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
            if (e.Parameter is Tuple<Action<ConnectionModel>, string> connectedAndAuth)
            {
                OnConnected = connectedAndAuth.Item1;
                // Exchange code for an access token
                var client = new HttpClient();
                var redirectUri = "http://127.0.0.1/";//Default fallback
#if __WASM__
                // Redirect back to where we are right now
                redirectUri = WebAssemblyRuntime.InvokeJS("location.origin") + "/";
#endif
                GoogleAuthorizationCodeFlow flow = GetFlow();
                flow.ExchangeCodeForTokenAsync("user", connectedAndAuth.Item2, redirectUri, CancellationToken.None)
                    .ContinueWith(response =>
                    {
                        Credential = new UserCredential(flow, "user", response.Result);
                        CreateDriveService();
                        LoadSettings();
                    });
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

        private GoogleAuthorizationCodeFlow GetFlow()
        {
            return new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = gClient.Secrets,
                DataStore = objectStorage,
                Scopes = new[]
                                {
                        DriveService.Scope.Drive
                    }
            });
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
            if (Credential != null)
            {
                Console.WriteLine("Already authenticated");
                return;
            }
            var token = objectStorage.Get<TokenResponse>();
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
                Credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    gClient.Secrets,
                    new[] {
                        SheetsService.Scope.Spreadsheets,
                        DriveService.Scope.Drive
                    },
                    "user", CancellationToken.None, objectStorage, receiver);
#endif
                Console.WriteLine("Creating drive service #1");
            }
            else
            {
                Credential = new UserCredential(GetFlow(), "user", token);
                Console.WriteLine("Loaded credentials from cache");
            }
            CreateDriveService();
        }

        private void CreateDriveService()
        {
            drive = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = Credential,
                ApplicationName = "MatesovaPrace",
            });
            Console.WriteLine("GDrive service created");
        }

        private async void GetClient()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = assembly.GetManifestResourceNames()
                    .Single(str => str.EndsWith("client.json"));
                using Stream stream = assembly.GetManifestResourceStream(resourceName);
                gClient = NewtonsoftJsonSerializer.Instance.Deserialize<GoogleClientSecrets>(stream);
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
            }
            catch(HttpRequestException e)
            {
                await new ContentDialog
                {
                    Content = e.Message,
                    Title = "Network Error",
                    XamlRoot = XamlRoot,
                    CloseButtonText = "Dismiss"
                }.ShowAsync();
            }
            catch(Exception e)
            {
                await new ContentDialog
                {
                    Content = e.Message,
                    Title = "General Error",
                    XamlRoot = XamlRoot,
                    CloseButtonText = "Dismiss"
                }.ShowAsync();
            }
        }

        public RelayCommand<FileListModel> MarkFileAsSelected { get; set; } = new RelayCommand<FileListModel>((FileListModel? file) =>
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