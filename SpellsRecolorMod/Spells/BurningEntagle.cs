using BlueprintCore.Utils;
using BlueprintCore.Utils.Assets;
using HarmonyLib;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using UnityEngine;

namespace SpellsRecolorMod.Spells
{
    internal class BurningEntagle
    {
        private static readonly LogWrapper AssetLogger = LogWrapper.Get("BurningEntagle");
        // Структура для хранения данных об элементальных способностях
        // Меняем FX у projectile, используемого способностью (берём первый из m_Projectiles)
        // Original BurningEntagle FX GUIDs
        public const string SourceFxGuid = "26c899a46e6ea2b47bd34fb572dfdfc8";
        public const string BurningEntagleAreaEffectGUID = "a72483b5218b48d3a223004af205657b";
        public const string BurningEntagleAbilityGUID = "8a76293f5ab8485da95ef6293a11358c";
        // Массив элементальных способностей с соответствующими FX
        private static readonly ElementalData.ElementalHeritageData[] ElementalHeritages = new[]
        {
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.StormLordsResolve,
                FxGuid = "a95fc886bd78484da066462d520fb094",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.RodOfBlizzard,
                FxGuid = "74a1fd0ad00a449793f26716a0ec98f4",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.AmplRodOfBlizzard,
                FxGuid = "5f0c17d54f1b41598a9b8ef34bcb9175",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.DevouringLust,
                FxGuid = "f0ff8546de7b446caae91dc780d3a6f1",
                Color = Colors.Death.Secondary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerEarth,
                FxGuid = "7b0bc1ed26524cf19f410d648e971793",
                Color = Colors.Acid.Primary,
                ElementName = "Earth (Acid)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerWater,
                FxGuid = "51851c430c6245faaf2716ab8fe8a04e",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerAir,
                FxGuid = "6f0525caa5c949dba83e3fb988e5c780",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerFire,
                FxGuid = "a67f75ba23854967bce47dce438cc6a2",
                Color = Colors.Fire.Primary,
                ElementName = "Fire"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerDeath,
                FxGuid = "f8b98a6bd9804d8abeaba46309a6f2e9",
                Color = Colors.Death.Secondary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerHoly,
                FxGuid = "ad719977c2e24b3a89acbdd495ff8a8d",
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
                    // Проверяем, является ли способность BurningEntagle
                    if (__instance.Blueprint.AssetGuid.ToString() == BurningEntagleAbilityGUID ||
                        __instance.Blueprint.name.Contains("BurningEntagle"))
                    {
                        AssetLogger.Info($"[SpellsRecolorMod]: BurningEntagle cast detected: {__instance.Blueprint.name}");

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
                        BlueprintAbilityAreaEffect BurningEntagleAreaEffectBP = BlueprintTool.Get<BlueprintAbilityAreaEffect>(BurningEntagleAreaEffectGUID);
                        // Если активен огненный херитедж или нет активного херитеджа - оставляем оригинальные ссылки
                        if (activeHeritage == null || activeHeritage.AbilityGuid == ElementalData.ElementalConstants.ElemHerFire)
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: {(activeHeritage == null ? "No active Elemental Heritage" : "Fire Heritage active")}, keeping original fire Burning Entagle");
                            RestoreOriginalLinks(BurningEntagleAreaEffectBP, SourceFxGuid);
                        }
                        else
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: Replacing assets for {activeHeritage.ElementName} Winter's Grasp");
                            ProjectileLinkReplacer(BurningEntagleAreaEffectBP, activeHeritage.FxGuid);
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
