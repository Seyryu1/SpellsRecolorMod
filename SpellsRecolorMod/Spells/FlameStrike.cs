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
    internal static class FlameStrike
    {
        private static readonly LogWrapper AssetLogger = LogWrapper.Get("FlameStrike");

        public const string SourceFxGuid = "d4d5d1e3ca05da444a969ecb217d8f15";
        public const string FlameStrikeAbilityGUID = "f9910c76efc34af41b6e43d5d8752f0f";
        public const string FireStormEffectGuid = "1758f889a811e3b4493141c3fdd2bd8f";
        public const string SnowBallEffectGuid = "9901a354779e9ed4cacfe186bd073d89";

        // Cash for effects
        private static GameObject cachedFireStormEffect = null;
        private static GameObject cashedSnowBallEffect = null;

        private static readonly ElementalData.ElementalHeritageData[] ElementalHeritages = new[]
        {
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.StormLordsResolve,
                FxGuid = "b8cfffa8861b473695d36cf7a63c02d4",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.RodOfBlizzard,
                FxGuid = "f586a1bee10145739a1fe735ccb15269",
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.AmplRodOfBlizzard,
                FxGuid = "703255ee71384111abc3df666e176bee",
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.DevouringLust,
                FxGuid = "c94e2ea924d04d8cb93fe516f3040065",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerEarth,
                FxGuid = "ee82e40c2eef4e8080829b294863ee33",
                Color = Colors.Acid.Primary,
                ElementName = "Earth (Acid)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerWater,
                FxGuid = "b04672a0ad994cf2ae66a03a433896a1",
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerAir,
                FxGuid = "5e13ca1cf91d4d6b949b5ebdae18f6bf",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerFire,
                FxGuid = "74f9954b49404e1fa11d49be34f0d678",
                Color = Colors.Fire.Primary,
                ElementName = "Fire"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerDeath,
                FxGuid = "cb292242e68f4c4cbc790a12d0a48d8f",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerHoly,
                FxGuid = "33098196810243d199f29c907c38ee65",
                Color = Colors.Holy.Primary,
                ElementName = "Holy"
            }
        };

        private static void ProjectileLinkReplacer(string newAssetGuid)
        {
            var abilityBP = BlueprintTool.Get<BlueprintAbility>(FlameStrikeAbilityGUID);
            AbilitySpawnFx spawnFx = abilityBP.Components[4] as AbilitySpawnFx;
            PrefabLink newLink = new()
            {
                AssetId = newAssetGuid
            };
            spawnFx.PrefabLink = newLink;
            AssetLogger.Info($"[SpellsRecolorMod]: Prefab hit now is {spawnFx.PrefabLink.AssetId}");
            AssetLogger.Info($"[SpellsRecolorMod]: Projectile links replaced:");
        }

        private static void RestoreOriginalLinks(string originalAssetGuid)
        {
            var abilityBP = BlueprintTool.Get<BlueprintAbility>(FlameStrikeAbilityGUID);
            AbilitySpawnFx spawnFx = abilityBP.Components[4] as AbilitySpawnFx;
            PrefabLink newLink = new()
            {
                AssetId = originalAssetGuid
            };
            spawnFx.PrefabLink = newLink;
            AssetLogger.Info($"[SpellsRecolorMod]: Prefab restored");
            AssetLogger.Info($"[SpellsRecolorMod]: Projectile links replaced:");
        }

        public static void Configure()
        {
            try
            {
                // Предварительно загружаем эффект грозы для использования в Air heritage
                try
                {
                    cachedFireStormEffect = ResourcesLibrary.TryGetResource<GameObject>(FireStormEffectGuid);
                    cashedSnowBallEffect = ResourcesLibrary.TryGetResource<GameObject>(SnowBallEffectGuid);
                    if (cachedFireStormEffect != null)
                    {
                        AssetLogger.Info($"[SpellsRecolorMod]: FireStorm effect loaded successfully");
                    }
                    else
                    {
                        AssetLogger.Info($"[SpellsRecolorMod]: Warning - FireStorm effect is null");
                    }
                }
                catch (Exception ex)
                {
                    AssetLogger.Info($"[SpellsRecolorMod]: Failed to load FireStorm effect: {ex.Message}");
                }

                // Регистрируем динамические префабы для каждого элемента с соответствующим цветом
                foreach (var heritage in ElementalHeritages)
                {
                    // Создаём замыкание для цвета
                    Color heritageColor = heritage.Color;
                    if (heritage.ElementName.Contains("Air"))
                    {
                        AssetTool.RegisterDynamicPrefabLink(heritage.FxGuid, SourceFxGuid, fx => {
                            AssetModifier.ModifyFx(fx, heritageColor);
                            AddEffect(fx, heritage, cachedFireStormEffect);
                        });
                        AssetLogger.Info($"[SpellsRecolorMod]: Registered dynamic prefab link for {heritage.ElementName} (fx): {heritage.FxGuid} -> {SourceFxGuid}");
                        continue;
                    }
                    if (heritage.ElementName.Contains("Water"))
                    {
                        AssetTool.RegisterDynamicPrefabLink(heritage.FxGuid, SourceFxGuid, fx => 
                        { AssetModifier.ModifyFx(fx, heritageColor); 
                          AddEffect(fx, heritage, cashedSnowBallEffect);
                        });
                        AssetLogger.Info($"[SpellsRecolorMod]: Registered dynamic prefab link for {heritage.ElementName} (fx): {heritage.FxGuid} -> {SourceFxGuid}");
                        continue;
                    }
                    if (heritage.ElementName.Contains("Death"))
                    {
                        AssetTool.RegisterDynamicPrefabLink(heritage.FxGuid, SourceFxGuid, fx => AssetModifier.ModifyFx(fx, heritageColor, AssetModifier.MaterialsOnlyConfig()));
                        AssetLogger.Info($"[SpellsRecolorMod]: Registered dynamic prefab link for {heritage.ElementName} (fx): {heritage.FxGuid} -> {SourceFxGuid}");
                        continue;
                    }
                    if (heritage.ElementName.Contains("Holy"))
                    {
                        AssetTool.RegisterDynamicPrefabLink(heritage.FxGuid, SourceFxGuid, fx => AssetModifier.ModifyFx(fx, new Color(1f, 1f, 1f, 1f)));
                        AssetLogger.Info($"[SpellsRecolorMod]: Registered dynamic prefab link for {heritage.ElementName} (fx): {heritage.FxGuid} -> {SourceFxGuid}");
                        continue;
                    }
                    if (heritage.ElementName.Contains("Acid"))
                    {
                        AssetTool.RegisterDynamicPrefabLink(heritage.FxGuid, SourceFxGuid, fx => AssetModifier.ModifyFx(fx, heritageColor));
                        AssetLogger.Info($"[SpellsRecolorMod]: Registered dynamic prefab link for {heritage.ElementName} (fx): {heritage.FxGuid} -> {SourceFxGuid}");
                        continue;
                    }
                    // Регистрируем FX префаб
                    AssetTool.RegisterDynamicPrefabLink(heritage.FxGuid, SourceFxGuid, fx => AssetModifier.ModifyFx(fx, heritageColor, AssetModifier.MaterialsOnlyConfig()));
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
                    // Проверяем, является ли способность FlameStrike
                    if (__instance.Blueprint.AssetGuid.ToString() == FlameStrikeAbilityGUID ||
                        __instance.Blueprint.name.Contains("FlameStrike"))
                    {
                        AssetLogger.Info($"[SpellsRecolorMod]: FlameStrike cast detected: {__instance.Blueprint.name}");

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

                        // Если активен огненный херитедж или нет активного херитеджа - оставляем оригинальные ссылки
                        if (activeHeritage == null || activeHeritage.AbilityGuid == ElementalData.ElementalConstants.ElemHerFire)
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: {(activeHeritage == null ? "No active Elemental Heritage" : "Fire Heritage active")}, keeping original fire Flame Strike");
                            RestoreOriginalLinks(SourceFxGuid);
                        }
                        else
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: Replacing assets for {activeHeritage.ElementName} FlameStrike");
                            ProjectileLinkReplacer(activeHeritage.FxGuid);
                        }
                    }
                }
                catch (Exception e)
                {
                    AssetLogger.Info($"[SpellsRecolorMod]: Error in AbilityData_Cast_Patch: {e}");
                }
            }
        }

        // Additional effects
        private static void AddEffect(GameObject fx, ElementalData.ElementalHeritageData element, GameObject cashedEffect)
        {
            if (cachedFireStormEffect == null)
            {
                AssetLogger.Info($"[SpellsRecolorMod]: Cannot add effect - cached FireStorm effect is null");
                return;
            }

            try
            {
                // Клонируем кэшированный эффект
                var explosionInstance = UnityEngine.Object.Instantiate(cashedEffect);
                explosionInstance.transform.SetParent(fx.transform, false);
                explosionInstance.transform.localPosition = Vector3.zero;
                explosionInstance.transform.localRotation = Quaternion.identity;
                explosionInstance.transform.localScale = new Vector3(1f, 1f, 1f);

                if (element.ElementName == "Air (Electricity)")
                {
                    // Безопасное удаление дочерних объектов с проверкой на null
                    explosionInstance.transform.Find("Lightnings").localScale = new Vector3(0.1f, 0.5f, 0.1f);
                    explosionInstance.transform.Find("GameObject/ProjectorDecal_Loop").localScale = new Vector3(0.1f, 0.2f, 0.1f);
                    var Lightnings = explosionInstance.transform.Find("Lightnings").GetComponent<ParticleSystem>().main;
                    var Loop = explosionInstance.transform.Find("GameObject/ProjectorDecal_Loop").GetComponent<ParticleSystem>().main;
                    Lightnings.startDelay = 0.7f;
                    Loop.startDelay = 0.7f;
                    DestroyChildSafe(explosionInstance.transform, "CameraShake");
                    DestroyChildSafe(explosionInstance.transform, "ShockWave_Distortion");
                    DestroyChildSafe(explosionInstance.transform, "ShockWave_Cylinder");
                    DestroyChildSafe(explosionInstance.transform, "Rescale");
                    DestroyChildSafe(explosionInstance.transform, "Sparks_CollisionBlue");
                    DestroyChildSafe(explosionInstance.transform, "Sparks_CollisionBlue2");
                    DestroyChildSafe(explosionInstance.transform, "RadialBlur");
                    DestroyChildSafe(explosionInstance.transform, "Point light (7)");
                    DestroyChildSafe(explosionInstance.transform, "RandomRotate");
                }
                if (element.ElementName.Contains("Water"))
                {
                    // Безопасное удаление дочерних объектов с проверкой на null
                    explosionInstance.transform.localScale = new Vector3(5f, 1f, 5f);
                    explosionInstance.transform.Find("Root/DropsWithTrail (2)").localScale = new Vector3(0.4f, 0.4f, 0.4f);
                    explosionInstance.transform.Find("Root/ShockWave (3)").localScale = new Vector3(5f, 5f, 5f);
                    explosionInstance.transform.Find("Root/StonesSmall").localScale = new Vector3(2f, 1f, 2f);
                    explosionInstance.transform.Find("Root/StonesBig").localScale = new Vector3(2f, 1f, 2f);
                    explosionInstance.transform.Find("Root/SnowFlakes").localScale = new Vector3(2f, 2f, 2f);
                    explosionInstance.transform.Find("Root/Dust").localScale = new Vector3(2f, 2f, 2f);
                    explosionInstance.transform.Find("Root/Smoke_End").localScale = new Vector3(3f, 3f, 3f);
                    explosionInstance.transform.Find("Root/Dust_End").localScale = new Vector3(2f, 2f, 2f);
                    var DropsWithTrail = explosionInstance.transform.Find("Root/DropsWithTrail (2)").GetComponent<ParticleSystem>().main;
                    var ShockWave = explosionInstance.transform.Find("Root/ShockWave (3)").GetComponent<ParticleSystem>().main;
                    var StonesSmall = explosionInstance.transform.Find("Root/StonesSmall").GetComponent<ParticleSystem>().main;
                    var SnowFlakes = explosionInstance.transform.Find("Root/SnowFlakes").GetComponent<ParticleSystem>().main;
                    var SnowFlakesBig = explosionInstance.transform.Find("Root/SnowFlakes_Big").GetComponent<ParticleSystem>().main;
                    var Dust = explosionInstance.transform.Find("Root/Dust").GetComponent<ParticleSystem>().main;
                    var Smoke_End = explosionInstance.transform.Find("Root/Smoke_End").GetComponent<ParticleSystem>().main;
                    var StonesBig = explosionInstance.transform.Find("Root/StonesBig").GetComponent<ParticleSystem>().main;
                    var DustEnd = explosionInstance.transform.Find("Root/Dust_End").GetComponent<ParticleSystem>().main;
                    DustEnd.startDelay = 0.5f;
                    StonesBig.startDelay = 0.5f;
                    SnowFlakesBig.startDelay = 0.5f;
                    DropsWithTrail.startDelay = 0.5f;
                    StonesSmall.startDelay = 0.5f;
                    Dust.startDelay = 0.5f;
                    Smoke_End.startDelay = 0.5f;
                    ShockWave.startDelay = 0.4f;
                    SnowFlakes.startDelay = 0.5f;
                    DestroyChildSafe(explosionInstance.transform, "Root/Dust");
                    DestroyChildSafe(explosionInstance.transform, "Root/StonesBig");
                    DestroyChildSafe(explosionInstance.transform, "Root/ShockWave");
                    DestroyChildSafe(explosionInstance.transform, "Root/Point light (2)");
                    DestroyChildSafe(explosionInstance.transform, "CameraShake");
                    DestroyChildSafe(explosionInstance.transform, "RadialBlur");
                    DestroyChildSafe(explosionInstance.transform, "Fresnel (1)");
                    DestroyChildSafe(fx.transform, "Glow_Blue");
                    DestroyChildSafe(fx.transform, "Shockwave_Bottom");
                    DestroyChildSafe(fx.transform, "Shockwave_FireSlow");
                    DestroyChildSafe(fx.transform, "Fire_WaveOuter");
                    DestroyChildSafe(fx.transform, "Fire_WaveOuter1");
                    DestroyChildSafe(fx.transform, "Decal");
                    DestroyChildSafe(fx.transform, "Point light (1)");
                }

                if (element.ElementName == "Holy")
                {
                    DestroyChildSafe(explosionInstance.transform, "Root/WeaponCenter+1 (1)/StartWave", obj => obj.localScale = new Vector3(0.4f, 1f, 0.4f));
                    DestroyChildSafe(explosionInstance.transform, "Root/WeaponCenter+1 (1)/Rays", obj => obj.localScale = new Vector3(0.4f, 0.3f, 0.4f));
                    DestroyChildSafe(explosionInstance.transform, "Root/WeaponCenter+1 (1)/Streams (1)", obj => obj.localScale = new Vector3(0.4f, 1f, 0.4f));
                    DestroyChildSafe(explosionInstance.transform, "CameraShake");
                }


                AssetLogger.Info($"[SpellsRecolorMod]: Successfully added effect for {element.ElementName}");
            }
            catch (Exception ex)
            {
                AssetLogger.Info($"[SpellsRecolorMod]: Error adding effect: {ex.Message}");
            }
        }
        private static void DestroyChildSafe(Transform parent, string path, System.Action<Transform> beforeDestroy = null)
        {
            try
            {
                var child = parent.Find(path);
                if (child != null)
                {
                    beforeDestroy?.Invoke(child);
                    if (beforeDestroy == null) // Только уничтожаем, если не было действия (действие означает изменение, а не удаление)
                    {
                        UnityEngine.Object.DestroyImmediate(child.gameObject);
                    }
                }
            }
            catch (Exception ex)
            {
                AssetLogger.Info($"[SpellsRecolorMod]: Warning - Failed to process child '{path}': {ex.Message}");
            }
        }
    }
}