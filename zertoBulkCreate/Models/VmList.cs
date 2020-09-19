using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zertoBulkCreate.Models
{
    public class CsvVMList
    {
        public string VMName { get; set; }
        public string VPGName { get; set; }
        public string BootGroupName { get; set; }
        public string ThinProvision { get; set; }
        public string UpdateVMNIC { get; set; }
        public string VMNICFailoverIPAddress { get; set; }
        public string VMNICFailoverIPMode { get; set; }
        public string VMNICFailoverIsConnected { get; set; }
        public string VMNICFailoverIsPrimary { get; set; }
        public string VMNICFailoverIsResetMacAddress { get; set; }
        public string VMNICFailoverTestIPAddress { get; set; }
        public string VMNICFailoverTestIPMode { get; set; }
        public string VMNICFailoverTestIsConnected { get; set; }
        public string VMNICFailoverTestIsPrimary { get; set; }
        public string VMNICFailoverTestIsResetMacAddress { get; set; }
        public string VMIdentifier { get; set; }
    }
}
