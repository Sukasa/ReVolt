using System.Reflection.Emit;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Cysharp.Threading.Tasks;
using HarmonyLib;

namespace ReVolt.Patches
{
    [HarmonyPatch(typeof(Cable))]
    public class CablePatches
    {
        private delegate void BaseInvokeDelegate(SmallGrid instance, Cell cell);
        private static readonly BaseInvokeDelegate BaseOnRegisteredInvoker = CreateBaseMethodInvoker();

        // Generate and cache IL to call SmallGrid.OnRegistered non-virtually, invoking the base implementation instead of derived ones.
        // Thi is used to hijack the Cable.OnRegistered function, still call its base, and then run the CableNetwork registration AFTER the colour
        // has been assigned.
        private static BaseInvokeDelegate CreateBaseMethodInvoker()
        {
            var methodInfo = typeof(SmallGrid).GetMethod(nameof(SmallGrid.OnRegistered));
            var dynamicMethod = new DynamicMethod("CallBaseRegistration", typeof(void), new[] { typeof(SmallGrid), typeof(Cell) }, typeof(Cable));

            var il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, methodInfo);
            il.Emit(OpCodes.Ret);

            return (BaseInvokeDelegate)dynamicMethod.CreateDelegate(typeof(BaseInvokeDelegate)); 
        }
        
        [HarmonyPrefix, HarmonyPatch(nameof(Cable.OnRegistered))]
        public static bool CableOnRegisteredPatch(Cell cell, Cable __instance)
        {
            if (!ReVolt.enablePrefabContent.Value)
                return true;

            if (GameManager.GameState == GameState.Loading || !GameManager.RunSimulation)
                return true;
            
            // Don't register Cable Networks right away when placing a cable; defer until we have the cable colour 
            BaseOnRegisteredInvoker.Invoke(__instance, cell);
            DeferredCableRegistration(__instance).Forget();

            return false;
        }

        private static async UniTaskVoid DeferredCableRegistration(Cable cable)
        {
            await UniTask.Yield();
            
            var cableNetwork = CableNetwork.Merge(CableNetwork.ConnectedNetworks(cable));
            if (cableNetwork != null)
                cableNetwork.Add(cable);
            else
                _ = new CableNetwork(cable);
        }
    }
}