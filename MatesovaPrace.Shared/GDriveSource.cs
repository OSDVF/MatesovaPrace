using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using MatesovaPrace.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

namespace MatesovaPrace
{
    class GDriveSource : ConnectionModel, IDataSource
    {
        public SheetsService Service { get; set; }

        public GDriveSource(UserCredential credential)
        {
            Service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = Credential = credential,
                ApplicationName = "MatesovaPrace",
            });
        }

        public async Task<ObservableCollection<PersonModel>> GetPeopleAsync()
        {
            ObservableCollection<PersonModel> people = new();
            var result = await Service.Spreadsheets.Values.Get(SheetId, "A2:Z60").ExecuteAsync();
            int index = 0;
            foreach (var row in result.Values)
            {
                var additionalItems = (row[19] as string)?.Split(",");
                Status status;
                var statusKnown = Enum.TryParse<Status>(row[20] as string, out status);
                try
                {
                    people.Add(new PersonModel(
                       row[0] as int? ?? -1,
                        row[1] as string,
                      row[2] as string,
                    uint.Parse(row[3] as string),
                        row[4] as string,
                        row[5] as string,
                        row[6] as string,
                    row[7] as string,
                    row[8] == "ano",
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
                    ));
                }
                catch (Exception e)
                {
                    throw new Exception($"An error occured while adding person with index {index}.", e);
                }
                index++;
            }
            return people;
        }
    }
}
