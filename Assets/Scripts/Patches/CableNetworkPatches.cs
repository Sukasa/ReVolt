using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Electrical;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Transactions;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects.Pipes;
using Console = System.Console;

namespace ReVolt.Patches
{
    [HarmonyPatch(typeof(CableNetwork))]
    public class CableNetworkPatches
    {
        private static readonly FieldInfo TickSetter = typeof(CableNetwork).GetField(nameof(CableNetwork.PowerTick));

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


        [HarmonyPrefix, HarmonyPatch("RebuildNetwork")]
        public static unsafe bool RebuildNetworkPatch(Cable cable, CableNetwork newNetwork, CableNetwork oldNetwork)
        {
            Span<SmallCellRef> buf1 = stackalloc SmallCellRef[32];
            var count1 = 0;
            cable.FillConnected<Cable>(buf1, ref count1);
            var cableQueue = new Queue<Cable>(count1);
            var cableSet = new HashSet<Cable>(oldNetwork != null ? oldNetwork.CableList.Count : 16 /*0x10*/)
            {
                cable
            };
            var span = buf1[..count1];
            for (var index = 0; index < span.Length; ++index)
            {
                var cable1 = span[index].Get<Cable>();
                if (cable1 != null)
                    cableQueue.Enqueue(cable1);
            }
            count1 = 0;
            cable.FillConnected<CableTray>(buf1, ref count1);
            span = buf1[..count1];
            for (var index = 0; index < span.Length; ++index)
            {
                var conn = span[index].Get<CableTray>().MatchCables(cable);
                foreach (var c in conn)
                    cableQueue.Enqueue(c);
            }

            Span<SmallCellRef> buf2 = stackalloc SmallCellRef[32];

            while (cableQueue.Count > 0)
            {
                var cable2 = cableQueue.Dequeue();
                if (cable2 != null && !cableSet.Contains(cable2) && !cable2.IsBeingDestroyed)
                {
                    cableSet.Add(cable2);
                    var count2 = 0;
                    cable2.FillConnected<Cable>(buf1, ref count2);
                    span = buf1[..count2];
                    for (var index = 0; index < span.Length; ++index)
                    {
                        var cable3 = span[index].Get<Cable>();
                        if (cable3 != null && !cableSet.Contains(cable3))
                            cableQueue.Enqueue(cable3);
                    }

                    if (oldNetwork != null)
                    {
                        int count3 = 0;
                        cable2.FillConnected<Device>(buf2, ref count3);
                        span = buf2[..count3];
                        for (var index = 0; index < span.Length; ++index)
                        {
                            SmallCellRef smallCellRef = span[index];
                            oldNetwork.RemoveDevice(cable2, smallCellRef.Get<Device>());
                        }
                    }

                    newNetwork.Add(cable2);
                    
                    int count = 0;
                    cable2.FillConnected<CableTray>(buf2, ref count);
                    span = buf2[..count];
                    for (var index = 0; index < span.Length; ++index)
                    {
                        var conn = span[index].Get<CableTray>().MatchCables(cable2);
                        foreach (var c in conn)
                            cableQueue.Enqueue(c);
                    }
                }
            }

            return false;
        }


        [HarmonyPrefix, HarmonyPatch(nameof(CableNetwork.ConnectedNetworks))]
        public static unsafe bool ConnectedNetworks(ref List<CableNetwork> __result, Cable cable)
        {
            Span<SmallCellRef> buf = stackalloc SmallCellRef[32];
            var count = 0;
            cable.FillConnected<Cable>(buf, ref count);
            __result = new List<CableNetwork>(count);
            var span = buf[..count];
            for (var index = 0; index < span.Length; ++index)
            {
                var cable1 = span[index].Get<Cable>();
                if (!__result.Contains(cable1.CableNetwork))
                    __result.Add(cable1.CableNetwork);
            }

            count = 0;
            cable.FillConnected<CableTray>(buf, ref count);

            if (CablePatches.GateTriggerRepeatRegistration && count > 0)
            {
                CablePatches.RetriggerRegistration = true;
                return false;
            }

            span = buf[..count];
            for (var index = 0; index < span.Length; ++index)
            {
                var tray = span[index].Get<CableTray>();
                tray.MatchCableNetworks(__result, cable);
            }

            return false;
        }

        /// <summary>
        ///     Injection function that replaces the default <seealso cref="PowerTick" /> class with a <seealso cref="RevoltTick" /> instance
        /// </summary>
        /// <param name="networkInstance">
        ///     The <seealso cref="CableNetwork"/> to inject the <seealso cref="RevoltTick"/> into
        /// </param>
        private static void Inject(CableNetwork networkInstance) => TickSetter.SetValue(networkInstance, new RevoltTick());
    }
}