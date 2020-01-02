using System;
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
using AwsForensicRefresh.AWS.Models;

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

                await ChooseAMImage(ec2);
                
                await ChooseSecurityGroup(ec2);
                
                await ChooseVpc(ec2);
                
                
                
            }
            Console.ReadLine();
        }

        
        private static async Task ChooseVpc(EC2 ec2)
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

            string chosenImage = Utils.UtilsConsole.ChooseOption(
                "Which VPC Subnet would you like to use for your new instance? ", allowedKeys);
        }
        
        private static async Task ChooseSecurityGroup(EC2 ec2)
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

            string chosenImage = Utils.UtilsConsole.ChooseOption(
                "Which Security Group would you like to use for your new instance? ", allowedKeys);
        }

        private static async Task ChooseAMImage(EC2 ec2)
        {
            int imageNumber = 0;
            string imageList = "";
            List<string> allowedKeys = new List<string>();
            List<AMImage> amImages = await ec2.DescribeImages();
            foreach (var image in amImages)
            {
                imageList = $"[{imageNumber}] {image.Name}-{image.ImageId}";
                Console.WriteLine($"{imageList}");
                allowedKeys.Add(imageNumber.ToString());
                imageNumber++;
            }

            string chosenImage = Utils.UtilsConsole.ChooseOption(
                "Which AMI Image number would you like to use for your new instance? ", allowedKeys);
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

            if (terminate == "N")
            {
                
            }
            else
            {
                var ec2Terminate = results[Convert.ToInt32(terminate)];

                Console.WriteLine();
                if (UtilsConsole.Confirm($"Do you really want to terminate {ec2Terminate.InstanceName}?"))
                {
                    // ec2.TerminateInstance(ec2Terminate);
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
