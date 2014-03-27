using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace KmsNgWorkflow
{
    public class Configuration
    {
        public static string ClientId
        {
            get { return "9adfb1bf-1eb2-4cf8-b4ef-7a4c0754d32d"; }
        }

        public static string ClientSecret
        {
            get { return "p7JnIaDZDha7QGGlk08mGVVPBtjE9xHOrzti0NspA5E="; }
        }

        public static string AuthorizingTenantName
        {
            get { return "ford.30.msods.msol-nova.com"; }
        }

        public static string AuthIdentityProvider
        {
            get { return "https://sts.30.ESTS.msol-nova.com"; }
        }

        public static string AuthorizingTenant 
        {
            get { return (String.Format("{0}/{1}", AuthIdentityProvider, AuthorizingTenantName)); }
        }

        public static string ResourceAppId
        {
            get { return "http://localhost/kmsng"; }
        }

        public static string Partition
        {
            get { return "ford"; }
        }

        public static string KeyFolder
        {
            get { return "/keys/" + Partition; }
        }

        public static string KeyPartition
        {
            get { return Partition; }
        }

        public static string AzureDomainName
        {
            get { return "cloudapp.net";  }
        }

        public static byte[] KeyBlob { get { return Resources.KeyTransferPackage_Ford_BVT_Key; } }
    }
}
