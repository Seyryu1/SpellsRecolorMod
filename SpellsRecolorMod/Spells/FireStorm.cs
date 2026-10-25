using BlueprintCore.Utils;
using BlueprintCore.Utils.Assets;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using UnityEngine;

namespace SpellsRecolorMod.Spells
{
    internal class FireStorm
    {
        private static readonly LogWrapper AssetLogger = LogWrapper.Get("FireStorm");
        public const string SourceFxGuid = "2372eb663aa47f14fb4edcfb20e8a745";
        public const string FireStormAbilityGUID = "e3d0dfe1c8527934294f241e0ae96a8d";
        // Массив элементальных способностей с соответствующими FX
        private static readonly ElementalData.ElementalHeritageData[] ElementalHeritages = new[]
        {
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.StormLordsResolve,
                FxGuid = "b1e9bf2bc1ce4b7ba95047f043ca0721",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.RodOfBlizzard,
                FxGuid = "4aa1f63eba5a4f0d8930c301d843114a",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.AmplRodOfBlizzard,
                FxGuid = "9bc620fdc8fb4b588c3f28db01496a55",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.DevouringLust,
                FxGuid = "d5459a6585b041d096fbff05862353be",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerEarth,
                FxGuid = "c56858e0db3748a08403fb27f0175635",
                Color = Colors.Acid.Primary,
                ElementName = "Earth (Acid)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerWater,
                FxGuid = "66d7d044ace14d38a4ebad51dfbd26ea",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerAir,
                FxGuid = "91c683c31efb438fbd3cb50f8a0d4b25",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerFire,
                FxGuid = "0d8182c2e5cc46c6aa64286310ee33db",
                Color = Colors.Fire.Primary,
                ElementName = "Fire"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerDeath,
                FxGuid = "63c5fb8302c546ab84ae96c51d9accd5",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerHoly,
                FxGuid = "23911a9603a8468e9f4f1e185df5893e",
                Color = Colors.Holy.Primary,
                ElementName = "Holy"
            }
        };

        private static void ApplyFxColor(string newFxGuid)
        {
            var abilityBP = BlueprintTool.Get<BlueprintAbility>(FireStormAbilityGUID);

            if (abilityBP.Components[8] is not AbilitySpawnFx spawnFxComponent)
            {
                AssetLogger.Info($"[SpellsRecolorMod]: Component at index [8] is not AbilitySpawnFx");
                return;
            }

            var prefabLink = spawnFxComponent.PrefabLink;
            spawnFxComponent.PrefabLink.AssetId = newFxGuid;

        }

        private static void RestoreOriginalFx()
        {
            var abilityBP = BlueprintTool.Get<BlueprintAbility>(FireStormAbilityGUID);

            if (abilityBP.Components[8] is not AbilitySpawnFx spawnFxComponent)
            {
                return;
            }
            var prefabLink = spawnFxComponent.PrefabLink;
            spawnFxComponent.PrefabLink.AssetId = SourceFxGuid;

        }

        public static void Configure()
        {
            try
            {
                foreach (var heritage in ElementalHeritages)
                {
                    Color heritageColor = heritage.Color;

                    AssetTool.RegisterDynamicPrefabLink(heritage.FxGuid, SourceFxGuid, fx =>
                    {
                        AssetModifier.ModifyFxZero(fx);
                        CustomRecolor(fx, heritage);
                        
                        // Добавляем эффекты для Holy и Air heritage
                        if (heritage.ElementName.Contains("Holy"))
                        {
                            AddEffect(fx, "b165b629696a60b4da33c0bcb34b1d8e", heritage);
                        }
                        else if (heritage.ElementName.Contains("Air"))
                        {
                            AddEffect(fx, "1758f889a811e3b4493141c3fdd2bd8f", heritage);
                        }
                    });
                    AssetLogger.Info($"[SpellsRecolorMod]: Registered dynamic prefab link for {heritage.ElementName} (fx): {heritage.FxGuid} -> {SourceFxGuid}");
                }
            }
            catch (Exception e)
            {
                AssetLogger.Info($"[SpellsRecolorMod]: Failed to register dynamic prefab link: {e}");
            }
        }

