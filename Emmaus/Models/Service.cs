using System.ComponentModel.DataAnnotations;

namespace Emmaus.Models
{
    public class Service
    {
        [RegularExpression(" ^[^,] + $", ErrorMessage = "Commas are not allowed")]
        public string Date { get; set; }
        [RegularExpression(" ^[^,] + $", ErrorMessage = "Commas are not allowed")]
        public string Summary { get; set; }
        [RegularExpression(" ^[^,] + $", ErrorMessage = "Commas are not allowed")]
        public string Speaker { get; set; }

        public override string ToString()
        {
            return string.Join(',', new string[] { Date, Summary, Speaker });
        }

        public void ParseStringToParameters(string csvString)
        {
            var array = csvString.Split(',');
            Date = array[0];
            Summary = array[1];
            Speaker = array[2];
        }
    }
}