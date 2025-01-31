﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fclp;
using System.Configuration;
using System.Net;
using System.Runtime.CompilerServices;
using Amazon;
using Amazon.Runtime;
using AwsForensicRefresh.AWS;
using AwsForensicRefresh.Utils;
using System.Reflection;
using AwsForensicRefresh.Models;

namespace AwsForensicRefresh
{
    class Program
    {
        private static AWSCredentials _awsCredentials;
        

        static async Task Main(string[] args)
        {
            var arguments = new FluentCommandLineParser<ApplicationArguments>();
            arguments.Setup(arg => arg.AccessKey)
                .As('k', "accesskey")
                .WithDescription("AWS Access Key");

            arguments.Setup(arg => arg.SecretKey)
                .As('s', "secretkey")
                .WithDescription("AWS Secret Key");
            
            arguments.Setup(arg => arg.AccountId)
                .As('a', "accountId")
                .WithDescription("AWS Account ID");

            arguments.Setup(arg => arg.AllowedSubnet)
                .As('u', "allowedsubnet")
                .WithDescription("Only allow access from this subnet");

            arguments.Setup(arg => arg.AWSRegion)
                .As('r', "awsregion")
                .WithDescription("AWS Region to delete and create new instance");

            arguments.Setup(arg => arg.TerminateInstanceID)
                .As('i', "instanceid")
                .WithDescription("Existing InstanceID to terminate");

            arguments.Setup(arg => arg.TerminateInstanceName)
                .As('n', "instancename")
                .WithDescription("Existing Instance Name to terminate");
            
            arguments.Setup(arg => arg.KeyName)
                .As('p', "keyname")
                .WithDescription("PEM Key pair to use for the new instance");
            
            arguments.Setup(arg => arg.WindowsInstanceType)
                .As('g', "windowsinstancetype")
                .SetDefault("t2.medium")
                .WithDescription("CPU Capacity, Memory and storage type for the Windows Forensic Instance");
            
            arguments.Setup(arg => arg.WindowsEbsVolumeSize)
                .As('h', "windowsebsvolumesize")
                .SetDefault("30")
                .WithDescription("Size in Gb of the disk volume associated with the Windows Forensic Instance");
            
            arguments.Setup(arg => arg.SiftInstanceType)
                .As('j', "siftinstancetype")
                .SetDefault("t2.micro")
                .WithDescription("CPU Capacity, Memory and storage type for the SIFT Forensic Instance");
            
            arguments.Setup(arg => arg.SiftEbsVolumeSize)
                .As('l', "siftebsvolumesize")
                .SetDefault("8")
                .WithDescription("Size in Gb of the disk volume associated with the SIFT Forensic Instance");

            arguments.SetupHelp("?", "help")
                .Callback(text => Console.WriteLine(text));

            var argumentResult = arguments.Parse(args);

            if (argumentResult.HasErrors || argumentResult.EmptyArgs)
            {
                Console.WriteLine("Usage:");
                arguments.HelpOption.ShowHelp(arguments.Options);
            }

            if (argumentResult.HasErrors == false)
            {
                var applicationArguments = arguments.Object;
                AppConfiguration(applicationArguments);
               
                EC2 ec2 = new EC2(arguments.Object.AccessKey, arguments.Object.SecretKey, arguments.Object.AccountId, arguments.Object.AWSRegion);
                bool instanceTerminated = false;

                if (UtilsConsole.Confirm("Would you like to terminate an existing Instance?"))
                {
                    instanceTerminated = await CheckTerminateInstance(ec2, applicationArguments);

                }

                ReturnAMImage returnAmImage = await ChooseAmImage(ec2, applicationArguments);
                string securityGroupId=  await ChooseSecurityGroup(ec2);
                string vpcId = await ChooseVpc(ec2);
                string owner = UtilsConsole.AskQuestion("Who is the owner of this Instance", Environment.UserName);
                string usageDescription = UtilsConsole.AskQuestion("What will be the usage for this Instance");
                string instanceName = Utils.UtilsConsole.AskQuestion("What name will this instance have",
                    EstimateInstanceName(owner, returnAmImage.Platform));

                // TODO Public IP, owner, usage and name Tags

                // check how many Elastic IP addresses are available. If there are 5 or more then check if an instance was terminated
                // If an instance was terminated then ask if your want to use that one
                // Otherwise check which instances are stopped then suggest one which is of the same platform as the instance type you would like to take the
                // elastic IP from

                //List<AwsForensicRefresh.Models.Address> addresses = new Addresses();
                Addresses addresses = new Addresses(ec2);
                int a = addresses.Count;

                ec2.RunInstance(returnAmImage.ImageId, vpcId, securityGroupId, owner, usageDescription, returnAmImage.InstanceType, Convert.ToInt32(returnAmImage.EbsVolumeSize), applicationArguments.KeyName, instanceName);


            }
            Console.ReadLine();
        }

        private static string EstimateInstanceName(string username, string platform)
        {
            if (platform.ToLower() == "windows")
                return $"{username}-win2k19-x64-forensic";
            else
                return $"{username}-SIFT-x64-forensic";
        }
        
