using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace MatesovaPrace
{
    public class MimeToIcon : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not string)
            {
                throw new ArgumentException();
            }
            switch (value as string)
            {
                case "application/pdf":
                    return Symbol.Page;
                case "application/vnd.google-apps.document":
                    return Symbol.Page2;
                case "application/vnd.google-apps.spreadsheet":
                    return Symbol.SelectAll;
                case "application/vnd.google-apps.form":
                    return Symbol.PreviewLink;
                case "application/vnd.google-apps.presentation":
                    return Symbol.SlideShow;
                default:
                    return Symbol.Document;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
