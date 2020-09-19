using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using CsvHelper;
using IniParser;
using IniParser.Model;
using Newtonsoft.Json.Linq;

using zertoBulkCreate.Helpers;
using zertoBulkCreate.Models;
using zertoBulkCreate.Services;

namespace zertoBulkCreate
{
    class Program
    {
        public class Options
        {
            [Option('m', "mode", Required = true, HelpText = "Specify the bulk create operation type (VRA | VPG | CLOUDVPG | VCDVPG | CLOUDVCDVPG")]
            public string Mode { get; set; }

            [Option('c', "csv", Required = true, HelpText = "Points to the csv file containing the objects to be created (VRAs or VPGs)")]
            public string Csv { get; set; }

            [Option('v', "vms", Required = false, HelpText = "Points to the csv file containing list of VMs (used when mode is set to create VPGs)")]
            public string VmList { get; set; }

            [Option('t', "waitTime", Default = 120, Required = false, HelpText = "The amount of time in seconds to wait between each create task")]
            public int WaitTime { get; set; }

            [Option('z', "zorg", Required = false, HelpText = "the ZORG name for the creation of Cloud VPGs (this is mandatory for CLOUDVPG & CLOUDVCDVPG modes)")]
            public string ZorgName { get; set; }

            public static bool debug = false;
        }

        private static void Initialise()
        {
            // Read zertoBulkCreate.ini variables
            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile("zertoBulkCreate.ini");
            ZvmConnection.zvm = data["ZVM"]["uri"];
            ZvmConnection.zvm_username = data["ZVM"]["username"];
            ZvmConnection.zvm_password = data["ZVM"]["password"];
            CloudZvmConnection.zvm = data["CLOUDZVM"]["uri"];
            CloudZvmConnection.zvm_username = data["CLOUDZVM"]["username"];
            CloudZvmConnection.zvm_password = data["CLOUDZVM"]["password"];
            ZertoZvm.ignoreSsl = Convert.ToBoolean(data["OPTIONS"]["ignoreSSL"]);
            Options.debug = Utils.IsBool(data["OPTIONS"]["debug"], false);
        }

        private static async Task<bool> Delay(int secs)
        {
            var count = 0;
            while (count < secs)
            {
                Output.Write(".");
                count++;
                await Task.Delay(1000);
            }

            return true;
        }

        private static async Task<bool> CheckTaskStatus(string taskId)
        {
            if (taskId.Contains("Message"))
            {
                return false;
            }
            var taskStatus = false;
            while (!taskStatus)
            {
                taskStatus = ZertoZvmApi.TaskComplete(taskId);
                Output.Write(".");
                await Task.Delay(5000);
            }
            return true;
        }