        // Harmony патч для перехвата каста способности
        [HarmonyPatch(typeof(AbilityData), nameof(AbilityData.Cast))]
        public static class AbilityData_Cast_Patch
        {
            [HarmonyPrefix]
            public static void Prefix(AbilityData __instance)
            {
                try
                {
                    // Проверяем, является ли способность FireStorm
                    if (__instance.Blueprint.AssetGuid.ToString() == FireStormAbilityGUID ||
                        (__instance.Blueprint.name == "FireStorm"))
                    {
                        AssetLogger.Info($"[SpellsRecolorMod]: Fire Storm cast detected: {__instance.Blueprint.name}");

                        // Получаем кастера
                        var caster = __instance.Caster;
                        if (caster == null)
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: Caster is null");
                            return;
                        }

                        AssetLogger.Info($"[SpellsRecolorMod]: Caster: {caster.CharacterName}");

                        // Ищем активную элементальную способность
                        ElementalData.ElementalHeritageData activeHeritage = null;

                        if (caster.ActivatableAbilities != null)
                        {
                            foreach (var ability in caster.ActivatableAbilities)
                            {
                                if (ability.IsOn)
                                {
                                    var heritageData = ElementalHeritages.FirstOrDefault(h => h.AbilityGuid == ability.Blueprint.AssetGuid.ToString());
                                    if (heritageData != null)
                                    {
                                        activeHeritage = heritageData;
                                        AssetLogger.Info($"[SpellsRecolorMod]: Active Elemental Heritage detected: {heritageData.ElementName}");
                                        break;
                                    }
                                }
                            }
                        }

                        // Применяем соответствующий цвет
                        if (activeHeritage == null || activeHeritage.AbilityGuid == ElementalData.ElementalConstants.ElemHerFire)
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: {(activeHeritage == null ? "No active Elemental Heritage" : "Fire Heritage active")}, keeping original Fire Storm");
                            RestoreOriginalFx();
                        }
                        else
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: Applying {activeHeritage.ElementName} color to Fire Storm");
                            ApplyFxColor(activeHeritage.FxGuid);
                        }
                    }
                }
                catch (Exception e)
                {
                    AssetLogger.Info($"[SpellsRecolorMod]: Error in AbilityData_Cast_Patch: {e}");
                }
            }
        }
        private static void CustomRecolor(GameObject fx, ElementalData.ElementalHeritageData heritage)
        {
            Dictionary<string, Color> colors = new Dictionary<string, Color>();

            if (heritage.ElementName.Contains("Acid"))
            {
                colors = new Dictionary<string, Color>
                {
                    ["PointLight"] = new Color(0.1f, 1f, 0.1f, 1f),
                    ["ShockWaveBottom"] = new Color(0.1f, 1f, 0.1f, 1f),
                    ["SectorNoise"] = new Color(0.3f, 1.5f, 0.3f, 1f),
                    ["FireLast"] = new Color(1f, 5f, 1f, 1f),
                    ["FireEdge"] = new Color(0.3f, 1.5f, 0.3f, 1f),
                    ["WaveBottom"] = new Color(0.3f, 1.5f, 0.3f, 1f),
                    ["DropsWithTrail"] = new Color(1f, 5f, 1f, 1f),
                    ["Wave"] = new Color(1f, 5f, 1f, 1f),
                };
            }
            if (heritage.ElementName.Contains("Death"))
            {
                colors = new Dictionary<string, Color>
                {
                    ["PointLight"] = new Color(0f, 0.4f, 0.4f, 1f),
                    ["ShockWaveBottom"] = new Color(0f, 0.4f, 0.4f, 1f),
                    ["SectorNoise"] = new Color(0f, 0.4f, 0.4f, 1f),
                    ["FireLast"] = new Color(0f, 0.4f, 0.4f, 1f),
                    ["FireEdge"] = new Color(0f, 0.4f, 0.4f, 1f),
                    ["WaveBottom"] = new Color(0f, 0.4f, 0.4f, 1f),
                    ["DropsWithTrail"] = new Color(0f, 0.4f, 0.4f, 1f),
                    ["Wave"] = new Color(0f, 0.4f, 0.4f, 1f),
                };
            }
            if (heritage.ElementName.Contains("Holy"))
            {
                colors = new Dictionary<string, Color>
                {
                    ["PointLight"] = new Color(1f, 0.8f, 0.4f, 1f),
                    ["ShockWaveBottom"] = new Color(0.9f, 1.4f, 3f, 1f),
                    ["SectorNoise"] = new Color(1f, 0.8f, 0.4f, 1f),
                    ["FireLast"] = new Color(0.4f, 0.8f, 2.5f, 1f),
                    ["FireEdge"] = new Color(1f, 1.5f, 4f, 1f),
                    ["WaveBottom"] = new Color(0.9f, 1.4f, 3f, 1f),
                    ["DropsWithTrail"] = new Color(1f, 0.8f, 0.4f, 1f),
                    ["Wave"] = new Color(0.9f, 1.4f, 3f, 1f),
                };
            }
            if (heritage.ElementName.Contains("Water"))
            {
                colors = new Dictionary<string, Color>
                {
                    ["PointLight"] = new Color(0.8f, 0.9f, 1f, 1f),
                    ["ShockWaveBottom"] = new Color(0.4f, 1f, 10f, 1f),
                    ["SectorNoise"] = new Color(0.5f, 1f, 4f, 1f),
                    ["FireLast"] = new Color(0.5f, 1.2f, 6f, 1f),
                    ["FireEdge"] = new Color(0.5f, 1.2f, 6f, 1f),
                    ["WaveBottom"] = new Color(0.4f, 1f, 10f, 1f),
                    ["DropsWithTrail"] = new Color(0.3f, 1.2f, 100f, 1f),
                    ["Wave"] = new Color(0.8f, 0.9f, 1f, 1f),
                };
            }
            if (heritage.ElementName.Contains("Air"))
            {
                colors = new Dictionary<string, Color>
                {
                    ["PointLight"] = new Color(0.2f, 0.3f, 1f, 1f),
                    ["ShockWaveBottom"] = new Color(0.2f, 0.4f, 10f, 1f),
                    ["SectorNoise"] = new Color(0.5f, 1f, 20f, 1f),
                    ["FireLast"] = new Color(0.3f, 0.6f, 6f, 1f),
                    ["FireEdge"] = new Color(0.2f, 0.5f, 6f, 1f),
                    ["WaveBottom"] = new Color(0.2f, 0.4f, 10f, 1f),
                    ["DropsWithTrail"] = new Color(0.3f, 1.2f, 100f, 1f),
                    ["Wave"] = new Color(0.5f, 1.2f, 12f, 1f),
                };
            }
            RecolorProcess(fx, colors);
        }

        private static void RecolorProcess(GameObject fx, Dictionary<string, Color> color)
        {
            fx.transform.Find("Point light").GetComponent<AnimatedLight>().m_Color = color["PointLight"];
            fx.transform.Find("Root/Shockwave_Bottom").GetComponent<Renderer>().material.color = color["ShockWaveBottom"];
            fx.transform.Find("Root/Sector_Noise").GetComponent<Renderer>().material.color = color["SectorNoise"];
            fx.transform.Find("Root/FireLast").GetComponent<Renderer>().material.color = color["FireLast"];
            fx.transform.Find("Root/FireLast (1)").GetComponent<Renderer>().material.color = color["FireLast"];
            fx.transform.Find("Root/Sparks").GetComponent<Renderer>().material.color = color["FireLast"];
            fx.transform.Find("Root/Sparks2").GetComponent<Renderer>().material.color = color["FireLast"];
            fx.transform.Find("Root/FireEdge").GetComponent<Renderer>().material.color = color["FireEdge"];
            fx.transform.Find("Root/WaveBottom").GetComponent<Renderer>().material.color = color["WaveBottom"];
            fx.transform.Find("Root/DropsWithTrail").GetComponent<ParticleSystemRenderer>().trailMaterial.color = color["DropsWithTrail"];
            fx.transform.Find("Root/DropsWithTrail/FireTiny").GetComponent<Renderer>().material.color = color["DropsWithTrail"];
            fx.transform.Find("Root/Sparks_Collision/FireTiny").GetComponent<Renderer>().material.color = color["DropsWithTrail"];
            fx.transform.Find("Root/Sparks_Collision (1)/FireTiny").GetComponent<Renderer>().material.color = color["DropsWithTrail"];
            fx.transform.Find("Root/SmallFloorFires/FireTiny").GetComponent<Renderer>().material.color = color["DropsWithTrail"];
            fx.transform.Find("Root/SmallFloorFires (1)/GroundFireTiny (1)").GetComponent<Renderer>().material.color = color["DropsWithTrail"];
            fx.transform.Find("Root/Wave").GetComponent<Renderer>().material.color = color["Wave"];
        }
        private static void AddEffect(GameObject fx, string explosionGuid, ElementalData.ElementalHeritageData element)
        {
            try
            {
                var explosionPrefab = ResourcesLibrary.TryGetResource<GameObject>(explosionGuid);
                if (explosionPrefab == null)
                {
                    AssetLogger.Info($"[SpellsRecolorMod]: Failed to load explosion prefab: {explosionGuid}");
                    return;
                }

                var explosionInstance = UnityEngine.Object.Instantiate(explosionPrefab, fx.transform);
                explosionInstance.name = $"{element.ElementName}_Explosion";
                explosionInstance.transform.localPosition = Vector3.zero;

                if (element.ElementName == "Air (Electricity)")
                {
                    var lightnings = explosionInstance.transform.Find("Lightnings");
                    if (lightnings != null)
                    {
                        lightnings.localScale = new Vector3(0.3f, 1f, 0.3f);
                    }

                    var gameObject = explosionInstance.transform.Find("GameObject");
                    if (gameObject != null)
                    {
                        gameObject.localScale = new Vector3(0.3f, 1f, 0.3f);
                    }

                    var energyWave = explosionInstance.transform.Find("Rescale/EnergyWave");
                    if (energyWave != null)
                    {
                        energyWave.localScale = new Vector3(0.7f, 1f, 0.7f);
                    }

                    var cameraShake = explosionInstance.transform.Find("CameraShake");
                    if (cameraShake != null)
                    {
                        UnityEngine.Object.DestroyImmediate(cameraShake.gameObject);
                    }

                    var shockWaveDistortion = explosionInstance.transform.Find("ShockWave_Distortion");
                    if (shockWaveDistortion != null)
                    {
                        UnityEngine.Object.DestroyImmediate(shockWaveDistortion.gameObject);
                    }

                    AssetLogger.Info($"[SpellsRecolorMod]: Added Air explosion effect to Fire Storm");
                }

                if (element.ElementName == "Holy")
                {
                    var startWave = explosionInstance.transform.Find("Root/WeaponCenter+1 (1)/StartWave");
                    if (startWave != null)
                    {
                        startWave.localScale = new Vector3(0.4f, 1f, 0.4f);
                    }

                    var rays = explosionInstance.transform.Find("Root/WeaponCenter+1 (1)/Rays");
                    if (rays != null)
                    {
                        rays.localScale = new Vector3(0.4f, 0.3f, 0.4f);
                    }

                    var streams = explosionInstance.transform.Find("Root/WeaponCenter+1 (1)/Streams (1)");
                    if (streams != null)
                    {
                        streams.localScale = new Vector3(0.4f, 1f, 0.4f);
                    }

                    var cameraShake = explosionInstance.transform.Find("CameraShake");
                    if (cameraShake != null)
                    {
                        UnityEngine.Object.DestroyImmediate(cameraShake.gameObject);
                    }

                    AssetLogger.Info($"[SpellsRecolorMod]: Added Holy explosion effect to Fire Storm");
                }
            }
            catch (Exception e)
            {
                AssetLogger.Info($"[SpellsRecolorMod]: Error in AddEffect: {e}");
            }
        }
    }        
    }

