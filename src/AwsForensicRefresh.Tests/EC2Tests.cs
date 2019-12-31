using System;
using System.Configuration;
using System.Threading.Tasks;
using Xunit;

namespace AwsForensicRefresh.Tests
{
    public class EC2Tests : IDisposable
    {
        private AWS.EC2 _ec2;
        
        public EC2Tests()
        {
            //var config = ConfigurationManager.OpenExeConfiguration(Assembly.GetCallingAssembly().Location);
            var config = ConfigurationManager.OpenExeConfiguration("AwsForensicRefresh.exe");
            var appSettings = (AppSettingsSection)config.GetSection("appSettings");
            
            _ec2 = new AWS.EC2(appSettings.Settings["AccessKey"].Value, appSettings.Settings["SecretKey"].Value, appSettings.Settings["AccountId"].Value, appSettings.Settings["AWSRegion"].Value);
        }

        [Fact]
        public async Task DescribeImages_ImagesReturned()
        {
            // Arrange
            int expected = 0;
            
            // Act
            var results = await _ec2.DescribeImages();
            
            // Assert
            Assert.True(results.Count > expected);
        }
        
        [Fact]
        public async Task DescribeInstances_InstancesReturned()
        {
            // Arrange
            int expected = 0;
            // Act
            var results = await _ec2.DescribeInstances();

            // Assert
            Assert.True(results.Count > expected);
        }

        public void Dispose()
        {
            
        }
    }
}