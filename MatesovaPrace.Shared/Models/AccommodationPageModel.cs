using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace MatesovaPrace.Models
{
    partial class AccommodationPageModel : INotifyPropertyChanged
    {
        private bool foundAuthCode = false;
        private string manualAuthCode;

        public event PropertyChangedEventHandler? PropertyChanged;
        public bool FoundAuthCode
        {
            get => foundAuthCode; set
            {
                if (foundAuthCode != value)
                {
                    foundAuthCode = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FoundAuthCode)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AutoAuthVisibility)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LoginRequestVisible)));
                }
            }
        }
        public Visibility AutoAuthVisibility => FoundAuthCode ? Visibility.Visible : Visibility.Collapsed;
        public Visibility LoginRequestVisible => Connection == null && !FoundAuthCode ? Visibility.Visible : Visibility.Collapsed;

        public ConnectionModel? Connection { get; set; }
        public ObservableCollection<PersonModel> People { get; set; } = new();
        public string ManualAuthCode
        {
            get => manualAuthCode; set
            {
                manualAuthCode = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ManualAuthCode)));

            }
        }
    }
}
