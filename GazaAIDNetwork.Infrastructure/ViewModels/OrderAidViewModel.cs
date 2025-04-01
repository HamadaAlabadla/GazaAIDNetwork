using GazaAIDNetwork.Core.Enums;
using GazaAIDNetwork.EF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GazaAIDNetwork.Core.Enums.Enums;

namespace GazaAIDNetwork.Infrastructure.ViewModels
{
    public class OrderAidViewModel
    {
        public string Id { get; set; }
        public int Quantity { get; set; }
        public string HusbandName { get; set; }
        public string HusbandIdNumber { get; set; }
        public string WifeName { get; set; }
        public string WifeIdNumber { get; set; }
        public int MemebersNumber { get; set; }
        public string ProjectAidName { get; set; }
        public string CycleAidName { get; set; }
        public string OrderAidStatus { get; set; } 
    }
}
