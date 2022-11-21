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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
        public bool HideUnlogged { get; set; } = false;
        public Visibility GridVisibility => model.Connection != null ? Visibility.Visible : Visibility.Collapsed;

        public MainPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;
            DataContext = model;

#if WINDOWS
            app = (Application.Current as App);
            app?.MainWindow.SetTitleBar(MainAppBar);
            SizeChanged += ResetTitlebar;
#endif
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            SizeChanged -= ResetTitlebar;
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
            Frame.Navigate(typeof(LoginPage), (Action<ConnectionModel>)OnLoggedIn);
        }

        void ManualAuth_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(LoginPage), new Tuple<Action<ConnectionModel>, string>(OnLoggedIn, model.ManualAuthCode));
        }

        void OnLoggedIn(ConnectionModel newConnection)
        {
            Console.WriteLine("Loggedd");

            model.Connection = newConnection;

        }
    }
}
