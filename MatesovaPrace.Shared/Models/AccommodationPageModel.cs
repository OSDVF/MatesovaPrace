using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace MatesovaPrace.Models
{
    partial class AccommodationPageModel : DependencyObject
    {
        public ConnectionModel? Connection { get; set; }
        public ObservableCollection<PersonModel> People { get; set; } = new();
    }
}
