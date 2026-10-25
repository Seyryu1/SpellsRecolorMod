using BlueprintCore.Utils;
using BlueprintCore.Utils.Assets;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities;
using UnityEngine;

namespace SpellsRecolorMod.Spells
{
    internal static class ChainLightning
    {

        private static readonly LogWrapper AssetLogger = LogWrapper.Get("ChainLightning");
        public const string SourceFxGuid = "c8785ab9b1fee5d489f5137ccfe42e3b";
        public const string ChainLightningProjGUID = "c7734162c01abdc478418bfb286ed7a5";
        public const string ChainLightningAbilityGUID = "645558d63604747428d55f0dd3a4cb58";

        private static readonly ElementalData.ElementalHeritageData[] ElementalHeritages = new[]
        {
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.RodOfBlizzard,
                FxGuid = "a5d1b93bcab540a7a06ef94c8ddba25d",
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.AmplRodOfBlizzard,
                FxGuid = "5f650aecfce04bddb62b6ca5d7dc2381",
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.DevouringLust,
                FxGuid = "8c1187477bd44ced851885fa81cefdef",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerEarth,
                FxGuid = "2ba996c38c334d58a0837a1030e392bf",
                Color = Colors.Acid.Primary,
                ElementName = "Earth (Acid)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerWater,
                FxGuid = "9b09c818346a4596974aaaeafde05c5f",
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerAir,
                FxGuid = "1ec10f402f054f57822059f6f352ad50",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerFire,
                FxGuid = "3cb1bfaeb43e445685fcda957f1cbf9f",
                Color = Colors.Fire.Primary,
                ElementName = "Fire"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerDeath,
                FxGuid = "5022cc6ba31f44c780e0fcbd7d82ab9d",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerHoly,
                FxGuid = "90d352e7c4ca4ba8a9b8cbf560ddb7f1",
                Color = Colors.Holy.Primary,
                ElementName = "Holy"
            }
        };

        private static void ProjectileLinkReplacer(BlueprintProjectile proj, string newAssetGuid)
        {
            var viewFx = proj.View;
            AssetLogger.Info($"[SpellsRecolorMod]: Base View is {viewFx.AssetId}:");
            viewFx.AssetId = newAssetGuid;
            AssetLogger.Info($"[SpellsRecolorMod]: New View is {viewFx.AssetId}:");
            var loadedPrefab = viewFx.Load();
            AssetLogger.Info($"[SpellsRecolorMod]: Projectile links replaced:");
        }

        private static void RestoreOriginalLinks(BlueprintProjectile proj, string originalAssetGuid)
        {
            var viewFx = proj.View;
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
                    // Проверяем, является ли способность ChainLightning
                    if (__instance.Blueprint.AssetGuid.ToString() == ChainLightningAbilityGUID ||
                        __instance.Blueprint.name.Contains("ChainLightning"))
                    {
                        AssetLogger.Info($"[SpellsRecolorMod]: ChainLightning cast detected: {__instance.Blueprint.name}");

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
                        BlueprintProjectile ChainLightningProjBP = BlueprintTool.Get<BlueprintProjectile>(ChainLightningProjGUID);
                        // Если активен огненный херитедж или нет активного херитеджа - оставляем оригинальные ссылки
                        if (activeHeritage == null || activeHeritage.AbilityGuid == ElementalData.ElementalConstants.ElemHerAir)
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: {(activeHeritage == null ? "No active Elemental Heritage" : "Air Heritage active")}, keeping original fire Chain Lightning");
                            RestoreOriginalLinks(ChainLightningProjBP, SourceFxGuid);
                        }
                        else
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: Replacing assets for {activeHeritage.ElementName} Chain Lightning");
                            ProjectileLinkReplacer(ChainLightningProjBP, activeHeritage.FxGuid);
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