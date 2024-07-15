using DavidUtils.Rendering;

namespace GENES.TreesGeneration.Rendering
{
    public class ForestRegionRenderer : RegionRenderer
    {
        private ForestRegionData ForestData => (ForestRegionData) Data;
        
        private PointsRenderer _treesRenderer;
        
        protected override void Awake()
        {
            base.Awake();
            
            _treesRenderer = GetComponent<PointsRenderer>() ?? gameObject.AddComponent<PointsRenderer>();
        }

        protected override void UpdateData()
        {
            // TODO Set Tree Points
            // _treesRenderer.UpdateAllObj(ForestData.);
            UpdateRadius();
        }

        private void UpdateRadius()
        {
            // TODO Set each Tree Radius
            // _treesRenderer.RadiusByPoint = ForestData.radiusByPoint;
        }

        public override void Clear()
        {
            base.Clear();
            _treesRenderer.Clear();
        }

        public override void ProjectOnTerrain(float offset = 0.1f, bool scaleToTerrainBounds = true)
        {
            base.ProjectOnTerrain(offset, scaleToTerrainBounds);
            _treesRenderer.ProjectedOnTerrain = true;
        }
    }
}
