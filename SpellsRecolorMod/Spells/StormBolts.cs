using BlueprintCore.Utils;
using BlueprintCore.Utils.Assets;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using UnityEngine;

namespace SpellsRecolorMod.Spells
{
    internal class StormBolts
    {
        private static readonly LogWrapper AssetLogger = LogWrapper.Get("StormBolts");
        public const string SourceFxGuid = "1758f889a811e3b4493141c3fdd2bd8f";
        public const string StormBoltsAbilityGUID = "7cfbefe0931257344b2cb7ddc4cdff6f";
        // Массив элементальных способностей с соответствующими FX
        private static readonly ElementalData.ElementalHeritageData[] ElementalHeritages = new[]
        {
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.AmplRodOfBlizzard,
                FxGuid = "64345a0dcd454bbcb13b238fb93c76da",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.RodOfBlizzard,
                FxGuid = "7a1fee3eaaf94682909df5fd0638dbde",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.DevouringLust,
                FxGuid = "c6a1a62ea99a46af82995b17628f47d6",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerEarth,
                FxGuid = "5466d0706ab74f998ed421c148457d8c",
                Color = Colors.Acid.Primary,
                ElementName = "Earth (Acid)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerWater,
                FxGuid = "92543c3b90054b6bb2aed3643066f0f0",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerAir,
                FxGuid = "be34e91bf178490c8ea5fab969942e1b",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerFire,
                FxGuid = "e6131188128f45be95e7ee52dbf41344",
                Color = Colors.Fire.Primary,
                ElementName = "Fire"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerDeath,
                FxGuid = "ad2eea9eaeae4aa99de7f336cf258c1a",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerHoly,
                FxGuid = "6d21315cea624c9abc56eb3b6a26b4e6",
                Color = Colors.Holy.Primary,
                ElementName = "Holy"
            }
        };

        private static void ApplyFxColor(string newFxGuid)
        {
            var abilityBP = BlueprintTool.Get<BlueprintAbility>(StormBoltsAbilityGUID);

            if (abilityBP.Components[3] is not AbilitySpawnFx spawnFxComponent)
            {
                AssetLogger.Info($"[SpellsRecolorMod]: Component at index [3] is not AbilitySpawnFx");
                return;
            }

            var prefabLink = spawnFxComponent.PrefabLink;
            spawnFxComponent.PrefabLink.AssetId = newFxGuid;

        }

        private static void RestoreOriginalFx()
        {
            var abilityBP = BlueprintTool.Get<BlueprintAbility>(StormBoltsAbilityGUID);

            if (abilityBP.Components[3] is not AbilitySpawnFx spawnFxComponent)
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
                    // Проверяем, является ли способность StormBolts
                    if (__instance.Blueprint.AssetGuid.ToString() == StormBoltsAbilityGUID ||
                        (__instance.Blueprint.name == "StormBolts"))
                    {
                        AssetLogger.Info($"[SpellsRecolorMod]: Storm Bolts cast detected: {__instance.Blueprint.name}");

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
                        if (activeHeritage == null || activeHeritage.AbilityGuid == ElementalData.ElementalConstants.ElemHerAir)
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: {(activeHeritage == null ? "No active Elemental Heritage" : "Air Heritage active")}, keeping original Call Lightning");
                            RestoreOriginalFx();
                        }
                        else
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: Applying {activeHeritage.ElementName} color to Storm Bolts");
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
            if (heritage.ElementName.Contains("Fire"))
            {
                colors = new Dictionary<string, Color>
                {
                    ["lightnings"] = new Color(1f, 0.08f, 0f, 1f),
                    ["PointLight"] = new Color(1f, 0.6f, 1f, 1f),
                    ["Sparks"] = new Color(2f, 0.2f, 0f, 1f),
                    ["SparksFar"] = new Color(1f, 1.5f, 2.5f, 1f),
                    ["SparksUpFast"] = new Color(4f, 0.2f, 0f, 1f),
                    ["Rescalelightnings"] = new Color(1f, 0.3f, 0f, 1f),
                    ["EnergyWave"] = new Color(4f, 0.6f, 0f, 1f),
                    ["CenterFlash"] = new Color(1f, 0.25f, 0f, 1f),
                };                
            }
            if (heritage.ElementName.Contains("Acid"))
            {
                colors = new Dictionary<string, Color>
                {
                    ["lightnings"] = new Color(0f, 0.5f, 0f, 1f),
                    ["PointLight"] = new Color(0f, 1f, 0f, 1f),
                    ["Sparks"] = new Color(0f, 0.6f, 0f, 1f),
                    ["SparksFar"] = new Color(0f, 0.6f, 0f, 1f),
                    ["SparksUpFast"] = new Color(0f, 0.6f, 0f, 1f),
                    ["Rescalelightnings"] = new Color(0f, 0.6f, 0f, 1f),
                    ["EnergyWave"] = new Color(0f, 2f, 0f, 1f),
                    ["CenterFlash"] = new Color(0f, 1f, 0f, 1f),
                };
            }
            if (heritage.ElementName.Contains("Death"))
            {
                colors = new Dictionary<string, Color>
                {
                    ["lightnings"] = new Color(0.2f, 0.6f, 0.5f, 1f),
                    ["PointLight"] = new Color(0f, 0.5f, 0.5f, 1f),
                    ["Sparks"] = new Color(0f, 1f, 0.25f, 1f),
                    ["SparksFar"] = new Color(0f, 1f, 0.25f, 1f),
                    ["SparksUpFast"] = new Color(0f, 1f, 0.25f, 1f),
                    ["Rescalelightnings"] = new Color(0f, 0.2f, 1f, 1f),
                    ["EnergyWave"] = new Color(0f, 0.5f, 2f, 1f),
                    ["CenterFlash"] = new Color(0.6f, 0.9f, 0.5f, 1f),
                };
            }
            if (heritage.ElementName.Contains("Holy"))
            {
                colors = new Dictionary<string, Color>
                {
                    ["lightnings"] = new Color(0.9f, 1f, 0.4f, 1f),
                    ["PointLight"] = new Color(0.9f, 1f, 0.4f, 1f),
                    ["Sparks"] = new Color(1.5f, 0.4f, 0.2f, 1f),
                    ["SparksFar"] = new Color(2f, 0.8f, 0.1f, 1f),
                    ["SparksUpFast"] = new Color(1f, 0.4f, 0.2f, 1f),
                    ["Rescalelightnings"] = new Color(0.9f, 1f, 0.4f, 1f),
                    ["EnergyWave"] = new Color(0.9f, 1f, 0.4f, 1f),
                    ["CenterFlash"] = new Color(1f, 1f, 0.3f, 1f),
                };
            }
            if (heritage.ElementName.Contains("Water"))
            {
                colors = new Dictionary<string, Color>
                {
                    ["lightnings"] = new Color(0.3f, 0.9f, 0.6f, 1f),
                    ["PointLight"] = new Color(0.8f, 0.8f, 1f, 1f),
                    ["Sparks"] = new Color(1.4f, 0.5f, 0.4f, 1f),
                    ["SparksFar"] = new Color(1.2f, 1.2f, 1f, 1f),
                    ["SparksUpFast"] = new Color(0.7f, 0.7f, 1f, 1f),
                    ["Rescalelightnings"] = new Color(0.5f, 0.6f, 1f, 1f),
                    ["EnergyWave"] = new Color(3f, 3f, 1.2f, 1f),
                    ["CenterFlash"] = new Color(1f, 1f, 1f, 1f),
                };
            }
            RecolorProcess(fx, colors);
        }

