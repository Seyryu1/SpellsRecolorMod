using BlueprintCore.Utils;
using BlueprintCore.Utils.Assets;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities;
using UnityEngine;

namespace SpellsRecolorMod.Spells
{
    internal class ScorchingRay
    {
        private static readonly LogWrapper AssetLogger = LogWrapper.Get("ScorchingRay");
        public const string SourceMissFxGuid = "ee17299746e406d4a9559e2274d18a1b";
        public const string SourceFxGuid = "81e003d5ea5b84b47b67349510f681b3";
        public const string SourceFxMissDecal = "85a59070f10741745af33c96a5d967f4";
        public const string ScorchingRayProj0GUID = "8cc159ce94d29fe46a94b80ce549161f";
        public const string ScorchingRayAbilityGUID = "8cc159ce94d29fe46a94b80ce549161f";

        private static readonly ElementalData.ElementalHeritageData[] ElementalHeritages = new[]
        {
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.StormLordsResolve,
                FxGuid = "43561ac11e1a4a048131726f04a341ef",
                MissFxGuid = "edd69e5a10544386b45855bf62925570",
                MissDecalFxGuid = "e6c1bca0879e45ad8658108c95452a60",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.RodOfBlizzard,
                FxGuid = "cc15f5665d9f41fdb58c0e93b40d9b73",
                MissFxGuid = "f604c95097bb483990d9ac250416ce46",
                MissDecalFxGuid = "29317aae926d4ba6ae48b9266d083184",
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.AmplRodOfBlizzard,
                FxGuid = "77a829ce106645cf86d27b9b54afeddc",
                MissFxGuid = "aff1af9d200f4be9a42d2b9c896ae0c3",
                MissDecalFxGuid = "a0a75f1e116c4656a815ffdffd30de97",
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.DevouringLust,
                FxGuid = "1268ab3a708a4fa99058979dc0258353",
                MissFxGuid = "3b456a8274b6414fad8b6c6b5ff682e2",
                MissDecalFxGuid = "8e866a1820934424a692130bcbc115f5",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerEarth,
                FxGuid = "c6bbd4de02ed45b5b0cd3e80b035eafa",
                MissFxGuid = "719917fa5a804fc3bd16207b89bc7ea1",
                MissDecalFxGuid = "01d2aba922fa45efb29474c8fa3b32c7",
                Color = Colors.Acid.Primary,
                ElementName = "Earth (Acid)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerWater,
                FxGuid = "3c13ea5428a34eba86c29e69700e23ae",
                MissFxGuid = "f00efbe2d1a447fb98a0f13335fc1e3b",
                MissDecalFxGuid = "c3f34d1ba47b4885b2469036048aad07",
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerAir,
                FxGuid = "3faeaa8f0f3c407ca347b7791ab415e7",
                MissFxGuid = "3ebb274ebc974321bc5fd58c134d3f59",
                MissDecalFxGuid = "56e0c917374543959b3ae363ba01cb62",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerFire,
                FxGuid = "ae562f5c1c844da8b27112e35206be70",
                MissFxGuid = "684f53728b2040a08880d34c54e0e3f9",
                MissDecalFxGuid = "81e48fcbf00a4fdfad8605666cf0dac3",
                Color = Colors.Fire.Primary,
                ElementName = "Fire"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerDeath,
                FxGuid = "5f6a6977d5264862bd840c996ecf780b",
                MissFxGuid = "15b0a93dd800445db517c2b6c26cf322",
                MissDecalFxGuid = "ddc3b5dfab0443f9b73d603521e3aafd",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerHoly,
                FxGuid = "13142766ae98499b843b862590d5e7b4",
                MissFxGuid = "310d7e10cdb54f059f08b3c1f9012099",
                MissDecalFxGuid = "ba35d9101a3941a5a0fb1a4e200b0dfc",
                Color = Colors.Holy.Primary,
                ElementName = "Holy"
            }
        };

        private static void ProjectileLinkReplacer(BlueprintProjectile proj, string newAssetGuid, string NewMissFxGuid, string NewDecalMissFxGuid)
        {
            var viewFx = proj.View;
            var MissFx = proj.ProjectileHit;
            PrefabLink newLinkMiss = new()
            { AssetId = NewMissFxGuid };
            PrefabLink newLinkDecalMiss = new()
            { AssetId = NewDecalMissFxGuid };
            AssetLogger.Info($"[SpellsRecolorMod]: Base View is {viewFx.AssetId}:");
            viewFx.AssetId = newAssetGuid;
            AssetLogger.Info($"[SpellsRecolorMod]: New View is {viewFx.AssetId}:");
            //var loadedPrefab = viewFx.Load();
            //AssetLogger.Info($"[SpellsRecolorMod]: Prefab loaded: {loadedPrefab?.name ?? "null"}");
            MissFx.MissFx = newLinkMiss;
            MissFx.MissDecalFx = newLinkDecalMiss;
            AssetLogger.Info($"[SpellsRecolorMod]: Prefab Miss now is {MissFx.MissFx.AssetId}");
            AssetLogger.Info($"[SpellsRecolorMod]: Prefab Decal Miss now is {MissFx.MissDecalFx.AssetId}");
            AssetLogger.Info($"[SpellsRecolorMod]: Projectile links replaced:");
        }

        private static void RestoreOriginalLinks(BlueprintProjectile proj, string originalAssetGuid, string originalMissFxGuid, string originalFxMissDecal)
        {
            var viewFx = proj.View;
            var hitFx = proj.ProjectileHit;
            PrefabLink originalLinkMiss = new()
            {AssetId = originalMissFxGuid };
            PrefabLink originalLinkMissDecal = new()
            { AssetId = originalFxMissDecal };
            viewFx.AssetId = originalAssetGuid;
            hitFx.MissFx = originalLinkMiss;
            hitFx.MissDecalFx = originalLinkMissDecal;
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
                    // Регистрируем Decal Miss FX префаб
                    AssetTool.RegisterDynamicPrefabLink(heritage.MissDecalFxGuid, SourceFxMissDecal, fx => AssetModifier.ModifyFx(fx, heritageColor));
                    AssetLogger.Info($"[SpellsRecolorMod]: Registered dynamic prefab link for {heritage.ElementName} (decal miss): {heritage.MissDecalFxGuid} -> {SourceFxMissDecal}");
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
                    // Проверяем, является ли способность ScorchingRay
                    if (__instance.Blueprint.AssetGuid.ToString() == ScorchingRayAbilityGUID ||
                        __instance.Blueprint.name.Contains("ScorchingRay"))
                    {
                        AssetLogger.Info($"[SpellsRecolorMod]: ScorchingRay cast detected: {__instance.Blueprint.name}");

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
                        BlueprintProjectile ScorchingRayProj0BP = BlueprintTool.Get<BlueprintProjectile>(ScorchingRayProj0GUID);
                        // Если активен огненный херитедж или нет активного херитеджа - оставляем оригинальные ссылки
                        if (activeHeritage == null || activeHeritage.AbilityGuid == ElementalData.ElementalConstants.ElemHerFire)
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: {(activeHeritage == null ? "No active Elemental Heritage" : "Fire Heritage active")}, keeping original fire Scorching Ray");
                            RestoreOriginalLinks(ScorchingRayProj0BP, SourceFxGuid, SourceMissFxGuid, SourceFxMissDecal);
                        }
                        else
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: Replacing assets for {activeHeritage.ElementName} Scorching Ray");
                            ProjectileLinkReplacer(ScorchingRayProj0BP, activeHeritage.FxGuid, activeHeritage.MissFxGuid, activeHeritage.MissDecalFxGuid);
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