        private static int GetCommitPolicy(string commitPolicy)
        {
            switch (commitPolicy.ToUpper())
            {
                case "ROLLBACK":
                    return 0;
                case "COMMIT":
                    return 1;
                default:
                    return 2;
            }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o =>
            {
                if (o.Mode.ToUpper() == "VRA" || o.Mode.ToUpper() == "VPG" || o.Mode.ToUpper() == "CLOUDVPG" || o.Mode.ToUpper() == "VCDVPG" || o.Mode.ToUpper() == "CLOUDVCDVPG")
                {
                    Initialise();
                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    Console.Clear();
                    Output.WriteLine("Zerto Bulk Creation Tool");
                    Output.WriteLine("------------------------------------------------------------------------------");
                    Output.WriteLine("Mode:          " + o.Mode.ToUpper());
                    Output.WriteLine("Local ZVM:     " + ZvmConnection.zvm);
                    if (o.Mode.ToUpper() == "CLOUDVPG" || o.Mode.ToUpper() == "CLOUDVCDVPG")
                    {
                        Output.WriteLine("Cloud ZVM:     " + CloudZvmConnection.zvm);
                        Output.WriteLine("ZORG:          " + o.ZorgName);
                    }
                    Output.WriteLine("WaitTime(s):   " + o.WaitTime.ToString());
                    Output.WriteLine("CSV:           " + o.Csv.ToUpper());
                    if (o.Mode.ToUpper() == "VPG" || o.Mode.ToUpper() == "CLOUDVPG" || o.Mode.ToUpper() == "VCDVPG" || o.Mode.ToUpper() == "CLOUDVCDVPG")
                        Output.WriteLine("VM List:       " + o.VmList.ToUpper());
                    Output.WriteLine("------------------------------------------------------------------------------");

                    try
                    {
                        if (o.Mode.ToUpper() == "CLOUDVCDVPG")
                        {
                            try
                            {
                                if (!string.IsNullOrEmpty(o.ZorgName))
                                {
                                    try
                                    {
                                        if (ZertoZvmApi.GetSession(ZvmConnection.zvm, ZvmConnection.zvm_username, ZvmConnection.zvm_password, "application/json") && ZertoZvmApi.GetSession(CloudZvmConnection.zvm, CloudZvmConnection.zvm_username, CloudZvmConnection.zvm_password, "application/json", true))
                                        {
                                            try
                                            {
                                                JArray Zorgs = JArray.Parse(ZertoZvmApi.GetZorgs(true));
                                                if (Zorgs.Count > 0)
                                                {
                                                    try
                                                    {
                                                        var Zorg = Zorgs.SelectToken("$.[?(@.ZorgName=='" + o.ZorgName + "')]");
                                                        if (Zorg != null)
                                                        {
                                                            try
                                                            {
                                                                //Import VPG CSV
                                                                var reader1 = new StreamReader(o.Csv);
                                                                CsvReader csv1 = new CsvReader(reader1);
                                                                csv1.Configuration.HasHeaderRecord = true;
                                                                csv1.Configuration.MissingFieldFound = null;
                                                                var vpgRecords = csv1.GetRecords<CsvCloudVPGList>();

                                                                //Import VM CSV
                                                                var reader2 = new StreamReader(o.VmList);
                                                                CsvReader csv2 = new CsvReader(reader2);
                                                                csv2.Configuration.HasHeaderRecord = true;
                                                                csv2.Configuration.HeaderValidated = null;
                                                                csv2.Configuration.MissingFieldFound = null;
                                                                var vmRecords = csv2.GetRecords<CsvVMList>();

                                                                //Create VM List
                                                                List<CsvVMList> vmList = new List<CsvVMList>();
                                                                foreach (var vm in vmRecords)
                                                                {
                                                                    try
                                                                    {
                                                                        vmList.Add(vm);
                                                                    }
                                                                    catch (Exception e)
                                                                    {
                                                                        Output.WriteLine("EXCEPTION PARSING VM LIST!");
                                                                        Output.WriteLine(e.Message);
                                                                        Output.WriteLine(e.Data.ToString());
                                                                    }
                                                                }

                                                                // Connect to ZVM API and start bulk creation process
                                                                foreach (var vpg in vpgRecords)
                                                                {
                                                                    try
                                                                    {
                                                                        Output.WriteLine("Starting VPG Creation Process");
                                                                        Output.WriteLine("--VPG: " + vpg.VPGName);

                                                                        // Set VPG Varibles
                                                                        var VPGName = vpg.VPGName;
                                                                        var ServiceProfile = vpg.ServiceProfile;
                                                                        var ReplicationPriority = Utils.IsPriority(vpg.ReplicationPriority);
                                                                        var RecoverySiteName = vpg.RecoverySiteName;
                                                                        var OrgVdcName = vpg.OrgVdcName;
                                                                        var FailoverNetwork = vpg.FailoverNetwork;
                                                                        var TestNetwork = vpg.TestNetwork;
                                                                        var ZorgIdentifier = Zorg.SelectToken("ZorgIdentifier").ToString();

                                                                        // Get identifiers for VPG settings
                                                                        Output.WriteLine("--Requesting Zerto Identifiers");
                                                                        Output.WriteDebug("ZorgIdentifier: " + ZorgIdentifier);
                                                                        var LocalSiteIdentifier = ZertoZvmApi.GetLocalSiteIdentifier();
                                                                        Output.WriteDebug("LocalSiteIdentifier: " + LocalSiteIdentifier);

                                                                        JArray ServiceProfiles = JArray.Parse(ZertoZvmApi.GetServiceProfiles());
                                                                        var ServiceProfileIdentifier = ServiceProfiles.SelectToken("$.[?(@.ServiceProfileName=='" + ServiceProfile + "')]").SelectToken("ServiceProfileIdentifier").ToString();
                                                                        Output.WriteDebug("ServiceProfileIdentifier: " + ServiceProfileIdentifier);

                                                                        JArray Sites = JArray.Parse(ZertoZvmApi.GetSites());
                                                                        var TargetSiteIdentifier = Sites.SelectToken("$.[?(@.VirtualizationSiteName=='" + RecoverySiteName + "')]").SelectToken("SiteIdentifier").ToString();
                                                                        Output.WriteDebug("TargetSiteIdentifier: " + TargetSiteIdentifier);

                                                                        JArray OrgVdcs = JArray.Parse(ZertoZvmApi.GetOrgVdcs(TargetSiteIdentifier));
                                                                        var OrgVdcIdentifier = OrgVdcs.SelectToken("$.[?(@.OrgVdcName=='" + OrgVdcName + "')]").SelectToken("Identifier").ToString();
                                                                        Output.WriteDebug("OrgVdcIdentifier: " + OrgVdcIdentifier);

                                                                        JArray OrgVdcNetworks = JArray.Parse(ZertoZvmApi.GetOrgVdcNetworks(TargetSiteIdentifier.ToString(), OrgVdcIdentifier.ToString()));
                                                                        var FailoverNetworkIdentifier = OrgVdcNetworks.SelectToken("$.[?(@.VirtualizationNetworkName=='" + FailoverNetwork + "')]").SelectToken("NetworkIdentifier").ToString();
                                                                        var TestNetworkIdentifier = OrgVdcNetworks.SelectToken("$.[?(@.VirtualizationNetworkName=='" + TestNetwork + "')]").SelectToken("NetworkIdentifier").ToString();
                                                                        Output.WriteDebug("FailoverNetworkIdentifier: " + FailoverNetworkIdentifier);
                                                                        Output.WriteDebug("TestNetworkIdentifier: " + TestNetworkIdentifier);

                                                                        // Get VM identifier for each VM in VPG
                                                                        Output.WriteLine("--Adding Virtual Machines");
                                                                        List<CsvVMList> vpgVms = new List<CsvVMList>();
                                                                        JArray Vms = JArray.Parse(ZertoZvmApi.GetVms(LocalSiteIdentifier));
                                                                        foreach (var vm in vmList)
                                                                        {
                                                                            try
                                                                            {
                                                                                if (vm.VPGName == VPGName)
                                                                                {
                                                                                    Output.WriteLine("----VM: " + vm.VMName);
                                                                                    vm.VMIdentifier = Vms.SelectToken("$.[?(@.VmName=='" + vm.VMName + "')]").SelectToken("VmIdentifier").ToString();
                                                                                    vpgVms.Add(vm);
                                                                                }
                                                                            }
                                                                            catch (Exception e)
                                                                            {
                                                                                Output.WriteLine("EXCEPTION PARSING VM LIST!");
                                                                                Output.WriteLine(e.Message);
                                                                                Output.WriteLine(e.Data.ToString());
                                                                            }
                                                                        }

                                                                        // Create VPG JSON Template
                                                                        Output.WriteLine("--Creating VPG JSON Template");
                                                                        var vpgJsonTemplate = File.ReadAllText("./Templates/cloudvcdvpg.txt");
                                                                        var vmJsonTemplate = File.ReadAllText("./Templates/vm.txt");
                                                                        var nicJsonTemplate = File.ReadAllText("./Templates/nic.txt");

                                                                        // Update JSON with VPG Settings
                                                                        try
                                                                        {

                                                                            JToken vpgJson = JToken.Parse(vpgJsonTemplate);
                                                                            JObject json = (JObject)vpgJson["Basic"];
                                                                            json["Name"] = VPGName;
                                                                            json["Priority"] = ReplicationPriority;
                                                                            json["ProtectedSiteIdentifier"] = LocalSiteIdentifier;
                                                                            json["RecoverySiteIdentifier"] = TargetSiteIdentifier;
                                                                            json["ServiceProfileIdentifier"] = ServiceProfileIdentifier;
                                                                            json["ZorgIdentifier"] = ZorgIdentifier;

                                                                            json = (JObject)vpgJson["Networks"]["Failover"]["VCD"];
                                                                            json["DefaultRecoveryOrgVdcNetworkIdentifier"] = FailoverNetworkIdentifier;

                                                                            json = (JObject)vpgJson["Networks"]["FailoverTest"]["VCD"];
                                                                            json["DefaultRecoveryOrgVdcNetworkIdentifier"] = TestNetworkIdentifier;

                                                                            json = (JObject)vpgJson["Recovery"]["VCD"];
                                                                            json["OrgVdcIdentifier"] = OrgVdcIdentifier;

                                                                            // Update JSON with VM Settings
                                                                            try
                                                                            {
                                                                                JArray vms = (JArray)vpgJson["Vms"];
                                                                                foreach (var vm in vpgVms)
                                                                                {
                                                                                    var vmJson = JToken.Parse(vmJsonTemplate);
                                                                                    json = (JObject)vmJson;
                                                                                    json["VmIdentifier"] = vm.VMIdentifier;

                                                                                    // Update Nic0 with NIC Settings
                                                                                    try
                                                                                    {
                                                                                        JArray nics = (JArray)vmJson["Nics"];
                                                                                        var nicJson = JToken.Parse(nicJsonTemplate);

                                                                                        // Failover Network
                                                                                        json = (JObject)nicJson.Value<JObject>("Failover").Value<JObject>("VCD");
                                                                                        if (Utils.IpMode(vm.VMNICFailoverIPMode) == 2)
                                                                                        {
                                                                                            json["IpMode"] = Utils.IpMode(vm.VMNICFailoverIPMode);
                                                                                            json["IpAddress"] = vm.VMNICFailoverIPAddress;
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            json["IpMode"] = Utils.IpMode(vm.VMNICFailoverIPMode);
                                                                                        }
                                                                                        json["IsConnected"] = Utils.IsBool(vm.VMNICFailoverIsConnected, true);
                                                                                        json["IsPrimary"] = Utils.IsBool(vm.VMNICFailoverIsPrimary, true);
                                                                                        json["IsResetMacAddress"] = Utils.IsBool(vm.VMNICFailoverIsResetMacAddress);
                                                                                        json["RecoveryOrgVdcNetworkIdentifier"] = FailoverNetworkIdentifier;

                                                                                        // Failover Test Network
                                                                                        json = (JObject)nicJson.Value<JObject>("FailoverTest").Value<JObject>("VCD");
                                                                                        if (Utils.IpMode(vm.VMNICFailoverTestIPMode) == 2)
                                                                                        {
                                                                                            json["IpMode"] = Utils.IpMode(vm.VMNICFailoverTestIPMode);
                                                                                            json["IpAddress"] = vm.VMNICFailoverTestIPAddress;
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            json["IpAddress"] = vm.VMNICFailoverTestIPAddress;
                                                                                        }
                                                                                        json["IsConnected"] = Utils.IsBool(vm.VMNICFailoverTestIsConnected, true);
                                                                                        json["IsPrimary"] = Utils.IsBool(vm.VMNICFailoverTestIsPrimary, true);
                                                                                        json["IsResetMacAddress"] = Utils.IsBool(vm.VMNICFailoverTestIsResetMacAddress);
                                                                                        json["RecoveryOrgVdcNetworkIdentifier"] = TestNetworkIdentifier;

                                                                                        nics.Add(nicJson);
                                                                                    }
                                                                                    catch (Exception e)
                                                                                    {
                                                                                        Output.WriteLine("EXCEPTION!");
                                                                                        Output.WriteLine(e.Message);
                                                                                        Output.WriteDebug(e.Data.ToString());
                                                                                    }
                                                                                    vms.Add(vmJson);
                                                                                }
                                                                            }
                                                                            catch (Exception e)
                                                                            {
                                                                                Output.WriteLine("EXCEPTION!");
                                                                                Output.WriteLine(e.Message);
                                                                                Output.WriteDebug(e.Data.ToString());
                                                                            }

                                                                            Output.WriteDebug(vpgJson.ToString());

                                                                            // POST VPG JSON request and get VpgSettingsIdentifier from ZVM API
                                                                            Output.WriteLine("--Requesting vpgSettingIdentifier");
                                                                            var vpgSettingsIdentifier = Utils.CleanString(ZertoZvmApi.GetVpgSettingsIdentifier(vpgJson));
                                                                            Output.WriteDebug(vpgSettingsIdentifier);

                                                                            if (Utils.IsValidUuid(vpgSettingsIdentifier))
                                                                            {
                                                                                // Get VpgSettingsIdentifier Template from ZVM API
                                                                                Output.WriteLine("--Requesting vpgSetting Template");
                                                                                var vpgSettingTemplate = ZertoZvmApi.GetVpgSettingsObject(vpgSettingsIdentifier);
                                                                                Output.WriteDebug(vpgSettingTemplate);
                                                                                
                                                                                // Commit VpgSettingsIdentifier Template to ZVM API to initiate VPG creation
                                                                                Output.WriteLine("--Requesting VPG Create Task");
                                                                                var vpgCreateTask = ZertoZvmApi.CommitVpgSettingsObject(vpgSettingsIdentifier);
                                                                                Output.WriteDebug(vpgCreateTask);

                                                                                //Pause for WaitTime
                                                                                Output.WriteLine("Pausing for " + o.WaitTime + " seconds...");
                                                                                Thread.Sleep(o.WaitTime * 1000);
                                                                                Output.WriteLine("");
                                                                            }
                                                                            else
                                                                            {
                                                                                Output.WriteLine("Aborting VPG Creation as vpgSettingIdentifier not valid!");
                                                                                Output.WriteLine("");
                                                                            }
                                                                        }
                                                                        catch (Exception e)
                                                                        {
                                                                            Output.WriteLine("EXCEPTION!");
                                                                            Output.WriteLine(e.Message);
                                                                            Output.WriteDebug(e.Data.ToString());
                                                                        }


                                                                    }
                                                                    catch (Exception e)
                                                                    {
                                                                        Output.WriteLine("EXCEPTION!");
                                                                        Output.WriteLine(e.Message);
                                                                        Output.WriteDebug(e.Data.ToString());
                                                                    }
                                                                }
                                                            }
                                                            catch (Exception e)
                                                            {
                                                                Output.WriteLine("EXCEPTION!");
                                                                Output.WriteLine(e.Message);
                                                                Output.WriteDebug(e.Data.ToString());
                                                            }
                                                        }
                                                        else
                                                        {
                                                            Output.WriteLine("WARNING!");
                                                            Output.WriteLine("Aborting as unable to find ZORG Identifier");
                                                        }
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        Output.WriteLine("EXCEPTION!");
                                                        Output.WriteLine(e.Message);
                                                        Output.WriteDebug(e.Data.ToString());
                                                    }
                                                }
                                                else
                                                {
                                                    Output.WriteLine("WARNING!");
                                                    Output.WriteLine("Aborting as unable to find any existing ZORGS");
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                Output.WriteLine("EXCEPTION!");
                                                Output.WriteLine(e.Message);
                                                Output.WriteDebug(e.Data.ToString());
                                            }
                                        }
                                        else
                                        {
                                            Output.WriteLine("WARNING!");
                                            Output.WriteLine("Aborting as unable to connect and authenticate to the ZVM using the supplied details");
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Output.WriteLine("EXCEPTION!");
                                        Output.WriteLine(e.Message);
                                        Output.WriteDebug(e.Data.ToString());
                                    }
                                }
                                else
                                {
                                    Output.WriteLine("WARNING!");
                                    Output.WriteLine("Aborting as no ZORG Name was specified, please ensure you set the ZORG name using the -z parameter");
                                }
                            }
                            catch (Exception e)
                            {
                                Output.WriteLine("EXCEPTION!");
                                Output.WriteLine(e.Message);
                                Output.WriteDebug(e.Data.ToString());
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Output.WriteLine("EXCEPTION!");
                        Output.WriteLine(e.Message);
                        Output.WriteDebug(e.Data.ToString());
                    }

                    watch.Stop();
                    Output.WriteLine("");
                    Output.WriteLine("Duration: " + TimeSpan.FromSeconds(watch.Elapsed.TotalSeconds).ToString(@"hh\:mm\:ss"));
                    Console.WriteLine();
                    Console.WriteLine("Press any key to close...");
                    Output.WriteLine("------------------------------------------------------------------------------");
                    Console.ReadKey();
                }
            });
        }
    }
}