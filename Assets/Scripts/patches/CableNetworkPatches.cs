using Assets.Scripts;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Electrical;
using HarmonyLib;
using System;

namespace ReVolt.Assets.Scripts.patches
{
    [HarmonyPatch(typeof(CableNetwork))]
    public class CableNetworkPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(CableNetwork.DirtyPowerAndDataDeviceLists))]
        public static void DirtyListsPatch(CableNetwork __instance)
        {
            if (__instance.PowerTick is not RevoltTick revoltTick)
                return;

            revoltTick.IsDirty = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, new Type[0])]
        public static void Constructor_None(CableNetwork __instance)
        {
            Inject(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, new Type[1] { typeof(Cable) })]
        public static void Constructor_Cable(CableNetwork __instance)
        {
            Inject(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, new Type[1] { typeof(long) })]
        public static void Constructor_Long(CableNetwork __instance)
        {
            Inject(__instance);
        }

        /// <summary>
        ///     Injection function that replaces the default <seealso cref="PowerTick" /> class with a <seealso cref="RevoltTick" /> instance
        /// </summary>
        /// <param name="networkInstance">
        ///     The <seealso cref="CableNetwork"/> to inject the <seealso cref="RevoltTick"/> into
        /// </param>
        private static void Inject(CableNetwork networkInstance)
        {
            ConsoleWindow.Print("Replacing powertick instance");
            if (networkInstance.PowerTick is not RevoltTick)
            {
                // PowerTick is a readonly variable, so use reflection to ignore that
                var fieldInfo = typeof(CableNetwork).GetField(nameof(CableNetwork.PowerTick));
                fieldInfo.SetValue(networkInstance, new RevoltTick());
                ConsoleWindow.Print("Replaced powertick instance!");
            }
        }
    }
}
