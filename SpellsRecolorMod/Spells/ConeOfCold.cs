using BlueprintCore.Utils;
using BlueprintCore.Utils.Assets;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using System;
using System.Linq;
using UnityEngine;

namespace SpellsRecolorMod.Spells
{
    internal class ConeOfCold
    {
        private static readonly LogWrapper AssetLogger = LogWrapper.Get("ConeOfCold");
        public const string SourceFxGuid = "37b11b5db5c96e84f93597d4d20953ba";
        public const string ConeOfColdProjGUID = "79a66a3766ae87146beb6000a73e8213";
        public const string ConeOfColdAbilityGUID = "e7c530f8137630f4d9d7ee1aa7b1edc0";
        public const string FireOriginalGuid = "be6f4bc3e379a664f830600518e6aee4";
        public const string AcidOriginalGuid = "5441cf36a7a8bb344bd937d410556de9";
        public const string AirOriginalGuid = "65f9693a83371c344925cefb2030b3f2";
        public const string HolyOriginalGuid = "8c6ac6d448b185943ac053c082c4c272";


        // Массив элементальных способностей с соответствующими FX
        private static readonly ElementalData.ElementalHeritageData[] ElementalHeritages = new[]
        {
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.StormLordsResolve,
                FxGuid = "c628a6828a744e9c8b74bcce7fc685d1",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.DevouringLust,
                FxGuid = "f023a314807249879d18f2f334489482",
                Color = Colors.Death.Secondary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerEarth,
                FxGuid = AcidOriginalGuid,
                Color = Colors.Acid.Primary,
                ElementName = "Earth (Acid)",
                UseOriginalPrefab = true,
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerAir,
                FxGuid = "6e2b4a390010447a8c6ba13a72745706",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerFire,
                FxGuid = FireOriginalGuid,
                Color = Colors.Fire.Primary,
                ElementName = "Fire",
                UseOriginalPrefab= true,
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerDeath,
                FxGuid = "853cee8158f1439a9cb58c17cd9aa8f2",
                Color = Colors.Death.Secondary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerHoly,
                FxGuid = "2ac6a2e75b0c45fc83b1aaed1a6232e4",
                Color = Colors.Holy.Secondary,
                ElementName = "Holy"
            }
        };

        private static void ProjectileLinkReplacer(BlueprintProjectile proj, string newAssetGuid)
        {
            var viewFx = proj.CastFx;
            AssetLogger.Info($"[SpellsRecolorMod]: Base View is {viewFx.AssetId}:");
            viewFx.AssetId = newAssetGuid;
            AssetLogger.Info($"[SpellsRecolorMod]: New View is {viewFx.AssetId}:");
            var loadedPrefab = viewFx.Load();
            AssetLogger.Info($"[SpellsRecolorMod]: Prefab loaded: {loadedPrefab?.name ?? "null"}");
            AssetLogger.Info($"[SpellsRecolorMod]: Projectile links replaced:");
        }

        private static void RestoreOriginalLinks(BlueprintProjectile proj, string originalAssetGuid)
        {
            var viewFx = proj.CastFx;
            viewFx.AssetId = originalAssetGuid;
            AssetLogger.Info($"[SpellsRecolorMod]: Restored original projectile links");
        }

        // Меняем FX у projectile, используемого способностью (берём первый из m_Projectiles)
        // Original ConeOfCold FX GUIDs

