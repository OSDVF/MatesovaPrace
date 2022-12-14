using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Json;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;

using MatesovaPrace.Models;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Windows.Devices.Geolocation;

namespace MatesovaPrace
{
    class GDriveSource : ConnectionModel, IDataSource
    {
        public SheetsService Service { get; set; }
        public static UWPObjectStorage ObjectStorage = new();
        private static GoogleClientSecrets? client;
        private static XPlatformCodeReceiver receiver = new XPlatformCodeReceiver();

        public static GoogleClientSecrets Client
        {
            get
            {
                if (client == null)
                {
                    client = GetClient();
                }
                return client;
            }
        }

        public GDriveSource(UserCredential credential)
        {
            Service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = Credential = credential,
                ApplicationName = "MatesovaPrace",
            });
        }

        public static async Task<GDriveSource> FromAuthCode(string authCode, string redirectUri)
        {
            GoogleAuthorizationCodeFlow flow = GetFlow();
            var response = await flow.ExchangeCodeForTokenAsync("user", authCode, redirectUri, CancellationToken.None);

            return new GDriveSource(new UserCredential(flow, "user", response));
        }

        public static async Task<GDriveSource> AuthorizeAsync()
        {
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                Client.Secrets,
                new[] {
                    DriveService.Scope.Drive
                },
                "user", CancellationToken.None, ObjectStorage, receiver
            );
            return new GDriveSource(credential);
        }

        public async Task<ObservableCollection<PersonModel>> GetPeopleAsync(bool excludeUnlogged = true)
        {
            ObservableCollection<PersonModel> people = new();
            var result = await Service.Spreadsheets.Values.Get(SheetId, "A2:AA60").ExecuteAsync();
            var imagesNotesRequest = Service.Spreadsheets.Values.Get(SheetId, "accommodation!J3:M53");
            imagesNotesRequest.ValueRenderOption = SpreadsheetsResource.ValuesResource.GetRequest.ValueRenderOptionEnum.FORMULA;
            var imagesNotesResult = await imagesNotesRequest.ExecuteAsync();
            int index = 0;
            HttpClient cl = new HttpClient();
            foreach (var row in result.Values)
            {
                if (excludeUnlogged && row[0] as string == "Z")
                {
                    continue;
                }
                var additionalItems = (row[19] as string)?.Split(",", StringSplitOptions.TrimEntries);
                Status status;
                var statusKnown = Enum.TryParse<Status>(row[20] as string, out status);
                try
                {
                    PersonModel newPerson = new PersonModel(
                                           row[0] as string,
                                            row[1] as string,
                                          row[2] as string,
                                        uint.Parse(row[3] as string),
                                            row[4] as string,
                                            row[5] as string,
                                            row[6] as string,
                                        row[7] as string,
                                        row[8] as string == "ano",
                                        row[9] as string,
                                            row[10] as string,
                                            row[11] as string,
                                            row[12] as string,
                                            row[14] as string,
                                            DateTime.Parse(row[15] as string),
                                        Enum.Parse(typeof(Meal), row[16] as string, true) as Meal? ?? Meal.Unknown,
                                            DateTime.Parse(row[17] as string),
                                        Enum.Parse(typeof(Meal), row[18] as string, true) as Meal? ?? Meal.Unknown,
                                                additionalItems,
                                        statusKnown ? status : Status.Unknown,
                                        float.Parse(row[21] as string),
                                        float.Parse(row[22] as string),
                                        row[23] as string,
                                        DateTime.Parse(row[24] as string),
                                    row.Count > 25 ? float.Parse((string)row[25]) : 0
                                        );
                    bool isInt = int.TryParse(row[0] as string, out int indexInSignPage);
                    if (imagesNotesResult.Values != null && isInt && imagesNotesResult.Values.Count > indexInSignPage - 1)
                    {
                        var inVal = imagesNotesResult.Values[indexInSignPage - 1];
                        if (inVal.Count > 3)
                        {
                            var imageF = (inVal[3] as string);
                            var imagePath = imageF.Substring(8, imageF.Length - 10);

                            try
                            {
                                var imageBytes = await cl.GetByteArrayAsync(imagePath);
                                newPerson.SerializableImage = Convert.ToBase64String(imageBytes);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex);
                                throw new Exception($"Could not fetch image for person {index}.", ex);
                            }
                        }

                        if (inVal.Count > 2)
                        {
                            newPerson.MatesNote = inVal[2] as string;
                        }

                        if (inVal.Count > 0)
                        {
                            newPerson.Paid = inVal[0].ToString();
                        }
                        if (inVal.Count > 1)
                        {
                            newPerson.PaidInPlace = inVal[1].ToString();
                        }
                    }
                    if (row.Count > 26 && string.IsNullOrEmpty(newPerson.Paid))
                    {
                        newPerson.Paid = row[26] as string;
                    }
                    newPerson.Dirty = false;
                    people.Add(newPerson);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    throw new Exception($"An error occured while adding person with index {index}.", e);
                }
                index++;
            }
            return people;
        }

        public static GoogleAuthorizationCodeFlow GetFlow()
        {
            return new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = Client.Secrets,
                DataStore = ObjectStorage,
                Scopes = new[]
                {
                    DriveService.Scope.Drive
                }
            });
        }

        public static GoogleClientSecrets GetClient()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = assembly.GetManifestResourceNames()
                                            .Single(str => str.EndsWith("client.json"));
            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            return NewtonsoftJsonSerializer.Instance.Deserialize<GoogleClientSecrets>(stream);
        }

        private string GetExcelColumnName(int columnNumber)
        {
            string columnName = "";

            while (columnNumber > 0)
            {
                int modulo = (columnNumber - 1) % 26;
                columnName = Convert.ToChar('A' + modulo) + columnName;
                columnNumber = (columnNumber - modulo) / 26;
            }

            return columnName;
        }
        public async Task Upload(IReadOnlyList<PersonModel> people, IEnumerable<int> indexes)
        {
            List<ValueRange> updateRanges = new();
            foreach (var i in indexes)
            {
                var person = people[i];
                string imageUrl = string.Empty;
                if (person.SerializableImage != null)
                {

                    Stream randStream;
                    var cachedPixels = Convert.FromBase64String(person.SerializableImage);
                    randStream = new MemoryStream(cachedPixels);

                    HttpContent fileStreamContent = new StreamContent(randStream);
                    using (var client = new HttpClient())
                    using (var formData = new MultipartFormDataContent())
                    {
                        formData.Add(fileStreamContent, "fileToUpload", SheetId + '-' + person.Email +
                            Regex.Replace(person.Name, @"[^\u0000-\u007F]+", string.Empty) + ".png");
                        var response = await client.PostAsync("https://prihlasky.travna.cz/server/www/upload.php", formData);
                        if (!response.IsSuccessStatusCode)
                        {
                            return;
                        }
                        imageUrl = await response.Content.ReadAsStringAsync();
                    }
                }

                updateRanges.Add(new ValueRange
                {
                    Range = $"accommodation!{GetExcelColumnName(2)}{i + 3}:{GetExcelColumnName(14)}",
                    Values = new List<IList<object>>{
                        new List<object>{
                            person.Name,person.Surname,
                            DateTime.Now.Year - person.BirthYear,
                            person.City,
                            person.ArrivalString,
                            person.DepartureString,
                            (person.Departure - person.Arrival).Days,
                            person.TotalPrice,
                            person.Paid,
                            person.PaidInPlace,
                            person.MatesNote,
                            imageUrl == string.Empty ? "" : "=IMAGE(\""+imageUrl+"\")"
                        }
                    }
                });
            }
            var request = new BatchUpdateValuesRequest
            {
                Data = updateRanges,
                ValueInputOption = "USER_ENTERED"
            };
            var request2 = Service.Spreadsheets.Values.BatchUpdate(request, SheetId);
            await request2.ExecuteAsync();
        }
        public Task PutIntoCacheAsync<T>(T obj, string key)
        {
            return ObjectStorage.StoreAsync<T>(key, obj);
        }
    }
}
