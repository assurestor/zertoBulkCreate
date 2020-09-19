using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using Newtonsoft.Json.Linq;

namespace zertoBulkCreate.Services
{
    public class ZertoZvm
    {
        //Zerto Virtual Manager
        public static string zvm_Version = "1.0";
        public static string zvm_ContentTypeValue = "application/json";
        public static string zvm_Session;
        public static string zvm_BaseUrl;
        public static string cloudzvm_Session;
        public static string cloudzvm_BaseUrl;
        public static bool ignoreSsl;

        //Query String Parameters
        public static Dictionary<string, string> Parameters = new Dictionary<string, string>();
        public static String BuildURLParametersString(Dictionary<string, string> parameters)
        {
            UriBuilder uriBuilder = new UriBuilder();
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            foreach (var urlParameter in parameters)
            {
                query[urlParameter.Key] = urlParameter.Value;
            }
            uriBuilder.Query = query.ToString();
            return uriBuilder.Query;
        }
    }
    public class ZertoZvmApi
    {
        //Zerto ZVM API - REST v1

        public static bool GetSession(string baseUrl, string username, string password, string contentType, bool cloud = false)
        {
            try
            {
                var url = "/v1/session/add";
                var credentials = Encoding.ASCII.GetBytes(username + ":" + password);
                var data = new StringContent("", Encoding.UTF8, contentType);
                if (ZertoZvm.ignoreSsl)
                {
                    ServicePointManager.ServerCertificateValidationCallback +=
                        (sender, cert, chain, sslPolicyErrors) => { return true; };
                }
                var client = new HttpClient
                {
                    BaseAddress = new Uri(baseUrl)
                };
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));
                var response = client.PostAsync(url, data);
                if (response.Result.IsSuccessStatusCode)
                {
                    if (!cloud)
                    {
                        ZertoZvm.zvm_BaseUrl = baseUrl;
                        ZertoZvm.zvm_Session = response.Result.Headers.GetValues("x-zerto-session").FirstOrDefault().ToString();
                    }
                    else
                    {
                        ZertoZvm.cloudzvm_BaseUrl = baseUrl;
                        ZertoZvm.cloudzvm_Session = response.Result.Headers.GetValues("x-zerto-session").FirstOrDefault().ToString();
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private static JToken GetResult(string url, Dictionary<string, string> urlParameters, bool cloud = false)
        {
            var baseUrl = ZertoZvm.zvm_BaseUrl;
            var session = ZertoZvm.zvm_Session;
            if (cloud)
            {
                baseUrl = ZertoZvm.cloudzvm_BaseUrl;
                session = ZertoZvm.cloudzvm_Session;
            }
            if (ZertoZvm.ignoreSsl)
            {
                ServicePointManager.ServerCertificateValidationCallback +=
                    (sender, cert, chain, sslPolicyErrors) => { return true; };
            }
            var client = new HttpClient
            {
                BaseAddress = new Uri(baseUrl)
            };
            client.DefaultRequestHeaders.Add("x-zerto-session", session);
            String parameters = ZertoZvm.BuildURLParametersString(urlParameters);
            var response = client.GetAsync(url + parameters);
            var content = response.Result.Content.ReadAsStringAsync();
            var result = JToken.Parse(content.Result.ToString());
            return result;
        }

        private static string PostRequest(string url, JToken request, bool cloud = false)
        {
            var baseUrl = ZertoZvm.zvm_BaseUrl;
            var session = ZertoZvm.zvm_Session;
            if (cloud)
            {
                baseUrl = ZertoZvm.cloudzvm_BaseUrl;
                session = ZertoZvm.cloudzvm_Session;
            }
            if (ZertoZvm.ignoreSsl)
            {
                ServicePointManager.ServerCertificateValidationCallback +=
                    (sender, cert, chain, sslPolicyErrors) => { return true; };
            }
            var client = new HttpClient
            {
                BaseAddress = new Uri(baseUrl)
            };
            client.DefaultRequestHeaders.Add("x-zerto-session", session);
            var serializer = new JavaScriptSerializer();
            var content = new StringContent(request.ToString(), Encoding.UTF8, ZertoZvm.zvm_ContentTypeValue);
            var result = client.PostAsync(url, content).Result.Content.ReadAsStringAsync().Result.ToString();
            return result;
        }

        public static string GetVpgs(bool cloud = false)
        {
            ZertoZvm.Parameters.Clear();
            try
            {
                return ZertoZvmApi.GetResult("/v1/vpgs", ZertoZvm.Parameters, cloud).ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string GetVpgSettingsIdentifier(JToken body, bool cloud = false)
        {
            ZertoZvm.Parameters.Clear();
            try
            {
                return ZertoZvmApi.PostRequest("/v1/vpgSettings", body, cloud);
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string GetVpgSettingsObject(string VpgSettingsIdentifier, bool cloud = false)
        {
            ZertoZvm.Parameters.Clear();
            try
            {
                return ZertoZvmApi.GetResult("/v1/vpgSettings/" + VpgSettingsIdentifier, ZertoZvm.Parameters, cloud).ToString(); ;
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string CommitVpgSettingsObject(string VpgSettingsIdentifier, bool cloud = false)
        {
            ZertoZvm.Parameters.Clear();
            try
            {
                return ZertoZvmApi.PostRequest("/v1/vpgSettings/" + VpgSettingsIdentifier + "/commit", "", cloud);
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string GetZorgs(bool cloud = false)
        {
            ZertoZvm.Parameters.Clear();
            try
            {
                return ZertoZvmApi.GetResult("/v1/zorgs", ZertoZvm.Parameters, cloud).ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string GetLocalSiteIdentifier(bool cloud = false)
        {
            ZertoZvm.Parameters.Clear();
            try
            {
                return ZertoZvmApi.GetResult("/v1/localsite", ZertoZvm.Parameters, cloud).SelectToken("SiteIdentifier").ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string GetSites(bool cloud = false)
        {
            ZertoZvm.Parameters.Clear();
            try
            {
                return ZertoZvmApi.GetResult("/v1/virtualizationsites", ZertoZvm.Parameters, cloud).ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string GetNetworks(string TargetSiteIdentifier, bool cloud = false)
        {
            ZertoZvm.Parameters.Clear();
            try
            {
                return ZertoZvmApi.GetResult("/v1/virtualizationsites/" + TargetSiteIdentifier + "/networks", ZertoZvm.Parameters, cloud).ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string GetClusters(string TargetSiteIdentifier, bool cloud = false)
        {
            ZertoZvm.Parameters.Clear();
            try
            {
                return ZertoZvmApi.GetResult("/v1/virtualizationsites/" + TargetSiteIdentifier + "/hostclusters", ZertoZvm.Parameters, cloud).ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string GetDatastores(string TargetSiteIdentifier, bool cloud = false)
        {
            ZertoZvm.Parameters.Clear();
            try
            {
                return ZertoZvmApi.GetResult("/v1/virtualizationsites/" + TargetSiteIdentifier + "/datastores", ZertoZvm.Parameters, cloud).ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string GetFolders(string TargetSiteIdentifier, bool cloud = false)
        {
            ZertoZvm.Parameters.Clear();
            try
            {
                return ZertoZvmApi.GetResult("/v1/virtualizationsites/" + TargetSiteIdentifier + "/folders", ZertoZvm.Parameters, cloud).ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string GetOrgVdcs(string TargetSiteIdentifier, bool cloud = false)
        {
            ZertoZvm.Parameters.Clear();
            try
            {
                return ZertoZvmApi.GetResult("/v1/virtualizationsites/" + TargetSiteIdentifier + "/orgvdcs", ZertoZvm.Parameters, cloud).ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string GetOrgVdcNetworks(string TargetSiteIdentifier, string OrgVdcIdentifier, bool cloud = false)
        {
            ZertoZvm.Parameters.Clear();
            try
            {
                return ZertoZvmApi.GetResult("/v1/virtualizationsites/" + TargetSiteIdentifier + "/orgvdcs/" + OrgVdcIdentifier + "/networks", ZertoZvm.Parameters, cloud).ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string GetOrgVdcStorageProfiles(string TargetSiteIdentifier, string OrgVdcIdentifier, bool cloud = false)
        {
            ZertoZvm.Parameters.Clear();
            try
            {
                return ZertoZvmApi.GetResult("/v1/virtualizationsites/" + TargetSiteIdentifier + "/orgvdcs/" + OrgVdcIdentifier + "/storageprofiles", ZertoZvm.Parameters, cloud).ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string GetServiceProfiles(bool cloud = false)
        {
            ZertoZvm.Parameters.Clear();
            try
            {
                return ZertoZvmApi.GetResult("/v1/serviceprofiles", ZertoZvm.Parameters, cloud).ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string GetVpgSettings(string VpgSettingsIdentifier, bool cloud = false)
        {
            ZertoZvm.Parameters.Clear();
            try
            {
                if (VpgSettingsIdentifier == null)
                {
                    return ZertoZvmApi.GetResult("/v1/vpgSettings", ZertoZvm.Parameters, cloud).ToString();
                }
                else
                {
                    return ZertoZvmApi.GetResult("/v1/vpgSettings/" + VpgSettingsIdentifier, ZertoZvm.Parameters, cloud).ToString();
                }
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string GetVms(string TargetSiteIdentifier, bool cloud = false)
        {
            ZertoZvm.Parameters.Clear();
            try
            {
                return ZertoZvmApi.GetResult("/v1/virtualizationsites/" + TargetSiteIdentifier + "/vms", ZertoZvm.Parameters, cloud).ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string GetVpgId(string vpgName, bool cloud = false)
        {
            ZertoZvm.Parameters.Clear();
            try
            {
                if (vpgName.Length > 0) { ZertoZvm.Parameters.Add("name", vpgName); }
                return ZertoZvmApi.GetResult("/v1/vpgs", ZertoZvm.Parameters, cloud).SelectToken("[0].VpgIdentifier").ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }


        //
        // Calls for zertoFailover
        //
        public static JToken FailoverTest(string vpgName, string vmName)
        {
            ZertoZvm.Parameters.Clear();
            try
            {
                if (vpgName.Length > 0) { ZertoZvm.Parameters.Add("vpgName", vpgName); }
                if (vmName.Length > 0) { ZertoZvm.Parameters.Add("vmName", vmName); }

                JArray vms = (JArray)(ZertoZvmApi.GetResult("/v1/vms", ZertoZvm.Parameters));
                var vpgId = vms.SelectToken("[0].VpgIdentifier");
                var checkpointId = ZertoZvmApi.GetResult("/v1/vpgs/" + vpgId + "/checkpoints/stats", ZertoZvm.Parameters).SelectToken("Latest.CheckpointIdentifier");
                JArray VmIdentifiers = new JArray();
                foreach (var vm in vms)
                {
                    VmIdentifiers.Add(vm.SelectToken("VmIdentifier"));
                }
                JToken request = new JObject(
                    new JProperty("CheckpointIdentifier", checkpointId),
                    new JProperty("VmIdentifiers", VmIdentifiers));

                var result = JToken.Parse(ZertoZvmApi.PostRequest("/v1/vpgs/" + vpgId + "/FailoverTest", request));
                return result;
            }
            catch (Exception e)
            {
                return JToken.Parse(e.ToString());
            }
        }

        public static JToken FailoverTestStop(string vpgName)
        {
            try
            {
                var vpgId = GetVpgId(vpgName);
                var result = JToken.Parse(ZertoZvmApi.PostRequest("/v1/vpgs/" + vpgId + "/FailoverTestStop", ""));
                return result;
            }
            catch (Exception e)
            {
                return JToken.Parse(e.ToString());
            }
        }

        public static JToken Failover(string vpgName, string vmName, int commitPolicy, int waitTime)
        {
            ZertoZvm.Parameters.Clear();
            try
            {
                if (vpgName.Length > 0) { ZertoZvm.Parameters.Add("vpgName", vpgName); }
                if (vmName.Length > 0) { ZertoZvm.Parameters.Add("vmName", vmName); }

                JArray vms = (JArray)(ZertoZvmApi.GetResult("/v1/vms", ZertoZvm.Parameters));
                var vpgId = vms.SelectToken("[0].VpgIdentifier");
                var checkpointId = ZertoZvmApi.GetResult("/v1/vpgs/" + vpgId + "/checkpoints/stats", ZertoZvm.Parameters).SelectToken("Latest.CheckpointIdentifier");
                JArray VmIdentifiers = new JArray();
                foreach (var vm in vms)
                {
                    VmIdentifiers.Add(vm.SelectToken("VmIdentifier"));
                }
                JToken request = new JObject(
                    new JProperty("CheckpointIdentifier", checkpointId),
                    new JProperty("CommitPolicy", commitPolicy),
                    new JProperty("ShutdownPolicy", 2),
                    new JProperty("TimeToWaitBeforeShutdownInSec", waitTime),
                    new JProperty("IsReverseProtection", false),
                    new JProperty("VmIdentifiers", VmIdentifiers));

                var result = JToken.Parse(ZertoZvmApi.PostRequest("/v1/vpgs/" + vpgId + "/Failover", request));
                return result;
            }
            catch (Exception e)
            {
                return JToken.Parse(e.ToString());
            }
        }

        public static JToken FailoverCommit(string vpgName)
        {
            try
            {
                var vpgId = GetVpgId(vpgName);

                JToken request = new JObject(
                    new JProperty("IsReverseProtection", false));

                var result = JToken.Parse(ZertoZvmApi.PostRequest("/v1/vpgs/" + vpgId + "/FailoverCommit", ""));
                return result;
            }
            catch (Exception e)
            {
                return JToken.Parse(e.ToString());
            }
        }

        public static JToken FailoverRollback(string vpgName)
        {
            try
            {
                var vpgId = GetVpgId(vpgName);
                var result = JToken.Parse(ZertoZvmApi.PostRequest("/v1/vpgs/" + vpgId + "/FailoverRollback", ""));
                return result;
            }
            catch (Exception e)
            {
                return JToken.Parse(e.ToString());
            }
        }

        public static int TaskStatus(string taskId)
        {
            ZertoZvm.Parameters.Clear();
            try
            {
                var taskStatus = Convert.ToInt32(ZertoZvmApi.GetResult("/v1/tasks/" + taskId, ZertoZvm.Parameters).SelectToken("Status.State"));
                return taskStatus;
            }
            catch
            {
                return -1;
            }
        }

        public static bool TaskComplete(string taskId)
        {
            ZertoZvm.Parameters.Clear();
            try
            {
                var taskStatus = Convert.ToInt32(ZertoZvmApi.GetResult("/v1/tasks/" + taskId, ZertoZvm.Parameters).SelectToken("Status.State"));
                if (taskStatus == 4 || taskStatus == 5 || taskStatus == 6)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return true;                //returns true if an error occurs to avoid any processes waiting for the task to finish
            }

        }
    }
}
