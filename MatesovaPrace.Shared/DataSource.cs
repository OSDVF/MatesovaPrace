using Google.Apis.Util.Store;

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
        public abstract Task Upload(IReadOnlyList<PersonModel> people, IEnumerable<int> indexes);
        public abstract Task PutIntoCacheAsync<T>(T obj, string key);
    }

    class DummyDataSource : IDataSource
    {
        readonly Action prompOnUse;
        public static UWPObjectStorage ObjectStorage = new();

        public DummyDataSource(Action prompOnUse)
        {
            this.prompOnUse = prompOnUse;
        }

        public Task<ObservableCollection<PersonModel>> GetPeopleAsync(bool excludeUnlogged = true)
        {
            prompOnUse();
            throw new Exception("Not connected to any data source");
        }

        public async Task PutIntoCacheAsync<T>(T obj, string key)
        {
            await ObjectStorage.StoreAsync(key, obj);
            prompOnUse();
            throw new Exception("Not connected to any data source");
        }

        public Task Upload(IReadOnlyList<PersonModel> people, IEnumerable<int> indexes)
        {
            prompOnUse();
            throw new Exception("Not connected to any data source");
        }
    }
}
