using BlueprintCore.Utils;
using Kingmaker.ResourceLinks;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SpellsRecolorMod
{
    internal static class AssetModifier
    {
        private static readonly LogWrapper Logger = LogWrapper.Get("AssetModifier");
        private static readonly Dictionary<string, Texture2D> TextureCache = new();
        private static Type _compositeTrailRendererType;
        private static Type _animatedLightType;
        private static Type _fxDecalType;

        // Кэш для рефлексии
        private static PropertyInfo _compositeMultiplyColorProperty;
        private static FieldInfo _compositeMultiplyColorField;
        private static PropertyInfo _compositeMaterialProperty;
        private static FieldInfo _compositeMaterialField;
        private static FieldInfo _compositeMaterialFieldPrivate;
        private static FieldInfo[] _animatedLightColorFields;
        private static PropertyInfo[] _animatedLightColorProperties;
        private static FieldInfo _fxDecalDefaultAlbedoColorField;
        private static FieldInfo _fxDecalMaterialField;
        private static bool _reflectionInitialized;

        // Кэш для свойств материалов
        private static readonly string[] StandardMaterialProperties = {
            "_Color", "_MultiplyColor", "_TintColor", "_EmissionColor", "_DefaultAlbedoColor", "_GlowColor", "_RimColor", "_CoreColor",
            "_MainColor", "_SpecColor", "_ReflectColor", "_OutlineColor", "_ShadowColor",
            "_HighlightColor", "_FresnelColor", "_AmbientColor", "_DiffuseColor", "_SubsurfaceColor",
            "_TransmissionColor", "_CenterColor", "_EdgeColor", "_InnerColor", "_OuterColor",
            "_BaseColor", "_ColorTint", "_MainTint", "_OverlayColor", "_BackColor", "_FrontColor"
        };

        private static readonly string[] TextureProperties = {
            "_MainTex", "_BumpMap", "_EmissionMap", "_MetallicGlossMap", "_OcclusionMap",
            "_ParallaxMap", "_DetailMask", "_DetailAlbedoMap", "_DetailNormalMap", "_MultiplyColor", "_DefaultAlbedoColor"
        };

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
            public bool EnableDetailedLogging { get; set; } = true;
            public bool UseTextureCache { get; set; } = true;
            public Color[] CustomColorRamp { get; set; } = null;
            public bool ProcessCompositeTrailRenderers { get; set; } = true;
            public bool ProcessAnimatedLights { get; set; } = true;
            public bool ProcessDecals { get; set; } = true;
        }

        static AssetModifier()
        {
            InitializeReflection();
        }

        private static void InitializeReflection()
        {
            if (_reflectionInitialized) return;

            try
            {
                // CompositeTrailRenderer
                _compositeTrailRendererType = Type.GetType("Kingmaker.Visual.Trails.CompositeTrailRenderer, Assembly-CSharp");
                if (_compositeTrailRendererType == null)
                {
                    var assembly = Array.Find(AppDomain.CurrentDomain.GetAssemblies(),
                        a => a.GetName().Name == "Assembly-CSharp");
                    _compositeTrailRendererType = assembly?.GetType("Kingmaker.Visual.Trails.CompositeTrailRenderer");
                }

                if (_compositeTrailRendererType != null)
                {
                    const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                    _compositeMultiplyColorProperty = _compositeTrailRendererType.GetProperty("MultiplyColor", flags);
                    _compositeMultiplyColorField = _compositeTrailRendererType.GetField("MultiplyColor", flags);
                    _compositeMaterialProperty = _compositeTrailRendererType.GetProperty("Material", flags);
                    _compositeMaterialField = _compositeTrailRendererType.GetField("Material", flags);
                    _compositeMaterialFieldPrivate = _compositeTrailRendererType.GetField("m_Material", flags);
                }

                // AnimatedLight
                var assembly2 = Array.Find(AppDomain.CurrentDomain.GetAssemblies(),
                    a => a.GetName().Name == "Assembly-CSharp");
                if (assembly2 != null)
                {
                    _animatedLightType = assembly2.GetType("AnimatedLight");
                    if (_animatedLightType != null)
                    {
                        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                        var allFields = _animatedLightType.GetFields(flags);
                        _animatedLightColorFields = Array.FindAll(allFields, f => f.FieldType == typeof(Color));

                        var allProps = _animatedLightType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        _animatedLightColorProperties = Array.FindAll(allProps, p => p.PropertyType == typeof(Color) && p.CanWrite);
                    }
                    // FxDecal
                    _fxDecalType = assembly2.GetType("Kingmaker.Visual.Decals.FxDecal");
                    if (_fxDecalType != null)
                    {
                        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                        _fxDecalDefaultAlbedoColorField = _fxDecalType.GetField("m_DefaultAlbedoColor", flags);
                        _fxDecalMaterialField = _fxDecalType.GetField("m_Material", flags);
                    }
                }

                _reflectionInitialized = true;
            }
            catch (Exception ex)
            {
                Logger.Info($"[AssetModifier]: Reflection initialization failed: {ex.Message}");
            }
        }

        public static void ModifyFx(GameObject fx, Color color) => ModifyFx(fx, color, new ModifierConfig());
        public static void ModifyFxZero(GameObject fx)
        { 
        }        

        public static void ModifyFx(GameObject fx, Color color, ModifierConfig config)
        {
            if (fx == null)
            {
                if (config.EnableLogging) Logger.Info("[AssetModifier]: ERROR - GameObject is null");
                return;
            }

            if (config.EnableDetailedLogging)
                Logger.Info($"[AssetModifier]: Starting modification of '{fx.name}' with color {ColorToString(color)}");

            int totalChildren = 0, processedCount = 0, filteredCount = 0;
            var transforms = fx.GetComponentsInChildren<Transform>(true);

            foreach (var child in transforms)
            {
                totalChildren++;

                if (config.EnableDetailedLogging)
                    Logger.Info($"[AssetModifier]: Processing child [{totalChildren}]: '{child.name}'");

                if (config.NameFilter != null && !config.NameFilter(child.name))
                {
                    filteredCount++;
                    continue;
                }

                bool modified = ProcessTransformComponents(child, color, config);

                if (modified)
                {
                    processedCount++;
                    if (config.EnableDetailedLogging)
                        Logger.Info("[AssetModifier]:   - ✓ Modified");
                }
            }

            if (config.EnableLogging)
                Logger.Info($"[AssetModifier]: Modified {processedCount}/{totalChildren} (filtered: {filteredCount})");
        }

        private static bool ProcessTransformComponents(Transform child, Color color, ModifierConfig config)
        {
            bool modified = false;

            if (config.ProcessParticleSystems) modified |= ProcessParticleSystem(child, color, config);
            if (config.ProcessRenderers) modified |= ProcessRenderer(child, color, config);
            if (config.ProcessTrailRenderers) modified |= ProcessTrailRenderer(child, color, config);
            if (config.ProcessLineRenderers) modified |= ProcessLineRenderer(child, color, config);
            if (config.ProcessSpriteRenderers) modified |= ProcessSpriteRenderer(child, color, config);
            if (config.ProcessCompositeTrailRenderers) modified |= ProcessCompositeTrailRenderer(child, color, config);
            if (config.ProcessAnimatedLights) modified |= ProcessAnimatedLight(child, color, config);
            if (config.ProcessLights) modified |= ProcessLight(child, color, config);
            if (config.ProcessDecals) modified |= ProcessDecal(child, color, config);

            return modified;
        }

        private static bool ProcessParticleSystem(Transform child, Color color, ModifierConfig config)
        {
            var ps = child.GetComponent<ParticleSystem>();
            if (ps == null) return false;

            if (config.EnableDetailedLogging)
                Logger.Info("[AssetModifier]:   - Found ParticleSystem");

            if (!config.ProcessMaterialsOnly)
            {
                var main = ps.main;
                main.startColor = CreateMinMaxGradient(main.startColor, color);

                if (ps.colorOverLifetime.enabled)
                {
                    var col = ps.colorOverLifetime;
                    col.color = new ParticleSystem.MinMaxGradient(CreateGradient(color, true));
                }

                if (ps.colorBySpeed.enabled)
                {
                    var col = ps.colorBySpeed;
                    col.color = new ParticleSystem.MinMaxGradient(CreateGradient(color, false));
                }
            }

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer?.material != null)
            {
                ProcessMaterial(renderer.material, color, 2f, config);
            }

            return true;
        }

        private static ParticleSystem.MinMaxGradient CreateMinMaxGradient(ParticleSystem.MinMaxGradient old, Color color)
        {
            return old.mode switch
            {
                ParticleSystemGradientMode.Color => new ParticleSystem.MinMaxGradient(Colors.ReplaceRGB(old.color, color)),
                ParticleSystemGradientMode.TwoColors => new ParticleSystem.MinMaxGradient(
                    Colors.ReplaceRGB(old.colorMin, color),
                    Colors.ReplaceRGB(old.colorMax, color)),
                _ => new ParticleSystem.MinMaxGradient(color)
            };
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
                Logger.Info($"[AssetModifier]:   - Found Renderer: '{renderer.material.name}'");

            if (config.ProcessAllMaterialProperties)
                ProcessMaterialInternal(renderer.material, color, config.EmissionMultiplier, StandardMaterialProperties, config);
            else
                ProcessMaterialInternal(renderer.material, color, config.EmissionMultiplier, config.MaterialProperties.ToArray(), config);

            return true;
        }

        private static void ProcessMaterial(Material material, Color color, float emissionMultiplier, ModifierConfig config)
        {
            ProcessMaterialInternal(material, color, emissionMultiplier, StandardMaterialProperties, config);
        }

        private static void ProcessMaterialInternal(Material material, Color color, float emissionMultiplier, string[] properties, ModifierConfig config)
        {
            int modifiedProps = 0;
            foreach (var prop in properties)
            {
                if (material.HasProperty(prop))
                {
                    var currentColor = material.GetColor(prop);
                    Color colorToApply = (prop.Contains("Emission") || prop.Contains("Glow"))
                        ? Colors.MultiplyRGBKeepAlpha(currentColor, color, emissionMultiplier)
                        : Colors.ReplaceRGB(currentColor, color);

                    material.SetColor(prop, colorToApply);
                    modifiedProps++;

                    if (config.EnableDetailedLogging)
                        Logger.Info($"[AssetModifier]:       ◦ {prop}: {ColorToString(currentColor)} → {ColorToString(colorToApply)}");
                }
            }

            if (config.ProcessTextures) ProcessMaterialTextures(material, color, config);
        }

        private static void ProcessMaterialTextures(Material material, Color color, ModifierConfig config)
        {
            foreach (var texProp in TextureProperties)
            {
                if (material.HasProperty(texProp))
                {
                    var originalTexture = material.GetTexture(texProp) as Texture2D;
                    if (originalTexture != null)
                    {
                        var recoloredTexture = RecolorTexture(originalTexture, color, config);
                        if (recoloredTexture != null)
                            material.SetTexture(texProp, recoloredTexture);
                    }
                }
            }
        }

        private static Texture2D RecolorTexture(Texture2D original, Color targetColor, ModifierConfig config)
        {
            if (original == null) return null;

            try
            {
                string cacheKey = $"{original.GetInstanceID()}_{targetColor.r:F2}_{targetColor.g:F2}_{targetColor.b:F2}";
                if (config.UseTextureCache && TextureCache.TryGetValue(cacheKey, out var cached))
                    return cached;

                if (!original.isReadable)
                    original = MakeTextureReadable(original);

                if (original == null) return null;

                var recolored = new Texture2D(original.width, original.height, original.format, original.mipmapCount > 1)
                {
                    name = $"{original.name}_Recolored"
                };

                var pixels = original.GetPixels();
                var multiplier = config.TextureColorMultiplier;

                for (int i = 0; i < pixels.Length; i++)
                {
                    ref var pixel = ref pixels[i];
                    pixel.r *= targetColor.r * multiplier;
                    pixel.g *= targetColor.g * multiplier;
                    pixel.b *= targetColor.b * multiplier;
                }

                recolored.SetPixels(pixels);
                recolored.Apply();

                if (config.UseTextureCache)
                    TextureCache[cacheKey] = recolored;

                return recolored;
            }
            catch
            {
                return null;
            }
        }

        private static Texture2D MakeTextureReadable(Texture2D original)
        {
            try
            {
                var tmp = RenderTexture.GetTemporary(original.width, original.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
                Graphics.Blit(original, tmp);
                var previous = RenderTexture.active;
                RenderTexture.active = tmp;

                var readable = new Texture2D(original.width, original.height);
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

        private static bool ProcessTrailRenderer(Transform child, Color color, ModifierConfig config)
        {
            var trail = child.GetComponent<TrailRenderer>();
            if (trail == null) return false;

            if (config.EnableDetailedLogging)
                Logger.Info("[AssetModifier]:   - Found TrailRenderer");

            if (!config.ProcessMaterialsOnly)
                trail.colorGradient = CreateGradient(color, true);

            if (trail.material != null)
                ProcessMaterial(trail.material, color, 2f, config);

            return true;
        }

        private static bool ProcessLineRenderer(Transform child, Color color, ModifierConfig config)
        {
            var line = child.GetComponent<LineRenderer>();
            if (line == null) return false;

            if (config.EnableDetailedLogging)
                Logger.Info("[AssetModifier]:   - Found LineRenderer");

            if (!config.ProcessMaterialsOnly)
            {
                line.colorGradient = CreateGradient(color, false);
                line.startColor = color;
                line.endColor = color;
            }

            if (line.material != null)
                ProcessMaterial(line.material, color, 2f, config);

            return true;
        }

        private static bool ProcessSpriteRenderer(Transform child, Color color, ModifierConfig config)
        {
            var sprite = child.GetComponent<SpriteRenderer>();
            if (sprite == null) return false;

            if (config.EnableDetailedLogging)
                Logger.Info("[AssetModifier]:   - Found SpriteRenderer");

            if (!config.ProcessMaterialsOnly)
                sprite.color = color;

            if (sprite.material != null)
                ProcessMaterial(sprite.material, color, 1f, config);

            return true;
        }

        private static bool ProcessLight(Transform child, Color color, ModifierConfig config)
        {
            if (config.ProcessMaterialsOnly) return false;

            var lightComponent = child.GetComponent<Light>();
            if (lightComponent == null) return false;

            if (config.EnableDetailedLogging)
                Logger.Info($"[AssetModifier]:   - Found Light: {ColorToString(lightComponent.color)} → {ColorToString(color)}");

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

        // Preset Configs
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

        private static bool ProcessCompositeTrailRenderer(Transform child, Color color, ModifierConfig config)
        {
            if (_compositeTrailRendererType == null) return false;

            var compositeTrail = child.GetComponent(_compositeTrailRendererType);
            if (compositeTrail == null) return false;

            if (config.EnableDetailedLogging)
                Logger.Info("[AssetModifier]:   - Found CompositeTrailRenderer");

            try
            {
                // Получаем материал для доступа к ColorAlphaRamp
                Material material = null;
                material ??= _compositeMaterialProperty?.GetValue(compositeTrail) as Material;
                material ??= _compositeMaterialField?.GetValue(compositeTrail) as Material;
                material ??= _compositeMaterialFieldPrivate?.GetValue(compositeTrail) as Material;

                if (!config.ProcessMaterialsOnly && material != null)
                {
                    // Пытаемся получить оригинальный ColorAlphaRamp
                    Color targetMultiplyColor = color; // Fallback к обычному цвету

                    if (material.HasProperty("_ColorAlphaRamp"))
                    {
                        var rampTexture = material.GetTexture("_ColorAlphaRamp") as Texture2D;
                        if (rampTexture != null)
                        {
                            try
                            {
                                Color firstPixel;

                                if (rampTexture.isReadable)
                                {
                                    firstPixel = rampTexture.GetPixel(0, 0);
                                }
                                else
                                {
                                    var readable = MakeTextureReadable(rampTexture);
                                    if (readable != null)
                                    {
                                        firstPixel = readable.GetPixel(0, 0);
                                    }
                                    else
                                    {
                                        // Не удалось прочитать текстуру
                                        firstPixel = Color.white;
                                    }
                                }

                                // Вычисляем новый MultiplyColor = targetColor / firstPixel
                                // Избегаем деления на ноль
                                float newR = firstPixel.r > 0.001f ? color.r / firstPixel.r : color.r;
                                float newG = firstPixel.g > 0.001f ? color.g / firstPixel.g : color.g;
                                float newB = firstPixel.b > 0.001f ? color.b / firstPixel.b : color.b;

                                targetMultiplyColor = new Color(newR, newG, newB, color.a);

                                if (config.EnableDetailedLogging)
                                {
                                    Logger.Info($"[AssetModifier]:     • _ColorAlphaRamp first pixel: {ColorToString(firstPixel)}");
                                    Logger.Info($"[AssetModifier]:     • Calculated MultiplyColor: {ColorToString(targetMultiplyColor)}");
                                }
                            }
                            catch (Exception ex)
                            {
                                if (config.EnableDetailedLogging)
                                    Logger.Info($"[AssetModifier]:     • Failed to read _ColorAlphaRamp: {ex.Message}");
                            }
                        }
                        else if (config.EnableDetailedLogging)
                        {
                            Logger.Info("[AssetModifier]:     • _ColorAlphaRamp texture is null");
                        }
                    }
                    else if (config.EnableDetailedLogging)
                    {
                        Logger.Info("[AssetModifier]:     • Material doesn't have _ColorAlphaRamp property");
                    }

                    // Устанавливаем вычисленный MultiplyColor
                    if (_compositeMultiplyColorProperty?.CanWrite == true)
                    {
                        var oldColor = (Color)_compositeMultiplyColorProperty.GetValue(compositeTrail);
                        _compositeMultiplyColorProperty.SetValue(compositeTrail, targetMultiplyColor);

                        if (config.EnableDetailedLogging)
                            Logger.Info($"[AssetModifier]:     • MultiplyColor: {ColorToString(oldColor)} → {ColorToString(targetMultiplyColor)}");
                    }
                    else if (_compositeMultiplyColorField != null)
                    {
                        var oldColor = (Color)_compositeMultiplyColorField.GetValue(compositeTrail);
                        _compositeMultiplyColorField.SetValue(compositeTrail, targetMultiplyColor);

                        if (config.EnableDetailedLogging)
                            Logger.Info($"[AssetModifier]:     • MultiplyColor: {ColorToString(oldColor)} → {ColorToString(targetMultiplyColor)}");
                    }
                }

                if (material != null)
                {
                    ProcessMaterial(material, color, 2f, config);
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (config.EnableLogging)
                    Logger.Info($"[AssetModifier]: Exception: {ex.Message}");
            }

            return false;
        }


        private static bool ProcessAnimatedLight(Transform child, Color color, ModifierConfig config)
        {
            if (_animatedLightType == null || config.ProcessMaterialsOnly) return false;

            var animatedLight = child.GetComponent(_animatedLightType);
            if (animatedLight == null) return false;

            if (config.EnableDetailedLogging)
                Logger.Info("[AssetModifier]:   - Found AnimatedLight");

            try
            {
                bool modified = false;

                if (_animatedLightColorFields != null)
                {
                    foreach (var field in _animatedLightColorFields)
                    {
                        var oldColor = (Color)field.GetValue(animatedLight);
                        field.SetValue(animatedLight, Colors.ReplaceRGB(oldColor, color));
                        modified = true;

                        if (config.EnableDetailedLogging)
                            Logger.Info($"[AssetModifier]:     • {field.Name}: {ColorToString(oldColor)} → {ColorToString(color)}");
                    }
                }

                if (_animatedLightColorProperties != null)
                {
                    foreach (var prop in _animatedLightColorProperties)
                    {
                        var oldColor = (Color)prop.GetValue(animatedLight);
                        prop.SetValue(animatedLight, Colors.ReplaceRGB(oldColor, color));
                        modified = true;

                        if (config.EnableDetailedLogging)
                            Logger.Info($"[AssetModifier]:     • {prop.Name}: {ColorToString(oldColor)} → {ColorToString(color)}");
                    }
                }

                return modified;
            }
            catch (Exception ex)
            {
                if (config.EnableLogging)
                    Logger.Info($"[AssetModifier]: Exception: {ex.Message}");
            }

            return false;
        }

        private static bool ProcessDecal(Transform child, Color color, ModifierConfig config)
        {
            if (_fxDecalType == null) return false;

            var fxDecal = child.GetComponent(_fxDecalType);
            if (fxDecal == null) return false;

            if (config.EnableDetailedLogging)
                Logger.Info("[AssetModifier]:   - Found FxDecal");

            try
            {
                Material material = null;
                material = _fxDecalMaterialField?.GetValue(fxDecal) as Material;

                if (!config.ProcessMaterialsOnly && _fxDecalDefaultAlbedoColorField != null)
                {
                    Color targetDefaultAlbedoColor = color;

                    if (material != null && material.HasProperty("_ColorAlphaRamp"))
                    {
                        var rampTexture = material.GetTexture("_ColorAlphaRamp") as Texture2D;
                        if (rampTexture != null)
                        {
                            try
                            {
                                Color firstPixel;

                                if (rampTexture.isReadable)
                                {
                                    firstPixel = rampTexture.GetPixel(0, 0);
                                }
                                else
                                {
                                    var readable = MakeTextureReadable(rampTexture);
                                    if (readable != null)
                                    {
                                        firstPixel = readable.GetPixel(0, 0);
                                    }
                                    else
                                    {
                                        firstPixel = Color.white;
                                    }
                                }

                                float newR = firstPixel.r > 0.001f ? color.r / firstPixel.r : color.r;
                                float newG = firstPixel.g > 0.001f ? color.g / firstPixel.g : color.g;
                                float newB = firstPixel.b > 0.001f ? color.b / firstPixel.b : color.b;

                                targetDefaultAlbedoColor = new Color(newR, newG, newB, color.a);

                                if (config.EnableDetailedLogging)
                                {
                                    Logger.Info($"[AssetModifier]:     • _ColorAlphaRamp first pixel: {ColorToString(firstPixel)}");
                                    Logger.Info($"[AssetModifier]:     • Calculated DefaultAlbedoColor: {ColorToString(targetDefaultAlbedoColor)}");
                                }
                            }
                            catch (Exception ex)
                            {
                                if (config.EnableDetailedLogging)
                                    Logger.Info($"[AssetModifier]:     • Failed to read _ColorAlphaRamp: {ex.Message}");
                            }
                        }
                    }

                    try
                    {
                        var oldColor = (Color)_fxDecalDefaultAlbedoColorField.GetValue(fxDecal);

                        if (config.EnableDetailedLogging)
                            Logger.Info($"[AssetModifier]:     • Old m_DefaultAlbedoColor: {ColorToString(oldColor)}");

                        _fxDecalDefaultAlbedoColorField.SetValue(fxDecal, targetDefaultAlbedoColor);

                        var verifyColor = (Color)_fxDecalDefaultAlbedoColorField.GetValue(fxDecal);

                        if (config.EnableDetailedLogging)
                            Logger.Info($"[AssetModifier]:     • New m_DefaultAlbedoColor: {ColorToString(verifyColor)}");
                    }
                    catch (Exception ex)
                    {
                        if (config.EnableDetailedLogging)
                            Logger.Info($"[AssetModifier]:     • SetValue failed: {ex.Message}");
                    }
                }

                if (material != null)
                {
                    // КРИТИЧЕСКИ ВАЖНО: Устанавливаем HDR-цвет в материал
                    Color targetDefaultAlbedoColor = color;

                    if (!config.ProcessMaterialsOnly && _fxDecalDefaultAlbedoColorField != null)
                    {
                        if (material.HasProperty("_ColorAlphaRamp"))
                        {
                            var rampTexture = material.GetTexture("_ColorAlphaRamp") as Texture2D;
                            if (rampTexture != null)
                            {
                                try
                                {
                                    Color firstPixel = rampTexture.isReadable
                                        ? rampTexture.GetPixel(255, 0)
                                        : (MakeTextureReadable(rampTexture)?.GetPixel(255, 0) ?? Color.white);

                                    float newR = firstPixel.r > 0.001f ? color.r / firstPixel.r : color.r;
                                    float newG = firstPixel.g > 0.001f ? color.g / firstPixel.g : color.g;
                                    float newB = firstPixel.b > 0.001f ? color.b / firstPixel.b : color.b;

                                    targetDefaultAlbedoColor = new Color(newR, newG, newB, color.a);
                                }
                                catch { }
                            }
                        }

                        if (material.HasProperty("_DefaultAlbedoColor"))
                        {
                            material.SetColor("_DefaultAlbedoColor", targetDefaultAlbedoColor);

                            if (config.EnableDetailedLogging)
                                Logger.Info($"[AssetModifier]:     • Set material._DefaultAlbedoColor: {ColorToString(targetDefaultAlbedoColor)}");
                        }

                        if (material.HasProperty("_BaseColor"))
                        {
                            material.SetColor("_BaseColor", targetDefaultAlbedoColor);

                            if (config.EnableDetailedLogging)
                                Logger.Info($"[AssetModifier]:     • Set material._BaseColor: {ColorToString(targetDefaultAlbedoColor)}");
                        }
                    }

                    // Обрабатываем остальные свойства материала, НО исключаем _DefaultAlbedoColor и _BaseColor
                    var propsToProcess = StandardMaterialProperties
                        .Where(p => p != "_DefaultAlbedoColor" && p != "_BaseColor")
                        .ToArray();
    
                    ProcessMaterialInternal(material, color, 2f, propsToProcess, config);
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (config.EnableLogging)
                    Logger.Info($"[AssetModifier]: Exception in ProcessDecal: {ex.Message}");
            }

            return false;
        }
    }
}