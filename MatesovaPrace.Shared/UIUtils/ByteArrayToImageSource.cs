using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MatesovaPrace
{
    public class ByteArrayToImageSource : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var byteArray = value as byte[];
            var bi = new BitmapImage();
            bi.SetSourceAsync(new MemoryStream(byteArray).AsRandomAccessStream());
            return bi;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
