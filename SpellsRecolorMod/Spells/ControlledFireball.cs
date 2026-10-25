using BlueprintCore.Utils;
using BlueprintCore.Utils.Assets;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities;
using UnityEngine;

namespace SpellsRecolorMod.Spells
{
    internal class ControlledFireball
    {
        private static readonly LogWrapper AssetLogger = LogWrapper.Get("ControlledFireball");
        public const string SourceHitFxGuid = "85c681ca38c69834a96c3255c99a152c";
        public const string SourceFxGuid = "bc3759a5cecc41a4ca47ed87b676c847";
        public const string ControlledFireballProjGUID = "8927afa172e0fc54484a29fa0c4c40c4";
        public const string ControlledFireballAbilityGUID = "f72f8f03bf0136c4180cd1d70eb773a5";
        // Массив элементальных способностей с соответствующими FX
        private static readonly ElementalData.ElementalHeritageData[] ElementalHeritages = new[]
        {
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.StormLordsResolve,
                FxGuid = "ce62a4abb6e641bf85551006b2c73fcd",
                HitFxGuid = "56fed255bc0b4d1ca0cbe6f82481a4c5",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.RodOfBlizzard,
                FxGuid = "15c55df3dccb4344a2600181a1c4fdad",
                HitFxGuid = "c35a836c8b434922b0d71ef2f23ceda0",
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.AmplRodOfBlizzard,
                FxGuid = "670d9d28064543c2af0e5117e8cc090a",
                HitFxGuid = "10d35c0967f84f42912dd53a9ceb8930",
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.DevouringLust,
                FxGuid = "aa2a62d206b54d6daf3923af6ff5d7aa",
                HitFxGuid = "0a204b8293a74e588f29f39082bcdb8f",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerEarth,
                FxGuid = "820812492a9e448a9da7d0ac0e0a5ac6",
                HitFxGuid = "08681479a46543479dda5684820eed04",
                Color = Colors.Acid.Primary,
                ElementName = "Earth (Acid)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerWater,
                FxGuid = "fce02e684c7b4c659441c4d3e43bcdaa",
                HitFxGuid = "5695e2f3da3f4cdd97ab8b12559dbe5f",
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerAir,
                FxGuid = "c3ce43ab15e24062adbf034c53f90d0b",
                HitFxGuid = "c05f138fd69a460c832c65afebdcb3dc",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerFire,
                FxGuid = "934e1931dd9c429e814943b61cae26a6",
                HitFxGuid = "2d343e2b7d8b477cb594e855784f3aa0",
                Color = Colors.Fire.Primary,
                ElementName = "Fire"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerDeath,
                FxGuid = "9148a334d9cb467eb051f69b9dd08ba9",
                HitFxGuid = "e8a24dfc4aff4a5c928fc31b4a518e22",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerHoly,
                FxGuid = "0695bbd96bd3484dbea2dc07ea9f0b0d",
                HitFxGuid = "d07356580eb4472cad23c9555f38afed",
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

        // Меняем FX у projectile, используемого способностью (берём первый из m_Projectiles)
        // Original ControlledFireball FX GUIDs

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
                    // Проверяем, является ли способность ControlledFireball
                    if (__instance.Blueprint.AssetGuid.ToString() == ControlledFireballAbilityGUID ||
                        __instance.Blueprint.name.Contains("ControlledFireball"))
                    {
                        AssetLogger.Info($"[SpellsRecolorMod]: ControlledFireball cast detected: {__instance.Blueprint.name}");

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
                        BlueprintProjectile ControlledFireballProjBP = BlueprintTool.Get<BlueprintProjectile>(ControlledFireballProjGUID);

                        // Если активен огненный херитедж или нет активного херитеджа - оставляем оригинальные ссылки
                        if (activeHeritage == null || activeHeritage.AbilityGuid == ElementalData.ElementalConstants.ElemHerFire)
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: {(activeHeritage == null ? "No active Elemental Heritage" : "Fire Heritage active")}, keeping original fire ControlledFireball");
                            RestoreOriginalLinks(ControlledFireballProjBP, SourceFxGuid, SourceHitFxGuid);
                        }
                        else
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: Replacing assets for {activeHeritage.ElementName} ControlledFireball");
                            ProjectileLinkReplacer(ControlledFireballProjBP, activeHeritage.FxGuid, activeHeritage.HitFxGuid);
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
