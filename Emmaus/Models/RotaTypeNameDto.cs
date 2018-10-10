using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Emmaus.Models
{
    public class RotaTypeNameDto : TableEntity
    {
        public string Name { get; set; }
        public string RotaType { get; set; }
    }
}
