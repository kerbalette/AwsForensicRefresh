using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Runtime;
using AwsForensicRefresh.Models;
using Address = AwsForensicRefresh.Models.Address;
using SecurityGroup = AwsForensicRefresh.Models.SecurityGroup;
using Subnet = AwsForensicRefresh.Models.Subnet;
using Vpc = AwsForensicRefresh.Models.Vpc;

namespace AwsForensicRefresh.AWS
{
    public class EC2
    {
        public EC2(AWSCredentials awsCredentials, string accountId, RegionEndpoint regionEndpoint) : this (awsCredentials.GetCredentials().AccessKey, awsCredentials.GetCredentials().SecretKey, accountId, regionEndpoint.SystemName)
        {
        }

        public EC2(string accessKey, string secretKey, string accountId, string regionEndpoint)
        {
            _awsCredentials = new BasicAWSCredentials(accessKey, secretKey);
            _accountId = accountId;
            _regionEndpoint = RegionEndpoint.GetBySystemName(regionEndpoint);
            _ec2Client = new AmazonEC2Client(_awsCredentials, _regionEndpoint);
        }
        
        private AWSCredentials _awsCredentials { get; set; }
        private string _accountId { get; set; }
        private RegionEndpoint _regionEndpoint { get; set; }
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
            if (address == null)
                return "";
            
            var response = _ec2Client.DescribeAddresses(new DescribeAddressesRequest());
            List<Amazon.EC2.Model.Address> addresses = response.Addresses;
            string retAddress = addresses.FirstOrDefault(s => s.PublicIp == address).AllocationId;
            return retAddress;
        }

        private string GetAssociationAddress(string address)
        {
            var response = _ec2Client.DescribeAddresses(new DescribeAddressesRequest());
            List<Amazon.EC2.Model.Address> addresses = response.Addresses;
            return addresses.First(s => s.PublicIp == address).AssociationId;
        }

        public void TerminateInstance(EC2Instance ec2Instance)
        {
            if (ec2Instance.PublicIpAddress != null)
            {
                string allocationId = GetAllocationAddress(ec2Instance.PublicIpAddress);
                string associationId = GetAssociationAddress(ec2Instance.PublicIpAddress);
                if (DisassociateAddress(associationId))
                    ReleaseAddress(allocationId);
            }

            DestroyInstance(ec2Instance);
        }

        public void RunInstance(string imageId, string vpcId, string securityGroupId, string owner, string usageDescription, string instanceType, int volumeSize, string keyName, string instanceName)
        {
            string subnetId = DescribeSubnets(vpcId).Result.SubnetId;
            
            
            List<BlockDeviceMapping> blockDeviceMappings = new List<BlockDeviceMapping>
            {
                new BlockDeviceMapping
                {
                    DeviceName = "/dev/sda1",
                    Ebs = new EbsBlockDevice {VolumeSize = volumeSize}
                }
            };

            var response = _ec2Client.RunInstances(new RunInstancesRequest
            {
                BlockDeviceMappings = blockDeviceMappings,
                ImageId = imageId,
                InstanceType = instanceType,
                KeyName = keyName,
                MinCount = 1,
                MaxCount = 1,
                SecurityGroupIds = new List<string>{securityGroupId},
                SubnetId = subnetId,
                TagSpecifications = new List<TagSpecification> {
                    new TagSpecification {
                        ResourceType = "instance",
                        Tags = new List<Tag> {
                            new Tag {
                                Key = "owner",
                                Value = owner
                            },
                            new Tag
                            {
                                Key = "usage-description",
                                Value = usageDescription
                            },
                            new Tag
                            {
                                Key = "Name",
                                Value = instanceName

                            }
                        }
                    }
                }
            });
        }
        
        public async Task<Subnet> DescribeSubnets(string vpcId)
        {
            List<Filter> filters = new List<Filter>
            {
                new Filter
                {
                    Name = "vpc-id",
                    Values = new List<string>
                    {
                        vpcId
                    }
                }
            };

            DescribeSubnetsRequest request = new DescribeSubnetsRequest {Filters = filters};

            DescribeSubnetsResponse response = await _ec2Client.DescribeSubnetsAsync(request);
            
            List<Subnet> subnets = new List<Subnet>();
            foreach (var item in response.Subnets)
            {
                subnets.Add(new Subnet(item.SubnetId, item.VpcId, item.AvailabilityZoneId, item.AvailabilityZone, item.CidrBlock));
            }

            return subnets.FirstOrDefault();
        }

        public async Task<List<Address>> DescribeAddresses()
        {
            DescribeAddressesRequest request = new DescribeAddressesRequest();
            DescribeAddressesResponse response = await _ec2Client.DescribeAddressesAsync(request);
            List<AwsForensicRefresh.Models.Address> addresses = new List<Address>();
            foreach (var item in response.Addresses)
            {
                addresses.Add(new Address(item.AllocationId, item.AssociationId, item.InstanceId, item.NetworkInterfaceId, item.PrivateIpAddress, item.PublicIp));
            }

            return addresses;
        }
        
        public async Task<List<AMImage>> DescribeImages()
        {
            DescribeImagesRequest request = new DescribeImagesRequest();
            request.Owners.Add(_accountId);
            DescribeImagesResponse response = await _ec2Client.DescribeImagesAsync(request);
            
            List<AMImage> amiImages = new List<AMImage>();
            foreach (var item in response.Images)
            {
                amiImages.Add(new AMImage(item.ImageId, item.Name, item.Description, item.CreationDate, item.Architecture, item.Platform, item.State, item.OwnerId));
            }

            return amiImages;
        }

        public async Task<List<Vpc>> DescribeVpcs()
        {
            DescribeVpcsRequest request = new DescribeVpcsRequest();
     
            DescribeVpcsResponse response = await _ec2Client.DescribeVpcsAsync();
            List<Vpc> vpcs = new List<Vpc>();
            foreach (var vpc in response.Vpcs)
            {
                vpcs.Add(new Vpc(vpc.VpcId, vpc.OwnerId, vpc.CidrBlock));
            }
            return vpcs;
        }
        
        
        public async Task<List<SecurityGroup>> DescribeSecurityGroups()
        {
            DescribeSecurityGroupsRequest request = new DescribeSecurityGroupsRequest();
     
            DescribeSecurityGroupsResponse response = await _ec2Client.DescribeSecurityGroupsAsync();
            List<SecurityGroup> securityGroups = new List<SecurityGroup>();
            foreach (var securityGroup in response.SecurityGroups)
            {
                securityGroups.Add(new SecurityGroup(securityGroup.GroupName, securityGroup.GroupId, securityGroup.Description, securityGroup.OwnerId, securityGroup.VpcId));
            }
            return securityGroups;
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
                            switch (tag.Key)
                            {
                                case "owner":
                                    ownerTag = tag.Value;
                                    break;
                                case "usage-description":
                                    usageDescription = tag.Value;
                                    break;
                                case "Name":
                                    instanceName = tag.Value;
                                    break;
                            }
                        }
                        
                        ec2Instances.Add(new EC2Instance(instance.InstanceId, instanceName, 
                            instance.PublicIpAddress, instance.PublicDnsName, instance.State.Name, 
                            instance.InstanceType,instance.ImageId, ownerTag, usageDescription));
                    }
                }

                if (response.NextToken == null)
                    done = true;
            }

            return ec2Instances.OrderBy(t => t.Owner).ToList();
        }
    }
}
