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
    internal class CallLightningStorm
    {
        private static readonly LogWrapper AssetLogger = LogWrapper.Get("CallLightningStorm");
        // Структура для хранения данных об элементальных способностях
        // Меняем FX у projectile, используемого способностью (берём первый из m_Projectiles)
        // Original CallLightningStorm FX GUIDs
        public const string SourceFxGuid = "b2c32c1999a42d547a8a0e3e7e958fc6";
        public const string CallLightningStormAbilityGUID = "cad052ef098f9f247ab73ae4c37ac687";
        // Массив элементальных способностей с соответствующими FX
        private static readonly ElementalData.ElementalHeritageData[] ElementalHeritages = new[]
        {
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.RodOfBlizzard,
                FxGuid = "b80420167ae74ac6901cef08ab96b800",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.AmplRodOfBlizzard,
                FxGuid = "2fa2d304aae84d00bec3ca4a84b17a2b",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.DevouringLust,
                FxGuid = "9c4bcd4a87264f89b7a8e5f61f5a6340",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerEarth,
                FxGuid = "97665db18b6c40d5a328c5e3d6cf8c76",
                Color = Colors.Acid.Primary,
                ElementName = "Earth (Acid)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerWater,
                FxGuid = "40fa8d1315e84ea3812598c676887a60",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerAir,
                FxGuid = "1780ff9aa6514694bf06a8880f998927",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerFire,
                FxGuid = "a7cf6150ce334fa8b25a928a21542e6e",
                Color = Colors.Fire.Primary,
                ElementName = "Fire"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerDeath,
                FxGuid = "5d8df75846694ce2b35264c3483947a7",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerHoly,
                FxGuid = "0dbcf2daddcb4bb39a381304591b42bc",
                Color = Colors.Holy.Primary,
                ElementName = "Holy"
            }
        };

        private static void ApplyFxColor(Color color)
        {
            var abilityBP = BlueprintTool.Get<BlueprintAbility>(CallLightningStormAbilityGUID);

            if (abilityBP.Components[2] is not AbilitySpawnFx spawnFxComponent)
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
            var abilityBP = BlueprintTool.Get<BlueprintAbility>(CallLightningStormAbilityGUID);

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
                    AssetTool.RegisterDynamicPrefabLink(heritage.FxGuid, SourceFxGuid, fx =>
                    {
                        AssetModifier.ModifyFx(fx, heritageColor);
                        CustomRecolor(fx, heritage);
                    });
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
                    // Проверяем, является ли способность CallLightningStorm
                    if (__instance.Blueprint.AssetGuid.ToString() == CallLightningStormAbilityGUID ||
                        __instance.Blueprint.name.Contains("CallLightningStorm"))
                    {
                        AssetLogger.Info($"[SpellsRecolorMod]: Call Lightning Storm cast detected: {__instance.Blueprint.name}");

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
                            AssetLogger.Info($"[SpellsRecolorMod]: {(activeHeritage == null ? "No active Elemental Heritage" : "Air Heritage active")}, keeping original Call Lightning Storm");
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
        private static void CustomRecolor(GameObject fx, ElementalData.ElementalHeritageData element)
        {
            if (element.ElementName.Contains("Fire"))
            {
                Color glowColor = new Color(1f, 0.7f, 0f, 1f);
                Color smallColor = new Color(2f, 2f, 2.5f, 0.8f);
                Color lightnColor = new Color(2f, 2.5f, 2.5f, 0.8f);
                Color bottomColor = new Color(3f, 3f, 3f, 0.8f);
                fx.transform.Find("GLowBig").GetComponent<ParticleSystemRenderer>().material.color = glowColor;
                fx.transform.Find("Lightn_RandomRotate/SmallLightnings").GetComponent<ParticleSystemRenderer>().material.color = smallColor;
                fx.transform.Find("Lightn_RandomRotate/Lightn").GetComponent<ParticleSystemRenderer>().material.color = bottomColor;
            }

        }
    }
}