        public static void Configure()
        {
            try
            {
                // Регистрируем динамические префабы для каждого элемента с соответствующим цветом
                foreach (var heritage in ElementalHeritages)
                {
                    if (heritage.UseOriginalPrefab)
                    {
                        AssetLogger.Info($"[SpellsRecolorMod]: Skipping dynamic prefab registration for {heritage.ElementName} - using original prefab: {heritage.FxGuid}");
                        continue;
                    }
                    // Создаём замыкание для цвета
                    Color heritageColor = heritage.Color;
                    if ( heritage.ElementName == "Holy" )
                    {
                        AssetTool.RegisterDynamicPrefabLink(heritage.FxGuid, HolyOriginalGuid, fx =>
                        {
                            AssetModifier.ModifyFx(fx, heritageColor, AssetModifier.NoOpConfig());
                            AddEffect(fx, heritage);
                        });
                        AssetLogger.Info($"[SpellsRecolorMod]: Registered dynamic prefab link for {heritage.ElementName} (fx): {heritage.FxGuid} -> {SourceFxGuid}");
                        continue;
                    }
                    if (heritage.ElementName == "Air (Electricity)")
                    {
                        AssetTool.RegisterDynamicPrefabLink(heritage.FxGuid, AirOriginalGuid, fx =>
                        {
                            AssetModifier.ModifyFx(fx, heritageColor, AssetModifier.NoOpConfig());
                            AddEffect(fx, heritage);
                        });
                        AssetLogger.Info($"[SpellsRecolorMod]: Registered dynamic prefab link for {heritage.ElementName} (fx): {heritage.FxGuid} -> {SourceFxGuid}");
                        continue;
                    }


                    // Регистрируем FX префаб
                    AssetTool.RegisterDynamicPrefabLink(heritage.FxGuid, SourceFxGuid, fx => AssetModifier.ModifyFx(fx, heritageColor));
                    AssetLogger.Info($"[SpellsRecolorMod]: Registered dynamic prefab link for {heritage.ElementName} (fx): {heritage.FxGuid} -> {SourceFxGuid}");
                }
            }
            catch (Exception e)
            {
                AssetLogger.Info($"[SpellsRecolorMod]: Failed to register dynamic prefab link: {e}");
            }
        }

        private static void AddEffect(GameObject fx, ElementalData.ElementalHeritageData element)
        {            
            if (element.ElementName == "Holy")
            {
                var Wave1PS = fx.transform.Find("Root/Wave (1)").GetComponent<ParticleSystem>().main;
                var WavePS = fx.transform.Find("Root/Wave").GetComponent<ParticleSystem>().main;
                var FireEdgePS = fx.transform.Find("Root/FireEdge").GetComponent<ParticleSystem>().main;
                var FireEdge2PS = fx.transform.Find("Root/FireEdge (2)").GetComponent<ParticleSystem>().main;
                var SectorNoisePS = fx.transform.Find("Root/Sector_Noise").GetComponent<ParticleSystem>().main;
                var SectorNoise1PS = fx.transform.Find("Root/Sector_Noise (1)").GetComponent<ParticleSystem>().main;

                Wave1PS.startColor = new Color (0.9f, 0.7f, 0.3f, 0.4f);
                WavePS.startColor = new Color(0.9f, 0.7f, 0.3f, 0.8f);
                FireEdgePS.startColor = new Color(0.9f, 0.7f, 0.3f, 0.6f);
                FireEdge2PS.startColor = new Color(0.9f, 0.7f, 0.3f, 1f);
                SectorNoisePS.startColor = new Color(0.9f, 0.7f, 0.3f, 0.7f);
                SectorNoise1PS.startColor = new Color(0.7f, 0.7f, 0.3f, 0.6f);
            }
            if (element.ElementName == "Air (Electricity)")
            {
                fx.transform.localScale = new Vector3(1.25f, 1f, 1.25f);
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
                    // Проверяем, является ли способность ConeOfCold
                    if (__instance.Blueprint.AssetGuid.ToString() == ConeOfColdAbilityGUID ||
                        __instance.Blueprint.name.Contains("ConeOfCold"))
                    {
                        AssetLogger.Info($"[SpellsRecolorMod]: Cone Of Cold cast detected: {__instance.Blueprint.name}");

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
                        BlueprintProjectile ConeOfColdProjBP = BlueprintTool.Get<BlueprintProjectile>(ConeOfColdProjGUID);
                        if (activeHeritage == null || activeHeritage.AbilityGuid == ElementalData.ElementalConstants.ElemHerWater)
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: {(activeHeritage == null ? "No active Elemental Heritage" : "Water Heritage active")}, keeping original fire Cone Of Cold");
                            RestoreOriginalLinks(ConeOfColdProjBP, SourceFxGuid);
                        }
                        else
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: Replacing assets for {activeHeritage.ElementName} ConeOfCold");
                            ProjectileLinkReplacer(ConeOfColdProjBP, activeHeritage.FxGuid);
                        }
                    }
                }
                catch (Exception e)
                {
                    AssetLogger.Info($"[SpellsRecolorMod]: Error in AbilityData_Cast_Patch: {e}");
                }
            }
        }
    }
}
