using Assets.Scripts.Objects;
using HarmonyLib;
using System;
using System.Collections.ObjectModel;
using UnityEngine;

namespace ReVolt.Assets.Scripts.patches
{
    [HarmonyPatch]
    public class PrefabPatch
    {
        public static ReadOnlyCollection<GameObject> PrefabContent { get; set; }
        [HarmonyPatch(typeof(Prefab), "LoadAll")]
        public static void Prefix()
        {
            if (!ReVolt.configEnablePrefabs.Value) // If the user has turned off adding prefabs, don't add prefabs.
                return;

            try
            {
                foreach (var gameObject in PrefabContent)
                {
                    Thing thing = gameObject.GetComponent<Thing>();
                    // Additional patching goes here, like setting references to materials(colors) or tools from the game

                    if (thing != null)
                    {
                        WorldManager.Instance.SourcePrefabs.Add(thing);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
                Debug.LogException(ex);
            }
        }
    }
}
