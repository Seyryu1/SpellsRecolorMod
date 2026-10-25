using BlueprintCore.Utils;
using BlueprintCore.Utils.Assets;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.Utility;
using Kingmaker.Visual.Particles.FxSpawnSystem;
using UnityEngine;

namespace SpellsRecolorMod.Spells
{
    internal class IncendiaryRunes
    {
        private static readonly LogWrapper AssetLogger = LogWrapper.Get("IncendiaryRunes");
        // Структура для хранения данных об элементальных способностях
        // Меняем FX у projectile, используемого способностью (берём первый из m_Projectiles)
        // Original IncendiaryRunes FX GUIDs
        public const string SourceFxGuid = "342ff67035c7cf94ab6fae66ff08fc97";
        public const string IncendiaryAbilitySpawnFxGUID = "c788cf748ec86bc45b986ac4de28cf45";
        public const string IncendiaryRunesAbilityGUID = "1626dbd11c4143b2a5641da8f227b78c";
        // Массив элементальных способностей с соответствующими FX
        private static readonly ElementalData.ElementalHeritageData[] ElementalHeritages = new[]
        {
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.StormLordsResolve,
                FxGuid = "08a2045a46f743b79c601f3b7432a09d",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.RodOfBlizzard,
                FxGuid = "58c74405c7c54406871ca196cdbcd9d7",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.AmplRodOfBlizzard,
                FxGuid = "f03cbd6b2c7945cfb9e5566b4d60762e",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.DevouringLust,
                FxGuid = "df5e43185b654c86a01c2d836b44b24d",
                Color = Colors.Death.Secondary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerEarth,
                FxGuid = "8d9b0e7530c64dedaf7309bf1c25b6d1",
                Color = Colors.Acid.Primary,
                ElementName = "Earth (Acid)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerWater,
                FxGuid = "4413cca655e94741858615e5f0a99a58",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerAir,
                FxGuid = "cbcfae32c7a84db298aa2ef1250bfc7a",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerFire,
                FxGuid = "0007c4e911f2417d8daa7e2789c3333b",
                Color = Colors.Fire.Primary,
                ElementName = "Fire"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerDeath,
                FxGuid = "ab4aaf963cfb47759c45216d45ab3094",
                Color = Colors.Death.Secondary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerHoly,
                FxGuid = "4c4b2abfa5d94ee0bb36b32379d1a32b",
                Color = Colors.Holy.Primary,
                ElementName = "Holy"
            }
        };
        
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

        // Патч для перехвата момента спавна FX - срабатывает ПЕРЕД отрисовкой руны
        [HarmonyPatch(typeof(AbilitySpawnFx), nameof(AbilitySpawnFx.DoSpawn))]
        public static class AbilitySpawnFx_DoSpawn_Patch
        {
            [HarmonyPrefix]
            public static void Prefix(AbilitySpawnFx __instance, AbilityExecutionContext context, TargetWrapper selectedTarget, List<IFxHandle> list, TargetWrapper target)
            {
                try
                {
                    // Проверяем, является ли способность IncendiaryRunes
                    if (context.AbilityBlueprint.AssetGuid.ToString() == IncendiaryRunesAbilityGUID ||
                        context.AbilityBlueprint.name.Contains("IncendiaryRunes"))
                    {
                        AssetLogger.Info($"[SpellsRecolorMod]: AbilitySpawnFx triggered for IncendiaryRunes");

                        // Получаем кастера из контекста
                        var caster = context.MaybeCaster;
                        if (caster == null)
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: Caster is null in spawn FX");
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

                        // Меняем AssetId НЕПОСРЕДСТВЕННО перед спавном
                        if (activeHeritage == null || activeHeritage.AbilityGuid == ElementalData.ElementalConstants.ElemHerFire)
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: {(activeHeritage == null ? "No active Elemental Heritage" : "Fire Heritage active")}, keeping original fire Incendiary Runes");
                            __instance.PrefabLink.AssetId = SourceFxGuid;
                        }
                        else
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: Replacing assets for {activeHeritage.ElementName} Incendiary Runes: {activeHeritage.FxGuid}");
                            __instance.PrefabLink.AssetId = activeHeritage.FxGuid;
                        }
                    }
                }
                catch (Exception e)
                {
                    AssetLogger.Info($"[SpellsRecolorMod]: Error in AbilitySpawnFx_DoSpawn_Patch: {e}");
                }
            }
        }
    }
}
