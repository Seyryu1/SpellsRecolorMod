using BlueprintCore.Utils;
using BlueprintCore.Utils.Assets;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Abilities;
using UnityEngine;

namespace SpellsRecolorMod.Spells
{
    internal class LightningBolt
    {
        private static readonly LogWrapper AssetLogger = LogWrapper.Get("LightningBolt");
        // Структура для хранения данных об элементальных способностях
        // Меняем FX у projectile, используемого способностью (берём первый из m_Projectiles)
        // Original LightningBolt FX GUIDs
        public const string SourceFxGuid = "c8785ab9b1fee5d489f5137ccfe42e3b";
        public const string LightningBoltProjGUID = "c7734162c01abdc478418bfb286ed7a5";
        public const string LightningBoltAbilityGUID = "d2cff9243a7ee804cb6d5be47af30c73";
        // Массив элементальных способностей с соответствующими FX
        private static readonly ElementalData.ElementalHeritageData[] ElementalHeritages = new[]
        {
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.RodOfBlizzard,
                FxGuid = "438668edcf70418390a40f19ed9df4ce",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.AmplRodOfBlizzard,
                FxGuid = "5677dae3cd7348498c4bb6e3ff65b786",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.DevouringLust,
                FxGuid = "71066456-eb684b6a85357ebce6449cb2",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerEarth,
                FxGuid = "1fb716601c52434591f994f6d1b9a840",
                Color = Colors.Acid.Primary,
                ElementName = "Earth (Acid)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerWater,
                FxGuid = "47cfbf77e08543b683e21519a2c90fe8",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerAir,
                FxGuid = "a79a07312c414f11add2ff0a98cf15f2",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerFire,
                FxGuid = "075c6b1c0f3c472396f762d9feda4044",
                Color = Colors.Fire.Primary,
                ElementName = "Fire"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerDeath,
                FxGuid = "a859b62048ec4c3db0f92e203010b168",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerHoly,
                FxGuid = "32a30096be8c4a238af68f2762eaeb07",
                Color = Colors.Holy.Primary,
                ElementName = "Holy"
            }
        };

        private static void ProjectileLinkReplacer(BlueprintProjectile proj, string newAssetGuid)
        {
            var viewFx = proj.View;
            var hitFx = proj.ProjectileHit;
            AssetLogger.Info($"[SpellsRecolorMod]: Base View is {viewFx.AssetId}:");
            viewFx.AssetId = newAssetGuid;
            AssetLogger.Info($"[SpellsRecolorMod]: New View is {viewFx.AssetId}:");
            var loadedPrefab = viewFx.Load();
            AssetLogger.Info($"[SpellsRecolorMod]: Prefab loaded: {loadedPrefab?.name ?? "null"}");
            AssetLogger.Info($"[SpellsRecolorMod]: Prefab hit now is {hitFx.HitFx.AssetId}");
            AssetLogger.Info($"[SpellsRecolorMod]: Projectile links replaced:");
        }

