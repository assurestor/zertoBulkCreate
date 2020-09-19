using System;
using System.Text.RegularExpressions;

namespace zertoBulkCreate.Helpers
{
    public class Utils
    {
        public static string CleanString(string strIn)
        {
            // Replace invalid characters with empty strings.
            try
            {
                return Regex.Replace(strIn, @"[^\w\.@-]", "",
                                     RegexOptions.None, TimeSpan.FromSeconds(1.5));
            }
            // If we timeout when replacing invalid characters, 
            // we should return Empty.
            catch (RegexMatchTimeoutException)
            {
                return String.Empty;
            }
        }

        public static bool IsBool(string strIn, bool def = false)
        {
            try
            {
                return Convert.ToBoolean(strIn);
            }
            catch
            {
                return def;
            }
        }

        public static bool IsValidUuid(string strIn)
        {
            try
            {
                Regex rgx = new Regex(@"\b[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}\b");
                return rgx.IsMatch(strIn);
            }
            catch
            {
                return false;
            }
        }

        public static string IsPriority(string strIn)
        {
            try
            {
                switch (strIn.ToLower())
                {
                    case "low":
                        return "Low";
                    case "medium":
                        return "Medium";
                    case "high":
                        return "High";
                    case "3":
                        return "Low";
                    case "2":
                        return "Medium";
                    case "1":
                        return "High";

                    default:
                        return "Medium";
                }
            }
            catch
            {
                return "Medium";
            }
        }

        public static int IpMode(string strIn)
        {
            try
            {
                // 0: Static IP Pool - pulls IP addresses from the network's IP pool.
                // 1: DHCP - pulls IP addresses from a DHCP server.
                // 2: Static Manual - allows you to specify an IP address.        
                switch (strIn.ToLower())
                {
                    case "0":
                        return 0;
                    case "static ip pool":
                        return 0;
                    case "staticippool":
                        return 0;
                    case "ip pool":
                        return 0;
                    case "ippool":
                        return 0;
                    case "dhcp":
                        return 1;
                    case "1":
                        return 1;
                    case "dynamic":
                        return 1;
                    case "2":
                        return 2;
                    case "static manual":
                        return 2;
                    case "staticmanual":
                        return 2;
                    case "manual":
                        return 2;

                    default:
                        return 0;
                }
            }
            catch
            {
                return 0;
            }
        }
    }
}
