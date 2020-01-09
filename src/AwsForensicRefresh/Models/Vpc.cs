namespace AwsForensicRefresh.Models
{
    public class Vpc
    {
        public string VpcId { get; set; }
        public string OwnerId { get; set; }
        public string CidrBlock { get; set; }

        public Vpc(string vpcId, string ownerId, string cidrBlock)
        {
            VpcId = vpcId;
            OwnerId = ownerId;
            CidrBlock = cidrBlock;
        }
    }
    
    
}