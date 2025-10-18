using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Electrical;
using HarmonyLib;
using System;
using System.Reflection;

namespace ReVolt.patches
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

        [HarmonyPostfix, HarmonyPatch(MethodType.Constructor, new Type[0])]
        public static void Constructor_None(CableNetwork __instance) => Inject(__instance);

        [HarmonyPostfix, HarmonyPatch(MethodType.Constructor, new Type[1] { typeof(Cable) })]
        public static void Constructor_Cable(CableNetwork __instance) => Inject(__instance);

        [HarmonyPostfix, HarmonyPatch(MethodType.Constructor, new Type[1] { typeof(long) })]
        public static void Constructor_Long(CableNetwork __instance) => Inject(__instance);

        /// <summary>
        ///     Injection function that replaces the default <seealso cref="PowerTick" /> class with a <seealso cref="RevoltTick" /> instance
        /// </summary>
        /// <param name="networkInstance">
        ///     The <seealso cref="CableNetwork"/> to inject the <seealso cref="RevoltTick"/> into
        /// </param>
        private static void Inject(CableNetwork networkInstance) => TickSetter.SetValue(networkInstance, new RevoltTick());
    }
}
