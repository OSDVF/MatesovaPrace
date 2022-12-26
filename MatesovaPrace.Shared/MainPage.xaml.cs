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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
        private UWPObjectStorage objectStorage;
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
            objectStorage = new();
            TryLoadState();
        }

        async void TryLoadState()
        {
            var sheetId = await objectStorage.GetAsync<string>("sheetId");
            if (sheetId != null)
            {
                var TokenResponse = await objectStorage.GetAsync<TokenResponse>("user");
                OnLoggedIn(new GDriveSource(new UserCredential(GDriveSource.GetFlow(objectStorage), "user", TokenResponse))
                {
                    SheetId = sheetId
                });
                Debug.WriteLine("Loaded state from storage");
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
            app?.MainWindow.SetTitleBar(MainAppBar);
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
            model.DataSource = newConnection;
            if (model.DataSource == null)
            {
                return;
            }
            model.PeopleLoading = true;
            try
            {
                model.People = await model.DataSource.GetPeopleAsync(HideUnlogged);
            }
            catch (Exception e)
            {
                await new ContentDialog
                {
                    Title = "Error Getting Accommodation Data",
                    Content = e.Message,
                    XamlRoot = XamlRoot,
                    CloseButtonText = "Dismiss"
                }.ShowAsync();
            }
            model.PeopleLoading = false;
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            OnLoggedIn(model.DataSource);
        }
        SemaphoreSlim dialogMutex = new(1);

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
                    changedItems.AddLast(model.People.IndexOf(e.ClickedItem as PersonModel));
                    if (model.AutoSave)
                    {
                        Upload();
                    }
                }
            }
            catch (Exception ex)
            {
                dialogMutex.Release();
                Debug.WriteLine(ex);
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
            cb.IsOpen = !cb.IsOpen;
        }

        public async void Upload()
        {
            model.Uploading = true;
            await model.DataSource!.Upload(model.People, changedItems);
            changedItems.Clear();
            model.Uploading = false;
        }
    }
}
