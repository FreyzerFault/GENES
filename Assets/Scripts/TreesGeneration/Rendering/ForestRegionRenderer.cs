using DavidUtils.Rendering;

namespace GENES.TreesGeneration.Rendering
{
    public class ForestRegionRenderer : TreesRegionRenderer
    {
        private ForestRegionData ForestData => (ForestRegionData) Data;
        
        // TODO Concretar implementacion de un bosque por tipo de arbol
        
        protected override void UpdateData()
        {
            base.UpdateData();
            
        }
    }
}
