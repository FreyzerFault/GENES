using DavidUtils.Rendering;

namespace GENES.TreesGeneration.Rendering
{
    public class TreesRegionRenderer: RegionRenderer
    {
        private TreesRegionData TreeData => (TreesRegionData) Data;
        
        protected PointsRenderer pointsRenderer;
        
        protected override void Awake()
        {
            base.Awake();
            
            pointsRenderer = GetComponent<PointsRenderer>() ?? gameObject.AddComponent<PointsRenderer>();
        }
        
        protected override void UpdateData()
        {
            base.UpdateData();

            UpdatePointsPositions();
            UpdatePointsRadius();
        }
        
        private void UpdatePointsPositions() => pointsRenderer.UpdateAllObj(TreeData.treePositions);
        private void UpdatePointsRadius() => pointsRenderer.RadiusByPoint = TreeData.radiusByTree;

        
        public override void Clear()
        {
            base.Clear();
            pointsRenderer.Clear();
        }
        
        
        public override bool ProjectedOnTerrain
        {
            get => base.ProjectedOnTerrain;
            set
            {
                base.ProjectedOnTerrain = value;
                pointsRenderer.ProjectedOnTerrain = value;
            }
        }
    }
}
