using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Electrical;
using HarmonyLib;
using System;
using System.Collections.Generic;
using Assets.Scripts.Objects.Pipes;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Assets.Scripts.GridSystem;
using ReVolt.Prefabs;

namespace ReVolt.Patches
{
    [HarmonyPatch(typeof(CableNetwork))]
    public class CableNetworkPatches
    {
        private static readonly FieldInfo TickSetter = typeof(CableNetwork).GetField(nameof(CableNetwork.PowerTick));

        internal static readonly ConditionalWeakTable<CableNetwork, List<Device>> CableNetworkExtendedDeviceLists = new();

        [HarmonyPostfix, HarmonyPatch(nameof(CableNetwork.DirtyPowerAndDataDeviceLists))]
        public static void DirtyPowerAndDataDeviceListsPatch(CableNetwork __instance)
        {
            if (__instance.PowerTick is RevoltTick revoltTick)
                revoltTick.IsDirty = true;
        }

        [HarmonyPostfix, HarmonyPatch(nameof(CableNetwork.DirtyDataDeviceList))]
        public static void DirtyDataDeviceListPatch(CableNetwork __instance)
        {
            if (__instance.PowerTick is RevoltTick revoltTick)
                revoltTick.IsDirty = true;
        }

        [HarmonyPostfix, HarmonyPatch(MethodType.Constructor, new Type[0])]
        public static void Constructor_None(CableNetwork __instance) => Inject(__instance);

        [HarmonyPostfix, HarmonyPatch(MethodType.Constructor, typeof(Cable))]
        public static void Constructor_Cable(CableNetwork __instance) => Inject(__instance);

        [HarmonyPostfix, HarmonyPatch(MethodType.Constructor, typeof(long))]
        public static void Constructor_Long(CableNetwork __instance) => Inject(__instance);

        private static int LocIdx(object s)
        {
            return s switch
            {
                LocalBuilder lb => lb.LocalIndex,
                int i => i,
                sbyte sb => sb,
                _ => -1
            };
        }

        internal static List<Device> GetExtendedDeviceList(CableNetwork Network)
        {
            if (CableNetworkExtendedDeviceLists.TryGetValue(Network, out var deviceList))
                return deviceList;

            deviceList = new(Network.DataDeviceList); // Create list copy
            HashSet<Device> seen = new(deviceList);

            for (var i = deviceList.Count - 1; i >= 0; i--)
            {
                if (deviceList[i] is not StructureDataBridge Bridge)
                    continue;

                var additional = Bridge.GetBridgedDevices(Network);
                var c = deviceList.Count;
                for (var j = additional.Count - 1; j >= 0; j--)
                {
                    if (seen.Contains(additional[j]))
                        continue;
                    deviceList.Add(additional[j]);
                    seen.Add(additional[j]);
                }

                c = deviceList.Count - c;
            }

            CableNetworkExtendedDeviceLists.AddOrUpdate(Network, deviceList);

            return deviceList;
        }

        [HarmonyPrefix, HarmonyPatch(nameof(CableNetwork.GetStackSize)), OptionPatch("enablePrefabContent")]
        public static bool GetStackSizePatch(CableNetwork __instance, ref int __result)
        {
            __result = GetExtendedDeviceList(__instance).Count;
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(nameof(CableNetwork.ReadMemory)), OptionPatch("enablePrefabContent")]
        public static bool ReadMemoryOverride(CableNetwork __instance, int address, ref double __result)
        {
            __result = GetExtendedDeviceList(__instance)[address].ReferenceId;

            return false;
        }


        public static void RebuildNetwork_CableTray(Cable cable, Queue<Cable> cableQueue)
        {
            Span<SmallCellRef> buf = stackalloc SmallCellRef[32];
            var count = 0;
            cable.FillConnected<CableTray>(buf, ref count);
            var span = buf[..count];
            for (var index = 0; index < span.Length; ++index)
            {
                var conn = span[index].Get<CableTray>().MatchCables(cable);
                foreach (var c in conn)
                    cableQueue.Enqueue(c);
            }
        }

        [HarmonyTranspiler, HarmonyPatch("RebuildNetwork"), OptionPatch("enablePrefabContent")]
        public static IEnumerable<CodeInstruction> RebuildNetworkInjector(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
        {
            var inject2PatternStep = 0;
            var InjectTrayConnections = SymbolExtensions.GetMethodInfo(() => RebuildNetwork_CableTray(null, null));

            foreach (var instruction in instructions)
            {
                inject2PatternStep = inject2PatternStep switch
                {
                    0 when instruction.opcode == OpCodes.Ldarg_1 => 1,
                    1 when instruction.opcode == OpCodes.Ldloc_S && LocIdx(instruction.operand) == 10 => 2,
                    2 when instruction.opcode == OpCodes.Callvirt => 3,
                    3 => 4,
                    4 => 4,
                    _ => 0
                };

                yield return instruction;

                if (instruction.opcode == OpCodes.Stloc_2)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloc_2);
                    yield return new CodeInstruction(OpCodes.Call, InjectTrayConnections);
                }

                if (inject2PatternStep != 3) continue;
                yield return new CodeInstruction(OpCodes.Ldloc, 10);
                yield return new CodeInstruction(OpCodes.Ldloc_2);
                yield return new CodeInstruction(OpCodes.Call, InjectTrayConnections);
            }
        }

        [HarmonyTranspiler, HarmonyPatch(nameof(CableNetwork.ConnectedNetworks)), OptionPatch("enablePrefabContent")]
        public static IEnumerable<CodeInstruction> ConnectedNetworksInjector(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
        {
            var InjectTrayConnections = SymbolExtensions.GetMethodInfo(() => ConnectedNetworksExtra(null, null));

            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ret)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloc_2);
                    yield return new CodeInstruction(OpCodes.Call, InjectTrayConnections);
                }

                yield return instruction;
            }
        }

        public static void ConnectedNetworksExtra(Cable cable, List<CableNetwork> networks)
        {
            Span<SmallCellRef> buf = stackalloc SmallCellRef[32];
            var count = 0;
            cable.FillConnected<CableTray>(buf, ref count);

            if (CablePatches.GateTriggerRepeatRegistration && count > 0)
            {
                CablePatches.RetriggerRegistration = true;
                return;
            }

            var span = buf[..count];
            for (var index = 0; index < span.Length; ++index)
            {
                var tray = span[index].Get<CableTray>();
                tray.MatchCableNetworks(networks, cable);
            }
        }

        /// <summary>
        ///     Injection function that replaces the default <seealso cref="PowerTick" /> class with a <seealso cref="RevoltTick" /> instance
        /// </summary>
        /// <param name="networkInstance">
        ///     The <seealso cref="CableNetwork"/> to inject the <seealso cref="RevoltTick"/> into
        /// </param>
        private static void Inject(CableNetwork networkInstance) => TickSetter.SetValue(networkInstance, (RevoltTick)Activator.CreateInstance(ReVolt.PowerTickType));
    }
}