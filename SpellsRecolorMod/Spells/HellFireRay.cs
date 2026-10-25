using BlueprintCore.Utils;
using BlueprintCore.Utils.Assets;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities;
using UnityEngine;

namespace SpellsRecolorMod.Spells
{
    internal class HellFireRay
    {
        private static readonly LogWrapper AssetLogger = LogWrapper.Get("HellFireRay");
        public const string SourceMissFxGuid = "ee17299746e406d4a9559e2274d18a1b";
        public const string SourceFxGuid = "f8933d82e0b43ef48872bf7a2b45bfbc";
        public const string SourceFxMissDecal = "85a59070f10741745af33c96a5d967f4";
        public const string HellFireRayProj0GUID = "64abeb7fa600aa94ab2ecd63daf687d9";
        public const string HellFireRayAbilityGUID = "700cfcbd0cb2975419bcab7dbb8c6210";

        private static readonly ElementalData.ElementalHeritageData[] ElementalHeritages = new[]
        {
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.StormLordsResolve,
                FxGuid = "6756bf84ae6f420ea1b342cdcb7b1b76",
                MissFxGuid = "e008b98a-be254977bcb13c64fa3ea259",
                MissDecalFxGuid = "c8d6da3d064b4908bb1ac529667c88a4",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.RodOfBlizzard,
                FxGuid = "f43b834279bd4906a2bc983eebb23358",
                MissFxGuid = "e0f6020f3d474748910eb020da8070f8",
                MissDecalFxGuid = "432f6ad243be404a91958aeb5db40bbd",
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.AmplRodOfBlizzard,
                FxGuid = "cea76ade8a7c4243987e545abb6959f7",
                MissFxGuid = "2e6fa022c53e41d38a09ef00c874592d",
                MissDecalFxGuid = "acf19f3e19364c1a95929c6c5f403fb7",
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.DevouringLust,
                FxGuid = "bf2d41733b5c4b148739b0c079a9061d",
                MissFxGuid = "c0a05f3473e148b2-9944bc0b7db3ed69",
                MissDecalFxGuid = "2509ee6d7b4b4645b142b939f64532b4",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerEarth,
                FxGuid = "e87a004b57984d46a06848e82d232258",
                MissFxGuid = "9738b82a57a0484581106af415c007ae",
                MissDecalFxGuid = "3bec1f0526874c5588d5dcdc84a74d58",
                Color = Colors.Acid.Primary,
                ElementName = "Earth (Acid)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerWater,
                FxGuid = "41a418d6e06c48b9bcfb20a61cd80c52",
                MissFxGuid = "7ec118ec192b4e1c8ccf4658d8069f1c",
                MissDecalFxGuid = "8b6dae534a944d289ac21fac5ff0ed83",
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerAir,
                FxGuid = "ecfcd9ae92244d5da857445a76381384",
                MissFxGuid = "3ebb274ebc974321bc5fd58c134d3f59",
                MissDecalFxGuid = "56e0c917374543959b3ae363ba01cb62",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerFire,
                FxGuid = "af5f53260cd04969b62017015938e598",
                MissFxGuid = "4c6e110388b84b549e7fb741889160ef",
                MissDecalFxGuid = "714b7d8cc68c4c819eef5e2bf595a715",
                Color = Colors.Fire.Primary,
                ElementName = "Fire"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerDeath,
                FxGuid = "5f6a6977d5264862bd840c996ecf780b",
                MissFxGuid = "15b0a93dd800445db517c2b6c26cf322",
                MissDecalFxGuid = "ddc3b5dfab0443f9b73d603521e3aafd",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerHoly,
                FxGuid = "13142766ae98499b843b862590d5e7b4",
                MissFxGuid = "310d7e10cdb54f059f08b3c1f9012099",
                MissDecalFxGuid = "ba35d9101a3941a5a0fb1a4e200b0dfc",
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
                    if (__instance.Blueprint.AssetGuid.ToString() == HellFireRayAbilityGUID ||
                        __instance.Blueprint.name.Contains("HellFireRay"))
                    {
                        AssetLogger.Info($"[SpellsRecolorMod]: HellFire Ray cast detected: {__instance.Blueprint.name}");

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

                        BlueprintProjectile HellFireRayProj0BP = BlueprintTool.Get<BlueprintProjectile>(HellFireRayProj0GUID);
                        if (activeHeritage == null || activeHeritage.AbilityGuid == ElementalData.ElementalConstants.ElemHerFire)
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: {(activeHeritage == null ? "No active Elemental Heritage" : "Fire Heritage active")}, keeping original HellFire Ray");
                            RestoreOriginalLinks(HellFireRayProj0BP, SourceFxGuid, SourceMissFxGuid, SourceFxMissDecal);
                        }
                        else
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: Replacing assets for {activeHeritage.ElementName} HellFire Ray");
                            SwapProjectileLinks(HellFireRayProj0BP, activeHeritage.FxGuid, activeHeritage.MissFxGuid, activeHeritage.MissDecalFxGuid);
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
