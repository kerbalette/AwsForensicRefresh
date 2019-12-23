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
        public EC2Instance(string instanceId, string instanceName, 
            string publicIPAddress, string publicDnsName, string instanceState, 
            string instanceType, string owner, string usageDescription)
        {
            InstanceId = instanceId;
            InstanceName = instanceName;
            PublicIPAddress = publicIPAddress;
            PublicDnsName = publicDnsName;
            InstanceState = instanceState;
            InstanceType = instanceType;
            Owner = owner;
            UsageDescription = usageDescription;
            
        }

        public string InstanceId { get; set; }
        public string InstanceName { get; set; }
        public string PublicIPAddress { get; set; }
        public string PublicDnsName { get; set; }
        public string InstanceState { get; set; }
        public string InstanceType { get; set; }
        public string Owner { get; set; }
        public string UsageDescription { get; set; }
    }
}
