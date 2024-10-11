using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.Rendering;
using UnityEngine;

namespace GENES.TreesGeneration.Rendering
{
    [RequireComponent(typeof(PointsRenderer))]
    public class OliveRegionRenderer : TreesRegionRenderer
    {
        private OliveRegionData OliveData => (OliveRegionData) Data;

        protected override void UpdateData()
        {
            base.UpdateData();
            
            UpdatePointsColor();
        }

        #region COLORS

        private void UpdatePointsColor() =>
            pointsRenderer.Colors = ColorByType(OliveData.oliveType).ToFilledArray(OliveData.OlivosCount).ToArray();
        
        private Color ColorByType(OliveType type) =>
            OliveData.oliveType switch
            {
                OliveType.Traditional => "#627b34".ToUnityColor(),
                OliveType.Intesive => "#6cb74a".ToUnityColor(),
                OliveType.SuperIntesive => "#36b733".ToUnityColor(),
                _ => Color.white
            };

        #endregion
    }
}
