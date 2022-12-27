using MatesovaPrace.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Protection;

namespace MatesovaPrace
{
    interface IDataSource
    {
        public abstract Task<ObservableCollection<PersonModel>> GetPeopleAsync(bool excludeUnlogged = true);
        public abstract Task Upload(IReadOnlyList<PersonModel> people, IEnumerable<int> index);
        public abstract Task PutIntoCacheAsync<T>(T obj, string key);
    }

    class DummyDataSource : IDataSource
    {
        readonly Action prompOnUse;
        public DummyDataSource(Action prompOnUse)
        {
            this.prompOnUse = prompOnUse;
        }

        public async Task<ObservableCollection<PersonModel>> GetPeopleAsync(bool excludeUnlogged = true)
        {
            prompOnUse();
            throw new Exception("Not connected to any data source");
        }

        public async Task PutIntoCacheAsync<T>(T obj, string key)
        {
            prompOnUse();
            throw new Exception("Not connected to any data source");
        }

        public async Task Upload(IReadOnlyList<PersonModel> people, IEnumerable<int> index)
        {
            prompOnUse();
            throw new Exception("Not connected to any data source");
        }
    }
}
