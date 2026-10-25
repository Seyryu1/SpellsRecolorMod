using BlueprintCore.Utils;
using BlueprintCore.Utils.Assets;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities;
using UnityEngine;

namespace SpellsRecolorMod.Spells
{
    internal class PolarRay
    {
        private static readonly LogWrapper AssetLogger = LogWrapper.Get("PolarRay");
        public const string SourceMissFxGuid = "68aa0188f6322c24eaefaf597bd5e79d";
        public const string SourceFxGuid = "7ededcefbd2e4514fbaff6a7d42df4ca";
        public const string SourceFxMissDecal = "934310124d9f2c84abb77cba20d6ef0e";
        public const string PolarRayProj0GUID = "68ce28c9ac213e7458670a72da007dd8";
        public const string PolarRayAbilityGUID = "17696c144a0194c478cbe402b496cb23";

        private static readonly ElementalData.ElementalHeritageData[] ElementalHeritages = new[]
        {
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.StormLordsResolve,
                FxGuid = "cf4852f88a134b79bea4771e32286655",
                MissFxGuid = "bb1039b0ca4e42e6beed359a782ee122",
                MissDecalFxGuid = "93d044fcf0a743e88f88862ef66f74ff",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.DevouringLust,
                FxGuid = "208107e207eb4b94b2c1248bb9a19ca0",
                MissFxGuid = "dd41657bde5d4d3fa32e3321c9983fe9",
                MissDecalFxGuid = "898f74f075ba441eb4a939ffdd7fb32a",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerEarth,
                FxGuid = "c6cec06f5afc48aa91dfeed024f693b0",
                MissFxGuid = "115b218f1e9c49b39d16e36fbed5bc2c",
                MissDecalFxGuid = "3b942e0a69c24a85ab1c42ad03d1a63a",
                Color = Colors.Acid.Primary,
                ElementName = "Earth (Acid)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerWater,
                FxGuid = "c785c4a034f642a0a6c9a8c2b0364d58",
                MissFxGuid = "48e191a092bc4772b16bc5f5f8c622b7",
                MissDecalFxGuid = "d7a0276489414827abc5769bba8ddd05",
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerAir,
                FxGuid = "459992f7b49042d09454132351eb4d8d",
                MissFxGuid = "d81401d0a5414a078d440909dc5ff0e0",
                MissDecalFxGuid = "5db7c6220bfd4e92b69ab86f26eae037",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerFire,
                FxGuid = "2d0cb36cc1714fb08c3a37a34750d1da",
                MissFxGuid = "7bd7c84a3b0149f2b1ea3d1290bc00ca",
                MissDecalFxGuid = "acf8a5d636394035b7522b919e88d1d1",
                Color = Colors.Fire.Primary,
                ElementName = "Fire"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerDeath,
                FxGuid = "9b9b0eb6c89642018223ed91f37f96b7",
                MissFxGuid = "8a4e5f01aa90461ea1bd2809c076ba36",
                MissDecalFxGuid = "61ac0305ef3b4b1895bc774abb5b9dd2",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerHoly,
                FxGuid = "6ee7b9dc54d9471696ffc3ce9916057f",
                MissFxGuid = "06d1a63e6d8d4c9fbad940e38b482cba",
                MissDecalFxGuid = "2faa3dd8a4784fa59535325cd738fb93",
                Color = Colors.Holy.Primary,
                ElementName = "Holy"
            }
        };

        private static void SwapProjectileLinks(BlueprintProjectile proj, string newAssetGuid, string newMissFxGuid, string newDecalMissFxGuid)
        {
            try
            {
                var viewFx = proj.View;
                var hitFx = proj.ProjectileHit;

                AssetLogger.Info($"[SpellsRecolorMod]: Base View is {viewFx.AssetId}");
                viewFx.AssetId = newAssetGuid;
                AssetLogger.Info($"[SpellsRecolorMod]: New View is {viewFx.AssetId}");

                hitFx.MissFx.AssetId = newMissFxGuid;
                hitFx.MissDecalFx.AssetId = newDecalMissFxGuid;

                AssetLogger.Info($"[SpellsRecolorMod]: Prefab Miss now is {hitFx.MissFx.AssetId}");
                AssetLogger.Info($"[SpellsRecolorMod]: Prefab Decal Miss now is {hitFx.MissDecalFx.AssetId}");
            }
            catch (Exception e)
            {
                AssetLogger.Info($"[SpellsRecolorMod]: Error in SwapProjectileLinks: {e}");
            }
        }

