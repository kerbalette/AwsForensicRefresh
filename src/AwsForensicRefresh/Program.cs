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

namespace AwsForensicRefresh
{
    class Program
    {
        private static AWSCredentials _awsCredentials;
        

        static async Task Main(string[] args)
        {
            var arguments = new FluentCommandLineParser<ApplicationArguments>();
            arguments.Setup(arg => arg.AccessKey)
                .As('a', "accesskey")
                .WithDescription("AWS Access Key");

            arguments.Setup(arg => arg.SecretKey)
                .As('s', "secretkey")
                .WithDescription("AWS Secret Key");

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
                _awsCredentials = new BasicAWSCredentials(arguments.Object.AccessKey, arguments.Object.SecretKey);
                AWS.EC2 ec2 = new EC2(_awsCredentials);

                bool tearDown = UtilsConsole.Confirm("\nWould you like to terminate an existing Instance?");
                if (tearDown)
                {
                    var results = await ec2.DescribeInstances();
                    int instanceNumber = 0;
                    List<string> instanceTerminateList = new List<string>();
                    string defaultTerminateInstanceNumber = "";
                    foreach (var result in results)
                    {
                        if (applicationArguments.TerminateInstanceID == result.InstanceId)
                            defaultTerminateInstanceNumber =
                                $"[{instanceNumber}] {result.InstanceName}-({result.InstanceState})-({result.Owner})-({result.InstanceId})";

                        Console.WriteLine($"[{instanceNumber}] {result.InstanceName}-({result.InstanceState})-({result.Owner})-({result.InstanceId})");
                        
                        instanceTerminateList.Add(instanceNumber.ToString());
                        instanceNumber++;
                    }
                    
                    string terminate = Utils.UtilsConsole.ChooseOption("Which instance would you like to terminate? ", instanceTerminateList);
                    Console.WriteLine("Stopping " + terminate);
                }

                
            }
            Console.ReadLine();
            
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
