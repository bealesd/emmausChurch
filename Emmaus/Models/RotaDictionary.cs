using System;
using System.Collections.Generic;

namespace Emmaus.Models
{
    public class RotaDictionary
    {
        public Dictionary<DateTime, NameRoles> DateNameJobListPairs = new Dictionary<DateTime, NameRoles>();
    }

    public class NameRoles
    {
        // TODO change Dictionary<string, List<string>> to Dictionary<string, List<rotaItemDto>> and use id in views for editing rotaItems
        public Dictionary<string, List<string>> KeyValues = new Dictionary<string, List<string>>();
    }
}