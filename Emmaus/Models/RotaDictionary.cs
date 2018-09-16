using System.Collections.Generic;

namespace Emmaus.Models
{
    public class RotaDictionary
    {
        public Dictionary<Date, NameRoles> DateNameJobListPairs = new Dictionary<Date, NameRoles>();
    }

    public class NameRoles
    {
        public Dictionary<string, List<string>> KeyValues = new Dictionary<string, List<string>>();
    }
}