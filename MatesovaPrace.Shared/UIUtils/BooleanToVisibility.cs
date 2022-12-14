using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Text;

namespace MatesovaPrace
{
    public class BooleanToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language) => value is bool
                ? (bool)(value ?? false) ^ (parameter as string ?? string.Empty).Equals("Reverse") ?
                    Visibility.Visible : Visibility.Collapsed
                : (object)((value != null) ^ (parameter as string ?? string.Empty).Equals("Reverse") ?
                    Visibility.Visible : Visibility.Collapsed);

        public object ConvertBack(object value, Type targetType, object parameter, string language) =>
            (Visibility)value == Visibility.Visible ^ (parameter as string ?? string.Empty).Equals("Reverse");

    }

}
