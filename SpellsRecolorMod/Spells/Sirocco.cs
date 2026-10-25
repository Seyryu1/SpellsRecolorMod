using BlueprintCore.Utils;
using BlueprintCore.Utils.Assets;
using HarmonyLib;
using JetBrains.Annotations;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.Visual.Decals;
using System.Drawing.Text;
using UnityEngine;

namespace SpellsRecolorMod.Spells
{
    internal class Sirocco
    {
        private static readonly LogWrapper AssetLogger = LogWrapper.Get("Sirocco");
        // Структура для хранения данных об элементальных способностях
        // Меняем FX у projectile, используемого способностью (берём первый из m_Projectiles)
        // Original Sirocco FX GUIDs
        public const string SourceFxGuid = "9f9ebe136ce5a9345b5b016f011c5aa6";
        public const string SiroccoAreaEffectGUID = "b21bc337e2beaa74b8823570cd45d6dd";
        public const string SiroccoAbilityGUID = "093ed1d67a539ad4c939d9d05cfe192c";
        // Массив элементальных способностей с соответствующими FX
        private static readonly ElementalData.ElementalHeritageData[] ElementalHeritages = new[]
        {
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.StormLordsResolve,
                FxGuid = "1d96a9fda3e244f982b7f9512989b9b8",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.RodOfBlizzard,
                FxGuid = "bb0110e3527d4b56a58aadf096d67c23",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.AmplRodOfBlizzard,
                FxGuid = "8f3d0bd2691f4594ac2a0ae4525aaff2",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.DevouringLust,
                FxGuid = "c284cebbb12f4a4aa634f7f058be00d9",
                Color = Colors.Death.Secondary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerEarth,
                FxGuid = "98b2303f8388487c90ccb810f67011ab",
                Color = Colors.Acid.Primary,
                ElementName = "Earth (Acid)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerWater,
                FxGuid = "d29134f39398464b9f2db0ca7c8998ee",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerAir,
                FxGuid = "65d5c38d01d440d4beb8e3c7eb704230",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerFire,
                FxGuid = "99b272c2cd734017944f2312de58de33",
                Color = Colors.Fire.Primary,
                ElementName = "Fire"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerDeath,
                FxGuid = "c2e9149542ba42ca87bf293b3623ae23",
                Color = Colors.Death.Secondary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerHoly,
                FxGuid = "c39f246209f9457ebc2c1cf2dbb97e80",
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
                        AssetModifier.ModifyFxZero(fx);
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
                    // Проверяем, является ли способность Sirocco
                    if (__instance.Blueprint.AssetGuid.ToString() == SiroccoAbilityGUID ||
                        __instance.Blueprint.name.Contains("Sirocco"))
                    {
                        AssetLogger.Info($"[SpellsRecolorMod]: Sirocco cast detected: {__instance.Blueprint.name}");

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
                        BlueprintAbilityAreaEffect SiroccoAreaEffectBP = BlueprintTool.Get<BlueprintAbilityAreaEffect>(SiroccoAreaEffectGUID);
                        // Если активен огненный херитедж или нет активного херитеджа - оставляем оригинальные ссылки
                        if (activeHeritage == null || activeHeritage.AbilityGuid == ElementalData.ElementalConstants.ElemHerFire)
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: {(activeHeritage == null ? "No active Elemental Heritage" : "Fire Heritage active")}, keeping original fire Sirocco");
                            RestoreOriginalLinks(SiroccoAreaEffectBP, SourceFxGuid);
                        }
                        else
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: Replacing assets for {activeHeritage.ElementName} Sirocco");
                            ProjectileLinkReplacer(SiroccoAreaEffectBP, activeHeritage.FxGuid);
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
            Dictionary<string, Color> colors = new Dictionary<string, Color>();
            if (element.ElementName.Contains("Water"))
            {
                colors = new Dictionary<string, Color>
                {
                    ["wave"] = new Color(1f, 2f, 6f, 0.5f), 
                    ["rings"] = new Color(0f, 10f, 40f, 0.9f), 
                    ["sparks"] = new Color(0.5f, 2f, 10f, 1f),
                    ["dust"] = new Color(1f, 1.5f, 2.5f, 1f), 
                    ["grass"] = new Color(3f, 6f, 10f, 0.55f),
                };
            }
            if (element.ElementName.Contains("Acid"))
            {
                colors = new Dictionary<string, Color>
                {
                    ["wave"] = new Color(0f, 2f, 0f, 0.5f), 
                    ["rings"] = new Color(0f, 30f, 0f, 0.8f), 
                    ["sparks"] = new Color(0f, 1f, 0f, 1f),
                    ["dust"] = new Color(0f, 2f, 0.5f, 1f), 
                    ["grass"] = new Color(0f, 4f, 0f, 0.7f),
                };
            }
            if (element.ElementName.Contains("Air"))
            {
                colors = new Dictionary<string, Color>
                {
                    ["wave"] = new Color(0f, 3f, 20f, 0.5f),
                    ["rings"] = new Color(0f, 25f, 200f, 0.8f),
                    ["sparks"] = new Color(0f, 1f, 15f, 1f),
                    ["dust"] = new Color(0f, 5f, 22f, 0.2f),
                    ["grass"] = new Color(0f, 2f, 10f, 0.5f),
                };
            }
            if (element.ElementName.Contains("Death"))
            {
                colors = new Dictionary<string, Color>
                {
                    ["wave"] = new Color(0f, 5f, 10f, 0.5f),
                    ["rings"] = new Color(0f, 35f, 140f, 0.76f),
                    ["sparks"] = new Color(0f, 2f, 10f, 1f),
                    ["dust"] = new Color(0f, 4f, 5f, 0.6f),
                    ["grass"] = new Color(0f, 2f, 5f, 0.6f),
                };
            }
            if (element.ElementName.Contains("Holy"))
            {
                colors = new Dictionary<string, Color>
                {
                    ["wave"] = new Color(5f, 6.5f, 5f, 0.5f),
                    ["rings"] = new Color(5f, 20f, 30f, 0.82f),
                    ["sparks"] = new Color(1f, 3f, 8f, 0.6f),
                    ["dust"] = new Color(0f, 7.5f, 12f, 0.3f),
                    ["grass"] = new Color(5f, 4f, 2f, 0.5f),
                };
            }
            RecolorProcess(fx, colors);
            return;
        }

    private static void RecolorProcess(GameObject fx, Dictionary<string, Color> color)
        {
            fx.transform.Find("Root/Wave (3)").GetComponent<Renderer>().material.color = color["wave"];
            fx.transform.Find("Root/Wave (2)").GetComponent<Renderer>().material.color = color["wave"];
            fx.transform.Find("Root/Wave (1)").GetComponent<Renderer>().material.color = color["wave"];
            fx.transform.Find("Root/Waves").GetComponent<Renderer>().material.color = color["wave"];
            fx.transform.Find("Root/SharpRings").GetComponent<Renderer>().material.color = color["rings"];
            fx.transform.Find("Sparks").GetComponent<Renderer>().material.color = color["sparks"];
            fx.transform.Find("SparksLong").GetComponent<Renderer>().material.color = color["sparks"];
            fx.transform.Find("Dust").GetComponent<Renderer>().material.color = color["dust"];
            fx.transform.Find("Grass").GetComponent<Renderer>().material.color = color["grass"];
            fx.transform.Find("Grass (1)").GetComponent<Renderer>().material.color = color["grass"];
        }
    }
}