        private static void RestoreOriginalLinks(BlueprintProjectile proj, string originalAssetGuid)
        {
            var viewFx = proj.View;
            var hitFx = proj.ProjectileHit;
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
                    // Создаём замыкание для цвета
                    Color heritageColor = heritage.Color;

                    // Регистрируем FX префаб
                    AssetTool.RegisterDynamicPrefabLink(heritage.FxGuid, SourceFxGuid, fx => { 
                        AssetModifier.ModifyFx(fx, heritageColor);
                        ModifyProjectileMainLightning(fx);
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

        private static void ModifyProjectileMainLightning(GameObject fx)
        {
            string[] objName = { "MainLightning01", "MainLightning00", "SmallLighting00", "SmallLighting01" };
            foreach (string name in objName)
            {
                var mainLightning = fx.transform.Find(name);
                if (mainLightning != null)
                {
                    var lineRenderer = mainLightning.GetComponent<LineRenderer>();
                    Color white = new(1f, 1f, 1f, 1f);
                    lineRenderer.startColor = white;
                }
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
                    // Проверяем, является ли способность LightningBolt
                    if (__instance.Blueprint.AssetGuid.ToString() == LightningBoltAbilityGUID ||
                        __instance.Blueprint.name.Contains("LightningBolt"))
                    {
                        AssetLogger.Info($"[SpellsRecolorMod]: LightningBolt cast detected: {__instance.Blueprint.name}");

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
                        BlueprintProjectile LightningBoltProjBP = BlueprintTool.Get<BlueprintProjectile>(LightningBoltProjGUID);

                        // Если активен огненный херитедж или нет активного херитеджа - оставляем оригинальные ссылки
                        if (activeHeritage == null || activeHeritage.AbilityGuid == ElementalData.ElementalConstants.ElemHerAir)
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: {(activeHeritage == null ? "No active Elemental Heritage" : "Lightning Heritage active")}, keeping original fire Lightning Bolt");
                            RestoreOriginalLinks(LightningBoltProjBP, SourceFxGuid);
                        }
                        else
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: Replacing assets for {activeHeritage.ElementName} Lightning Bolt");
                            ProjectileLinkReplacer(LightningBoltProjBP, activeHeritage.FxGuid);
                        }
                    }
                }
                catch (Exception e)
                {
                    AssetLogger.Info($"[SpellsRecolorMod]: Error in AbilityData_Cast_Patch: {e}");
                }
            }
        }        
        static private void CustomRecolor(GameObject fx, ElementalData.ElementalHeritageData element)
        {
            if (element.ElementName.Contains("Death"))
            {
                var MainLightning00Color = new Color(0.32f, 3f, 1.7f, 0.67f);
                var SmallLightning00Color = new Color(0.32f, 3f, 1.7f, 0.55f);
                fx.transform.Find("MainLightning00").GetComponent<LineRenderer>().material.color = MainLightning00Color;
                fx.transform.Find("MainLightning01").GetComponent<LineRenderer>().material.color = MainLightning00Color;
                fx.transform.Find("SmallLighting00").GetComponent<LineRenderer>().material.color = SmallLightning00Color;
                fx.transform.Find("SmallLighting01").GetComponent<LineRenderer>().material.color = SmallLightning00Color;
            }
            if (element.ElementName.Contains("Fire"))
            {
                var MainLightning00Color = new Color(22f, 1.2f, 0.22f, 0.64f);
                var SmallLightning00Color = new Color(22f, 1.2f, 0.22f, 0.65f);
                fx.transform.Find("MainLightning00").GetComponent<LineRenderer>().material.color = MainLightning00Color;
                fx.transform.Find("MainLightning01").GetComponent<LineRenderer>().material.color = MainLightning00Color;
                fx.transform.Find("SmallLighting00").GetComponent<LineRenderer>().material.color = SmallLightning00Color;
                fx.transform.Find("SmallLighting01").GetComponent<LineRenderer>().material.color = SmallLightning00Color;
            }
            if (element.ElementName.Contains("Holy"))
            {
                var MainLightning00Color = new Color(1.5f, 1.4f, 0.35f, 0.65f);
                var SmallLightning00Color = new Color(1f, 0.8f, 0.32f, 1f);
                fx.transform.Find("MainLightning00").GetComponent<LineRenderer>().material.color = MainLightning00Color;
                fx.transform.Find("MainLightning01").GetComponent<LineRenderer>().material.color = MainLightning00Color;
                fx.transform.Find("SmallLighting00").GetComponent<LineRenderer>().material.color = SmallLightning00Color;
                fx.transform.Find("SmallLighting01").GetComponent<LineRenderer>().material.color = SmallLightning00Color;
            }
            if (element.ElementName.Contains("Acid"))
            {
                var MainLightning00Color = new Color(0.35f, 4f, 0.52f, 0.65f);
                var SmallLightning00Color = new Color(0.35f, 4f, 0.52f, 0.79f);
                fx.transform.Find("MainLightning00").GetComponent<LineRenderer>().material.color = MainLightning00Color;
                fx.transform.Find("MainLightning01").GetComponent<LineRenderer>().material.color = MainLightning00Color;
                fx.transform.Find("SmallLighting00").GetComponent<LineRenderer>().material.color = SmallLightning00Color;
                fx.transform.Find("SmallLighting01").GetComponent<LineRenderer>().material.color = SmallLightning00Color;
            }
            if (element.ElementName.Contains("Water"))
            {
                var MainLightning00Color = new Color(2f, 2f, 1.2f, 0.65f);
                var SmallLightning00Color = new Color(2f, 2f, 1.2f, 0.79f);
                var GlowColor = new Color(1f, 1f, 2f, 0.5f);
                fx.transform.Find("MainLightning00").GetComponent<LineRenderer>().material.color = MainLightning00Color;
                fx.transform.Find("MainLightning01").GetComponent<LineRenderer>().material.color = MainLightning00Color;
                fx.transform.Find("SmallLighting00").GetComponent<LineRenderer>().material.color = SmallLightning00Color;
                fx.transform.Find("SmallLighting01").GetComponent<LineRenderer>().material.color = SmallLightning00Color;
                fx.transform.Find("Glow").GetComponent<LineRenderer>().material.color = GlowColor;
            }
        }
    }
}
