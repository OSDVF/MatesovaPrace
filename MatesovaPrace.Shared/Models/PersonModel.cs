using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    class PersonModel
    {
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
        public string ArrivalString
        {
            get
            {
                return Arrival.ToString("dd.MM.yyyy");
            }
        }
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

        public string[] AdditionalItems { get; set; } = new string[0];
        public Status Status { get; set; }

        public float NightPrice { get; set; }
        public float TotalPrice { get; set; }
        public string? InternalNote { get; set; }
        public DateTime SignupDate { get; set; }
        public float ExtraItemsPrice { get; set; }

    }
}
