using Google.Apis.Auth.OAuth2;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatesovaPrace.Models
{
    internal partial class ConnectionModel : INotifyPropertyChanged
    {
        private UserCredential credential;
        private string sheetId;

        public UserCredential Credential
        {
            get => credential; set
            {
                if (credential != value)
                {
                    credential = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Credential)));
                }
            }
        }
        public string SheetId
        {
            get => sheetId; set
            {
                if(sheetId != value)
                {
                    sheetId = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SheetId)));

                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
