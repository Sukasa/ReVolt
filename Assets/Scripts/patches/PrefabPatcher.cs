using Assets.Scripts.Objects;
using HarmonyLib;
using StationeersMods.Interface;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace ReVolt.patches
{
    [HarmonyPatch]
    public class PrefabPatcher
    {
        private static readonly Dictionary<string, StationeersColor> MATERIAL_MAP = new() {
            {"Blu", StationeersColor.BLUE},
            {"Gra", StationeersColor.GRAY},
            {"Gre", StationeersColor.GREEN},
            {"Ora", StationeersColor.ORANGE},
            {"Red", StationeersColor.RED},
            {"Yel", StationeersColor.YELLOW},
            {"Whi", StationeersColor.WHITE},
            {"Bla", StationeersColor.BLACK},
            {"Bro", StationeersColor.BROWN},
            {"Kha", StationeersColor.KHAKI},
            {"Pin", StationeersColor.PINK},
            {"Pur", StationeersColor.PURPLE},
        };

        private static Material MatchMaterial(Material checkMaterial)
        {
            if (MATERIAL_MAP.TryGetValue(checkMaterial.name[5..8], out StationeersColor match))
                return StationeersModsUtility.GetMaterial(match, checkMaterial.name[^3..] == "ive" ? ShaderType.EMISSIVE : ShaderType.NORMAL);

            return checkMaterial;
        }

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
                    if (thing != null)
                    {
                        foreach (var renderer in thing.GetComponentsInChildren<MeshRenderer>())
                        {
                            var materials = renderer.sharedMaterials;
                            for (var i = 0; i < materials.Length; i++)
                                materials[i] = MatchMaterial(materials[i]);
                            
                            renderer.sharedMaterials = materials;
                        }

                        if (thing is IPatchable SelfPatcher)
                            SelfPatcher.PatchPrefab();

                        if (thing.PaintableMaterial != null)
                            thing.PaintableMaterial = MatchMaterial(thing.PaintableMaterial);

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
