using System.Collections.Generic;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using HarmonyLib;

namespace ReVolt.Patches
{
    internal static class NetworkChanges
    {
        internal static void MarkTopology(CableNetwork network)
        {
            if (network?.PowerTick is RevoltTick)
                network.DirtyPowerAndDataDeviceLists();
        }

        internal static void MarkPair(CableNetwork previous, CableNetwork current)
        {
            MarkTopology(previous);
            if (!ReferenceEquals(previous, current)) MarkTopology(current);
        }

        internal static HashSet<CableNetwork> CaptureDeviceNetworks(Device device)
        {
            var result = new HashSet<CableNetwork>(ReferenceComparer<CableNetwork>.Instance);
            if (device == null)
                return result;
            // Registered connections cover even devices with separate secondary ports.
            foreach (CableNetwork n in device.ConnectedCableNetworks)
                if (n != null) result.Add(n);
            AddCableNetworks(result, device.PowerCables);
            AddCableNetworks(result, device.DataCables);
            return result;
        }

        private static void AddCableNetworks(HashSet<CableNetwork> networks, Cable[] cables)
        {
            if (cables == null) return;
            foreach (Cable cable in cables)
                if (cable != null && cable.CableNetwork != null) networks.Add(cable.CableNetwork);
        }
    }

    [HarmonyPatch(typeof(CableNetwork))]
    internal static class CableAddedPatch
    {
        [HarmonyPrefix, HarmonyPatch(nameof(CableNetwork.Add), typeof(Cable))]
        public static void Prefix(Cable cable, out CableNetwork __state) => __state = cable?.CableNetwork;
        [HarmonyPostfix, HarmonyPatch(nameof(CableNetwork.Add), typeof(Cable))]
        public static void Postfix(CableNetwork __instance, Cable cable, CableNetwork __state)
        {
            if (cable != null && !ReferenceEquals(__state, cable.CableNetwork))
                NetworkChanges.MarkPair(__state, __instance);
        }
    }

    [HarmonyPatch(typeof(CableNetwork))]
    internal static class CableRemovedPatch
    {
        [HarmonyPrefix, HarmonyPatch(nameof(CableNetwork.Remove), typeof(Cable))]
        public static void Prefix(Cable cable, out CableNetwork __state) => __state = cable?.CableNetwork;
        [HarmonyPostfix, HarmonyPatch(nameof(CableNetwork.Remove), typeof(Cable))]
        public static void Postfix(CableNetwork __instance, Cable cable, CableNetwork __state)
        {
            if (ReferenceEquals(__state, __instance) && (cable == null || !ReferenceEquals(cable.CableNetwork, __instance)))
                NetworkChanges.MarkTopology(__instance);
        }
    }

    [HarmonyPatch(typeof(CableNetwork))]
    internal static class CableNetworkMergePatch
    {
        [HarmonyPostfix, HarmonyPatch(nameof(CableNetwork.Merge), typeof(CableNetwork))]
        public static void Postfix(CableNetwork __instance, CableNetwork oldNetwork)
        {
            if (oldNetwork != null && !ReferenceEquals(__instance, oldNetwork))
                NetworkChanges.MarkPair(__instance, oldNetwork);
        }
    }

    [HarmonyPatch(typeof(CableNetwork))]
    internal static class PartialDeviceRemovalPatch
    {
        [HarmonyPostfix, HarmonyPatch(nameof(CableNetwork.RemoveDevice), typeof(Cable), typeof(Device))]
        public static void Postfix(CableNetwork __instance) => NetworkChanges.MarkTopology(__instance);
    }

    [HarmonyPatch(typeof(Device))]
    internal static class DeviceConnectionsPatch
    {
        [HarmonyPrefix, HarmonyPatch(nameof(Device.InitializeDataConnection))]
        public static void Prefix(Device __instance, out HashSet<CableNetwork> __state)
        {
            __state = NetworkChanges.CaptureDeviceNetworks(__instance);
        }

        [HarmonyPostfix, HarmonyPatch(nameof(Device.InitializeDataConnection))]
        public static void Postfix(Device __instance, HashSet<CableNetwork> __state)
        {
            var networks = NetworkChanges.CaptureDeviceNetworks(__instance);
            networks.UnionWith(__state);
            foreach (CableNetwork network in networks) NetworkChanges.MarkTopology(network);
        }
    }

    [HarmonyPatch(typeof(CableFuse))]
    internal static class FuseAddedPatch
    {
        [HarmonyPrefix, HarmonyPatch(nameof(CableFuse.AddToNetwork))]
        public static void Prefix(CableFuse __instance, out CableNetwork __state) => __state = __instance.RegisteredNetwork;
        [HarmonyPostfix, HarmonyPatch(nameof(CableFuse.AddToNetwork))]
        public static void Postfix(CableFuse __instance, CableNetwork __state) => NetworkChanges.MarkPair(__state, __instance.RegisteredNetwork);
    }

    [HarmonyPatch(typeof(CableFuse))]
    internal static class FuseRemovedPatch
    {
        [HarmonyPrefix, HarmonyPatch(nameof(CableFuse.RemoveFromNetwork))]
        public static void Prefix(CableFuse __instance, out CableNetwork __state) => __state = __instance.RegisteredNetwork;
        [HarmonyPostfix, HarmonyPatch(nameof(CableFuse.RemoveFromNetwork))]
        public static void Postfix(CableNetwork __state) => NetworkChanges.MarkTopology(__state);
    }
}
