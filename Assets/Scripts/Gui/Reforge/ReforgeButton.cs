using Gui.Theme.Wrappers;
using Services.Localization;
using Services.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Gui.Reforge
{
    class ReforgeButton : MonoBehaviour
    {
        [Inject] private readonly IResourceLocator _resourceLocator;
        [Inject] private readonly ILocalization _localization;

        private static Dictionary<ReforgeStyle, string> StyleIconMap = new Dictionary<ReforgeStyle, string> {
            {ReforgeStyle.RandomAll, "Textures/GUI/scrap_icon" },
            {ReforgeStyle.RandomModification, "Icons/icon_refresh" },
            {ReforgeStyle.UpgradeThenRandom, "Textures/StarMap/base" },
        };

        private static Dictionary<ReforgeStyle, string> StyleTextMap = new Dictionary<ReforgeStyle, string> {
            {ReforgeStyle.RandomAll, "$ReforgeAll" },
            {ReforgeStyle.RandomModification, "$ReforgeModification" },
            {ReforgeStyle.UpgradeThenRandom, "$ReforgeUpgrade" },
        };

        public ThemedImage Icon;
        public ThemedText Text;

        public void UpdateReforgeStyle(ReforgeStyle reforgeStyle)
        {
            Icon.sprite = _resourceLocator.GetSprite(StyleIconMap[reforgeStyle]);
            Text.text = _localization.GetString(StyleTextMap[reforgeStyle]);
        }
    }
}
