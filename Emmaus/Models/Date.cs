using System;
using Emmaus.Helper;

namespace Emmaus.Models
{
    public class Date
    {
        public int Day { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public Date(int year, int month, int day)
        {
            Year = year;
            Month = month;
            Day = day;
        }

        public override bool Equals(object obj)
        {
            var rota = obj as Date;
            if (rota == null)
                return false;

            return Day == rota.Day && Month == rota.Month && rota.Year == rota.Year;
        }

        public string ToFriendly()
        {
            return new DateTime(Year, Month, Day).ToFriendlyDate();
        }

        public override string ToString()
        {
            return Year.ToString() + " " + Month.ToString() + " " + Day.ToString();
        }
    }
}
