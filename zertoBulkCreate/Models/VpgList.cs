using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zertoBulkCreate.Models
{
    public class CsvVPGList
    {
        public string VPGName { get; set; }
        public string JournalHistoryInHours { get; set; }
        public string ReplicationPriority { get; set; }
        public string RecoverySiteName { get; set; }
        public string RpoAlertInSeconds { get; set; }
        public string TestIntervalInMinutes { get; set; }
        public string ClusterName { get; set; }
        public string FailoverNetwork { get; set; }
        public string TestNetwork { get; set; }
        public string DatastoreName { get; set; }
        public string JournalDatastore { get; set; }
        public string JournalHardLimitInMB { get; set; }
        public string JournalWarningThresholdInMB { get; set; }
        public string vCenterFolder { get; set; }
    }
}
