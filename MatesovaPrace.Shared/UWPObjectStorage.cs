using Google.Apis.Json;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;
using Windows.Storage;

namespace Google.Apis.Util.Store
{
    /// <summary>
    /// File data store that implements <see cref="IDataStore"/>. This store creates a different file for each 
    /// combination of type and key. This file data store stores a JSON format of the specified object.
    /// </summary>
    public class UWPObjectStorage : IDataStore
    {
        private static readonly Task CompletedTask = Task.FromResult(0);

        readonly string folderPath;
        /// <summary>Gets the full folder path.</summary>
        public string FolderPath { get { return folderPath; } }

        /// <summary>
        /// Constructs a new file data store. If <c>fullPath</c> is <c>false</c> the path will be used as relative to 
        /// <c>Environment.SpecialFolder.ApplicationData"</c> on Windows, or <c>$HOME</c> on Linux and MacOS,
        /// otherwise the input folder will be treated as absolute.
        /// The folder is created if it doesn't exist yet.
        /// </summary>
        /// <param name="folder">Folder path.</param>
        /// <param name="fullPath">
        /// Defines whether the folder parameter is absolute or relative to
        /// <c>Environment.SpecialFolder.ApplicationData</c> on Windows, or<c>$HOME</c> on Linux and MacOS.
        /// </param>
        public UWPObjectStorage()
        {
            folderPath = ApplicationData.Current.LocalFolder.Path;
        }

        /// <summary>
        /// Stores the given value for the given key. It creates a new file (named <see cref="GenerateStoredKey"/>) in 
        /// <see cref="FolderPath"/>.
        /// </summary>
        /// <typeparam name="T">The type to store in the data store.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="value">The value to store in the data store.</param>
        public async Task StoreAsync<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key MUST have a value");
            }

            var serialized = NewtonsoftJsonSerializer.Instance.Serialize(value);
            using var stream = await ApplicationData.Current.LocalFolder.OpenStreamForWriteAsync(GenerateStoredKey(key, typeof(T)), CreationCollisionOption.ReplaceExisting);
            await stream.WriteAsync(Encoding.Unicode.GetBytes(serialized));
        }

        /// <summary>
        /// Deletes the given key. It deletes the <see cref="GenerateStoredKey"/> named file in 
        /// <see cref="FolderPath"/>.
        /// </summary>
        /// <param name="key">The key to delete from the data store.</param>
        public Task DeleteAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key MUST have a value");
            }

            var filePath = Path.Combine(folderPath, GenerateStoredKey(key, typeof(T)));
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return CompletedTask;
        }

        /// <summary>
        /// Returns the stored value for the given key or <c>null</c> if the matching file (<see cref="GenerateStoredKey"/>
        /// in <see cref="FolderPath"/> doesn't exist.
        /// </summary>
        /// <typeparam name="T">The type to retrieve.</typeparam>
        /// <param name="key">The key to retrieve from the data store.</param>
        /// <returns>The stored object.</returns>
        public async Task<T> GetAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key MUST have a value");
            }

            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();

            try
            {
                using var stream = await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync(GenerateStoredKey(key, typeof(T)));
                try
                {
                    var obj = stream.ReadToEnd();
                    return NewtonsoftJsonSerializer.Instance.Deserialize<T>(obj);
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
            catch (Exception)
            {
                return default(T);
            }
        }

        /// <summary>
        /// Clears all values in the data store. This method deletes all files in <see cref="FolderPath"/>.
        /// </summary>
        public Task ClearAsync()
        {
            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true);
                Directory.CreateDirectory(folderPath);
            }

            return CompletedTask;
        }

        /// <summary>Creates a unique stored key based on the key and the class type.</summary>
        /// <param name="key">The object key.</param>
        /// <param name="t">The type to store or retrieve.</param>
        public static string GenerateStoredKey(string key, Type t)
        {
            return string.Format("{0}-{1}", t.FullName, key);
        }
    }
}