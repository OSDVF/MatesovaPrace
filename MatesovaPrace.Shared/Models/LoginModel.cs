using Google.Apis.Drive.v3.Data;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatesovaPrace.Models
{
    internal class LoginModel
    {
        public string? SheetId { get; set; }
        public string FindString { get; set; } = "";
        public bool IncludeSharedFiles { get; set; } = true;
        public ObservableCollection<FileListModel> FoundFiles { get; set; } = new();
        public string NextPageToken { get; set; }

        public FileListModel? SelectedFile { get; set; }
    }

    internal class FileListModel
    {
        public string Name { get; set; }
        public string Id { get; set; }

        public File Info { get; set; }
    }
}
