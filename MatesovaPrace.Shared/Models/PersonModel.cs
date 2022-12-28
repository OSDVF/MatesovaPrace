using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using System.Diagnostics;
using Windows.Graphics.Display;
using System.ComponentModel;
using Newtonsoft.Json;

namespace MatesovaPrace.Models
{
    public enum Meal
    {
        VECERE, SNIDANE, OBED, Unknown
    }
    public enum Status
    {
        New, Paid, Cancelled, Free, Unknown
    }
    class PersonModel : INotifyPropertyChanged
    {
        private string matesNote;
        private bool dirty;
        private RenderTargetBitmap? signature = null;

        public PersonModel(string order, string name, string surname, uint birthYear, string email, string phone, string city, string instrument, bool firstTime, string? whoInvited, string? health, string? food, string? note, string type, DateTime arrival, Meal firstMeal, DateTime departure, Meal lastMeal, string[]? additionalItems, Status status, float nightPrice, float totalPrice, string? internalNote, DateTime signupDate, float extraItemsPrice)
        {
            Order = order;
            Name = name;
            Surname = surname;
            BirthYear = birthYear;
            Email = email;
            Phone = phone;
            City = city;
            Instrument = instrument;
            FirstTime = firstTime;
            WhoInvited = whoInvited;
            Health = health;
            Food = food;
            Note = note;
            Type = type;
            Arrival = arrival;
            FirstMeal = firstMeal;
            Departure = departure;
            LastMeal = lastMeal;
            AdditionalItems = additionalItems ?? AdditionalItems;
            Status = status;
            NightPrice = nightPrice;
            TotalPrice = totalPrice;
            InternalNote = internalNote;
            SignupDate = signupDate;
            ExtraItemsPrice = extraItemsPrice;

            AcceptSignatureCommand = new RelayCommand<Grid>(AcceptSignature);
            ClearSignatureCommand = new RelayCommand(ClearSignature);
        }

        public PersonModel()
        {
            AcceptSignatureCommand = new RelayCommand<Grid>(AcceptSignature);
            ClearSignatureCommand = new RelayCommand(ClearSignature);
        }

        public string Order { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public uint BirthYear { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string City { get; set; }
        public string Instrument { get; set; }
        public bool FirstTime { get; set; }
        public string? WhoInvited { get; set; }
        public string? Health { get; set; }
        public string? Food { get; set; }
        public string? Note { get; set; }
        public string Type { get; set; }
        public DateTime Arrival { get; set; }
        [JsonIgnore]
        public string ArrivalString
        {
            get
            {
                return Arrival.ToString("dd.MM.yyyy");
            }
        }
        [JsonIgnore]
        public string DepartureString
        {
            get
            {
                return Departure.ToString("dd.MM.yyyy");
            }
        }
        public Meal FirstMeal { get; set; }
        public DateTime Departure { get; set; }
        public Meal LastMeal { get; set; }

        public string[] AdditionalItems { get; set; } = Array.Empty<string>();
        public Status Status { get; set; }

        public float NightPrice { get; set; }
        public float TotalPrice { get; set; }
        public string? InternalNote { get; set; }
        public DateTime SignupDate { get; set; }
        public float ExtraItemsPrice { get; set; }
        public RenderTargetBitmap? Signature
        {
            get => signature; set
            {
                if(signature != value)
                {
                    signature = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Signature)));
                }
            }
        }
        [JsonIgnore]
        public RelayCommand<Grid> AcceptSignatureCommand { get; set; }
        [JsonIgnore]
        public RelayCommand ClearSignatureCommand { get; set; }

        public bool Dirty
        {
            get => dirty; set
            {
                if (dirty != value)
                {
                    dirty = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Dirty)));
                }
            }
        }
        public string MatesNote
        {
            get => matesNote; set
            {
                if (matesNote != value)
                {
                    matesNote = value;
                    Dirty = true;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MatesNote)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public async void AcceptSignature(Grid renderedGrid)
        {
            Signature = new();
            try
            {
                await Signature.RenderAsync(renderedGrid, (int)renderedGrid.ActualWidth, (int)renderedGrid.ActualHeight);
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
            }
        }

        public void ClearSignature()
        {
            Signature = null;
        }

        public async Task<InMemoryRandomAccessStream> GetSignaturePNG()
        {
            try
            {
                var pixelBuffer = await Signature.GetPixelsAsync();
                var encoded = new InMemoryRandomAccessStream();
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, encoded);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, (uint)Signature.PixelWidth, (uint)Signature.PixelHeight, 96, 96, pixelBuffer.ToArray());
                await encoder.FlushAsync();
                encoded.Seek(0);
#if false
                var path = Path.GetTempFileName();
                using var str = new FileStream(path, FileMode.Create);
                encoded.AsStream().CopyTo(str);
                encoded.Seek(0);
                Debug.WriteLine(path);
#endif
                return encoded;
            }

            catch (Exception ex)
            {
                Debug.Write(ex);
            }
            return null;
        }
    }
}
