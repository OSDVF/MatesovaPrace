using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;

namespace MatesovaPrace
{
    public class Entry : TextBox
    {
        public event EventHandler<KeyRoutedEventArgs>? Confirmed;
        public Entry() : base()
        {
            KeyUp += Entry_KeyUp;
            DefaultStyleKey = typeof(TextBox);
        }

        private void Entry_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if(e.Key == Windows.System.VirtualKey.Enter)
            {
                Confirmed?.Invoke(this, e);
                e.Handled = true;
            }
        }
    }
}
