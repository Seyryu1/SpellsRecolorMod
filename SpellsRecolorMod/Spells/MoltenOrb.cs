using BlueprintCore.Utils;
using BlueprintCore.Utils.Assets;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities;
using UnityEngine;

namespace SpellsRecolorMod.Spells
{
    internal static class MoltenOrb
    {
        private static readonly LogWrapper AssetLogger = LogWrapper.Get("MoltenOrb");

        private static readonly ElementalData.ElementalHeritageData[] ElementalHeritages = new[]
        {
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.StormLordsResolve,
                FxGuid = "101b5c13a8cc464d891c0cfe71efe66e",
                HitFxGuid = "b523d7e99aab455cbf28a5de2ad12567",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.RodOfBlizzard,
                FxGuid = "feb8d21e9c294b87a6da4c2a1cf8ea12",
                HitFxGuid = "d6c60e1577f24a48b061cf90597893d0",
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.AmplRodOfBlizzard,
                FxGuid = "6cee06ea611c4e65bc9460f132cb241a",
                HitFxGuid = "dcaa1a522b9a4f61b99e16bf5859e444",
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.DevouringLust,
                FxGuid = "a37d6d0ac0bb49aab7480dab753c3dee",
                HitFxGuid = "71b196d28131401c896503f1ed6d9571",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerEarth,
                FxGuid = "8325920c36c945228df8b54bd498584a",
                HitFxGuid = "d82b34fdd1924b398e42cda59d2458d7",
                Color = Colors.Acid.Primary,
                ElementName = "Earth (Acid)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerWater,
                FxGuid = "036a918e662a4e699cef082846e3d2d3",
                HitFxGuid = "e4163a2d249448b39feb8a52cf528e2a",
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerAir,
                FxGuid = "62f67e21852a4aa1825bb53e4244d544",
                HitFxGuid = "c686117731ce4037b0c258d44dc64adc",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerFire,
                FxGuid = "33e4215a7bc347189de234c06f5d785a",
                HitFxGuid = "9021481dc6dc43a5bef9aef227c3720b",
                Color = Colors.Fire.Primary,
                ElementName = "Fire"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerDeath,
                FxGuid = "2cd46322d70442fcbec5c138f8b54b85",
                HitFxGuid = "8f29cb290b6945c2b6fd9d49ee0c21ad",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerHoly,
                FxGuid = "6f511e11b97b4fa8979466f1647edef3",
                HitFxGuid = "42b5e69287dd4bdfa7c0d2472159d196",
                Color = Colors.Holy.Primary,
                ElementName = "Holy"
            }
        };

        private static void ProjectileLinkReplacer(BlueprintProjectile proj, string newAssetGuid, string NewHitFxGuid)
        {
            var viewFx = proj.View;
            var hitFx = proj.ProjectileHit;
            PrefabLink newLinkHit = new()
            {
                AssetId = NewHitFxGuid
            };
            AssetLogger.Info($"[SpellsRecolorMod]: Base View is {viewFx.AssetId}:");
            viewFx.AssetId = newAssetGuid;
            AssetLogger.Info($"[SpellsRecolorMod]: New View is {viewFx.AssetId}:");
            var loadedPrefab = viewFx.Load();
            AssetLogger.Info($"[SpellsRecolorMod]: Prefab loaded: {loadedPrefab?.name ?? "null"}");
            hitFx.MissFx = newLinkHit;
            hitFx.HitFx = newLinkHit;
            AssetLogger.Info($"[SpellsRecolorMod]: Prefab hit now is {hitFx.HitFx.AssetId}");
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
        // Original Molten Orb FX GUIDs
        public const string SourceHitFxGuid = "3c025334aa12cbb4d894a2413e3aeb84";
        public const string SourceFxGuid = "3b70dc3b18737e64bac4412dce50dcc3";
        public const string MoltenOrbProjGUID = "49c812020338e90479b54cfc5b1f6305";
        public const string MoltenOrbAbilityGUID = "42a65895ba0cb3a42b6019039dd2bff1";

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
                    AssetTool.RegisterDynamicPrefabLink(heritage.HitFxGuid, SourceHitFxGuid, fx => AssetModifier.ModifyFx(fx, heritageColor));
                    AssetLogger.Info($"[SpellsRecolorMod]: Registered dynamic prefab link for {heritage.ElementName} (hit): {heritage.HitFxGuid} -> {SourceHitFxGuid}");
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
                    // Проверяем, является ли способность Molten Orb
                    if (__instance.Blueprint.AssetGuid.ToString() == MoltenOrbAbilityGUID ||
                        __instance.Blueprint.name.Contains("MoltenOrb"))
                    {
                        AssetLogger.Info($"[SpellsRecolorMod]: Molten Orb cast detected: {__instance.Blueprint.name}");

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
                        BlueprintProjectile MoltenOrbProjBP = BlueprintTool.Get<BlueprintProjectile>(MoltenOrbProjGUID);

                        // Если активен огненный херитедж или нет активного херитеджа - оставляем оригинальные ссылки
                        if (activeHeritage == null || activeHeritage.AbilityGuid == ElementalData.ElementalConstants.ElemHerFire)
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: {(activeHeritage == null ? "No active Elemental Heritage" : "Fire Heritage active")}, keeping original fire Molten Orb");
                            RestoreOriginalLinks(MoltenOrbProjBP, SourceFxGuid, SourceHitFxGuid);
                        }
                        else
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: Replacing assets for {activeHeritage.ElementName} Molten Orb");
                            ProjectileLinkReplacer(MoltenOrbProjBP, activeHeritage.FxGuid, activeHeritage.HitFxGuid);
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