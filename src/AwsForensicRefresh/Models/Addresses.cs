using AwsForensicRefresh.AWS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwsForensicRefresh.Models
{
    public class Addresses
    {
        private List<AwsForensicRefresh.Models.Address> addresses;
        private EC2 _ec2;

        public Addresses(EC2 ec2)
        {
            _ec2 = ec2;
            AddressesAsync().Wait();
        }

        public int Count
        {
            get 
            {
                int count = addresses.Where(x => x.IsExternal == true).Count();
                return count;
            }
        }

        public async Task<List<Address>> AddressesAsync()
        {
            addresses = await _ec2.DescribeAddresses();
            return addresses;
        }

        
    }
}
