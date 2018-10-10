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
    }

    public enum RotaType
    {
        YouthClub, Band, Projection
    }

    public enum Roles
    {
        admin, band, projector, services, youth
    }
}