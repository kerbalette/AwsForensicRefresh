using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        public EC2(AWSCredentials awsCredentials)
        {
            AwsCredentials = awsCredentials;
            _ec2Client = new AmazonEC2Client(awsCredentials, RegionEndpoint.APSoutheast2);
        }
        private AWSCredentials AwsCredentials { get; set; }
        private AmazonEC2Client _ec2Client;

        private void DestroyInstance(EC2Instance ec2Instance)
        {
            var terminateResponse = _ec2Client.TerminateInstances(new TerminateInstancesRequest
            {
                InstanceIds = new List<string> {ec2Instance.InstanceId}
            });
        }

        private bool DisassociateAddress(string associationId)
        {
            var disassociateResponse = _ec2Client.DisassociateAddress(new DisassociateAddressRequest
            {
                AssociationId = associationId
            });

            return disassociateResponse.HttpStatusCode == HttpStatusCode.OK ? true : false;
        }
        private bool ReleaseAddress(string allocationId)
        {
            var releaseResponse = _ec2Client.ReleaseAddress(new ReleaseAddressRequest
            {
                AllocationId = allocationId
            });

            return releaseResponse.HttpStatusCode == HttpStatusCode.OK ? true : false;
        }

        private string GetAllocationAddress(string address)
        {
            var response = _ec2Client.DescribeAddresses(new DescribeAddressesRequest());
            List<Address> addresses = response.Addresses;
            return addresses.First(s => s.PublicIp == address).AllocationId;
        }

        private string GetAssociationAddress(string address)
        {
            var response = _ec2Client.DescribeAddresses(new DescribeAddressesRequest());
            List<Address> addresses = response.Addresses;
            return addresses.First(s => s.PublicIp == address).AssociationId;
        }

        public void TerminateInstance(EC2Instance ec2Instance)
        {
            string allocationId = GetAllocationAddress(ec2Instance.PublicIPAddress);
            string associationId = GetAssociationAddress(ec2Instance.PublicIPAddress);
            if (DisassociateAddress(associationId));
                ReleaseAddress(allocationId);
            
            DestroyInstance(ec2Instance);
        }
        
        public async Task<List<EC2Instance>> DescribeInstances()
        {
            List<EC2Instance> ec2Instances = new List<EC2Instance>();


            bool done = false;
            var instanceIds = new List<string>();
            DescribeInstancesRequest request = new DescribeInstancesRequest();
            while (!done)
            {
                DescribeInstancesResponse response = await _ec2Client.DescribeInstancesAsync(request);

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
