using Google.Apis.Auth.OAuth2;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatesovaPrace.Models
{
    internal partial class ConnectionModel
    {
        public UserCredential Credential { get; set; }
        public String SheetId { get; set; }
    }
}
