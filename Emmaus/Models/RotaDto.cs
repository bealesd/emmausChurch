using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Emmaus.Models
{
    public class RotaItemDto : TableEntity
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string Type { get; set; }
        public DateTime DateTime { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
        //public void UnPackDate()
        //{
        //    if (Date == null)
        //    {
        //        throw new Exception("No date set.");
        //    }
        //    Day = Date.Day;
        //    Month = Date.Month;
        //    Year = Date.Year;
        //}
        //public void PackDate()
        //{
        //    if (Date != null)
        //    {
        //        throw new Exception("Date already set.");
        //    }
        //    Date = new Date(Year, Month, Day);
        //}
    }

    public static class RotaLeaders
    {
        private static List<string> leaders = new List<string>();
        public static IEnumerable<string> Leaders {
            get
            {
                if (leaders.Count() == 0)
                {
                    Enum.GetNames(typeof(YouthClubLeader)).ToList().ForEach(n => leaders.Add(n));
                    Enum.GetNames(typeof(BandLeader)).ToList().ForEach(n => leaders.Add(n));
                    Enum.GetNames(typeof(ProjectionLeader)).ToList().ForEach(n => leaders.Add(n));
                    leaders = leaders.Distinct().OrderBy(n => n).ToList();
                }
                return leaders;
            }
            private set
            {
            }
        }
    }

    public enum YouthClubLeader
    {
        Damian, John, Lucy, Miriam
    }

    public enum YouthClubRole
    {
        craft, food, games, help, talk
    }

    public enum BandLeader
    {
        Hetty, Keith, Mark, Miriam
    }

    public enum BandRole
    {
        guitar, piano, singer
    }

    public enum ProjectionLeader
    {
        Bill, Carol, Chris, Damian, DavidH
    }

    public enum ProjectionRole
    {
        laptop
    }

    public enum Roles
    {
        admin, band, projector, services, youth
    }
}