using Amazon.EC2.Model;

namespace AwsForensicRefresh.AWS.Models
{
    public class SecurityGroup
    {
        public string Description { get; set; }
        public string GroupId { get; set; }
        public string GroupName { get; set; }
        public string OwnerId { get; set; }
        public string VpcId { get; set; }

        public SecurityGroup(string groupName, string groupId, string description, string ownerId, string vpcId)
        {
            GroupName = groupName;
            GroupId = groupId;
            Description = description;
            OwnerId = ownerId;
            VpcId = vpcId;
        }
    }
}