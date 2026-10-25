using BlueprintCore.Utils;
using BlueprintCore.Utils.Assets;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.Visual;
using UnityEngine;
using System.Collections.Generic;

namespace SpellsRecolorMod.Spells
{
    internal static class AcidicSpray
    {
        private static readonly LogWrapper AssetLogger = LogWrapper.Get("AicidicSpray");
        public const string SourceFxGuid = "83e04b0f59232264287926c38407a058";
        public const string AicidicSprayProjGUID = "33af0c7694f8d734397bd03e6d4b72f1";
        public const string AicidicSprayAbilityGUID = "c543eef6d725b184ea8669dd09b3894c";

        // Кэш для хранения оригинальных текстур материалов
        private static readonly Dictionary<string, Texture2D> OriginalTextureCache = new();
        private static bool _originalTexturesCached = false;

        // Массив элементальных способностей с соответствующими FX
        private static readonly ElementalData.ElementalHeritageData[] ElementalHeritages = new[]
        {
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.StormLordsResolve,
                FxGuid = "0450bc6e81a64f699fb7c0c6c083c803",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.DevouringLust,
                FxGuid = "23f8613d297445f3becc00ae87d39436",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.RodOfBlizzard,
                FxGuid = "d0ef8ea67a7b48a39ca49a5f576ad269",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.AmplRodOfBlizzard,
                FxGuid = "15415c3293234243b607472a896fd6d8",
                Color = Colors.Water.Secondary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerEarth,
                FxGuid = "a26636d4e43f48fb85f80ab8178c163f",
                Color = Colors.Acid.Primary,
                ElementName = "Earth (Acid)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerWater,
                FxGuid = "a9d5d725de60474aac3ab1a35e7dd519",
                Color = Colors.Water.Primary,
                ElementName = "Water (Cold)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerAir,
                FxGuid = "4af28e0cc5f54e9aad8a9e54128f4196",
                Color = Colors.Air.Primary,
                ElementName = "Air (Electricity)"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerFire,
                FxGuid = "12d2dfb09a2549afacf29fb5b95f94ce",
                Color = Colors.Fire.Primary,
                ElementName = "Fire"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerDeath,
                FxGuid = "c8b65015b26948e280a5faa028945a5f",
                Color = Colors.Death.Primary,
                ElementName = "Death"
            },
            new ElementalData.ElementalHeritageData
            {
                AbilityGuid = ElementalData.ElementalConstants.ElemHerHoly,
                FxGuid = "f121d5ae753c44ea911a49fe592e402a",
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
            var loadedPrefabView = viewFx.Load();
            AssetLogger.Info($"[SpellsRecolorMod]: Prefab loaded: {(loadedPrefabView != null ? loadedPrefabView.name : "null")}");
        }

        private static void RestoreOriginalLinks(BlueprintProjectile proj, string originalAssetGuid)
        {
            var viewFx = proj.View;
            
            // Восстанавливаем GUID
            viewFx.AssetId = originalAssetGuid;
            
            // Загружаем оригинальный префаб
            var originalPrefabView = viewFx.Load();
            
            if (originalPrefabView != null)
            {
                // Получаем GameObject из ProjectileView
                var originalPrefab = originalPrefabView.gameObject;
                
                AssetLogger.Info($"[SpellsRecolorMod]: Restoring original materials for '{originalPrefab.name}'");
                
                // Кэшируем оригинальные текстуры при первом запуске
                if (!_originalTexturesCached)
                {
                    CacheOriginalTextures(originalPrefab);
                    _originalTexturesCached = true;
                }
                
                // Восстанавливаем оригинальные текстуры
                RestoreOriginalTextures(originalPrefab);
            }
            
            AssetLogger.Info($"[SpellsRecolorMod]: Restored original projectile links and materials");
        }

