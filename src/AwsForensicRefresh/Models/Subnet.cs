namespace AwsForensicRefresh.Models
{
    public class Subnet
    {
        public string SubnetId { get; set; }
        public string VpcId { get; set; }
        public string AvailabilityZoneId { get; set; }
        public string AvailabilityZone { get; set; }
        public string CidrBlock { get; set; }

        public Subnet(string subnetId, string vpcId, string availabilityZoneId, string availabilityZone, string cidrBlock)
        {
            SubnetId = subnetId;
            VpcId = vpcId;
            AvailabilityZoneId = availabilityZoneId;
            AvailabilityZone = availabilityZone;
            CidrBlock = cidrBlock;
        }
    }
}