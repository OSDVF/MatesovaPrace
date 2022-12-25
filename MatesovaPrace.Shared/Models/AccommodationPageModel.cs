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
        private IDataSource? dataSource;
        private bool peopleLoading;
        private ObservableCollection<PersonModel> people = new();

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
        public Visibility LoginRequestVisible => DataSource == null && !FoundAuthCode ? Visibility.Visible : Visibility.Collapsed;

        public IDataSource? DataSource
        {
            get => dataSource; set
            {
                if (dataSource != value)
                {
                    dataSource = value;
                    PropertyChanged?.Invoke(this, new(nameof(DataSource)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LoginRequestVisible)));
                }
            }
        }
        public ObservableCollection<PersonModel> People
        {
            get => people; set
            {
                if(people != value)
                {
                    people = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(People)));
                }
            }
        }
        public string ManualAuthCode
        {
            get => manualAuthCode; set
            {
                manualAuthCode = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ManualAuthCode)));
            }
        }

        public bool PeopleLoading
        {
            get => peopleLoading; set
            {
                if (peopleLoading != value)
                {
                    peopleLoading = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PeopleLoading)));

                }
            }
        }
    }
}
