using BlueprintCore.Utils;
using BlueprintCore.Utils.Assets;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities;
using UnityEngine;

namespace SpellsRecolorMod.Spells
{
    internal static class FireBall
    {
        private static readonly LogWrapper AssetLogger = LogWrapper.Get("FireBall");
        public const string SourceHitFxGuid = "85c681ca38c69834a96c3255c99a152c";
        public const string SourceFxGuid = "bc3759a5cecc41a4ca47ed87b676c847";
        public const string FireBallProjGUID = "8927afa172e0fc54484a29fa0c4c40c4";
        public const string FireBallAbilityGUID = "2d81053d7ccf4eb4a88e942429e84b73";

        // Массив элементальных способностей с соответствующими FX
        private static readonly ElementalData.ElementalHeritageData[] ElementalHeritages = new[]
        {
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.StormLordsResolve,
                FxGuid = "b6fcc8fd8cbd408896d3e8e6d59eb859",
                HitFxGuid = "ceec4c77e0e24374a10a86976f36186b",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.RodOfBlizzard,
                FxGuid = "a9a4e03e48404238a37e068e73261257",
                HitFxGuid = "ddc230040c994c8685054f97066d4eb1",
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.AmplRodOfBlizzard,
                FxGuid = "87541283166a4a0d93b7878a7fb5a8db",
                HitFxGuid = "56ee9fe5e8a3462e907501870be28036",
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.DevouringLust,
                FxGuid = "a59c5e72ff9d4e2282639fa93e2dd421",
                HitFxGuid = "4a267bfdb73245859211882dcaa1fe97",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerEarth,
                FxGuid = "cb16590d809a40219d5ea4d0f43a0c56",
                HitFxGuid = "08681479a46543479dda5684820eed04",
                Color = Colors.Acid.Primary,
                ElementName = "Earth (Acid)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerWater,
                FxGuid = "fce02e684c7b4c659441c4d3e43bcdaa",
                HitFxGuid = "5695e2f3da3f4cdd97ab8b12559dbe5f",
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerAir,
                FxGuid = "c3ce43ab15e24062adbf034c53f90d0b",
                HitFxGuid = "c05f138fd69a460c832c65afebdcb3dc",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerFire,
                FxGuid = "934e1931dd9c429e814943b61cae26a6",
                HitFxGuid = "2d343e2b7d8b477cb594e855784f3aa0",
                Color = Colors.Fire.Primary,
                ElementName = "Fire"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerDeath,
                FxGuid = "9148a334d9cb467eb051f69b9dd08ba9",
                HitFxGuid = "e8a24dfc4aff4a5c928fc31b4a518e22",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerHoly,
                FxGuid = "0695bbd96bd3484dbea2dc07ea9f0b0d",
                HitFxGuid = "d07356580eb4472cad23c9555f38afed",
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

        public static void Configure()
        {
            try
            {
                // Регистрируем эффекты с дополнительными элементами
                ElementalData.ElementalHeritageData air = ElementalHeritages.FirstOrDefault(s => s.ElementName.Contains("Air"));
                AssetTool.RegisterDynamicPrefabLink(air.FxGuid, SourceFxGuid, fx =>
                {
                    AssetModifier.ModifyFx(fx, Colors.Air.Primary);
                    ModifyProjectileSphere(fx, air);
                });
                AssetTool.RegisterDynamicPrefabLink(air.HitFxGuid, SourceHitFxGuid, fx =>
                {
                    AssetModifier.ModifyFx(fx, Colors.Air.Primary);
                    AddEffect(fx, "1758f889a811e3b4493141c3fdd2bd8f", air);
                });

                ElementalData.ElementalHeritageData holy = ElementalHeritages.FirstOrDefault(s => s.ElementName.Contains("Holy"));
                AssetTool.RegisterDynamicPrefabLink(holy.FxGuid, SourceFxGuid, fx =>
                {
                    AssetModifier.ModifyFx(fx, Colors.Holy.Primary);
                    ModifyProjectileSphere(fx, holy);
                });
                AssetTool.RegisterDynamicPrefabLink(holy.HitFxGuid, SourceHitFxGuid, fx =>
                {
                    AssetModifier.ModifyFx(fx, Colors.Holy.Primary);
                    AddEffect(fx, "b165b629696a60b4da33c0bcb34b1d8e", holy);
                });

                // Регистрируем остальные эффекты
                foreach (var heritage in ElementalHeritages)
                {
                    if (heritage.AbilityGuid == ElementalData.ElementalConstants.ElemHerAir ||
                        heritage.AbilityGuid == ElementalData.ElementalConstants.ElemHerHoly)
                    {
                        continue;
                    }

                    Color color = heritage.Color;
                    AssetTool.RegisterDynamicPrefabLink(heritage.FxGuid, SourceFxGuid, fx =>
                    {
                        AssetModifier.ModifyFx(fx, color);
                        ModifyProjectileSphere(fx, heritage);
                    });
                    AssetTool.RegisterDynamicPrefabLink(heritage.HitFxGuid, SourceHitFxGuid, fx => AssetModifier.ModifyFx(fx, color));
                }
            }
            catch (Exception e)
            {
                AssetLogger.Info($"[SpellsRecolorMod]: Failed to register dynamic prefab link: {e}");
            }
        }

        // Метод для кастомной модификации MainSphere00 в projectile
        private static void ModifyProjectileSphere(GameObject fx, ElementalData.ElementalHeritageData element)
        {
            var mainSphere = fx.transform.Find("MainSphere00");
            if (mainSphere != null)
            {
                var particleSystem = mainSphere.GetComponent<ParticleSystem>();
                var main = particleSystem.main;
                Color white = new(1f, 1f, 1f, 1f);
                Color black = new(0f, 0f, 0f, 1f);
                if (element.ElementName.Contains("Air")){ main.startColor = white; }
                if (element.ElementName.Contains("Water")) { main.startColor = white; }
                if (element.ElementName.Contains("Death")) { main.startColor = black; }
                if (element.ElementName.Contains("Holy")) { main.startColor = white; }

            }
        }

        // Универсальный метод добавления дополнительного эффекта взрыва
        private static void AddEffect(GameObject fx, string explosionGuid, ElementalData.ElementalHeritageData element)
        {
            var explosionPrefab = ResourcesLibrary.TryGetResource<GameObject>(explosionGuid);
            var explosionInstance = UnityEngine.Object.Instantiate(explosionPrefab);
            UnityEngine.Object.DestroyImmediate(fx.transform.Find("Root/BillboardPart").gameObject);
            UnityEngine.Object.DestroyImmediate(fx.transform.Find("Root/FireFlash").gameObject);
            explosionInstance.transform.SetParent(fx.transform, false);
            explosionInstance.transform.localPosition = Vector3.zero;
            explosionInstance.transform.localRotation = Quaternion.identity;
            explosionInstance.transform.localScale = new Vector3(1f, 1f, 1f);
            if (element.ElementName == "Air (Electricity)")
            {
                explosionInstance.transform.Find("Lightnings").localScale = new Vector3(0.3f, 1f, 0.3f);
                explosionInstance.transform.Find("GameObject").localScale = new Vector3(0.3f, 1f, 0.3f);
                explosionInstance.transform.Find("Rescale/EnergyWave").localScale = new Vector3(0.7f, 1f, 0.7f);
                UnityEngine.Object.DestroyImmediate(explosionInstance.transform.Find("CameraShake").gameObject);
                UnityEngine.Object.DestroyImmediate(explosionInstance.transform.Find("ShockWave_Distortion").gameObject);
            }
            if (element.ElementName == "Holy")
            {
                explosionInstance.transform.Find("Root/WeaponCenter+1 (1)/StartWave").localScale = new Vector3(0.4f, 1f, 0.4f);
                explosionInstance.transform.Find("Root/WeaponCenter+1 (1)/Rays").localScale = new Vector3(0.4f, 0.3f, 0.4f);
                explosionInstance.transform.Find("Root/WeaponCenter+1 (1)/Streams (1)").localScale = new Vector3(0.4f, 1f, 0.4f);
                UnityEngine.Object.DestroyImmediate(explosionInstance.transform.Find("CameraShake").gameObject);
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
                    // Проверяем, является ли способность Fireball
                    if (__instance.Blueprint.AssetGuid.ToString() == FireBallAbilityGUID ||
                        __instance.Blueprint.name.Contains("Fireball"))
                    {
                        AssetLogger.Info($"[SpellsRecolorMod]: Fireball cast detected: {__instance.Blueprint.name}");

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
                        BlueprintProjectile FireBallProjBP = BlueprintTool.Get<BlueprintProjectile>(FireBallProjGUID);

                        // Если активен огненный херитедж или нет активного херитеджа - оставляем оригинальные ссылки
                        if (activeHeritage == null || activeHeritage.AbilityGuid == ElementalData.ElementalConstants.ElemHerFire)
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: {(activeHeritage == null ? "No active Elemental Heritage" : "Fire Heritage active")}, keeping original fire fireball");
                            RestoreOriginalLinks(FireBallProjBP, SourceFxGuid, SourceHitFxGuid);
                        }
                        else
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: Replacing assets for {activeHeritage.ElementName} fireball");
                            ProjectileLinkReplacer(FireBallProjBP, activeHeritage.FxGuid, activeHeritage.HitFxGuid);
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