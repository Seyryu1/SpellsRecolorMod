using BlueprintCore.Utils;
using BlueprintCore.Utils.Assets;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities;
using UnityEngine;

namespace SpellsRecolorMod.Spells
{
    internal static class SnowBall
    {
        private static readonly LogWrapper AssetLogger = LogWrapper.Get("SnowBall");

        private static readonly ElementalData.ElementalHeritageData[] ElementalHeritages = new[]
        {
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.StormLordsResolve,
                FxGuid = "baf27e70b22e49f1a986b932ba80e787",
                HitFxGuid = "818b96f32b8c44da8ae06a0480b66635",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.DevouringLust,
                FxGuid = "b10a167c8d934874a749b096e1daf374",
                HitFxGuid = "48db72c5a166437db7656b17e39831ac",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerEarth,
                FxGuid = "d75b437bc4b74adf9f27ddfb9bbbb0d7",
                HitFxGuid = "c0583255fe1d44308983e2eb01a2467f",
                Color = Colors.Acid.Primary,
                ElementName = "Earth (Acid)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerWater,
                FxGuid = "c1ac18d34a5b459caf946be0f44e4d9d",
                HitFxGuid = "3f89848e25964f8fad7c989579142495",
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerAir,
                FxGuid = "e04928be0c4f42df85ce17c01139b530",
                HitFxGuid = "3401520bf5e64787adae3b0fa006851e",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerFire,
                FxGuid = "e0f2f8a3392b46498efa117fd9b156f0",
                HitFxGuid = "79299c3d83c84d3c9250ed13e4a5c250",
                Color = Colors.Fire.Primary,
                ElementName = "Fire"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerDeath,
                FxGuid = "a1efd413c5e448feb58a9e375eb6b100",
                HitFxGuid = "65fa9fab89da4f23925523e2cc5dc3f7",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerHoly,
                FxGuid = "27554f8e6faf42b78080fd9470e5b9e9",
                HitFxGuid = "2d3e5696bb5e4bcf9a9aba3196e08337",
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
        // Original Snowball FX GUIDs
        public const string SourceHitFxGuid = "9901a354779e9ed4cacfe186bd073d89";
        public const string SourceFxGuid = "7e645b7f0627ef543a19ac8e5f226d9e";
        public const string SnowBallProjGUID = "81a8bff536bae184bacb3a58f0bc381a";
        public const string SnowBallAbilityGUID = "9f10909f0be1f5141bf1c102041f93d9";
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
                    // Проверяем, является ли способность Snowball
                    if (__instance.Blueprint.AssetGuid.ToString() == SnowBallAbilityGUID ||
                        __instance.Blueprint.name.Contains("Snowball"))
                    {
                        AssetLogger.Info($"[SpellsRecolorMod]: Snowball cast detected: {__instance.Blueprint.name}");

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
                        BlueprintProjectile SnowBallProjBP = BlueprintTool.Get<BlueprintProjectile>(SnowBallProjGUID);

                        // Если активен огненный херитедж или нет активного херитеджа - оставляем оригинальные ссылки
                        if (activeHeritage == null || activeHeritage.AbilityGuid == ElementalData.ElementalConstants.ElemHerWater)
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: {(activeHeritage == null ? "No active Elemental Heritage" : "Water Heritage active")}, keeping original fire Snowball");
                            RestoreOriginalLinks(SnowBallProjBP, SourceFxGuid, SourceHitFxGuid);
                        }
                        else
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: Replacing assets for {activeHeritage.ElementName} Snowball");
                            ProjectileLinkReplacer(SnowBallProjBP, activeHeritage.FxGuid, activeHeritage.HitFxGuid);
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