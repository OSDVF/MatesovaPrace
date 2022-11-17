using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MatesovaPrace
{
    public partial class WebLoginMessage : Page
    {
        public WebLoginMessage()
        {
            InitializeComponent();
        }

        void Back_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }
    }
}
