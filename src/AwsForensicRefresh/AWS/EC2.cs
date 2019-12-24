using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Runtime;
using AwsForensicRefresh.AWS.Models;

namespace AwsForensicRefresh.AWS
{
    public class EC2
    {
        private AWSCredentials AwsCredentials { get; set; }
        public EC2(AWSCredentials awsCredentials)
        {
            AwsCredentials = awsCredentials;
        }

        public async Task<List<EC2Instance>> DescribeInstances()
        {
            List<EC2Instance> ec2Instances = new List<EC2Instance>();


            bool done = false;
            var instanceIds = new List<string>();
            AmazonEC2Client ec2Client = new AmazonEC2Client(AwsCredentials, RegionEndpoint.APSoutheast2);
            DescribeInstancesRequest request = new DescribeInstancesRequest();
            while (!done)
            {
                DescribeInstancesResponse response = await ec2Client.DescribeInstancesAsync(request);

                foreach (Reservation reservation in response.Reservations)
                {
                    foreach (Instance instance in reservation.Instances)
                    {
                        string ownerTag = "";
                        string usageDescription = "";
                        string instanceName = "";
                        foreach (var tag in instance.Tags)
                        {
                            if (tag.Key == "owner")
                                ownerTag = tag.Value;

                            if (tag.Key == "usage-description")
                                usageDescription = tag.Value;
                            
                            if (tag.Key == "Name")
                                instanceName = tag.Value;
                        }
                        ec2Instances.Add(new EC2Instance(instance.InstanceId, instanceName, 
                            instance.PublicIpAddress, instance.PublicDnsName, instance.State.Name, 
                            instance.InstanceType,ownerTag,usageDescription));
                    }
                }

                if (response.NextToken == null)
                    done = true;
            }

            return ec2Instances.OrderBy(t => t.Owner).ToList();
        }
    }
}