        private static async Task<string> ChooseVpc(EC2 ec2)
        {
            int vpcNumber = 0;
            string vpcList = "";
            List<string> allowedKeys = new List<string>();
            List<Vpc> vpcs = await ec2.DescribeVpcs();
            foreach (var vpc in vpcs)
            {
                vpcList = $"[{vpcNumber}] {vpc.VpcId} {vpc.CidrBlock}";
                Console.WriteLine($"{vpcList}");
                allowedKeys.Add(vpcNumber.ToString());
                vpcNumber++;
            }

            string chosenVpc = Utils.UtilsConsole.ChooseOption(
                "Which VPC Subnet would you like to use for your new instance? ", allowedKeys);
            
            
            return vpcs[Convert.ToInt32(chosenVpc)].VpcId;
        }
        
        private static async Task<string> ChooseSecurityGroup(EC2 ec2)
        {
            int securityGroupNumber = 0;
            string securityGroupList = "";
            List<string> allowedKeys = new List<string>();
            List<SecurityGroup> securityGroups = await ec2.DescribeSecurityGroups();
            foreach (var securityGroup in securityGroups)
            {
                securityGroupList = $"[{securityGroupNumber}] {securityGroup.GroupName} {securityGroup.VpcId}";
                Console.WriteLine($"{securityGroupList}");
                allowedKeys.Add(securityGroupNumber.ToString());
                securityGroupNumber++;
            }

            string chosenSecurityGroup = Utils.UtilsConsole.ChooseOption(
                "Which Security Group would you like to use for your new instance? ", allowedKeys);

            return securityGroups[Convert.ToInt32(chosenSecurityGroup)].GroupId;
        }

        private static async Task<ReturnAMImage> ChooseAmImage(EC2 ec2, ApplicationArguments applicationArguments)
        {
            int imageNumber = 0;
            string imageList = "";
            List<string> allowedKeys = new List<string>();
            List<AMImage> amImages = await ec2.DescribeImages();
            foreach (var image in amImages)
            {
                string platform = "";
                if (image.Platform == "Windows")
                    platform = "windows";
                else
                    platform = "linux";
                
                imageList = $"[{imageNumber}] {image.Name}-{image.ImageId}-{platform}";
                Console.WriteLine($"{imageList}");
                allowedKeys.Add(imageNumber.ToString());
                imageNumber++;
            }
            string chosenImage = Utils.UtilsConsole.ChooseOption(
                "Which AMI Image number would you like to use for your new instance? ", allowedKeys);

            var imageToUse = amImages[Convert.ToInt32(chosenImage)];
            ReturnAMImage returnAmImage;
            if (imageToUse.Platform == "Windows")
            {
                returnAmImage.InstanceType = applicationArguments.WindowsInstanceType;
                returnAmImage.EbsVolumeSize = applicationArguments.WindowsEbsVolumeSize;
                returnAmImage.Platform = "windows";
            }
            else
            {
                returnAmImage.InstanceType = applicationArguments.SiftInstanceType;
                returnAmImage.EbsVolumeSize = applicationArguments.SiftEbsVolumeSize;
                returnAmImage.Platform = "linux";
            }

            returnAmImage.ImageId = imageToUse.ImageId;
            return returnAmImage;
        }

        private static async Task<bool> CheckTerminateInstance(EC2 ec2, ApplicationArguments applicationArguments)
        {
            bool terminateInstance = false;
            var results = await ec2.DescribeInstances();
            int instanceNumber = 0;
            List<string> allowedKeys = new List<string>();
            string InstanceList = "";
            foreach (var result in results)
            {
                InstanceList =
                    $"[{instanceNumber}] {result.InstanceName}-({result.InstanceState})-({result.Owner})-({result.InstanceId})";

                if (applicationArguments.TerminateInstanceID == result.InstanceId)
                    InstanceList =
                        $"[{instanceNumber}] * {result.InstanceName}-({result.InstanceState})-({result.Owner})-({result.InstanceId})";

                Console.WriteLine($"{InstanceList}");

                allowedKeys.Add(instanceNumber.ToString());
                instanceNumber++;
            }

            Console.WriteLine();

            string terminate =
                Utils.UtilsConsole.ChooseOption("Which instance would you like to terminate? or press [N] for None ",
                    allowedKeys);

            if (terminate != "N")
            {
                var ec2Terminate = results[Convert.ToInt32(terminate)];

                Console.WriteLine();
                if (UtilsConsole.Confirm($"Do you really want to terminate {ec2Terminate.InstanceName}?"))
                {
                    ec2.TerminateInstance(ec2Terminate);
                    terminateInstance = true;
                }
            }
            return terminateInstance;
        }

        static string PromptForInput(string message)
        {
            Console.Write(message);
            return (Console.ReadLine());
        }

        public void ListInstances()
        {

        }

        #region AppConfiguration

        private static void AppConfiguration(ApplicationArguments applicationArguments)
        {
            PropertyInfo[] properties = applicationArguments.GetType().GetProperties();
            foreach (PropertyInfo prop in properties)
            {
                var propertyValue = prop.GetValue(applicationArguments, null);
                if (propertyValue == null)
                {
                    propertyValue = ReadString(prop.Name);
                    prop.SetValue(applicationArguments, propertyValue);
                }
                else
                {
                    AddUpdateAppSettings(prop.Name, propertyValue.ToString());
                }
            }
        }

        private static string ReadString(string key)
        {
            var appSettings = ConfigurationManager.AppSettings;
            string result = appSettings[key] ?? "Not Found";
            return result;
        }

        private static void AddUpdateAppSettings(string key, string value)
        {
            var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var settings = configFile.AppSettings.Settings;
            if (settings[key] == null)
                settings.Add(key, value);
            else
                settings[key].Value = value;

            configFile.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
        }
        #endregion
    }
}
