using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Globalization;

namespace Emmaus.Models
{
    public class Service : TableEntity
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "date")]
        public DateTime Date { get; set; }

        private string _story;
        [JsonProperty(PropertyName = "story")]
        public string Story
        {
            get
            {
                return _story.Trim() ?? string.Empty;
            }
            set
            {
                _story = value.Trim() ?? string.Empty;
            }
        }

        private string _text;
        [JsonProperty(PropertyName = "text")]
        public string Text
        {
            get
            {
                return _text.Trim() ?? string.Empty;
            }
            set
            {
                _text = value.Trim() ?? string.Empty;
            }
        }

        private string _speaker;
        [JsonProperty(PropertyName = "speaker")]
        public string Speaker
        {
            get
            {
                return _speaker.Trim() ?? string.Empty;
            }
            set
            {
                _speaker = value.Trim() ?? string.Empty;
            }
        }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        public string ToMonth()
        {
            return Date.ToString("MMM", CultureInfo.CurrentCulture);
        }

        public string ToDayMonth()
        {
            return String.Concat(Date.Day, " ", Date.ToString("MMM", CultureInfo.CurrentCulture));
        }
    }
}