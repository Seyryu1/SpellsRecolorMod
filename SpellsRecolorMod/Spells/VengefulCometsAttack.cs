using BlueprintCore.Utils;
using BlueprintCore.Utils.Assets;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities;
using UnityEngine;

namespace SpellsRecolorMod.Spells
{
    internal class VengefulCometsAttack
    {
        private static readonly LogWrapper AssetLogger = LogWrapper.Get("VengefulCometsAttack");
        public const string SourceFxGuid = "5d1e7e70b54da38489617cf9c9743bf8";
        public const string VengefulCometsAttackProjGUID = "e82d266e0c068ab418a163fc41c40731";
        public const string VengefulCometsAttackAbilityGUID = "788a5a29ade54215af346efa2287073b";

        // GUID оригинальных префабов для Air, Fire, Earth (используются напрямую)
        public const string OriginalAirFxGuid = "f1aeb981909bd214fac365a6df2290df";
        public const string OriginalFireFxGuid = "cc57baf198e4bf64ab4096eb00856629";
        public const string OriginalEarthFxGuid = "5d740f7719f37154eab95b23be41cf2b";
        public const string OriginalDeathFxGuid = "d124cb8580da6d74ab5102b41c45eea0";

        private static readonly ElementalData.ElementalHeritageData[] ElementalHeritages = new[]
        {
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.StormLordsResolve,
                FxGuid = OriginalAirFxGuid, // Используем оригинальный GUID
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)",
                UseOriginalPrefab = true // Флаг для пропуска регистрации
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.StormLordsResolve,
                FxGuid = OriginalDeathFxGuid,
                Color = Colors.Death.Primary,
                ElementName = "Death",
                UseOriginalPrefab = false
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerEarth,
                FxGuid = OriginalEarthFxGuid, // Используем оригинальный GUID
                Color = Colors.Acid.Primary,
                ElementName = "Earth (Acid)",
                UseOriginalPrefab = true // Флаг для пропуска регистрации
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerWater,
                FxGuid = "130508639f3d4835a21ef31869dcce50",
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)",
                UseOriginalPrefab = true
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerAir,
                FxGuid = OriginalAirFxGuid, // Используем оригинальный GUID
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)",
                UseOriginalPrefab = true // Флаг для пропуска регистрации
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerFire,
                FxGuid = OriginalFireFxGuid, // Используем оригинальный GUID
                Color = Colors.Fire.Primary,
                ElementName = "Fire",
                UseOriginalPrefab = true // Флаг для пропуска регистрации
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerDeath,
                FxGuid = OriginalDeathFxGuid,
                Color = Colors.Death.Primary,
                ElementName = "Death",
                UseOriginalPrefab = false
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerHoly,
                FxGuid = "02975cff4b354b02baca077646f4bbcf",
                Color = Colors.Holy.Primary,
                ElementName = "Holy",
                UseOriginalPrefab = false
            }
        };

        private static void ProjectileLinkReplacer(BlueprintProjectile proj, string newAssetGuid)
        {
            var viewFx = proj.View;
            AssetLogger.Info($"[SpellsRecolorMod]: Base View is {viewFx.AssetId}:");
            viewFx.AssetId = newAssetGuid;
            AssetLogger.Info($"[SpellsRecolorMod]: New View is {viewFx.AssetId}:");
            AssetLogger.Info($"[SpellsRecolorMod]: Projectile links replaced:");
        }

        private static void RestoreOriginalLinks(BlueprintProjectile proj, string originalAssetGuid)
        {
            var viewFx = proj.View;
            viewFx.AssetId = originalAssetGuid;
            AssetLogger.Info($"[SpellsRecolorMod]: Restored original projectile links");
        }
        
        public static void Configure()
        {
            try
            {
                // Регистрируем динамические префабы только для тех элементов, которые требуют модификации
                foreach (var heritage in ElementalHeritages)
                {
                    // Пропускаем регистрацию для Air, Fire, Earth - используем оригинальные префабы
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
                    // Проверяем, является ли способность VengefulCometsAttack
                    if (__instance.Blueprint.AssetGuid.ToString() == VengefulCometsAttackAbilityGUID ||
                        __instance.Blueprint.name.Contains("VengefulCometsAttack"))
                    {
                        AssetLogger.Info($"[SpellsRecolorMod]: VengefulCometsAttack cast detected: {__instance.Blueprint.name}");

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
                        BlueprintProjectile VengefulCometsAttackProjBP = BlueprintTool.Get<BlueprintProjectile>(VengefulCometsAttackProjGUID);
                        
                        // Если активен огненный херитедж или нет активного херитеджа - оставляем оригинальные ссылки
                        if (activeHeritage == null || activeHeritage.AbilityGuid == ElementalData.ElementalConstants.ElemHerWater)
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: {(activeHeritage == null ? "No active Elemental Heritage" : "Water Heritage active")}, keeping original fire VengefulCometsAttack");
                            RestoreOriginalLinks(VengefulCometsAttackProjBP, SourceFxGuid);
                        }
                        else
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: Replacing assets for {activeHeritage.ElementName} VengefulCometsAttack with {(activeHeritage.UseOriginalPrefab ? "original" : "modified")} prefab: {activeHeritage.FxGuid}");
                            ProjectileLinkReplacer(VengefulCometsAttackProjBP, activeHeritage.FxGuid);
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
