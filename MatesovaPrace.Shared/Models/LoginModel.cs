﻿using Google.Apis.Drive.v3.Data;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using File = Google.Apis.Drive.v3.Data.File;

namespace MatesovaPrace.Models
{
    internal class LoginModel : INotifyPropertyChanged
    {
        private FileListModel? selectedFile;

        public string? SheetId { get; set; }
        public string FindString { get; set; } = "";
        public bool IncludeSharedFiles { get; set; } = true;
        public ObservableCollection<FileListModel> FoundFiles { get; set; } = new();
        public string? NextPageToken { get; set; }

        public FileListModel? SelectedFile
        {
            get => selectedFile; set
            {
                if(selectedFile != value)
                {
                    selectedFile = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedFile)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedFileInfoVisible)));
                }
            }
        }
        public Visibility SelectedFileInfoVisible => SelectedFile == null ? Visibility.Collapsed : Visibility.Visible;

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class FileListModel
    {
        public string? Name { get; set; }
        public string? Id { get; set; }

        public File? Info { get; set; }
    }
}
