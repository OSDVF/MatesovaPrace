using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatesovaPrace.Models
{
    partial class PersonModel : DependencyObject
    {
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public uint? BirthYear { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        
    }
}
