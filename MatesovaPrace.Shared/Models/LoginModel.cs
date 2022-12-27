using Google.Apis.Drive.v3.Data;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel.Activation;
using File = Google.Apis.Drive.v3.Data.File;

namespace MatesovaPrace.Models
{
    internal class LoginModel : INotifyPropertyChanged
    {
        private FileDetailModel? selectedFile;
        private bool searching;
        private bool detailLoading;
        private GDriveSource? source;

        public string? SheetId { get; set; }
        public string FindString { get; set; } = "";
        public bool IncludeSharedFiles { get; set; } = true;
        public ObservableCollection<FileListModel> FoundFiles { get; set; } = new();
        public string? NextPageToken { get; set; }

        public FileDetailModel? SelectedFile
        {
            get => selectedFile; set
            {
                if (selectedFile != value)
                {
                    selectedFile = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedFile)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedFileInfoVisible)));
                }
            }
        }
        public Visibility SelectedFileInfoVisible => SelectedFile == null ? Visibility.Collapsed : Visibility.Visible;

        public bool Searching
        {
            get => searching; internal set
            {
                if (searching != value)
                {
                    searching = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Searching)));
                }
            }
        }

        public bool DetailLoading
        {
            get => detailLoading; internal set
            {
                if (detailLoading != value)
                {
                    detailLoading = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DetailLoading)));
                }
            }
        }

        public GDriveSource? Source
        {
            get => source; set
            {
                if (source != value)
                {
                    source = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Source)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }



    public class FileListModel
    {
        public string? Name { get; set; }
        public string? Id { get; set; }

        public string? Mime { get; set; }
    }

    public class FileDetailModel: FileListModel
    {
        public byte[] Thumbnail { get; set; }

        public FileDetailModel(string name, string id, string mime, byte[] thumbnail)
        {
            Name = name;
            Id = id;
            Mime = mime;
            Thumbnail = thumbnail;
        }

        public FileDetailModel(FileListModel listModel, byte[] thumbnail)
        {
            Name = listModel.Name;
            Id = listModel.Id;
            Mime = listModel.Mime;
            Thumbnail = thumbnail;
        }
    }
}