        private static void RecolorProcess(GameObject fx, Dictionary<string, Color> color)
        {
            var lightningMain = fx.transform.Find("Lightnings").GetComponent<ParticleSystem>().main;
            lightningMain.startColor = color["lightnings"];
            fx.transform.Find("Point light (7)").GetComponent<AnimatedLight>().m_Color = color["PointLight"];
            fx.transform.Find("Rescale/DropsWithTrail").GetComponent<Renderer>().material.color = color["Sparks"];
            fx.transform.Find("Rescale/DropsWithTrail/FireTiny").GetComponent<Renderer>().material.color = color["Sparks"];
            fx.transform.Find("Rescale/Sparks_Far").GetComponent<Renderer>().material.color = color["SparksFar"];
            fx.transform.Find("Rescale/SparksUpFast").GetComponent<Renderer>().material.color = color["SparksUpFast"];
            var rescalelightnColor = fx.transform.Find("Rescale/Lightnings").GetComponent<ParticleSystem>().main;
            rescalelightnColor.startColor = color["Rescalelightnings"];
            fx.transform.Find("Rescale/EnergyWave").GetComponent<Renderer>().material.color = color["SparksUpFast"];
            fx.transform.Find("Rescale/CenterFlash (1)").GetComponent<Renderer>().material.color = color["CenterFlash"];
            var electricityAoeColor = fx.transform.Find("GameObject/ProjectorDecal_Loop/GroundLightning/Electricity_Aoe").GetComponent<ParticleSystem>().main;
            electricityAoeColor.startColor = color["CenterFlash"];
            fx.transform.Find("GameObject/ProjectorDecal_Loop/GroundLightning").GetComponent<Renderer>().material.color = color["EnergyWave"];
            fx.transform.Find("GameObject/ProjectorDecal_Loop/GroundLightning/SmokeBottom").GetComponent<Renderer>().material.color = color["Sparks"];
            fx.transform.Find("GameObject/ProjectorDecal_Loop/GroundLightning/SparksUpFast").GetComponent<Renderer>().material.color = color["Sparks"];
            fx.transform.Find("GameObject/ProjectorDecal_Loop/GroundLightning/BackElec (1)").GetComponent<Renderer>().material.color = color["Sparks"];
            fx.transform.Find("Sparks_CollisionBlue/FireTiny").GetComponent<Renderer>().material.color = color["Sparks"];
            fx.transform.Find("Sparks_CollisionBlue2/FireTiny").GetComponent<Renderer>().material.color = color["Sparks"];
        }
    }
}

