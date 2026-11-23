using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Assets.Scripts;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Pipes;
using Util.Commands;
using HarmonyLib;
using UnityEngine;
using Newtonsoft.Json;

namespace ReVolt.Commands
{
    public class RVDebugCommand : CommandBase
    {
        [Serializable]
        public class NetworkMismatchInfo
        {
            public string Name;
            public double PotentialPower;
            public double RequiredPower;
            public double ActualPower;

            [SerializeField] public List<string> DeviceList;

            [SerializeField] public List<string> PowerDeviceList;

            [SerializeField] public List<string> DataDeviceList;

            [SerializeField] public List<string> TickPowerDeviceList;
        }

        [Serializable]
        public class PatchInfo
        {
            [SerializeField] public string TargetMethod;

            [SerializeField] public List<string> Patches = new();
        }

        [Serializable]
        public class RVDebugOutput
        {
            public string Time;

            [SerializeField] public List<PatchInfo> Patches = new();

            [SerializeField] public List<NetworkMismatchInfo> PowerNetworks = new();

            public List<string> LoadedMods = new();
        }

        public override string Execute(string[] args)
        {
            var Out = new RVDebugOutput
            {
                Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            ConsoleWindow.Print("Building mod data getters");
            var SLP = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name == "StationeersLaunchPad");

            if (SLP == null)
                ConsoleWindow.PrintError("Didn't get SLP reference!");
            else
            {
                var LoadedModInfo = SLP.GetType("StationeersLaunchPad.Loading.LoadedMod")!.GetField("Info", BindingFlags.Instance | BindingFlags.Public);
                var NameGetter = SLP.GetType("StationeersLaunchPad.Metadata.ModInfo")!.GetProperty("Name", BindingFlags.Instance | BindingFlags.Public);
                var WSGetter = SLP.GetType("StationeersLaunchPad.Metadata.ModInfo")!.GetProperty("WorkshopHandle", BindingFlags.Instance | BindingFlags.Public);

                if (LoadedModInfo == null)
                    ConsoleWindow.PrintError("Didn't get LoadedModInfo Getter!");
                else if (NameGetter == null)
                    ConsoleWindow.PrintError("Didn't get ModName Getter!");
                else if (WSGetter == null)
                    ConsoleWindow.PrintError("Didn't get WSHandle Getter!");
                else
                {
                    ConsoleWindow.Print("Beginning mod scan");
                    var LoadedMods = SLP.GetType("StationeersLaunchPad.Loading.ModLoader")!.GetField("LoadedMods", BindingFlags.Static | BindingFlags.Public)!.GetValue(null);
                    if (LoadedMods is IEnumerable modList)
                    {
                        foreach (var mod in modList)
                        {
                            var ModInfo = LoadedModInfo.GetValue(mod);
                            Out.LoadedMods.Add($"{NameGetter.GetValue(ModInfo)} [{WSGetter.GetValue(ModInfo)}]");
                        }
                    }
                    else
                    {
                        ConsoleWindow.PrintError("Didn't get an enumerable list!");
                    }
                }
            }

            var fixedNetworks = 0;
            var PatchList = Harmony.GetAllPatchedMethods().ToArray();

            ConsoleWindow.PrintAction($"Found {PatchList.Length} patches");

            foreach (var method in PatchList.OrderBy(x => $"{x.DeclaringType!.FullName}.{x.Name}"))
            {
                var PI = new PatchInfo
                {
                    TargetMethod = $"{method.DeclaringType!.FullName}.{method.Name}"
                };
                Out.Patches.Add(PI);

                var patchInfo = Harmony.GetPatchInfo(method);

                if (patchInfo == null)
                    continue;

                var patchType = "Prefix";
                PI.Patches.AddRange(patchInfo.Prefixes.Select(PatchDescriptor));

                patchType = "Postfix";
                PI.Patches.AddRange(patchInfo.Postfixes.Select(PatchDescriptor));

                patchType = "Transpiler";
                PI.Patches.AddRange(patchInfo.Transpilers.Select(PatchDescriptor));

                patchType = "Finalizer";
                PI.Patches.AddRange(patchInfo.Finalizers.Select(PatchDescriptor));

                patchType = "ILManipulator";
                PI.Patches.AddRange(patchInfo.ILManipulators.Select(PatchDescriptor));


                continue;

                string PatchDescriptor(Patch patch) => $"{(patch.PatchMethod.ReturnParameter?.ParameterType == typeof(bool) ? "Interrupter" : patchType)}: {patch.PatchMethod.DeclaringType!.FullName}.{patch.PatchMethod.Name} ({patch.owner})";
            }

            CableNetwork.AllCableNetworks.ForEach(DescribeNetwork);
            var OutputFilename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RevoltDebug.json");
            File.WriteAllText(OutputFilename, JsonConvert.SerializeObject(Out, Formatting.Indented));

            ConsoleWindow.PrintAction($"Found {CableNetwork.AllCableNetworks.ActiveCount} cable networks");

            if (fixedNetworks > 0)
                ConsoleWindow.PrintAction($"Found {fixedNetworks} desynchronized PowerTicks and flagged dirty");

            ConsoleWindow.PrintAction($"Wrote debug output to {OutputFilename}");

            return null;

            void DescribeNetwork(CableNetwork network)
            {
                var NMI = new NetworkMismatchInfo
                {
                    Name = $"Cable Network {network.ReferenceId}",
                    PotentialPower = network.PotentialLoad,
                    RequiredPower = network.RequiredLoad,
                    ActualPower = network.CurrentLoad,
                    DeviceList = network.DeviceList.Select(DeviceDescriptor).ToList(),
                    PowerDeviceList = network.PowerDeviceList.Select(DeviceDescriptor).ToList(),
                    DataDeviceList = network.DataDeviceList.Select(DeviceDescriptor).ToList(),
                    TickPowerDeviceList = network.PowerTick.Devices.Select(DeviceDescriptor).ToList()
                };

                if (network.PowerTick is RevoltTick Tick && NMI.DeviceList.Count != Tick.DeviceCountAtLastRebuild)
                {
                    Tick.IsDirty = true;
                    fixedNetworks++;
                }

                Out.PowerNetworks.Add(NMI);
                return;

                string DeviceDescriptor(Device y) => $"{y.DisplayName} [{y.GetType().Name}#{y.ReferenceId}]";
            }
        }

        public override string HelpText => "Provides a Re-Volt diagnostics report for helping track down bugs";
        public override string[] Arguments => null;
        public override bool IsLaunchCmd => false;
        public override CommandScope Scope => CommandScope.HostOrSinglePlayer;
    }
}