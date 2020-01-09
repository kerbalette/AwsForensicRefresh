using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwsForensicRefresh.Models
{
    public class Address
    {
        public string AllocationId { get; set; }
        public string AssociationId { get; set; }
        public string InstanceId { get; set; }
        public string NetworkInterfaceId { get; set; }
        public string PrivateIpAddress { get; set; }
        public string PublicIp { get; set; }

        public bool IsExternal { get
            {
                if (PublicIp.Length > 0)
                    return true;

                return false;
            } }

        public Address(string allocationId, string associationId, string instanceId, string networkInterfaceId, string privateIpAddress, string publicIp)
        {
            AllocationId = allocationId;
            AssociationId = associationId;
            InstanceId = instanceId;
            NetworkInterfaceId = networkInterfaceId;
            PrivateIpAddress = privateIpAddress;
            PublicIp = publicIp;
        }
    }

    
}
