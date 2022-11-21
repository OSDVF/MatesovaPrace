using Google.Apis.Auth.OAuth2;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.ComponentModel;

namespace MatesovaPrace.Models
{
    public class UserCircleModel : INotifyPropertyChanged
    {
        private UserCredential? userCredential;

        public UserCredential? UserCredential
        {
            get => userCredential; set
            {
                if(userCredential != value)
                {
                    userCredential = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UserCredential)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CircleFill)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PopupText)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Icon)));
                }
            }
        }
        public Brush CircleFill => UserCredential == null ? new SolidColorBrush(Colors.Gray) : new SolidColorBrush(Colors.CornflowerBlue);

        public string PopupText => UserCredential == null ? "Not logged in" : "Logged in";
        public Symbol Icon => UserCredential == null ? Symbol.BlockContact : Symbol.Contact;

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
