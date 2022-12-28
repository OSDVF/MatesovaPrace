using CommunityToolkit.Mvvm.Input;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util.Store;

using MatesovaPrace.Models;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Connectivity;
using Windows.Security.Cryptography.Core;
using Windows.UI.Core;
#if __WASM__
using Uno.Foundation;
#endif

namespace MatesovaPrace
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        AccommodationPageModel model = new();
        private App? app;
        private SignatureDialog signatureDialog;
        private LinkedList<int> changedItems = new();
        public RelayCommand UploadCommand { get; set; }
        public bool HideUnlogged { get; set; } = true;

        public MainPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;
            DataContext = model;

#if WINDOWS
            app = (Application.Current as App);
            SizeChanged += ResetTitlebar;
#endif
            Loaded += MainPage_Loaded;
            UploadCommand = new RelayCommand(Upload);
            TryLoadState();
        }

        async void TryLoadState()
        {
            model.CachedPeople = await GDriveSource.ObjectStorage.GetAsync<ObservableCollection<PersonModel>>("people");
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                model.Offline = false;
                var sheetId = await GDriveSource.ObjectStorage.GetAsync<string>("sheetId");
                if (sheetId != null)
                {
                    var TokenResponse = await GDriveSource.ObjectStorage.GetAsync<TokenResponse>("user");
                    var newSource = new GDriveSource(new UserCredential(GDriveSource.GetFlow(), "user", TokenResponse))
                    {
                        SheetId = sheetId
                    };
                    bool changes = false;
                    List<int> indexes = new List<int>();
                    var i = 0;
                    foreach (var person in model.CachedPeople)
                    {
                        if (person.Dirty)
                        {
                            changes = true;
                            if(person.SignatureOrCached != null)
                            {
                                indexes.Add(i);
                            }
                        }
                        i++;
                    }
                    if (changes)
                    {
                        var result = await new ContentDialog
                        {
                            Title = "There are saved changes",
                            Content = "Upload your saved version and overwrite online data? Or use the online version and throw away your version?",
                            PrimaryButtonText = "Upload",
                            SecondaryButtonText = "Ignore local changes",
                            XamlRoot = XamlRoot
                        }.ShowAsync();
                        if (result == ContentDialogResult.Primary)
                        {
                            try
                            {
                                newSource.Upload(model.CachedPeople, indexes);
                                model.Offline = false;
                            }
                            catch(Exception ex)
                            {
                                model.Offline = true;
                                Debug.WriteLine(ex);
                            }

                            model.People = model.CachedPeople;
                            model.DataSource = newSource;

                        }
                        else
                        {
                            OnLoggedIn(newSource);
                        }
                    }
                    else
                    {
                        OnLoggedIn(newSource);
                    }
#if !ANDROID
                    Debug.WriteLine("Loaded state from storage");
#endif
                }
            }
            else
            {
                model.Offline = true;
                if (!registeredNetworkStatusNotif)
                {
                    var networkStatusCallback = new NetworkStatusChangedEventHandler(OnNetworkStatusChange);
                    NetworkInformation.NetworkStatusChanged += networkStatusCallback;
                    registeredNetworkStatusNotif = true;
                }
            }
        }

        private void OnNetworkStatusChange(object sender)
        {
            // get the ConnectionProfile that is currently used to connect to the Internet                
            ConnectionProfile InternetConnectionProfile = NetworkInformation.GetInternetConnectionProfile();

            if (InternetConnectionProfile == null)
            {
                DispatcherQueue.TryEnqueue(() => model.Offline = true);
            }
            else
            {
                if (model.People == null || model.People.Count == 0)
                {
                    DispatcherQueue.TryEnqueue(TryLoadState);
                }
            }
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            signatureDialog = new()
            {
                XamlRoot = XamlRoot
            };
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            SizeChanged -= ResetTitlebar;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ResetTitlebar(null, null);
            SizeChanged += ResetTitlebar;
        }

        void ResetTitlebar(object sender, SizeChangedEventArgs e)
        {
#if WINDOWS
            app?.MainWindow.SetTitleBar(MainAppBar);
#endif
        }

        void Load(object sender, RoutedEventArgs e)
        {
#if __WASM__
            // Automatic navigation
            string authCode = WebAssemblyRuntime.InvokeJS("new URLSearchParams(location.search).get('code')");
            if(!string.IsNullOrEmpty(authCode))
            {
                model.ManualAuthCode = authCode;
                model.FoundAuthCode = true;
            }
#endif
        }

        private void ShowLoginPage_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(LoginPage), (Action<IDataSource?>)OnLoggedIn);
        }

        void ManualAuth_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(LoginPage), new Tuple<Action<IDataSource>, string>(OnLoggedIn, model.ManualAuthCode));
        }

        async void OnLoggedIn(IDataSource? newConnection)
        {
            bool changes = false;
            foreach (var person in model.People)
            {
                if (person.Dirty)
                {
                    changes = true;
                    break;
                }
            }
            if (changes)
            {
                var result = await new ContentDialog
                {
                    Title = "There are changes",
                    Content = "Upload your changes first and overwrite online data? Or use the online version and throw away your changes?",
                    PrimaryButtonText = "Upload",
                    SecondaryButtonText = "Ignore local changes",
                    XamlRoot = XamlRoot
                }.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    Upload();
                    return;
                }
            }
            model.DataSource = newConnection;
            if (model.DataSource == null)
            {
                return;
            }
            model.PeopleLoading = true;
            try
            {
                model.People = await model.DataSource.GetPeopleAsync(HideUnlogged);
                try
                {
                    await model.DataSource.PutIntoCacheAsync(model.People, "people");
                    model.Offline = false;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                await new ContentDialog
                {
                    Title = "Error Getting Accommodation Data",
                    Content = e.Message,
                    XamlRoot = XamlRoot,
                    CloseButtonText = "Dismiss"
                }.ShowAsync();
                model.DataSource = null;
                model.Offline = true;
            }
            model.PeopleLoading = false;
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            OnLoggedIn(model.DataSource);
        }
        SemaphoreSlim dialogMutex = new(1);
        private bool registeredNetworkStatusNotif;

        private async void ListView_Click(object sender, ItemClickEventArgs e)
        {
            await dialogMutex.WaitAsync();
            signatureDialog.DataContext = e.ClickedItem;
            try
            {
                var result = await signatureDialog.ShowAsync();
                dialogMutex.Release();
                if (result == ContentDialogResult.Primary)
                {
                    var person = e.ClickedItem as PersonModel;
                    person.Dirty = true;
                    changedItems.AddLast(model.People.IndexOf(person));
                    if (model.AutoSave)
                    {
                        await person.GetSignaturePNG;
                        Upload();
                    }
                }
            }
            catch (Exception ex)
            {
                dialogMutex.Release();
                System.Diagnostics.Debug.WriteLine(ex);
            }

        }

        public void Logout_Click(object sender, RoutedEventArgs e)
        {
            new UWPObjectStorage().DeleteAsync<TokenResponse>("user");
            model.DataSource = null;
        }

        private void CommandBar_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var cb = (sender as CommandBar);
            cb!.IsOpen = !cb.IsOpen;
        }

        private void LoadCachedPeople_Click(object sender, RoutedEventArgs e)
        {
            model.People = model.CachedPeople;
            model.DataSource = new DummyDataSource(async () =>
            {
                await new ContentDialog
                {
                    Title = "Not connected",
                    Content = "Connect to a Google Sheet first",
                    CloseButtonText = "Close",
                    XamlRoot = XamlRoot
                }.ShowAsync();
            });
        }

        public async void Upload()
        {
            model.Uploading = true;
            if (model.DataSource == null)
            {
                TryLoadState();
                model.Uploading = false;
                return;
            }
            try
            {
                await model.DataSource.PutIntoCacheAsync(model.People, "people");
                await model.DataSource!.Upload(model.People, changedItems);
                foreach (var i in changedItems)
                {
                    model.People[i].Dirty = false;
                }
                changedItems.Clear();
                model.Offline = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                model.Offline = true;
            }
            model.Uploading = false;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            foreach (var i in changedItems)
            {
                model.People[i].Dirty = false;
            }
            model.DataSource.PutIntoCacheAsync(model.People, "people");
            changedItems.Clear();
        }
    }
}
