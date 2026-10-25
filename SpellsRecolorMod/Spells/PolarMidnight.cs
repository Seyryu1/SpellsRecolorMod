using BlueprintCore.Utils;
using BlueprintCore.Utils.Assets;
using HarmonyLib;
using JetBrains.Annotations;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.Visual.Decals;
using System.Drawing.Text;
using UnityEngine;

namespace SpellsRecolorMod.Spells
{
    internal class PolarMidnight
    {
        private static readonly LogWrapper AssetLogger = LogWrapper.Get("PolarMidnight");
        // Структура для хранения данных об элементальных способностях
        // Меняем FX, используемого способностью
        public const string SourceFxGuid = "393869d56cd3b134f8294fac4ebb8463";
        public const string PolarMidnightAreaEffectGUID = "5afa2db8ceb8bee4980b321d036b781b";
        public const string PolarMidnightAbilityGUID = "ba48abb52b142164eba309fd09898856";
        // Массив элементальных способностей с соответствующими FX
        private static readonly ElementalData.ElementalHeritageData[] ElementalHeritages = new[]
        {
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.StormLordsResolve,
                FxGuid = "8386677fe18c462e9b07541d4621d735",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.DevouringLust,
                FxGuid = "2e9806572d2049ffb01f1585ae0486f5",
                Color = Colors.Death.Secondary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerEarth,
                FxGuid = "6cfea77929e340f2bcd6fe589a22f164",
                Color = Colors.Acid.Primary,
                ElementName = "Earth (Acid)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerWater,
                FxGuid = "737a050934f34c5da05c3448b0d67a27",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerAir,
                FxGuid = "656d8c6a6875477ab735e9fe57821b66",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerFire,
                FxGuid = "11ab0f3824df40e08dfce43ddcf2cb4e",
                Color = Colors.Fire.Primary,
                ElementName = "Fire"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerDeath,
                FxGuid = "ffd53684b6a64c0a927815e02bf5bcd1",
                Color = Colors.Death.Secondary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerHoly,
                FxGuid = "c3b38763b4ad43e0a1d0950bb7b82ee4",
                Color = Colors.Holy.Primary,
                ElementName = "Holy"
            }
        };

        private static void ProjectileLinkReplacer(BlueprintAbilityAreaEffect areaEffect, string newAssetGuid)
        {
            var Fx = areaEffect.Fx;
            AssetLogger.Info($"[SpellsRecolorMod]: Base View is {Fx.AssetId}:");
            Fx.AssetId = newAssetGuid;
            AssetLogger.Info($"[SpellsRecolorMod]: New View is {Fx.AssetId}:");
            var loadedPrefab = Fx.Load();
            AssetLogger.Info($"[SpellsRecolorMod]: Prefab loaded: {loadedPrefab?.name ?? "null"}");
            AssetLogger.Info($"[SpellsRecolorMod]: Projectile links replaced:");
        }

