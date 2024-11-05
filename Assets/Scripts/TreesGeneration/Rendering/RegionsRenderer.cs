using System;
using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.Rendering;
using UnityEngine;

namespace GENES.TreesGeneration.Rendering
{
    public class RegionsRenderer: DynamicRenderer<RegionRenderer>
    {
        protected override string DefaultChildName => "Region";

        public RegionGenerator regionGenerator;
        
        public RegionData[] Data => regionGenerator.Data;

        private bool NoData => Data == null || Data.Length == 0;

        private void OnValidate() => renderObjs.ForEach(UpdateCommonProperties);

        private void OnEnable()
        {
            regionGenerator.OnRegionPopulated += InstantiateRendererWithData;
            regionGenerator.OnClear += Clear;
            // regionGenerator.OnEndedGeneration += ;
        }

        private void OnDisable()
        {
            regionGenerator.OnRegionPopulated -= InstantiateRendererWithData;
            regionGenerator.OnClear -= Clear;
        }


        private void InstantiateRendererWithData(RegionData data)
        {
            RegionRenderer rr = null;
            switch (data.type)
            {
                case RegionType.Olive:
                    rr = InstantiateObj<OliveRegionRenderer>(objName: data.Name); 
                    break;
                case RegionType.Forest:
                    rr = InstantiateObj<ForestRegionRenderer>(objName: data.Name); 
                    break;
            }
            if (rr == null) throw new Exception("RegionRenderer not implemented for type " + data.type);
            rr.Data = data;
            renderObjs.Add(rr);
        }
        
        public void UpdateAllRegions(RegionData[] data)
        {
            Debug.Log($"Update All Regions (data: {data.Length})", this);
            if (data.Length < renderObjs.Count)
                renderObjs.RemoveRange(data.Length, renderObjs.Count - data.Length);
            else
                renderObjs.AddRange(data.Skip(renderObjs.Count).Select(_ => InstantiateObj()));
            renderObjs.ForEach((r, i) => r.Data = data[i]);
        }
        
        public void UpdateRegion(int i, RegionData data)
        {
            if (i < 0 || i >= renderObjs.Count) return;
            renderObjs[i].Data = data;
        }
        

        #region COMMON PROP

        [Range(0,1)]
        [SerializeField] private float thickness;
        public float Thickness
        {
            get => thickness;
            set
            {
                thickness = value;
                UpdateThickness();
            }
        }
        private void UpdateThickness()
        {
            renderObjs.ForEach(r => r.Thickness = thickness);
        }
        
        
        protected override void UpdateCommonProperties(RegionRenderer renderObj)
        {
            renderObj.Thickness = thickness;
        }

        #endregion

        #region INDIVIDUAL PROPS
        
        public override void UpdateColor() => 
            renderObjs.ForEach((r, i) => r.Color = GetColor(i));

        #endregion


        #region TERRAIN PROJECTION

        protected override void ProjectOnTerrain()
        {
            renderObjs.ForEach(r => r.ProjectOnTerrain());
        }

        #endregion
    }
}
