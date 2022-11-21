using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Google.Apis.Auth.OAuth2;
using MatesovaPrace.Models;
using Microsoft.UI.Xaml.Input;

namespace MatesovaPrace
{
    public sealed partial class UserCircle : UserControl
    {
        public UserCredential? Credential
        {
            get { return GetValue(CredentialProperty) as UserCredential; }
            set => SetValue(CredentialProperty, value);
        }
        public static readonly DependencyProperty CredentialProperty =
            DependencyProperty.Register(nameof(Credential), typeof(UserCredential), typeof(UserCircle), new PropertyMetadata(null,
                (userCircle, val) =>
                (userCircle as UserCircle).Model.UserCredential = val.NewValue as UserCredential)
            );


        public UserCircleModel Model
        {
            get { return (UserCircleModel)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Model.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register(nameof(Model), typeof(UserCircleModel), typeof(UserCircle), new PropertyMetadata(new UserCircleModel()));

        public UserCircle()
        {
            InitializeComponent();
            Model.UserCredential = Credential;
        }

        public void PointerEnteredCircle(object sender, PointerRoutedEventArgs e)
        {
            InnerPopup.IsOpen = true;
        }
        public void PointerExitedCircle(object sender, PointerRoutedEventArgs e)
        {
            InnerPopup.IsOpen = false;
        }
    }
}
