using MatesovaPrace.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;

namespace MatesovaPrace
{
    interface IDataSource
    {
        public abstract Task<ObservableCollection<PersonModel>> GetPeopleAsync(bool excludeUnlogged = true);
    }
}
