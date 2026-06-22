using Assets.Scripts.Objects;
using HarmonyLib;
using LaunchPadBooster.Utils;
using System;
using System.Collections.Generic;
using Assets.Scripts.Objects.Motherboards;
using UnityEngine;


namespace ReVolt.Patches
{
    //[HarmonyPatch]
    public class PrefabPatcher
    {
        // Material matching functions adapted from tom_is_unlucky's FPGA mod material processor
        private static readonly Dictionary<string, ColorType> MATERIAL_MAP = new() {
            {"blu", ColorType.Blue},
            {"gra", ColorType.Gray},
            {"gre", ColorType.Green},
            {"ora", ColorType.Orange},
            {"red", ColorType.Red},
            {"yel", ColorType.Yellow},
            {"whi", ColorType.White},
            {"bla", ColorType.Black},
            {"bro", ColorType.Brown},
            {"kha", ColorType.Khaki},
            {"pin", ColorType.Pink},
            {"pur", ColorType.Purple},
        };
        

        private static Material MatchMaterial(Material checkMaterial)
        {
            return MATERIAL_MAP.TryGetValue(checkMaterial.name[5..8].ToLower(), out var match) ? PrefabUtils.GetColorMaterial(match, checkMaterial.name[^3..] == "ive") : checkMaterial;
        }

        public static List<GameObject> PrefabContent { get; set; }
        //[HarmonyPatch(typeof(Prefab), "LoadAll")]
        public static void Prefix()
        {
            if (!ReVolt.enablePrefabContent.Value) // If the user has turned off adding prefabs, don't add prefabs.
                return;

            try
            {
                foreach (var gameObject in PrefabContent)
                {
                    var thing = gameObject.GetComponent<Thing>();
                    if (thing == null)
                        continue;
                    
                    foreach (var renderer in thing.GetComponentsInChildren<MeshRenderer>())
                    {
                        var materials = renderer.sharedMaterials;
                        for (var i = 0; i < materials.Length; i++)
                            materials[i] = MatchMaterial(materials[i]);
                            
                        renderer.sharedMaterials = materials;
                    }
                    if (thing.PaintableMaterial != null)
                        thing.PaintableMaterial = MatchMaterial(thing.PaintableMaterial);

                    WorldManager.Instance.SourcePrefabs.Add(thing);
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
