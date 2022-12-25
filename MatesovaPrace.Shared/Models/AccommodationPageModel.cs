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
        private ObservableCollection<float> columnWidths = new() {
            20.0f,
            80.0f,
            80.0f,
            20.0f,
            150.0f,
            100.0f,
            90.0f,
            80.0f,
            80.0f,
            100.0f,
            50.0f,
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        public bool FoundAuthCode
        {
            get => foundAuthCode; set
            {
                if (foundAuthCode != value)
                {
                    foundAuthCode = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FoundAuthCode)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LoginRequestVisible)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TitleText)));
                }
            }
        }
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
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TitleText)));
                }
            }
        }
        public ObservableCollection<PersonModel> People
        {
            get => people; set
            {
                if (people != value)
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

        public ObservableCollection<float> ColumnWidths
        {
            get => columnWidths; set
            {
                if (columnWidths != value)
                {
                    columnWidths = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ColumnWidths)));
                }
            }
        }

        public string TitleText
        {
            get
            {
                if (DataSource == null)
                {
                    if (FoundAuthCode)
                    {
                        return "Google Drive access granted";
                    }
                    else
                    {
                        return "Connect to Google Drive first";
                    }
                }
                else
                {
                    return "Matesova Práce";
                }
            }
        }
    }
}
