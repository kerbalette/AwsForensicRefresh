using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwsForensicRefresh
{
    public class ApplicationArguments
    {
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string AccountId { get; set; }
        public string AllowedSubnet { get; set; }
        public string AWSRegion { get; set; }
        public string TerminateInstanceID { get; set; }
        public string TerminateInstanceName { get; set; }
    }
}
