using BlueprintCore.Utils;
using BlueprintCore.Utils.Assets;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using UnityEngine;

namespace SpellsRecolorMod.Spells
{
    internal class JoltingPortent
    {
        private static readonly LogWrapper AssetLogger = LogWrapper.Get("JoltingPortent");
        public const string SourceFxGuid = "503b78b507366cc4da0f462cb40131f6";
        public const string JoltingPortentBuffGUID = "27fa15e9738aee1499aed308cdef7084";
        public const string JoltingPortentAbilityGUID = "0dd638688edf68a4da865752d7b9ee82";

        private static readonly ElementalData.ElementalHeritageData[] ElementalHeritages = new[]
        {
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.RodOfBlizzard,
                FxGuid = "b50acc08-3c3b45c8a055a7c67587435e",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.AmplRodOfBlizzard,
                FxGuid = "55f31cdc944248cfa4e655e978595082",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.DevouringLust,
                FxGuid = "44fa6028980a4604945ecca607bd6b36",
                Color = Colors.Death.Secondary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerEarth,
                FxGuid = "70dd8a90b50f4ff4aa0101bb67e143bc",
                Color = Colors.Acid.Primary,
                ElementName = "Earth (Acid)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerWater,
                FxGuid = "7efad1c9899f4a7586fa319606b5d1e2",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerAir,
                FxGuid = "61e9675acd4c40c5a9b8dbedc48ec143",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerFire,
                FxGuid = "afc40a8f553240f48de1d6d6e37c8b19",
                Color = Colors.Fire.Primary,
                ElementName = "Fire"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerDeath,
                FxGuid = "80e36ca0a46b48be9deb9782b6feffd5",
                Color = Colors.Death.Secondary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerHoly,
                FxGuid = "c69b83ae5e4747b495b6c502b799b400",
                Color = Colors.Holy.Primary,
                ElementName = "Holy"
            }
        };

        public static void Configure()
        {
            try
            {
                foreach (var heritage in ElementalHeritages)
                {
                    Color heritageColor = heritage.Color;

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
                    if (__instance.Blueprint.AssetGuid.ToString() == JoltingPortentAbilityGUID ||
                        __instance.Blueprint.name.Contains("JoltingPortent"))
                    {
                        AssetLogger.Info($"[SpellsRecolorMod]: JoltingPortent cast detected: {__instance.Blueprint.name}");

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

                        BlueprintBuff JoltingPortentBuffBP = BlueprintTool.Get<BlueprintBuff>(JoltingPortentBuffGUID);
                        
                        try
                        {
                            var trigger = JoltingPortentBuffBP.Components[0] as AddOffensiveActionTrigger;
                            var conditional = trigger.Action.Actions[0] as Conditional;
                            var savingThrow = conditional.IfFalse.Actions[0] as ContextActionSavingThrow;
                            var conditionalSaved = savingThrow.Actions.Actions[0] as ContextActionConditionalSaved;
                            var spawnFx = conditionalSaved.Failed.Actions[1] as ContextActionSpawnFx;

                            if (activeHeritage == null || activeHeritage.AbilityGuid == ElementalData.ElementalConstants.ElemHerAir)
                            {
                                AssetLogger.Info($"[SpellsRecolorMod]: {(activeHeritage == null ? "No active Elemental Heritage" : "Air Heritage active")}, keeping original Jolting Portent");
                                spawnFx.PrefabLink.AssetId = SourceFxGuid;
                            }
                            else
                            {
                                AssetLogger.Info($"[SpellsRecolorMod]: Replacing assets for {activeHeritage.ElementName} Jolting Portent");
                                spawnFx.PrefabLink.AssetId = activeHeritage.FxGuid;
                                AssetLogger.Info($"[SpellsRecolorMod]: Changed FX to: {activeHeritage.FxGuid}");
                            }
                        }
                        catch (Exception e)
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: Error modifying buff FX: {e}");
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
