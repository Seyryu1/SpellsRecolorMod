using BlueprintCore.Utils;
using BlueprintCore.Utils.Assets;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities;
using UnityEngine;

namespace SpellsRecolorMod.Spells
{
    internal static class BurningArc
    {

        private static readonly LogWrapper AssetLogger = LogWrapper.Get("BurningArc");
        public const string SourceMissFxGuid = "ee17299746e406d4a9559e2274d18a1b";
        public const string SourceFxGuid = "81e003d5ea5b84b47b67349510f681b3";
        public const string BurningArcProj0GUID = "0549a3702fff3994b8d14295ee3f4e3b";
        public const string BurningArcProj1GUID = "9ee4b4229966cc14ba58c189c387e2c6";
        public const string BurningArcAbilityGUID = "eaac3d36e0336cb479209a6f65e25e7c";

        private static readonly ElementalData.ElementalHeritageData[] ElementalHeritages = new[]
        {
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.StormLordsResolve,
                FxGuid = "a669318f6d1c48c58dd260cc6d421a97",
                MissFxGuid = "789a99fd947147118138ae977df80154",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.RodOfBlizzard,
                FxGuid = "98c407de972e434da8c53ffb599eaff3",
                MissFxGuid = "c262bd207d414d1daec9e45d6dcc1186",
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.AmplRodOfBlizzard,
                FxGuid = "05c7c858b1ca435f8012b1514fffdf96",
                MissFxGuid = "5fe6b53805d6459284901991605d0770",
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.DevouringLust,
                FxGuid = "491bae387e284170b6dcdcef0bf24de0",
                MissFxGuid = "c443a1d44d4f4839be39e8a8660b5e77",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerEarth,
                FxGuid = "62e277e2e443496986c858be44d6fe4e",
                MissFxGuid = "7c8e259c8995469fbb05ca9d2710ff13",
                Color = Colors.Acid.Primary,
                ElementName = "Earth (Acid)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerWater,
                FxGuid = "b806fbc89fe24a609fb9a1a28d588f0b",
                MissFxGuid = "904918963ccc4a059d934226d795bb1a",
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerAir,
                FxGuid = "65a2380004a24b9eac2a5d03c9c37eda",
                MissFxGuid = "ddf0fec15d35459ca6aa34e27cd86982",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerFire,
                FxGuid = "bee7f9f29f964d75b80107ed83318fe6",
                MissFxGuid = "a99723908c7048a4be308a5421989f26",
                Color = Colors.Fire.Primary,
                ElementName = "Fire"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerDeath,
                FxGuid = "84986c1fcedc476497d8504ac4d8b35d",
                MissFxGuid = "02e2afba845648a0a6fb9bf5bccf1ef4",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerHoly,
                FxGuid = "434ad9959007406b936598696b43cb46",
                MissFxGuid = "0baad73f96ef430184bbc36eeb4ef7aa",
                Color = Colors.Holy.Primary,
                ElementName = "Holy"
            }
        };

        private static void ProjectileLinkReplacer(BlueprintProjectile proj, string newAssetGuid, string NewMissFxGuid)
        {
            var viewFx = proj.View;
            var MissFx = proj.ProjectileHit;
            PrefabLink newLinkMiss = new()
            {
                AssetId = NewMissFxGuid
            };
            AssetLogger.Info($"[SpellsRecolorMod]: Base View is {viewFx.AssetId}:");
            viewFx.AssetId = newAssetGuid;
            AssetLogger.Info($"[SpellsRecolorMod]: New View is {viewFx.AssetId}:");
            var loadedPrefab = viewFx.Load();
            AssetLogger.Info($"[SpellsRecolorMod]: Prefab loaded: {loadedPrefab?.name ?? "null"}");
            MissFx.MissFx = newLinkMiss;
            AssetLogger.Info($"[SpellsRecolorMod]: Prefab hit now is {MissFx.MissFx.AssetId}");
            AssetLogger.Info($"[SpellsRecolorMod]: Projectile links replaced:");
        }

        private static void RestoreOriginalLinks(BlueprintProjectile proj, string originalAssetGuid, string originalHitFxGuid)
        {
            var viewFx = proj.View;
            var hitFx = proj.ProjectileHit;
            PrefabLink originalLinkHit = new()
            {
                AssetId = originalHitFxGuid
            };
            viewFx.AssetId = originalAssetGuid;
            hitFx.MissFx = originalLinkHit;
            hitFx.HitFx = originalLinkHit;
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
                    AssetTool.RegisterDynamicPrefabLink(heritage.FxGuid, SourceFxGuid, fx => AssetModifier.ModifyFx(fx, heritageColor));
                    AssetLogger.Info($"[SpellsRecolorMod]: Registered dynamic prefab link for {heritage.ElementName} (fx): {heritage.FxGuid} -> {SourceFxGuid}");

                    // Регистрируем HitFX префаб
                    AssetTool.RegisterDynamicPrefabLink(heritage.MissFxGuid, SourceMissFxGuid, fx => AssetModifier.ModifyFx(fx, heritageColor));
                    AssetLogger.Info($"[SpellsRecolorMod]: Registered dynamic prefab link for {heritage.ElementName} (miss): {heritage.MissFxGuid} -> {SourceMissFxGuid}");
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
                    // Проверяем, является ли способность BurningArc
                    if (__instance.Blueprint.AssetGuid.ToString() == BurningArcAbilityGUID ||
                        __instance.Blueprint.name.Contains("BurningArc"))
                    {
                        AssetLogger.Info($"[SpellsRecolorMod]: BurningArc cast detected: {__instance.Blueprint.name}");

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
                        BlueprintProjectile BurningArcProj0BP = BlueprintTool.Get<BlueprintProjectile>(BurningArcProj0GUID);
                        BlueprintProjectile BurningArcProj1BP = BlueprintTool.Get<BlueprintProjectile>(BurningArcProj1GUID);
                        // Если активен огненный херитедж или нет активного херитеджа - оставляем оригинальные ссылки
                        if (activeHeritage == null || activeHeritage.AbilityGuid == ElementalData.ElementalConstants.ElemHerFire)
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: {(activeHeritage == null ? "No active Elemental Heritage" : "Fire Heritage active")}, keeping original fire Burning Arc");
                            RestoreOriginalLinks(BurningArcProj0BP, SourceFxGuid, SourceMissFxGuid);
                            RestoreOriginalLinks(BurningArcProj1BP, SourceFxGuid, SourceMissFxGuid);
                        }
                        else
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: Replacing assets for {activeHeritage.ElementName} Burning Arc");
                            ProjectileLinkReplacer(BurningArcProj0BP, activeHeritage.FxGuid, activeHeritage.MissFxGuid);
                            ProjectileLinkReplacer(BurningArcProj1BP, activeHeritage.FxGuid, activeHeritage.MissFxGuid);
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