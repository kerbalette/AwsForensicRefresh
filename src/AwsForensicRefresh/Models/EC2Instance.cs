using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.EC2.Model;

namespace AwsForensicRefresh.AWS.Models
{
    public class EC2Instance
    {
        public EC2Instance(string instanceId, string instanceName, string publicIPAddress, string instanceState)
        {
            InstanceId = instanceId;
            InstanceName = instanceName;
            PublicIPAddress = publicIPAddress;
            InstanceState = instanceState;
        }

        public string InstanceId { get; set; }
        public string InstanceName { get; set; }
        public string PublicIPAddress { get; set; }
        public string InstanceState { get; set; }
    }
}
