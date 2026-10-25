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
    internal class BurningHands
    {
        private static readonly LogWrapper AssetLogger = LogWrapper.Get("BurningHands");
        public const string SourceFxGuid = "d77c04077f6a54f43bebab61035c1efc";
        public const string BurningHandsProjGUID = "6dfc5e4c7d9ae3048984744222dbd0fa";
        public const string BurningHandsAbilityGUID = "4783c3709a74a794dbe7c8e7e0b1b038";
        public const string WaterOriginalGuid = "921700b1280d41f44a12fada77bbfe87";
        public const string AcidOriginalGuid = "6e500f2641860074fa22be72f1b7d046";
        public const string AirOriginalGuid = "3949e6f246c520b438b07d95b00d69b6";

        // Массив элементальных способностей с соответствующими FX
        private static readonly ElementalData.ElementalHeritageData[] ElementalHeritages = new[]
        {
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.StormLordsResolve,
                FxGuid = AirOriginalGuid,
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)",
                UseOriginalPrefab= true,
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.RodOfBlizzard,
                FxGuid = WaterOriginalGuid,
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)",
                UseOriginalPrefab= true,
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.AmplRodOfBlizzard,
                FxGuid = WaterOriginalGuid,
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)",
                UseOriginalPrefab= true,
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.DevouringLust,
                FxGuid = "04ac6386dea040679a38f857dd76fc28",
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
                AbilityGuid = ElementalData.ElementalConstants.ElemHerWater,
                FxGuid = WaterOriginalGuid,
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)",
                UseOriginalPrefab= true,
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerAir,
                FxGuid = AirOriginalGuid,
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)",
                UseOriginalPrefab= true,
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerFire,
                FxGuid = "07323ce4202f42cbb9099b4866e89755",
                Color = Colors.Fire.Primary,
                ElementName = "Fire"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerDeath,
                FxGuid = "36c6f7e587d9484fa35c8b55fe97c374",
                Color = Colors.Death.Secondary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerHoly,
                FxGuid = "29d992d1350e4198864c7ce795c67d26",
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
        // Original BurningHands FX GUIDs

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

        // Harmony патч для перехвата каста способности
        [HarmonyPatch(typeof(AbilityData), nameof(AbilityData.Cast))]
        public static class AbilityData_Cast_Patch
        {
            [HarmonyPrefix]
            public static void Prefix(AbilityData __instance)
            {
                try
                {
                    // Проверяем, является ли способность BurningHands
                    if (__instance.Blueprint.AssetGuid.ToString() == BurningHandsAbilityGUID ||
                        __instance.Blueprint.name.Contains("BurningHands"))
                    {
                        AssetLogger.Info($"[SpellsRecolorMod]: BurningHands cast detected: {__instance.Blueprint.name}");

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
                        BlueprintProjectile BurningHandsProjBP = BlueprintTool.Get<BlueprintProjectile>(BurningHandsProjGUID);
                        if (activeHeritage == null || activeHeritage.AbilityGuid == ElementalData.ElementalConstants.ElemHerFire)
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: {(activeHeritage == null ? "No active Elemental Heritage" : "Fire Heritage active")}, keeping original fire Burning Hands");
                            RestoreOriginalLinks(BurningHandsProjBP, SourceFxGuid);
                        }
                        else
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: Replacing assets for {activeHeritage.ElementName} BurningHands");
                            ProjectileLinkReplacer(BurningHandsProjBP, activeHeritage.FxGuid);
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
