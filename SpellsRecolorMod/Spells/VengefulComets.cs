using BlueprintCore.Utils;
using BlueprintCore.Utils.Assets;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using UnityEngine;

namespace SpellsRecolorMod.Spells
{
    internal class VengefulComets
    {
        private static readonly LogWrapper AssetLogger = LogWrapper.Get("VengefulComets");

        // Меняем FX у projectile, используемого способностью (берём первый из m_Projectiles)
        // Original VengefulComets FX GUIDs
        public const string SourceFxGuid = "4828572a4d3cd3547bf5ff2e9e62ee1d";
        public const string VengefulCometsBuffGUID = "df180c5adffa4d6dbb16e57fc1c48232";
        public const string VengefulCometsAbilityGUID = "0e1272506f9f4480b7c3e7e1e53b6439";
        public const string AirOriginalGuid = "2739a726098da0d4c9c2a06bcd0dabaa";
        public const string FireOriginalGuid = "732d4f8e3c4512c4bacce19acf66a7f8";

        private static readonly ElementalData.ElementalHeritageData[] ElementalHeritages = new[]
        {
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.StormLordsResolve,
                FxGuid = AirOriginalGuid,
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)",
                UseOriginalPrefab = true
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.DevouringLust,
                FxGuid = "f30caa9a8dd64868bf3a9ab8e49de9b7",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerEarth,
                FxGuid = "8c267791333d4f3598650204b32c1497",
                Color = Colors.Acid.Primary,
                ElementName = "Earth (Acid)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerWater,
                FxGuid = "dc066cf25b034532b362c728a4e46a63",
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerAir,
                FxGuid = AirOriginalGuid,
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)",
                UseOriginalPrefab = true
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerFire,
                FxGuid = FireOriginalGuid,
                Color = Colors.Fire.Primary,
                ElementName = "Fire",
                UseOriginalPrefab = true
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerDeath,
                FxGuid = "b24f7eff09474b0b883d63a96b461dcb",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerHoly,
                FxGuid = "dd1298837dc546f7a4d4227a7a347b18",
                Color = Colors.Holy.Primary,
                ElementName = "Holy"
            }
        };

        private static void ProjectileLinkReplacer(BlueprintBuff buff, string newAssetGuid)
        {
            var viewFx = buff.FxOnStart;
            PrefabLink newLink = new()
            {
                AssetId = newAssetGuid
            };
            AssetLogger.Info($"[SpellsRecolorMod]: Base View is {viewFx.AssetId}:");
            viewFx.AssetId = newAssetGuid;
            AssetLogger.Info($"[SpellsRecolorMod]: New View is {viewFx.AssetId}:");
            var loadedPrefab = viewFx.Load();
            AssetLogger.Info($"[SpellsRecolorMod]: Prefab loaded: {loadedPrefab?.name ?? "null"}");
            AssetLogger.Info($"[SpellsRecolorMod]: buff links replaced:");
        }

        private static void RestoreOriginalLinks(BlueprintBuff buff, string originalAssetGuid)
        {
            var viewFx = buff.FxOnStart;
            PrefabLink originalLinkHit = new()
            {
                AssetId = originalAssetGuid
            };
            viewFx.AssetId = originalAssetGuid;
            AssetLogger.Info($"[SpellsRecolorMod]: Restored original projectile links");
        }


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
                    // Проверяем, является ли способность VengefulComets
                    if (__instance.Blueprint.AssetGuid.ToString() == VengefulCometsAbilityGUID ||
                        __instance.Blueprint.name.Contains("VengefulComets"))
                    {
                        AssetLogger.Info($"[SpellsRecolorMod]: VengefulComets cast detected: {__instance.Blueprint.name}");

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
                        BlueprintAbility VengefulCometsAbilityBP = BlueprintTool.Get<BlueprintAbility>(VengefulCometsAbilityGUID);
                        BlueprintBuff VengefulCometsBuffBP = BlueprintTool.Get<BlueprintBuff>(VengefulCometsBuffGUID);

                        // Если активен огненный херитедж или нет активного херитеджа - оставляем оригинальные ссылки
                        if (activeHeritage == null || activeHeritage.AbilityGuid == ElementalData.ElementalConstants.ElemHerWater)
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: {(activeHeritage == null ? "No active Elemental Heritage" : "Water Heritage active")}, keeping original fire VengefulComets");
                            RestoreOriginalLinks(VengefulCometsBuffBP, SourceFxGuid);
                        }
                        else
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: Replacing assets for {activeHeritage.ElementName} VengefulComets");
                            ProjectileLinkReplacer(VengefulCometsBuffBP, activeHeritage.FxGuid);
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
