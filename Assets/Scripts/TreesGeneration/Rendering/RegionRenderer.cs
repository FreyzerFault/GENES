using DavidUtils.ExtensionMethods;
using DavidUtils.Rendering;
using UnityEditor.Graphs;
using UnityEngine;

namespace GENES.TreesGeneration.Rendering
{
    public abstract class RegionRenderer : PolygonRenderer
    {
        private static Color OliveColor = "#948159".ToUnityColor();
        private static Color ForestColor = "#50b747".ToUnityColor();
        
        protected RegionData data;
        public virtual RegionData Data
        {
            get => data;
            set
            {
                data = value;
                UpdateData();
            }
        }

        public bool IsOlive => data is OliveRegionData;
        public bool IsForest => data is ForestRegionData;

        protected virtual void UpdateData()
        {
            Polygon = data.polygon;
            Color = data.type switch {
                RegionType.Olive => OliveColor,
                RegionType.Forest => ForestColor,
                _ => Color
            };
        }

        protected override void Awake()
        {
            if (data == null) return;
            
            UpdateData();
            UpdateAllProperties();
        }
    }
}
