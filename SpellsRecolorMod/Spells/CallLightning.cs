using BlueprintCore.Utils;
using BlueprintCore.Utils.Assets;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using UnityEngine;

namespace SpellsRecolorMod.Spells
{
    internal class CallLightning
    {
        private static readonly LogWrapper AssetLogger = LogWrapper.Get("CallLightning");
        // Структура для хранения данных об элементальных способностях
        // Меняем FX у projectile, используемого способностью (берём первый из m_Projectiles)
        // Original CallLightning FX GUIDs
        public const string SourceFxGuid = "503b78b507366cc4da0f462cb40131f6";
        public const string CallLightningAbilityGUID = "0bd54216d38852947930320f6269a9d7";
        // Массив элементальных способностей с соответствующими FX
        private static readonly ElementalData.ElementalHeritageData[] ElementalHeritages = new[]
        {
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.RodOfBlizzard,
                FxGuid = "bbf099224c914b3b90d0a8aa55f699e2",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.AmplRodOfBlizzard,
                FxGuid = "840aff0331e74e1fa637707f58f85e6e",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.DevouringLust,
                FxGuid = "3c73999f53ea47789c79a8d7b32e8696",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerEarth,
                FxGuid = "abd215a7da9948b1a5a8d0a8a52dc26b",
                Color = Colors.Acid.Primary,
                ElementName = "Earth (Acid)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerWater,
                FxGuid = "c2a2fb39fdf44c19a499485c9bca9bcc",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerAir,
                FxGuid = "bffe8257184b4175927a0f4858049b25",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerFire,
                FxGuid = "51e761df11b044e3a66c8192694a8d53",
                Color = Colors.Fire.Primary,
                ElementName = "Fire"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerDeath,
                FxGuid = "4423c3c2ed944ad8a4fb2dcd4ff2c7bf",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerHoly,
                FxGuid = "66afd455f09447cd80c11f2f7cf27d8b",
                Color = Colors.Holy.Primary,
                ElementName = "Holy"
            }
        };

        private static void ApplyFxColor(Color color)
        {
            var abilityBP = BlueprintTool.Get<BlueprintAbility>(CallLightningAbilityGUID);

            if (abilityBP.Components[1] is not AbilitySpawnFx spawnFxComponent)
            {
                AssetLogger.Info($"[SpellsRecolorMod]: Component at index [1] is not AbilitySpawnFx");
                return;
            }

            // Получаем загруженный префаб
            var prefabLink = spawnFxComponent.PrefabLink;
            var loadedPrefab = prefabLink.Load();

            if (loadedPrefab == null)
            {
                AssetLogger.Info($"[SpellsRecolorMod]: Failed to load prefab");
                return;
            }

            AssetLogger.Info($"[SpellsRecolorMod]: Loaded prefab: {loadedPrefab.name}");

            // Модифицируем префаб напрямую
            AssetModifier.ModifyFx(loadedPrefab, color);

            AssetLogger.Info($"[SpellsRecolorMod]: Applied color modification to prefab");
        }

        private static void RestoreOriginalFx()
        {
            var abilityBP = BlueprintTool.Get<BlueprintAbility>(CallLightningAbilityGUID);

            if (abilityBP.Components[1] is not AbilitySpawnFx spawnFxComponent)
            {
                return;
            }

            // Перезагружаем оригинальный префаб
            var prefabLink = spawnFxComponent.PrefabLink;
            
            // Сбрасываем AssetId на оригинальный, если он был изменён
            if (prefabLink.AssetId != SourceFxGuid)
            {
                prefabLink.AssetId = SourceFxGuid;
            }

            // Принудительно перезагружаем
            var loadedPrefab = prefabLink.Load();
            if (loadedPrefab != null)
            {
                // Восстанавливаем оригинальный вид (можно загрузить заново)
                AssetLogger.Info($"[SpellsRecolorMod]: Restored original prefab");
            }
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
                    // Проверяем, является ли способность CallLightning
                    if (__instance.Blueprint.AssetGuid.ToString() == CallLightningAbilityGUID ||
                        (__instance.Blueprint.name == "CallLightning"))
                    {
                        AssetLogger.Info($"[SpellsRecolorMod]: Call Lightning cast detected: {__instance.Blueprint.name}");

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

                        // Применяем соответствующий цвет
                        if (activeHeritage == null || activeHeritage.AbilityGuid == ElementalData.ElementalConstants.ElemHerAir)
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: {(activeHeritage == null ? "No active Elemental Heritage" : "Air Heritage active")}, keeping original Call Lightning");
                            RestoreOriginalFx();
                        }
                        else
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: Applying {activeHeritage.ElementName} color to Call Lightning");
                            ApplyFxColor(activeHeritage.Color);
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
