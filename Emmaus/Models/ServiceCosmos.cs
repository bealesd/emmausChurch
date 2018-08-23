using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace Emmaus.Models
{
    public class ServiceCosmos
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [RegularExpression(" ^[^,] + $", ErrorMessage = "Commas are not allowed")]
        [JsonProperty(PropertyName = "summary")]
        public string Summary { get; set; }

        [JsonProperty(PropertyName = "date")]
        public DateTime Date { get; set; }

        [RegularExpression(" ^[^,] + $", ErrorMessage = "Commas are not allowed")]
        [JsonProperty(PropertyName = "speaker")]
        public string Speaker { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        public override string ToString()
        {
            return string.Join(',', new string[] { Date.ToString(), Summary, Speaker });
        }
    }
}