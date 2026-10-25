using Kingmaker.Visual.Trails;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace SpellsRecolorMod
{
    internal class Colors
    {
        public static readonly ElementColors Fire = new ElementColors
        {
            Primary = new Color(1f, 0.6f, 0.2f, 1f),      // Ярко-оранжевый
            Secondary = new Color(1f, 0.8f, 0f, 1f),    // Жёлтый
            Accent = new Color(0.8f, 0f, 0f, 1f),       // Тёмно-красный
            Glow = new Color(1f, 0.6f, 0.2f, 1f)        // Светло-оранжевый
        };

        public static readonly ElementColors Water = new ElementColors
        {
            Primary = new(0.8f, 0.9f, 1f, 1f),    // Светло-голубой
            Secondary = new Color(0.5f, 1f, 1f, 1f),  // Ледяной голубой
            Accent = new Color(0.2f, 0.5f, 0.8f, 1f),   // Тёмно-синий
            Glow = new Color(0.8f, 1f, 1f, 1f)          // Белый с голубизной
        };

        public static readonly ElementColors Air = new ElementColors
        {
            Primary = new Color(0.3f, 0.78f, 1f, 1f),    // Электрический голубой
            Secondary = new Color(1f, 1f, 0.8f, 1f),    // Белый с жёлтым
            Accent = new Color(0.4f, 0.4f, 1f, 1f),     // Фиолетовый
            Glow = new Color(0.9f, 0.95f, 1f, 1f)       // Яркий белый
        };

        public static readonly ElementColors Acid = new ElementColors
        {
            Primary = new Color(0.43f, 0.86f, 0.43f, 1f),  // Зелёный
            Secondary = new Color(0.6f, 1f, 0.3f, 0.2f),  // Светло-зелёный
            Accent = new Color(0.4f, 0.6f, 0.2f, 1f),   // Оливковый
            Glow = new Color(0.8f, 1f, 0.6f, 1f)        // Светящийся зелёный
        };
        public static readonly ElementColors Death = new ElementColors
        {
            Primary = new Color(0.01f, 0.4f, 0.4f, 1f),  // Бледно зелёный
            Secondary = new Color(0.1f, 0.3f, 0.2f, 1f),  // Светло-зелёный
            Accent = new Color(0.4f, 0.6f, 0.2f, 1f),   // Оливковый
            Glow = new Color(0.8f, 1f, 0.6f, 1f)        // Светящийся зелёный
        };
        public static readonly ElementColors Holy = new ElementColors
        {
            Primary = new Color(0.9f, 0.8f, 0.4f, 1f),  // Светло жёлтый
            Secondary = new Color(1f, 1f, 0.7f, 1f),  // Светло-зелёный
            Accent = new Color(0.4f, 0.6f, 0.2f, 1f),   // Оливковый
            Glow = new Color(0.8f, 1f, 0.6f, 1f)        // Светящийся зелёный
        };

        public struct ElementColors
        {
            public Color Primary;    // Основной цвет
            public Color Secondary;  // Вторичный цвет
            public Color Accent;     // Акцентный цвет
            public Color Glow;       // Цвет свечения
        }

        /// <summary>
        /// Заменяет RGB компоненты цвета, сохраняя альфа-канал
        /// </summary>
        public static Color ReplaceRGB(Color original, Color newRGB)
        {
            return new Color(newRGB.r, newRGB.g, newRGB.b, original.a);
        }

        /// <summary>
        /// Умножает только RGB компоненты на множитель, сохраняя альфа-канал
        /// </summary>
        public static Color MultiplyRGB(Color color, float multiplier)
        {
            return new Color(
                color.r * multiplier,
                color.g * multiplier,
                color.b * multiplier,
                color.a
            );
        }

        /// <summary>
        /// Умножает только RGB компоненты на множитель, сохраняя альфа-канал исходного цвета
        /// </summary>
        public static Color MultiplyRGBKeepAlpha(Color original, Color colorToMultiply, float multiplier)
        {
            return new Color(
                colorToMultiply.r * multiplier,
                colorToMultiply.g * multiplier,
                colorToMultiply.b * multiplier,
                original.a
            );
        }
        
        public static Color[] CreateSmoothRamp(int resolution, params (Color color, float position)[] keyPoints)
        {
            if (keyPoints == null || keyPoints.Length == 0)
                return Enumerable.Repeat(Color.white, resolution).ToArray();

            // Сортируем точки по позиции
            var sortedPoints = keyPoints.OrderBy(kp => kp.position).ToArray();
            
            Color[] ramp = new Color[resolution];

            for (int i = 0; i < resolution; i++)
            {
                float t = i / (float)(resolution - 1); // normalized position (0-1)

                // Находим две ближайшие точки для интерполяции
                (Color color, float position) before = sortedPoints[0]; // По умолчанию первая точка
                (Color color, float position) after = sortedPoints[1];  // По умолчанию последняя точка
                
                // Ищем точку "до" текущей позиции
                for (int j = 0; j < sortedPoints.Length; j++)
                {
                    if (sortedPoints[j].position <= t)
                        before = sortedPoints[j];
                    else
                        break;
                }
                
                // Ищем точку "после" текущей позиции
                for (int j = sortedPoints.Length - 1; j >= 0; j--)
                {
                    if (sortedPoints[j].position >= t)
                        after = sortedPoints[j];
                    else
                        break;
                }

                // Интерполяция
                if (before.position == after.position)
                {
                    // Точное попадание на ключевую точку
                    ramp[i] = before.color;
                }
                else
                {
                    // Интерполяция между двумя точками
                    float localT = (t - before.position) / (after.position - before.position);
                    ramp[i] = Color.Lerp(before.color, after.color, localT);
                }
            }

            return ramp;
        }

        public static CompositeTrailRenderer a = new();
        
        public static class ColorRampPresets
        {            
            
            /// <summary>
            /// Огненный эффект (красный → оранжевый → желтый)
            /// </summary>
            public static Color[] FireRamp(int resolution = 256)
            {
                return CreateSmoothRamp(resolution,
                    (new Color(0.2f, 0f, 0f, 1f), 0f),      // Dark-red, transparent
                    (new Color(1f, 0f, 0f, 1f), 0.3f),    // Red
                    (new Color(1f, 0.5f, 0f, 1f), 0.6f),    // Orange
                    (new Color(1f, 1f, 0.5f, 1f), 1f)       // Yellow
                );
            }
            public static Color[] RedFull(int resolution = 256)
            {
                return CreateSmoothRamp(resolution,
                    (new Color(1f, 0f, 0f, 1f), 0f),      // Dark-red, transparent
                    (new Color(1f, 0f, 0f, 1f), 0.3f),    // Red
                    (new Color(1f, 0f, 0f, 1f), 0.6f),    // Orange
                    (new Color(1f, 0f, 0f, 1f), 1f)       // Yellow
                );
            }
            public static Color[] WhiteTotal(int resolution = 256)
            {
                return CreateSmoothRamp(resolution,
                    (new Color(1f, 1f, 1f, 1f), 0f),      // Начало
                    (new Color(1f, 1f, 1f, 1f), 1f)       // Конец
                );
            }

            public static CompositeTrailRenderer a = new();
            
            /// <summary>
            /// Ледяной эффект (голубой → белый)
            /// </summary>
            public static Color[] IceRamp(int resolution = 256)
            {
                return CreateSmoothRamp(resolution,
                    (new Color(0f, 0.3f, 0.5f, 0f), 0f),    // Темно-синий, прозрачный
                    (new Color(0f, 0.7f, 1f, 0.7f), 0.4f),  // Голубой
                    (new Color(0.7f, 0.9f, 1f, 1f), 1f)     // Светло-голубой
                );
            }

            /// <summary>
            /// Электрический эффект (синий → фиолетовый → белый)
            /// </summary>
            public static Color[] LightningRamp(int resolution = 256)
            {
                return CreateSmoothRamp(resolution,
                    (new Color(0f, 0f, 0.5f, 0f), 0f),      // Темно-синий, прозрачный
                    (new Color(0.3f, 0f, 1f, 0.8f), 0.4f),  // Фиолетовый
                    (new Color(0.7f, 0.5f, 1f, 1f), 0.7f),  // Светло-фиолетовый
                    (new Color(1f, 1f, 1f, 1f), 1f)         // Белый
                );
            }

            public static Color[] TestRamp(int resolution = 256)
            {
                return CreateSmoothRamp(resolution,
                    (new Color(0.5f, 0.5f, 0.5f, 0.5f), 0f),      // Темно-синий, прозрачный
                    (new Color(0.5f, 0.5f, 0.5f, 0.5f), 0.4f),  // Фиолетовый
                    (new Color(0.5f, 0.5f, 0.5f, 0.5f), 0.7f),  // Светло-фиолетовый
                    (new Color(0.5f, 0.5f, 0.5f, 0.5f), 1f)         // Белый
                );
            }

            /// <summary>
            /// Ядовитый эффект (темно-зеленый → ярко-зеленый)
            /// </summary>
            public static Color[] PoisonRamp(int resolution = 256)
            {
                return CreateSmoothRamp(resolution,
                    (new Color(0f, 0.2f, 0f, 0f), 0f),      // Темно-зеленый, прозрачный
                    (new Color(0f, 0.8f, 0f, 0.7f), 0.5f),  // Зеленый
                    (new Color(0.5f, 1f, 0f, 1f), 1f)       // Желто-зеленый
                );
            }

            /// <summary>
            /// Эффект затухания (полная непрозрачность → полная прозрачность)
            /// </summary>
            public static Color[] FadeOutRamp(Color baseColor, int resolution = 256)
            {
                Color[] ramp = new Color[resolution];
                for (int i = 0; i < resolution; i++)
                {
                    float alpha = 1f - (i / (float)(resolution - 1));
                    ramp[i] = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                }
                return ramp;
            }

            /// <summary>
            /// Эффект вспышки (прозрачность → яркость → прозрачность)
            /// </summary>
            public static Color[] FlashRamp(Color baseColor, int resolution = 256)
            {
                Color[] ramp = new Color[resolution];
                for (int i = 0; i < resolution; i++)
                {
                    float t = i / (float)(resolution - 1);
                    float alpha = Mathf.Sin(t * Mathf.PI); // Синусоидальная кривая
                    ramp[i] = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                }
                return ramp;
            }
        }

    }

}
