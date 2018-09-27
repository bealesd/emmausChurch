using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;

namespace Emmaus.Models
{
    public class RotaDto : TableEntity
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string Type { get; set; }
        public Date Date { get; set; }
        public int Day { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
        public void UnPackDate()
        {
            if (Date == null)
            {
                throw new Exception("No date set.");
            }
            Day = Date.Day;
            Month = Date.Month;
            Year = Date.Year;
        }
        public void PackDate()
        {
            if (Date != null)
            {
                throw new Exception("Date already set.");
            }
            Date = new Date(Year, Month, Day);
        }

    }

    public enum YouthClubLeader
    {
        CarolMiller, JohnMiller, MiriamBamfield, DamianSelby, Lucy
    }

    public enum YouthClubRole
    {
        help, food, craft, games, talk
    }

    public enum BandLeader
    {
         MiriamBamfield, Keith, Hetty, MarkHathaway
    }

    public enum BandRole
    {
        guitar, piano, singer
    }

    public enum ProjectionLeader
    {
        BillBeales, DamianSelby, CarolMiller, ChrisSmith, DavidHathaway 
    }

    public enum ProjectionRole
    {
        laptop
    }

    public enum Roles
    {
        admin, projector, youth, band, services
    }
}