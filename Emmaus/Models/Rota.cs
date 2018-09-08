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
        public DateTime Date { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
    }

    public enum YouthClubLeader
    {
        carol, john, miriam, damian, lucy
    }
    public enum YouthClubRole
    {
        help, food, craft, games, talk
    }
}