        /// <summary>
        /// Кэширует оригинальные текстуры _ColorAlphaRamp из префаба
        /// </summary>
        private static void CacheOriginalTextures(GameObject prefab)
        {
            try
            {
                // Ищем CompositeTrailRenderer
                var compositeTrailType = System.Type.GetType("Kingmaker.Visual.Trails.CompositeTrailRenderer, Assembly-CSharp");
                if (compositeTrailType == null)
                {
                    AssetLogger.Info($"[SpellsRecolorMod]: CompositeTrailRenderer type not found for caching");
                    return;
                }

                var compositeTrails = prefab.GetComponentsInChildren(compositeTrailType, true);
                AssetLogger.Info($"[SpellsRecolorMod]: Found {compositeTrails.Length} CompositeTrailRenderer components for caching");

                foreach (var trail in compositeTrails)
                {
                    var materialProperty = compositeTrailType.GetProperty("Material", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    
                    if (materialProperty != null)
                    {
                        var material = materialProperty.GetValue(trail) as Material;
                        
                        if (material != null && material.HasProperty("_ColorAlphaRamp"))
                        {
                            var originalTexture = material.GetTexture("_ColorAlphaRamp") as Texture2D;
                            
                            if (originalTexture != null)
                            {
                                // ИЗМЕНЕНО: Используем только имя материала как ключ
                                string cacheKey = material.name;
                                
                                if (!OriginalTextureCache.ContainsKey(cacheKey))
                                {
                                    // Создаём копию текстуры
                                    Texture2D textureCopy = new Texture2D(originalTexture.width, originalTexture.height, 
                                        originalTexture.format, false)
                                    {
                                        name = originalTexture.name + "_Original",
                                        wrapMode = originalTexture.wrapMode
                                    };
                                    
                                    // Копируем пиксели
                                    if (originalTexture.isReadable)
                                    {
                                        textureCopy.SetPixels(originalTexture.GetPixels());
                                    }
                                    else
                                    {
                                        // Используем RenderTexture для копирования
                                        RenderTexture tmp = RenderTexture.GetTemporary(originalTexture.width, originalTexture.height);
                                        Graphics.Blit(originalTexture, tmp);
                                        RenderTexture previous = RenderTexture.active;
                                        RenderTexture.active = tmp;
                                        textureCopy.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
                                        RenderTexture.active = previous;
                                        RenderTexture.ReleaseTemporary(tmp);
                                    }
                                    
                                    textureCopy.Apply();
                                    
                                    OriginalTextureCache[cacheKey] = textureCopy;
                                    AssetLogger.Info($"[SpellsRecolorMod]: Cached original texture for material '{material.name}' (key: {cacheKey})");
                                }
                                else
                                {
                                    AssetLogger.Info($"[SpellsRecolorMod]: Material '{material.name}' already cached");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AssetLogger.Info($"[SpellsRecolorMod]: Error caching original textures: {ex.Message}");
            }
        }

        /// <summary>
        /// Восстанавливает оригинальные текстуры из кэша
        /// </summary>
        private static void RestoreOriginalTextures(GameObject prefab)
        {
            try
            {
                var compositeTrailType = System.Type.GetType("Kingmaker.Visual.Trails.CompositeTrailRenderer, Assembly-CSharp");
                if (compositeTrailType == null) return;

                var compositeTrails = prefab.GetComponentsInChildren(compositeTrailType, true);
                AssetLogger.Info($"[SpellsRecolorMod]: Restoring textures for {compositeTrails.Length} CompositeTrailRenderer components");

                foreach (var trail in compositeTrails)
                {
                    var materialProperty = compositeTrailType.GetProperty("Material", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    
                    if (materialProperty != null)
                    {
                        var material = materialProperty.GetValue(trail) as Material;
                        
                        if (material != null && material.HasProperty("_ColorAlphaRamp"))
                        {
                            // ИЗМЕНЕНО: Используем только имя материала как ключ
                            string cacheKey = material.name;
                            
                            AssetLogger.Info($"[SpellsRecolorMod]: Looking for cached texture with key: '{cacheKey}'");
                            
                            if (OriginalTextureCache.TryGetValue(cacheKey, out Texture2D originalTexture))
                            {
                                material.SetTexture("_ColorAlphaRamp", originalTexture);
                                AssetLogger.Info($"[SpellsRecolorMod]: ✓ Restored original _ColorAlphaRamp for material '{material.name}'");
                            }
                            else
                            {
                                AssetLogger.Info($"[SpellsRecolorMod]: ✗ No cached texture found for material '{material.name}' (key: {cacheKey})");
                                AssetLogger.Info($"[SpellsRecolorMod]:   Available keys in cache: {string.Join(", ", OriginalTextureCache.Keys)}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AssetLogger.Info($"[SpellsRecolorMod]: Error restoring original textures: {ex.Message}");
            }
        }

        public static void Configure()
        {
            try
            {
                // Регистрируем остальные эффекты
                foreach (var heritage in ElementalHeritages)
                {
                    var config = AssetModifier.FullRecolorConfig();
                    Color color = heritage.Color;
                    AssetTool.RegisterDynamicPrefabLink(heritage.FxGuid, SourceFxGuid, fx =>
                    {
                        AssetModifier.ModifyFx(fx, color, config);
                    });
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
                    // Проверяем, является ли способность AicidicSpray
                    if (__instance.Blueprint.AssetGuid.ToString() == AicidicSprayAbilityGUID ||
                        __instance.Blueprint.name.Contains("AicidicSpray"))
                    {
                        AssetLogger.Info($"[SpellsRecolorMod]: Aicidic Spray cast detected: {__instance.Blueprint.name}");

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
                        BlueprintProjectile AicidicSprayProjBP = BlueprintTool.Get<BlueprintProjectile>(AicidicSprayProjGUID);

                        // Если активен огненный херитедж или нет активного херитеджа - оставляем оригинальные ссылки
                        if (activeHeritage == null || activeHeritage.AbilityGuid == ElementalData.ElementalConstants.ElemHerEarth)
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: {(activeHeritage == null ? "No active Elemental Heritage" : "Earth Heritage active")}, keeping original fire Aicidic Spray");
                            RestoreOriginalLinks(AicidicSprayProjBP, SourceFxGuid);
                        }
                        else
                        {
                            AssetLogger.Info($"[SpellsRecolorMod]: Replacing assets for {activeHeritage.ElementName} AicidicSpray");
                            ProjectileLinkReplacer(AicidicSprayProjBP, activeHeritage.FxGuid);
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