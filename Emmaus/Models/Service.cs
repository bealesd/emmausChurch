using Newtonsoft.Json;
using System;
using System.Globalization;

namespace Emmaus.Models
{
    public class Service
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "summary")]
        public string Summary { get; set; }

        [JsonProperty(PropertyName = "date")]
        public DateTime Date { get; set; }

        [JsonProperty(PropertyName = "speaker")]
        public string Speaker { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        public string ToDayMonth()
        {
            return String.Concat( Date.Day, " ", Date.ToString("MMM", CultureInfo.CurrentCulture));
        }
    }
}