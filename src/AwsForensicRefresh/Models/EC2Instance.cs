using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.EC2.Model;

namespace AwsForensicRefresh.Models
{
    public class EC2Instance
    {
        public EC2Instance(string instanceId, string instanceName, 
            string publicIpAddress, string publicDnsName, string instanceState, 
            string instanceType, string imageId, string owner, string usageDescription)
        {
            InstanceId = instanceId;
            InstanceName = instanceName;
            PublicIpAddress = publicIpAddress;
            PublicDnsName = publicDnsName;
            InstanceState = instanceState;
            InstanceType = instanceType;
            ImageId = imageId;
            Owner = owner;
            UsageDescription = usageDescription;
            
        }

        public string InstanceId { get; set; }
        public string InstanceName { get; set; }
        public string PublicIpAddress { get; set; }
        public string PublicDnsName { get; set; }
        public string InstanceState { get; set; }
        public string InstanceType { get; set; }
        public string ImageId { get; set; }
        public string Owner { get; set; }
        public string UsageDescription { get; set; }
    }
}
