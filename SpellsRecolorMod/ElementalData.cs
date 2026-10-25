using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SpellsRecolorMod
{
    internal class ElementalData
    {
        /// <summary>
        /// Структура для хранения данных об элементальных способностях
        /// </summary>
        internal class ElementalHeritageData
        {
            public string AbilityGuid { get; set; }
            public string FxGuid { get; set; }
            public string HitFxGuid { get; set; }
            public string MissFxGuid { get; set; }
            public string MissDecalFxGuid { get; set; }
            public string ElementName { get; set; }
            public Color Color { get; set; }
            public bool UseOriginalPrefab { get; set; } = false;
        }

        internal static class ElementalConstants
        {
            // Elemental heritage abilities GUIDs - основные способности стихийного наследия
            public const string ElemHerAir = "5f6315dfeb74a564f96f460d72f7206c"; // Аркан стихийного наследия - Воздух
            public const string ElemHerEarth = "94ce51ed666fc8d42830aa9fe48897f9"; // Аркан стихийного наследия - Земля
            public const string ElemHerWater = "dd484f0706325de40aee5dba15fbce45"; // Аркан стихийного наследия - Вода
            public const string ElemHerFire = "924dfcd481c0be54c959c2846b3fb7da"; // Аркан стихийного наследия - Огонь
            public const string ElemHerDeath = "2596b57f85864fd4db057bbc9e23c379"; // Death of Elements, Lich
            public const string ElemHerHoly = "47a75b1761704940a69fc3bdc1c56699"; // Perfect Mind, Gold Dragon
            public const string StormLordsResolve = "ae908f59269c54c4d83ca51a63be8db4"; // Storm Lord's Resolve
            public const string DevouringLust = "e89bc94ed3845d1489ba13dd0d651ff1"; // Devouring Lust Rod
            public const string RodOfBlizzard = "1c70e3d9e1f74c42904c044cf001fc0b"; // Rod of Mortiferous Blizzard
            public const string AmplRodOfBlizzard = "c2348d30c6a84726b116707e2ab2d47b"; // DLC 5 Amplified Rod of Mortiferous Blizzard

        }
    }
}