        private static void RestoreOriginalLinks(BlueprintProjectile proj, string originalAssetGuid, string originalMissFxGuid, string originalFxMissDecal)
        {
            try
            {
                var viewFx = proj.View;
                var hitFx = proj.ProjectileHit;

                viewFx.AssetId = originalAssetGuid;
                hitFx.MissFx.AssetId = originalMissFxGuid;
                hitFx.MissDecalFx.AssetId = originalFxMissDecal;

                AssetLogger.Info($"[SpellsRecolorMod]: Restored original projectile links");
            }
            catch (Exception e)
            {
                AssetLogger.Info($"[SpellsRecolorMod]: Error in RestoreOriginalLinks: {e}");
            }
        }

        public static void Configure()
        {
            try
            {
                foreach (var heritage in ElementalHeritages)
                {
                    Color heritageColor = heritage.Color;

                    // Регистрируем FX префаб
                    AssetTool.RegisterDynamicPrefabLink(heritage.FxGuid, SourceFxGuid, fx =>
                    {
                        try
                        {
                            AssetModifier.ModifyFxZero(fx);
                            AssetModifier.ModifyFx(fx, heritageColor);
                        }
                        catch (Exception e)
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: Error modifying FX for {heritage.ElementName}: {e}");
                        }
                    });
                    AssetLogger.Info($"[SpellsRecolorMod]: Registered dynamic prefab link for {heritage.ElementName} (fx): {heritage.FxGuid} -> {SourceFxGuid}");

                    // Регистрируем HitFX префаб
                    AssetTool.RegisterDynamicPrefabLink(heritage.MissFxGuid, SourceMissFxGuid, fx =>
                    {
                        try
                        {
                            AssetModifier.ModifyFxZero(fx);
                            AssetModifier.ModifyFx(fx, heritageColor);
                        }
                        catch (Exception e)
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: Error modifying Miss FX for {heritage.ElementName}: {e}");
                        }
                    });
                    AssetLogger.Info($"[SpellsRecolorMod]: Registered dynamic prefab link for {heritage.ElementName} (miss): {heritage.MissFxGuid} -> {SourceMissFxGuid}");

                    // Регистрируем Decal Miss FX префаб
                    AssetTool.RegisterDynamicPrefabLink(heritage.MissDecalFxGuid, SourceFxMissDecal, fx =>
                    {
                        try
                        {
                            AssetModifier.ModifyFxZero(fx);
                            AssetModifier.ModifyFx(fx, heritageColor);
                        }
                        catch (Exception e)
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: Error modifying Decal Miss FX for {heritage.ElementName}: {e}");
                        }
                    });
                    AssetLogger.Info($"[SpellsRecolorMod]: Registered dynamic prefab link for {heritage.ElementName} (decal miss): {heritage.MissDecalFxGuid} -> {SourceFxMissDecal}");
                }
            }
            catch (Exception e)
            {
                AssetLogger.Info($"[SpellsRecolorMod]: Failed to register dynamic prefab link: {e}");
            }
        }

        [HarmonyPatch(typeof(AbilityData), nameof(AbilityData.Cast))]
        public static class AbilityData_Cast_Patch
        {
            [HarmonyPrefix]
            public static void Prefix(AbilityData __instance)
            {
                try
                {
                    if (__instance.Blueprint.AssetGuid.ToString() == PolarRayAbilityGUID ||
                        __instance.Blueprint.name.Contains("PolarRay"))
                    {
                        AssetLogger.Info($"[SpellsRecolorMod]: Polar Ray cast detected: {__instance.Blueprint.name}");

                        var caster = __instance.Caster;
                        if (caster == null)
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: Caster is null");
                            return;
                        }

                        AssetLogger.Info($"[SpellsRecolorMod]: Caster: {caster.CharacterName}");

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

                        BlueprintProjectile PolarRayProj0BP = BlueprintTool.Get<BlueprintProjectile>(PolarRayProj0GUID);
                        if (activeHeritage == null || activeHeritage.AbilityGuid == ElementalData.ElementalConstants.ElemHerWater)
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: {(activeHeritage == null ? "No active Elemental Heritage" : "Water Heritage active")}, keeping original Polar Ray");
                            RestoreOriginalLinks(PolarRayProj0BP, SourceFxGuid, SourceMissFxGuid, SourceFxMissDecal);
                        }
                        else
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: Replacing assets for {activeHeritage.ElementName} Polar Ray");
                            SwapProjectileLinks(PolarRayProj0BP, activeHeritage.FxGuid, activeHeritage.MissFxGuid, activeHeritage.MissDecalFxGuid);
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
