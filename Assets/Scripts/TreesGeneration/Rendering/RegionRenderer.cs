using DavidUtils.Rendering;

namespace GENES.TreesGeneration.Rendering
{
    public abstract class RegionRenderer : PolygonRenderer
    {
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

        protected abstract void UpdateData();
    }
}
