using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zertoBulkCreate.Models
{
    public class CsvCloudVPGList
    {
        public string VPGName { get; set; }
        public string ServiceProfile { get; set; }
        public string ReplicationPriority { get; set; }
        public string RecoverySiteName { get; set; }
        public string ClusterName { get; set; }
        public string DatastoreName { get; set; }
        public string JournalDatastore { get; set; }
        public string vCenterFolder { get; set; }
        public string OrgVdcName { get; set; }
        public string FailoverNetwork { get; set; }
        public string TestNetwork { get; set; }
    }
}
