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

namespace MatesovaPrace
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        AccommodationPageModel model = new();
        public Visibility LoginRequestVisible => model.Connection == null ? Visibility.Visible : Visibility.Collapsed;
        public bool HideUnlogged { get; set; } = false;
        public MainPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;
            DataContext = model;
        }

        private void ShowLoginPage_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(LoginPage), (Action<ConnectionModel>)OnLoggedIn);
        }

        void OnLoggedIn(ConnectionModel newConnection)
        {
            Console.WriteLine("Loggedd");

            model.Connection = newConnection;

        }
    }
}