        private static void RestoreOriginalLinks(BlueprintAbilityAreaEffect areaEffect, string originalAssetGuid)
        {
            var Fx = areaEffect.Fx;
            Fx.AssetId = originalAssetGuid;
            AssetLogger.Info($"[SpellsRecolorMod]: Restored original projectile links");
        }
        public static void Configure()
        {
            try
            {
                // Регистрируем динамические префабы для каждого элемента с соответствующим цветом
                foreach (var heritage in ElementalHeritages)
                {
                    // Создаём замыкание для цвета
                    Color heritageColor = heritage.Color;

                    // Регистрируем FX префаб
                    AssetTool.RegisterDynamicPrefabLink(heritage.FxGuid, SourceFxGuid, fx =>
                    {
                        AssetModifier.ModifyFxZero(fx);
                        CustomlRecolor(fx, heritage);
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
                    // Проверяем, является ли способность Polar Midnight
                    if (__instance.Blueprint.AssetGuid.ToString() == PolarMidnightAbilityGUID ||
                        __instance.Blueprint.name.Contains("Midnight"))
                    {
                        AssetLogger.Info($"[SpellsRecolorMod]: Polar Midnight cast detected: {__instance.Blueprint.name}");

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

                        // Получаем projectile и меняем ассеты в зависимости от активной способности
                        BlueprintAbilityAreaEffect PolarMidnightAreaEffectBP = BlueprintTool.Get<BlueprintAbilityAreaEffect>(PolarMidnightAreaEffectGUID);
                        // Если активен огненный херитедж или нет активного херитеджа - оставляем оригинальные ссылки
                        if (activeHeritage == null || activeHeritage.AbilityGuid == ElementalData.ElementalConstants.ElemHerWater)
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: {(activeHeritage == null ? "No active Elemental Heritage" : "Water Heritage active")}, keeping original fire Polar Midnight");
                            RestoreOriginalLinks(PolarMidnightAreaEffectBP, SourceFxGuid);
                        }
                        else
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: Replacing assets for {activeHeritage.ElementName} Polar Midnight");
                            ProjectileLinkReplacer(PolarMidnightAreaEffectBP, activeHeritage.FxGuid);
                        }
                    }
                }
                catch (Exception e)
                {
                    AssetLogger.Info($"[SpellsRecolorMod]: Error in AbilityData_Cast_Patch: {e}");
                }
            }
        }
        private static void CustomlRecolor(GameObject fx, ElementalData.ElementalHeritageData element)
        {
            Dictionary<string, Color> colors = new Dictionary<string, Color>();
            if (element.ElementName.Contains("Fire"))
            {
                colors = new Dictionary<string, Color>
                {
                    ["StartWave"] = new Color(1f, 0.5f, 0.2f, 1f),
                    ["Decal"] = new Color(2f, 0.8f, 0.4f, 1f),
                    ["FogLoop"] = new Color(1f, 0.4f, 0.4f, 0.8f),
                };
            }
            if (element.ElementName.Contains("Acid"))
            {
                colors = new Dictionary<string, Color>
                {
                    ["StartWave"] = new Color(0.1f, 0.9f, 0.1f, 1f),
                    ["Decal"] = new Color(0f, 1.5f, 0f, 1f),
                    ["FogLoop"] = new Color(0f, 1f, 0f, 0.4f),
                };
            }
            if (element.ElementName.Contains("Air"))
            {
                colors = new Dictionary<string, Color>
                {
                    ["StartWave"] = new Color(0.2f, 0.5f, 0.7f, 1f),
                    ["Decal"] = new Color(0.4f, 1.4f, 0.9f, 1f),
                    ["FogLoop"] = new Color(0.2f, 0.8f, 1f, 0.6f),
                };
            }
            if (element.ElementName.Contains("Death"))
            {
                colors = new Dictionary<string, Color>
                {
                    ["StartWave"] = new Color(0f, 0.8f, 0.7f, 1f),
                    ["Decal"] = new Color(0f, 0.8f, 0.4f, 1f),
                    ["FogLoop"] = new Color(0f, 0.8f, 0.6f, 0.6f),
                };
            }
            if (element.ElementName.Contains("Holy"))
            {
                colors = new Dictionary<string, Color>
                {
                    ["StartWave"] = new Color(1f, 0.8f, 0.4f, 1f),
                    ["Decal"] = new Color(2f, 1.6f, 0.8f, 1f),
                    ["FogLoop"] = new Color(1f, 0.8f, 0.6f, 0.9f),
                };
            }
            RecolorProcess(fx, colors);
            return;
        }

        private static void RecolorProcess(GameObject fx, Dictionary<string, Color> color)
        {
            var StartWaveMain = fx.transform.Find("GameObject/StartWave").GetComponent<ParticleSystem>().main;
            StartWaveMain.startColor = color["StartWave"];
            fx.transform.Find("GameObject/Decal").GetComponent<FxDecal>().SharedMaterial.color = color["Decal"];
            var FogLoopMain = fx.transform.Find("GameObject/Fog_Loop_start").GetComponent<ParticleSystem>().main;
            FogLoopMain.startColor = color["FogLoop"];
            var ColdEnergyMain = fx.transform.Find("GameObject/Cold_Area_ProjectorCollision_starter/Point_Ground_starter/ColdEnergy").GetComponent<ParticleSystem>().main;
            ColdEnergyMain.startColor = color["FogLoop"];
            var SnowFlakesBigMain = fx.transform.Find("GameObject/Cold_Area_ProjectorCollision_starter/SnowFlakesBig_1").GetComponent<ParticleSystem>().main;
            SnowFlakesBigMain.startColor = color["FogLoop"];
        }
    }
}
