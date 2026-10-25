using BlueprintCore.Utils;
using BlueprintCore.Utils.Assets;
using HarmonyLib;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.Visual.Decals;
using UnityEngine;

namespace SpellsRecolorMod.Spells
{
    internal class AcidFog
    {
        private static readonly LogWrapper AssetLogger = LogWrapper.Get("AcidFog");
        // Структура для хранения данных об элементальных способностях
        // Меняем FX у projectile, используемого способностью (берём первый из m_Projectiles)
        // Original AcidFog FX GUIDs
        public const string SourceFxGuid = "9b925e994ccece64c94087c4050866a9";
        public const string AcidFogAreaEffectGUID = "f4dc3f53090627945b83f16ebf3146a6";
        public const string AcidFogAbilityGUID = "dbf99b00cd35d0a4491c6cc9e771b487";
        // Массив элементальных способностей с соответствующими FX
        private static readonly ElementalData.ElementalHeritageData[] ElementalHeritages = new[]
        {
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.StormLordsResolve,
                FxGuid = "da1dfd8a803342b3895a6b4ff621fd80",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.DevouringLust,
                FxGuid = "42e8b4a9ab0b4f04a42a318e2a96497d",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.RodOfBlizzard,
                FxGuid = "3cd4283ae0914d5aa934b4f22fc00ae9",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.AmplRodOfBlizzard,
                FxGuid = "9ec6a316ff954c788cc4e37766296945",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerEarth,
                FxGuid = "42db310bcf6c46f693194ce4189b5b22",
                Color = Colors.Acid.Primary,
                ElementName = "Earth (Acid)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerWater,
                FxGuid = "c05aeabbd4954c3b9a1660150bd27d11",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerAir,
                FxGuid = "4813963d12f94a9b96445a8f1fe30880",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerFire,
                FxGuid = "0a1d93842a1c42198021aab25dadaa47",
                Color = Colors.Fire.Primary,
                ElementName = "Fire"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerDeath,
                FxGuid = "2c328e19ec6443d192a55634907d11f8",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerHoly,
                FxGuid = "1f92300241554b4abc135c16f051fbf0",
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
                    AssetTool.RegisterDynamicPrefabLink(heritage.FxGuid, SourceFxGuid, fx => 
                    {
                    AssetModifier.ModifyFx(fx, heritageColor);
                    CustomlRecolor(fx, heritage);
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
                    // Проверяем, является ли способность AcidFog
                    if (__instance.Blueprint.AssetGuid.ToString() == AcidFogAbilityGUID ||
                        __instance.Blueprint.name.Contains("AcidFog"))
                    {
                        AssetLogger.Info($"[SpellsRecolorMod]: AcidFog cast detected: {__instance.Blueprint.name}");

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
                        BlueprintAbilityAreaEffect AcidFogAreaEffectBP = BlueprintTool.Get<BlueprintAbilityAreaEffect>(AcidFogAreaEffectGUID);
                        // Если активен огненный херитедж или нет активного херитеджа - оставляем оригинальные ссылки
                        if (activeHeritage == null || activeHeritage.AbilityGuid == ElementalData.ElementalConstants.ElemHerEarth)
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: {(activeHeritage == null ? "No active Elemental Heritage" : "Earth Heritage active")}, keeping original fire Acid Fog");
                            RestoreOriginalLinks(AcidFogAreaEffectBP, SourceFxGuid);
                        }
                        else  
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: Replacing assets for {activeHeritage.ElementName} Winter's Grasp");
                            ProjectileLinkReplacer(AcidFogAreaEffectBP, activeHeritage.FxGuid);
                        }
                    }
                }
                catch (Exception e)
                {
                    AssetLogger.Info($"[SpellsRecolorMod]: Error in AbilityData_Cast_Patch: {e}");
                }
            }
        }

        private static void CustomlRecolor(GameObject fx, ElementalData.ElementalHeritageData element)
        {
            if (element.ElementName.Contains("Air"))
            {
                var colorLoop = new Color(0.3f, 0.8f, 2f, 1f);
                var colorDecal = new Color(6f, 3f, 8f, 0.55f);
                fx.transform.Find("Root/Fog_Loop (1)/Fog_Loop").GetComponent<Renderer>().material.color = colorLoop;
                fx.transform.Find("Decal").GetComponent<FxDecal>().m_Material.SetColor("_BaseColor", colorDecal);
                AssetLogger.Info($"[SpellsRecolorMod]: Recolored individual elements for {fx.transform.Find("Root/Fog_Loop (1)/Fog_Loop").GetComponent<Renderer>().material.color} Acid Fog");
            }
            if (element.ElementName.Contains("Fire"))
            {
                var colorLoop = new Color(2.5f, 0.4f, 0f, 1f);
                var colorDecal = new Color(20f, 4f, 2f, 0.55f);
                fx.transform.Find("Root/Fog_Loop (1)/Fog_Loop").GetComponent<Renderer>().material.color = colorLoop;
                fx.transform.Find("Decal").GetComponent<FxDecal>().m_Material.SetColor("_BaseColor", colorDecal);
                AssetLogger.Info($"[SpellsRecolorMod]: Recolored individual elements for {fx.transform.Find("Root/Fog_Loop (1)/Fog_Loop").GetComponent<Renderer>().material.color} Acid Fog");
            }
            if (element.ElementName.Contains("Water"))
            {
                var colorLoop = new Color(4.5f, 1f, 2.3f, 1f);
                var colorDecal = new Color(3.5f, 3f, 4.5f, 0.7f);
                fx.transform.Find("Root/Fog_Loop (1)/Fog_Loop").GetComponent<Renderer>().material.color = colorLoop;
                fx.transform.Find("Decal").GetComponent<FxDecal>().m_Material.SetColor("_BaseColor", colorDecal);
                AssetLogger.Info($"[SpellsRecolorMod]: Recolored individual elements for {fx.transform.Find("Root/Fog_Loop (1)/Fog_Loop").GetComponent<Renderer>().material.color} Acid Fog");
            }
            if (element.ElementName.Contains("Death"))
            {
                var colorLoop = new Color(1.2f, 4f, 10f, 1f);
                var colorDecal = new Color(0f, 2.5f, 3f, 0.7f);
                fx.transform.Find("Root/Fog_Loop (1)/Fog_Loop").GetComponent<Renderer>().material.color = colorLoop;
                fx.transform.Find("Decal").GetComponent<FxDecal>().m_Material.SetColor("_BaseColor", colorDecal);
                AssetLogger.Info($"[SpellsRecolorMod]: Recolored individual elements for {fx.transform.Find("Root/Fog_Loop (1)/Fog_Loop").GetComponent<Renderer>().material.color} Acid Fog");
            }
            if (element.ElementName.Contains("Holy"))
            {
                var colorLoop = new Color(4f, 1.25f, 5.5f, 1f);
                var colorDecal = new Color(8f, 3.7f, 4f, 0.62f);
                fx.transform.Find("Root/Fog_Loop (1)/Fog_Loop").GetComponent<Renderer>().material.color = colorLoop;
                fx.transform.Find("Decal").GetComponent<FxDecal>().m_Material.SetColor("_BaseColor", colorDecal);
                AssetLogger.Info($"[SpellsRecolorMod]: Recolored individual elements for {fx.transform.Find("Root/Fog_Loop (1)/Fog_Loop").GetComponent<Renderer>().material.color} Acid Fog");
            }
            return;

        }
    }
}
