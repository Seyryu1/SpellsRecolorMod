using BlueprintCore.Utils;
using BlueprintCore.Utils.Assets;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities;
using UnityEngine;

/*namespace SpellsRecolorMod.Spells
{
using BlueprintCore.Utils;
using Kingmaker.ResourceLinks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpellsRecolorMod
{
    internal static class AssetModifier
    {
        private static readonly LogWrapper Logger = LogWrapper.Get("AssetModifier");
        private static readonly Dictionary<string, Texture2D> TextureCache = new();
        private static Type _compositeTrailRendererType;
        private static Type _animatedLightType; // ДОБАВЛЕНО

        public class ModifierConfig
        {
            public bool ProcessParticleSystems { get; set; } = true;
            public bool ProcessRenderers { get; set; } = true;
            public bool ProcessLights { get; set; } = true;
            public bool ProcessAllMaterialProperties { get; set; } = false;
            public bool ProcessTrailRenderers { get; set; } = true;
            public bool ProcessLineRenderers { get; set; } = true;
            public bool ProcessSpriteRenderers { get; set; } = true;
            public bool ProcessTextures { get; set; } = false;
            public bool ProcessMaterialsOnly { get; set; } = false;
            public float EmissionMultiplier { get; set; } = 1f;
            public float TextureColorMultiplier { get; set; } = 1f;
            public Func<string, bool> NameFilter { get; set; } = null;
            public List<string> MaterialProperties { get; set; } = new() { "_Color", "_TintColor", "_EmissionColor", "_GlowColor", "_RimColor", "_CoreColor", "_MultiplyColor" };
            public bool EnableLogging { get; set; } = true;
            public bool EnableDetailedLogging { get; set; } = false;
            public bool UseTextureCache { get; set; } = true;
            public Color[] CustomColorRamp { get; set; } = null;
            public bool ProcessCompositeTrailRenderers { get; set; } = true;
            public bool ProcessAnimatedLights { get; set; } = true; // ДОБАВЛЕНО
        }

        public static void ModifyFx(GameObject fx, Color color) => ModifyFx(fx, color, new ModifierConfig());

        public static void ModifyFx(GameObject fx, Color color, ModifierConfig config)
        {
            if (fx == null)
            {
                if (config.EnableLogging) Logger.Info($"[AssetModifier]: ERROR - GameObject is null");
                return;
            }

            if (config.EnableDetailedLogging)
                Logger.Info($"[AssetModifier]: Starting modification of '{fx.name}' with color {ColorToString(color)}");

            int totalChildren = 0, processedCount = 0, filteredCount = 0;

            foreach (Transform child in fx.GetComponentsInChildren<Transform>(true))
            {
                totalChildren++;
                
                if (config.EnableDetailedLogging)
                    Logger.Info($"[AssetModifier]: Processing child [{totalChildren}]: '{child.name}' (path: {GetTransformPath(child)})");

                if (config.NameFilter != null && !config.NameFilter(child.name))
                {
                    filteredCount++;
                    if (config.EnableDetailedLogging)
                        Logger.Info($"[AssetModifier]:   - Skipped by NameFilter");
                    continue;
                }

                bool modified = false;
                if (config.ProcessParticleSystems) modified |= ProcessParticleSystem(child, color, config);
                if (config.ProcessRenderers) modified |= ProcessRenderer(child, color, config);
                if (config.ProcessTrailRenderers) modified |= ProcessTrailRenderer(child, color, config);
                if (config.ProcessLineRenderers) modified |= ProcessLineRenderer(child, color, config);
                if (config.ProcessSpriteRenderers) modified |= ProcessSpriteRenderer(child, color, config);
                if (config.ProcessCompositeTrailRenderers) modified |= ProcessCompositeTrailRenderer(child, color, config);
                if (config.ProcessAnimatedLights) modified |= ProcessAnimatedLight(child, color, config); // ДОБАВЛЕНО
                if (config.ProcessLights) modified |= ProcessLight(child, color, config);
                
                if (modified)
                {
                    processedCount++;
                    if (config.EnableDetailedLogging)
                        Logger.Info($"[AssetModifier]:   - ✓ Modified");
                }
                else if (config.EnableDetailedLogging)
                {
                    Logger.Info($"[AssetModifier]:   - No components to modify");
                }
            }

            if (config.EnableLogging)
                Logger.Info($"[AssetModifier]: Modified {processedCount}/{totalChildren} (filtered: {filteredCount})");
        }

        private static bool ProcessParticleSystem(Transform child, Color color, ModifierConfig config)
        {
            var ps = child.GetComponent<ParticleSystem>();
            if (ps == null) return false;

            if (config.EnableDetailedLogging)
                Logger.Info($"[AssetModifier]:   - Found ParticleSystem");

            if (!config.ProcessMaterialsOnly)
            {
                var main = ps.main;
                var oldStartColor = main.startColor;
                
                if (oldStartColor.mode == ParticleSystemGradientMode.Color)
                {
                    main.startColor = Colors.ReplaceRGB(oldStartColor.color, color);
                }
                else if (oldStartColor.mode == ParticleSystemGradientMode.TwoColors)
                {
                    main.startColor = new ParticleSystem.MinMaxGradient(
                        Colors.ReplaceRGB(oldStartColor.colorMin, color),
                        Colors.ReplaceRGB(oldStartColor.colorMax, color)
                    );
                }
                else
                {
                    main.startColor = color;
                }

                if (config.EnableDetailedLogging)
                    Logger.Info($"[AssetModifier]:     • startColor: {MinMaxGradientToString(oldStartColor)} → {MinMaxGradientToString(main.startColor)}");

                if (ps.colorOverLifetime.enabled)
                {
                    var colorOverLifetime = ps.colorOverLifetime;
                    var oldColor = colorOverLifetime.color;
                    colorOverLifetime.color = new ParticleSystem.MinMaxGradient(CreateGradient(color, true));
                    
                    if (config.EnableDetailedLogging)
                        Logger.Info($"[AssetModifier]:     • colorOverLifetime: {MinMaxGradientToString(oldColor)} → Gradient (fadeOut)");
                }

                if (ps.colorBySpeed.enabled)
                {
                    var colorBySpeed = ps.colorBySpeed;
                    var oldColor = colorBySpeed.color;
                    colorBySpeed.color = new ParticleSystem.MinMaxGradient(CreateGradient(color, false));
                    
                    if (config.EnableDetailedLogging)
                        Logger.Info($"[AssetModifier]:     • colorBySpeed: {MinMaxGradientToString(oldColor)} → Gradient");
                }
            }
            else if (config.EnableDetailedLogging)
            {
                Logger.Info($"[AssetModifier]:     • Skipped particle colors (ProcessMaterialsOnly mode)");
            }

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer?.material != null)
            {
                if (config.EnableDetailedLogging)
                    Logger.Info($"[AssetModifier]:     • Processing ParticleSystemRenderer material: '{renderer.material.name}'");
                ProcessMaterial(renderer.material, color, 2f, config);
            }

            return true;
        }

        private static Gradient CreateGradient(Color color, bool fadeOut)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                fadeOut ? new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
                        : new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
            );
            return gradient;
        }

        private static bool ProcessRenderer(Transform child, Color color, ModifierConfig config)
        {
            var renderer = child.GetComponent<Renderer>();
            if (renderer?.material == null) return false;

            if (config.EnableDetailedLogging)
                Logger.Info($"[AssetModifier]:   - Found Renderer with material: '{renderer.material.name}' (shader: {renderer.material.shader.name})");

            if (config.ProcessAllMaterialProperties)
                ProcessMaterial(renderer.material, color, config.EmissionMultiplier, config);
            else
                ProcessSpecificMaterialColors(renderer.material, color, config);

            return true;
        }

        private static void ProcessMaterial(Material material, Color color, float emissionMultiplier, ModifierConfig config)
        {
            string[] props = {
                "_Color", "_MultiplyColor", "_TintColor", "_EmissionColor", "_GlowColor", "_RimColor", "_CoreColor",
                "_MainColor", "_SpecColor", "_ReflectColor", "_OutlineColor", "_ShadowColor",
                "_HighlightColor", "_FresnelColor", "_AmbientColor", "_DiffuseColor", "_SubsurfaceColor",
                "_TransmissionColor", "_CenterColor", "_EdgeColor", "_InnerColor", "_OuterColor",
                "_BaseColor", "_ColorTint", "_MainTint", "_OverlayColor", "_BackColor", "_FrontColor"
            };

            int modifiedProps = 0;
            foreach (var prop in props)
            {
                if (material.HasProperty(prop))
                {
                    var currentColor = material.GetColor(prop);
                    
                    Color colorToApply;
                    if (prop.Contains("Emission") || prop.Contains("Glow"))
                    {
                        colorToApply = Colors.MultiplyRGBKeepAlpha(currentColor, color, emissionMultiplier);
                    }
                    else
                    {
                        colorToApply = Colors.ReplaceRGB(currentColor, color);
                    }
                    
                    material.SetColor(prop, colorToApply);
                    modifiedProps++;

                    if (config.EnableDetailedLogging)
                        Logger.Info($"[AssetModifier]:       ◦ {prop}: {ColorToString(currentColor)} → {ColorToString(colorToApply)}");
                }
            }

            if (config.EnableDetailedLogging && modifiedProps == 0)
                Logger.Info($"[AssetModifier]:       ◦ No color properties found in material");

            if (config.ProcessTextures) ProcessMaterialTextures(material, color, config);

            if (config.CustomColorRamp != null && config.CustomColorRamp.Length > 0)
            {
                ApplyCustomColorRamp(material, config.CustomColorRamp, config);
            }
        }

        private static void ProcessMaterialTextures(Material material, Color color, ModifierConfig config)
        {
            string[] textureProps = { "_MainTex", "_BumpMap", "_EmissionMap", "_MetallicGlossMap", "_OcclusionMap", "_ParallaxMap", "_DetailMask", "_DetailAlbedoMap", "_DetailNormalMap", "_MultiplyColor" };

            int processedTextures = 0;
            foreach (var texProp in textureProps)
            {
                if (material.HasProperty(texProp))
                {
                    var originalTexture = material.GetTexture(texProp) as Texture2D;
                    if (originalTexture != null)
                    {
                        if (config.EnableDetailedLogging)
                            Logger.Info($"[AssetModifier]:       ◦ Processing texture '{texProp}': '{originalTexture.name}' ({originalTexture.width}x{originalTexture.height})");

                        var recoloredTexture = RecolorTexture(originalTexture, color, config);
                        if (recoloredTexture != null)
                        {
                            material.SetTexture(texProp, recoloredTexture);
                            processedTextures++;
                            
                            if (config.EnableDetailedLogging)
                                Logger.Info($"[AssetModifier]:         → Texture recolored successfully");
                        }
                        else if (config.EnableDetailedLogging)
                        {
                            Logger.Info($"[AssetModifier]:         → Failed to recolor texture");
                        }
                    }
                }
            }

            if (config.EnableDetailedLogging && processedTextures == 0)
                Logger.Info($"[AssetModifier]:       ◦ No textures processed");
        }

        private static Texture2D RecolorTexture(Texture2D original, Color targetColor, ModifierConfig config)
        {
            if (original == null) return null;

            try
            {
                string cacheKey = $"{original.GetInstanceID()}_{targetColor.r:F2}_{targetColor.g:F2}_{targetColor.b:F2}";
                if (config.UseTextureCache && TextureCache.ContainsKey(cacheKey))
                {
                    if (config.EnableDetailedLogging)
                        Logger.Info($"[AssetModifier]:         → Using cached texture");
                    return TextureCache[cacheKey];
                }

                if (!original.isReadable)
                {
                    if (config.EnableDetailedLogging)
                        Logger.Info($"[AssetModifier]:         → Texture not readable, converting...");
                    
                    original = MakeTextureReadable(original);
                    if (original == null)
                    {
                        if (config.EnableDetailedLogging)
                            Logger.Info($"[AssetModifier]:         → Failed to make texture readable");
                        return null;
                    }
                }

                Texture2D recolored = new(original.width, original.height, original.format, original.mipmapCount > 1) { name = $"{original.name}_Recolored" };
                Color[] pixels = original.GetPixels();

                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = new Color(
                        pixels[i].r * targetColor.r * config.TextureColorMultiplier,
                        pixels[i].g * targetColor.g * config.TextureColorMultiplier,
                        pixels[i].b * targetColor.b * config.TextureColorMultiplier,
                        pixels[i].a
                    );

                recolored.SetPixels(pixels);
                recolored.Apply();

                if (config.UseTextureCache)
                {
                    TextureCache[cacheKey] = recolored;
                    if (config.EnableDetailedLogging)
                        Logger.Info($"[AssetModifier]:         → Texture cached");
                }

                return recolored;
            }
            catch (Exception ex)
            {
                if (config.EnableDetailedLogging)
                    Logger.Info($"[AssetModifier]:         → Exception: {ex.Message}");
                return null;
            }
        }

        private static Texture2D MakeTextureReadable(Texture2D original)
        {
            try
            {
                RenderTexture tmp = RenderTexture.GetTemporary(original.width, original.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
                Graphics.Blit(original, tmp);
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = tmp;

                Texture2D readable = new(original.width, original.height);
                readable.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
                readable.Apply();

                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(tmp);
                return readable;
            }
            catch { return null; }
        }

        public static void ClearTextureCache()
        {
            foreach (var texture in TextureCache.Values)
                if (texture != null) UnityEngine.Object.Destroy(texture);
            TextureCache.Clear();
        }

        private static void ProcessSpecificMaterialColors(Material material, Color color, ModifierConfig config)
        {
            int modifiedProps = 0;
            foreach (var prop in config.MaterialProperties)
            {
                if (material.HasProperty(prop))
                {
                    var currentColor = material.GetColor(prop);
                    
                    Color colorToApply;
                    if (prop.Contains("Emission") || prop.Contains("Glow"))
                    {
                        colorToApply = Colors.MultiplyRGBKeepAlpha(currentColor, color, config.EmissionMultiplier);
                    }
                    else
                    {
                        colorToApply = Colors.ReplaceRGB(currentColor, color);
                    }
                    
                    material.SetColor(prop, colorToApply);
                    modifiedProps++;

                    if (config.EnableDetailedLogging)
                        Logger.Info($"[AssetModifier]:       ◦ {prop}: {ColorToString(currentColor)} → {ColorToString(colorToApply)}");
                }
            }

            if (config.EnableDetailedLogging && modifiedProps == 0)
                Logger.Info($"[AssetModifier]:       ◦ No specified properties found in material");

            if (config.ProcessTextures) ProcessMaterialTextures(material, color, config);
            
        }

        private static bool ProcessTrailRenderer(Transform child, Color color, ModifierConfig config)
        {
            var trail = child.GetComponent<TrailRenderer>();
            if (trail == null) return false;

            if (config.EnableDetailedLogging)
            {
                Logger.Info($"[AssetModifier]:   - Found TrailRenderer");
                Logger.Info($"[AssetModifier]:     • time: {trail.time}, minVertexDistance: {trail.minVertexDistance}");
            }

            if (!config.ProcessMaterialsOnly)
            {
                trail.colorGradient = CreateGradient(color, true);

                if (config.EnableDetailedLogging)
                    Logger.Info($"[AssetModifier]:     • colorGradient: → {ColorToString(color)} (fadeOut)");
            }
            else if (config.EnableDetailedLogging)
            {
                Logger.Info($"[AssetModifier]:     • Skipped colorGradient (ProcessMaterialsOnly mode)");
            }

            if (trail.material != null)
            {
                if (config.EnableDetailedLogging)
                    Logger.Info($"[AssetModifier]:     • Processing material: '{trail.material.name}'");
                ProcessMaterial(trail.material, color, 2f, config);
            }

            return true;
        }

        private static bool ProcessLineRenderer(Transform child, Color color, ModifierConfig config)
        {
            var line = child.GetComponent<LineRenderer>();
            if (line == null) return false;

            if (config.EnableDetailedLogging)
            {
                Logger.Info($"[AssetModifier]:   - Found LineRenderer");
                Logger.Info($"[AssetModifier]:     • positionCount: {line.positionCount}, widthMultiplier: {line.widthMultiplier}");
            }

            if (!config.ProcessMaterialsOnly)
            {
                if (config.EnableDetailedLogging)
                {
                    Logger.Info($"[AssetModifier]:     • startColor: {ColorToString(line.startColor)} → {ColorToString(color)}");
                    Logger.Info($"[AssetModifier]:     • endColor: {ColorToString(line.endColor)} → {ColorToString(color)}");
                }

                line.colorGradient = CreateGradient(color, false);
                line.startColor = color;
                line.endColor = color;
            }
            else if (config.EnableDetailedLogging)
            {
                Logger.Info($"[AssetModifier]:     • Skipped line colors (ProcessMaterialsOnly mode)");
            }

            if (line.material != null)
            {
                if (config.EnableDetailedLogging)
                    Logger.Info($"[AssetModifier]:     • Processing material: '{line.material.name}'");
                ProcessMaterial(line.material, color, 2f, config);
            }

            return true;
        }

        private static bool ProcessSpriteRenderer(Transform child, Color color, ModifierConfig config)
        {
            var sprite = child.GetComponent<SpriteRenderer>();
            if (sprite == null) return false;

            if (config.EnableDetailedLogging)
            {
                Logger.Info($"[AssetModifier]:   - Found SpriteRenderer");
                Logger.Info($"[AssetModifier]:     • sprite: {(sprite.sprite != null ? sprite.sprite.name : "null")}");
            }

            if (!config.ProcessMaterialsOnly)
            {
                if (config.EnableDetailedLogging)
                    Logger.Info($"[AssetModifier]:     • color: {ColorToString(sprite.color)} → {ColorToString(color)}");
                
                sprite.color = color;
            }
            else if (config.EnableDetailedLogging)
            {
                Logger.Info($"[AssetModifier]:     • Skipped sprite color (ProcessMaterialsOnly mode)");
            }

            if (sprite.material != null)
            {
                if (config.EnableDetailedLogging)
                    Logger.Info($"[AssetModifier]:     • Processing material: '{sprite.material.name}'");
                ProcessMaterial(sprite.material, color, 1f, config);
            }

            return true;
        }

        private static bool ProcessLight(Transform child, Color color, ModifierConfig config)
        {
            if (config.ProcessMaterialsOnly)
            {
                if (config.EnableDetailedLogging)
                {
                    var light = child.GetComponent<Light>();
                    if (light != null)
                        Logger.Info($"[AssetModifier]:   - Found Light (skipped in ProcessMaterialsOnly mode)");
                }
                return false;
            }

            var lightComponent = child.GetComponent<Light>();
            if (lightComponent == null) return false;

            if (config.EnableDetailedLogging)
            {
                Logger.Info($"[AssetModifier]:   - Found Light");
                Logger.Info($"[AssetModifier]:     • type: {lightComponent.type}, intensity: {lightComponent.intensity}, range: {lightComponent.range}");
                Logger.Info($"[AssetModifier]:     • color: {ColorToString(lightComponent.color)} → {ColorToString(color)}");
            }

            lightComponent.color = color;
            return true;
        }

        private static string ColorToString(Color color)
        {
            return $"RGBA({color.r:F2}, {color.g:F2}, {color.b:F2}, {color.a:F2})";
        }

        private static string MinMaxGradientToString(ParticleSystem.MinMaxGradient gradient)
        {
            return gradient.mode switch
            {
                ParticleSystemGradientMode.Color => $"Color({ColorToString(gradient.color)})",
                ParticleSystemGradientMode.TwoColors => $"TwoColors({ColorToString(gradient.colorMin)}, {ColorToString(gradient.colorMax)})",
                ParticleSystemGradientMode.Gradient => "Gradient",
                ParticleSystemGradientMode.TwoGradients => "TwoGradients",
                _ => "Unknown"
            };
        }

        private static string GetTransformPath(Transform transform)
        {
            string path = transform.name;
            Transform current = transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }

        // Предустановленные конфигурации
        public static ModifierConfig AggressiveConfig() => new() { ProcessAllMaterialProperties = true, EmissionMultiplier = 2f };
        public static ModifierConfig TextureRecolorConfig() => new() { ProcessTextures = true, TextureColorMultiplier = 1f, UseTextureCache = true };
        public static ModifierConfig FullRecolorConfig() => new() { ProcessAllMaterialProperties = true, ProcessTextures = true, TextureColorMultiplier = 0.8f, EmissionMultiplier = 2f, UseTextureCache = true };
        public static ModifierConfig ParticlesOnlyConfig() => new() { ProcessLights = false, ProcessTrailRenderers = true, ProcessLineRenderers = true };
        public static ModifierConfig FilteredConfig(Func<string, bool> nameFilter) => new() { NameFilter = nameFilter };
        public static ModifierConfig CustomMaterialConfig(List<string> properties) => new() { MaterialProperties = properties };
        public static ModifierConfig MinimalConfig() => new() { ProcessLights = false, ProcessTrailRenderers = false, ProcessLineRenderers = false, ProcessSpriteRenderers = false, MaterialProperties = new() { "_Color", "_TintColor" }, EmissionMultiplier = 1f, EnableLogging = false };
        public static ModifierConfig SilentConfig() => new() { EnableLogging = false, EnableDetailedLogging = false };
        public static ModifierConfig NoOpConfig() => new() { ProcessParticleSystems = false, ProcessRenderers = false, ProcessLights = false, ProcessTrailRenderers = false, ProcessLineRenderers = false, ProcessSpriteRenderers = false, ProcessTextures = false, EnableLogging = false };
        public static ModifierConfig DetailedLoggingConfig() => new() { EnableLogging = true, EnableDetailedLogging = true };
        public static ModifierConfig MaterialsOnlyConfig() => new() { ProcessMaterialsOnly = true, ProcessAllMaterialProperties = true, EmissionMultiplier = 2f };
        
        public static ModifierConfig ConfigWithCustomRamp(Color[] gradientColors)
        {
            return new ModifierConfig
            {
                CustomColorRamp = gradientColors,
                EnableDetailedLogging = false
                
            };
        }

        /// <summary>
        /// Конфигурация для изменения ТОЛЬКО _ColorAlphaRamp без изменения других свойств материала
        /// </summary>
        public static ModifierConfig RampOnlyConfig(Color[] gradientColors)
        {
            return new ModifierConfig
            {
                // Отключаем все изменения компонентов
                ProcessParticleSystems = false,
                ProcessLights = false,
                ProcessTrailRenderers = false,
                ProcessLineRenderers = false,
                ProcessSpriteRenderers = false,
                
                // Включаем только обработку рендереров и CompositeTrailRenderer
                ProcessRenderers = true,
                ProcessCompositeTrailRenderers = true,
                
                // ВАЖНО: Не меняем цвета материалов
                ProcessAllMaterialProperties = false,
                MaterialProperties = new List<string>(), // Пустой список = не трогаем цвета
                
                // Не трогаем текстуры
                ProcessTextures = false,
                
                // Устанавливаем кастомный рамп
                CustomColorRamp = gradientColors,
                
                // Логирование (опционально)
                EnableLogging = false,
                EnableDetailedLogging = false
            };
        }
        public static ModifierConfig FullRecolorWithRamp(Color[] gradientColors)
        {
            return new ModifierConfig
            {
                // Полное перекрашивание
                ProcessAllMaterialProperties = true,
                ProcessTextures = true,
                TextureColorMultiplier = 0.8f,
                EmissionMultiplier = 1f,
                UseTextureCache = true,

                // + Кастомный рамп
                CustomColorRamp = gradientColors,

                // Все компоненты
                ProcessParticleSystems = true,
                ProcessRenderers = true,
                ProcessLights = true,
                ProcessTrailRenderers = true,
                ProcessLineRenderers = true,
                ProcessSpriteRenderers = true,
                ProcessCompositeTrailRenderers = true,

                EnableLogging = false,
                EnableDetailedLogging = true
            };
        }

        private static Texture2D CreateColorAlphaRamp(Color[] colors)
        {
            int width = colors.Length;
            Texture2D texture = new Texture2D(width, 1, TextureFormat.RGBA32, false)
            {
                name = "CustomColorAlphaRamp",
                wrapMode = TextureWrapMode.Clamp
            };
            
            for (int i = 0; i < width; i++)
            {
                texture.SetPixel(i, 0, colors[i]);
            }
            
            texture.Apply();
            return texture;
        }

        private static void ApplyCustomColorRamp(Material material, Color[] colors, ModifierConfig config)
        {
            if (colors == null || colors.Length < 2)
            {
                if (config.EnableLogging)
                    Logger.Info($"[AssetModifier]:       ◦ CustomColorRamp: Invalid color array");
                return;
            }

            try
            {
                if (!material.HasProperty("_ColorAlphaRamp"))
                {
                    return;
                }

                var oldTexture = material.GetTexture("_ColorAlphaRamp") as Texture2D;
        
                if (oldTexture != null)
                {
                    int width = oldTexture.width;
                    Texture2D rampTexture = new Texture2D(width, 1, TextureFormat.RGBA32, false)
                    {
                        name = "CustomColorAlphaRamp",
                        wrapMode = TextureWrapMode.Clamp
                    };
            
                    Color[] originalPixels = null;
                    try
                    {
                        if (oldTexture.isReadable)
                        {
                            originalPixels = oldTexture.GetPixels();
                            Logger.Info($"[AssetModifier]: Original texture readable, got {originalPixels.Length} pixels");
                        }
                        else
                        {
                            var readableTexture = MakeTextureReadable(oldTexture);
                            if (readableTexture != null)
                            {
                                originalPixels = readableTexture.GetPixels();
                                Logger.Info($"[AssetModifier]: Converted texture to readable, got {originalPixels.Length} pixels");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Info($"[AssetModifier]: Failed to read pixels: {ex.Message}");
                    }

                    // ИЗМЕНЕНО: Правильная интерполяция для любой ширины
                    for (int i = 0; i < width; i++)
                    {
                        // Нормализованная позиция (0.0 - 1.0)
                        float t = i / (float)(width - 1);
                
                        // Находим позицию в массиве градиента
                        float gradientPos = t * (colors.Length - 1);
                        int index0 = Mathf.FloorToInt(gradientPos);
                        int index1 = Mathf.Min(index0 + 1, colors.Length - 1);
                        float lerp = gradientPos - index0;
                
                        // Интерполируем между двумя цветами
                        Color newColor = Color.Lerp(colors[index0], colors[index1], lerp);
                
                        // Сохраняем оригинальную альфу
                        if (originalPixels != null && i < originalPixels.Length)
                        {
                            newColor.a = originalPixels[i].a;
                        }
                
                        rampTexture.SetPixel(i, 0, newColor);
                    }
    
                    rampTexture.Apply();
                    material.SetTexture("_ColorAlphaRamp", rampTexture);
            
                    Logger.Info($"[AssetModifier]: ✓ Applied {width}px ramp to '{material.name}'");
                }
                else
                {
                    var rampTexture = CreateColorAlphaRamp(colors);
                    material.SetTexture("_ColorAlphaRamp", rampTexture);
                    Logger.Info($"[AssetModifier]: Created new {colors.Length}px ramp for '{material.name}'");
                }
            }
            catch (Exception ex)
            {
                Logger.Info($"[AssetModifier]: ✗ EXCEPTION: {ex.Message}");
            }
        }

        private static bool ProcessCompositeTrailRenderer(Transform child, Color color, ModifierConfig config)
        {
            // Инициализируем тип только один раз
            if (_compositeTrailRendererType == null)
            {
                try
                {
                    _compositeTrailRendererType = Type.GetType("Kingmaker.Visual.Trails.CompositeTrailRenderer, Assembly-CSharp");

                    if (_compositeTrailRendererType == null)
                    {
                        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                        foreach (var assembly in assemblies)
                        {
                            if (assembly.GetName().Name == "Assembly-CSharp")
                            {
                                _compositeTrailRendererType = assembly.GetType("Kingmaker.Visual.Trails.CompositeTrailRenderer");
                                if (_compositeTrailRendererType != null)
                                    break;
                            }
                        }

                    }

                    if (_compositeTrailRendererType != null)
                    {
                        Logger.Info($"[AssetModifier]: CompositeTrailRenderer type found: {_compositeTrailRendererType.FullName}");
                    }
                    else
                    {
                        Logger.Info($"[AssetModifier]: CompositeTrailRenderer type NOT FOUND");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Info($"[AssetModifier]: Failed to find CompositeTrailRenderer type: {ex.Message}");
                }
            }

            if (_compositeTrailRendererType == null)
                return false;

            var compositeTrail = child.GetComponent(_compositeTrailRendererType);
            if (compositeTrail == null) return false;

            if (config.EnableDetailedLogging)
            {
                Logger.Info($"[AssetModifier]:   - Found CompositeTrailRenderer");
                Logger.Info($"[AssetModifier]:     • type: {compositeTrail.GetType().Name}");
            }

            try
            {
                if (!config.ProcessMaterialsOnly)
                {
                    var multiplyColorProperty = _compositeTrailRendererType.GetProperty("MultiplyColor",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    
                    var multiplyColorField = _compositeTrailRendererType.GetField("MultiplyColor",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                    if (multiplyColorProperty != null && multiplyColorProperty.CanWrite)
                    {
                        var oldColor = (Color)multiplyColorProperty.GetValue(compositeTrail);
                        var newColor = Colors.ReplaceRGB(oldColor, color);
                        multiplyColorProperty.SetValue(compositeTrail, newColor);
                        
                        if (config.EnableDetailedLogging)
                            Logger.Info($"[AssetModifier]:     • MultiplyColor (property): {ColorToString(oldColor)} → {ColorToString(newColor)}");
                    }
                    else if (multiplyColorField != null)
                    {
                        var oldColor = (Color)multiplyColorField.GetValue(compositeTrail);
                        var newColor = Colors.ReplaceRGB(oldColor, color);
                        multiplyColorField.SetValue(compositeTrail, newColor);
                        
                        if (config.EnableDetailedLogging)
                            Logger.Info($"[AssetModifier]:     • MultiplyColor (field): {ColorToString(oldColor)} → {ColorToString(newColor)}");
                    }
                    else if (config.EnableDetailedLogging)
                    {
                        Logger.Info($"[AssetModifier]:     • MultiplyColor property/field not found");
                    }
                }

                // Получаем материал через рефлексию
                var materialProperty = _compositeTrailRendererType.GetProperty("Material",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                var materialField = _compositeTrailRendererType.GetField("Material",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                var materialFieldPrivate = _compositeTrailRendererType.GetField("m_Material",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                Material material = null;

                if (materialProperty != null)
                {
                    material = materialProperty.GetValue(compositeTrail) as Material;
                    if (config.EnableDetailedLogging)
                        Logger.Info($"[AssetModifier]:     • Material found via property 'Material'");
                }
                else if (materialField != null)
                {
                    material = materialField.GetValue(compositeTrail) as Material;
                    if (config.EnableDetailedLogging)
                        Logger.Info($"[AssetModifier]:     • Material found via field 'Material'");
                }
                else if (materialFieldPrivate != null)
                {
                    material = materialFieldPrivate.GetValue(compositeTrail) as Material;
                    if (config.EnableDetailedLogging)
                        Logger.Info($"[AssetModifier]:     • Material found via private field 'm_Material'");
                }
                else
                {
                    if (config.EnableDetailedLogging)
                    {
                        Logger.Info($"[AssetModifier]:     • Material property/field not found. Available members:");
                        var allMembers = _compositeTrailRendererType.GetMembers(
                            System.Reflection.BindingFlags.Public |
                            System.Reflection.BindingFlags.NonPublic |
                            System.Reflection.BindingFlags.Instance);
                        foreach (var member in allMembers)
                        {
                            if (member.Name.Contains("Material") || member.Name.Contains("material") || member.Name.Contains("Color"))
                                Logger.Info($"[AssetModifier]:       - {member.MemberType}: {member.Name}");
                        }
                    }
                    return false;
                }

                if (material != null)
                {
                    if (config.EnableDetailedLogging)
                        Logger.Info($"[AssetModifier]:     • Processing CompositeTrailRenderer material: '{material.name}' (shader: {material.shader.name})");

                    ProcessMaterial(material, color, 2f, config);
                    return true;
                }
                else if (config.EnableDetailedLogging)
                {
                    Logger.Info($"[AssetModifier]:     • Material is null");
                }
            }
            catch (Exception ex)
            {
                if (config.EnableLogging)
                    Logger.Info($"[AssetModifier]:     • Exception accessing CompositeTrailRenderer: {ex.Message}\n{ex.StackTrace}");
            }

            return false;
        }

        

        private static bool ProcessAnimatedLight(Transform child, Color color, ModifierConfig config)
        {
            // Инициализируем тип только один раз
            if (_animatedLightType == null)
            {
                try
                {
                    // Простой прямой поиск в Assembly-CSharp
                    var assembly = AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
            
                    if (assembly != null)
                    {
                        // Ищем тип AnimatedLight без namespace
                        _animatedLightType = assembly.GetType("AnimatedLight");
                        
                        if (_animatedLightType == null)
                        {
                            // Пробуем найти через полный перебор типов
                            var allTypes = assembly.GetTypes();
                            Logger.Info($"[AssetModifier]: Searching through {allTypes.Length} types in Assembly-CSharp");
                            
                            foreach (var type in allTypes)
                            {
                                if (type.Name.Equals("AnimatedLight", StringComparison.OrdinalIgnoreCase))
                                {
                                    _animatedLightType = type;
                                    Logger.Info($"[AssetModifier]: ✓ AnimatedLight found: {type.FullName}");
                                    break;
                                }
                            }
                        }
                        else
                        {
                            Logger.Info($"[AssetModifier]: ✓ AnimatedLight found directly: {_animatedLightType.FullName}");
                        }
                    }
            
                    if (_animatedLightType == null)
                    {
                        Logger.Info($"[AssetModifier]: ✗ AnimatedLight type NOT FOUND");
                        
                        // ДИАГНОСТИКА: Логируем все компоненты на "Point light"
                        Logger.Info($"[AssetModifier]: Components on '{child.name}':");
                        var components = child.GetComponents<Component>();
                        foreach (var comp in components)
                        {
                            if (comp != null)
                            {
                                var compType = comp.GetType();
                                Logger.Info($"[AssetModifier]:   • {compType.FullName ?? compType.Name}");
                                
                                // Если это MonoBehaviour, показываем все поля с Color
                                if (comp is MonoBehaviour)
                                {
                                    var fields = compType.GetFields(
                                        System.Reflection.BindingFlags.Public | 
                                        System.Reflection.BindingFlags.NonPublic | 
                                        System.Reflection.BindingFlags.Instance);
                                    
                                    var colorFields = fields.Where(f => f.FieldType == typeof(Color)).ToList();
                                    if (colorFields.Any())
                                    {
                                        Logger.Info($"[AssetModifier]:     Color fields:");
                                        foreach (var field in colorFields)
                                        {
                                            var value = field.GetValue(comp);
                                            Logger.Info($"[AssetModifier]:       - {field.Name}: {ColorToString((Color)value)}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Info($"[AssetModifier]: Exception finding AnimatedLight: {ex.Message}\n{ex.StackTrace}");
                }
            }

            if (_animatedLightType == null)
                return false;

            var animatedLight = child.GetComponent(_animatedLightType);
            if (animatedLight == null) return false;

            Logger.Info($"[AssetModifier]:   - ✓ Found AnimatedLight component");

            if (config.ProcessMaterialsOnly)
            {
                if (config.EnableDetailedLogging)
                    Logger.Info($"[AssetModifier]:     • Skipped (ProcessMaterialsOnly mode)");
                return false;
            }

            try
            {
                bool modified = false;
                
                // Получаем ВСЕ поля типа Color
                var allFields = _animatedLightType.GetFields(
                    System.Reflection.BindingFlags.Public | 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance);
                
                var colorFields = allFields.Where(f => f.FieldType == typeof(Color)).ToList();
                
                Logger.Info($"[AssetModifier]:     Found {colorFields.Count} Color fields");
                
                foreach (var field in colorFields)
                {
                    var oldColor = (Color)field.GetValue(animatedLight);
                    var newColor = Colors.ReplaceRGB(oldColor, color);
                    field.SetValue(animatedLight, newColor);
                    
                    Logger.Info($"[AssetModifier]:     • {field.Name}: {ColorToString(oldColor)} → {ColorToString(newColor)}");
                    modified = true;
                }
                
                // Также проверяем свойства
                var allProperties = _animatedLightType.GetProperties(
                    System.Reflection.BindingFlags.Public | 
                    System.Reflection.BindingFlags.Instance);
                
                var colorProperties = allProperties.Where(p => p.PropertyType == typeof(Color) && p.CanWrite).ToList();
                
                if (colorProperties.Any())
                {
                    Logger.Info($"[AssetModifier]:     Found {colorProperties.Count} Color properties");
                    
                    foreach (var prop in colorProperties)
                    {
                        var oldColor = (Color)prop.GetValue(animatedLight);
                        var newColor = Colors.ReplaceRGB(oldColor, color);
                        prop.SetValue(animatedLight, newColor);
                        
                        Logger.Info($"[AssetModifier]:     • {prop.Name} (property): {ColorToString(oldColor)} → {ColorToString(newColor)}");
                        modified = true;
                    }
                }
                
                if (!modified)
                {
                    Logger.Info($"[AssetModifier]:     ✗ No Color fields/properties found to modify");
                }
                
                return modified;
            }
            catch (Exception ex)
            {
                Logger.Info($"[AssetModifier]:     ✗ Exception: {ex.Message}\n{ex.StackTrace}");
            }

            return false;
        }
    }
}
*/