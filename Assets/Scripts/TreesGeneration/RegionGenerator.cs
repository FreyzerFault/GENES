using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DavidUtils;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry;
using DavidUtils.Geometry.Generators;
using TreesGeneration.Rendering;
using UnityEngine;

namespace TreesGeneration
{
    // Generación de una región a partir del Voronoi
    // De esto heredan todas los generadores más específicos con las reglas de cada tipo de "bioma"
    public class RegionGenerator : VoronoiGenerator
    {
        [Space]
        [Header("GENERACIÓN DE REGIONES")]
        [SerializeField] protected Polygon[] regions;
        
        // SETTINGS
        [Space]
        [Header("SETTINGS")]
        public GenerationSettingsSO generationSettings;

        public OliveGenSettings oliveSettings => generationSettings.OliveGenSettings;
        public ForestGenSettings forestSettings => generationSettings.ForestGenSettings;
        
        protected readonly Dictionary<Polygon, RegionData> regionsData = new();
        
        public RegionData[] Data => regionsData.Values.ToArray();
        public OliveRegionData[] OliveData => regionsData.Values.OfType<OliveRegionData>().ToArray();
        public ForestRegionData[] ForestData => regionsData.Values.OfType<ForestRegionData>().ToArray();

        [SerializeField] private int numRegions;
        public int NumRegions
        {
            get => numRegions;
            set
            {
                numRegions = value;
                regions = new Polygon[numRegions];
            }
        }


        public Action<RegionData[]> OnEndedGeneration;
        public Action<RegionData> OnRegionPopulated;
        public Action OnClear;
        
        
        #region GENERATION PIPELINE

        public int iterations;
        private const int MaxIterations = 1000;
        
        public bool Ended => regionsData.Count >= numRegions || iterations > MaxIterations;
        
        private bool _animatedPopulation = true;
        public bool AnimatedPopulation
        {
            get => _animatedPopulation;
            set => _animatedPopulation = value;
        }
        
        public override void Reset()
        {
            base.Reset();
            ResetRegions();
            Renderer.Clear();
        }

        protected  void ResetRegions()
        {
            regions = Array.Empty<Polygon>();
            regionsData.Clear();
            iterations = 0;
            OnClear?.Invoke();
        }

        public override void Run()
        {
            ResetDelaunay();
            ResetVoronoi();
            ResetRegions();
            if (_animatedPopulation)
            {
                animationCoroutine = StartCoroutine(RunCoroutine());
            }
            else
            {
                delaunay.RunTriangulation();
                voronoi.GenerateVoronoi();
                
                DefineRegions();
                OnAllPolygonsCreated();
                
                PopulateAllRegions();
            }
        }
        
        protected override void Run_OneIteration()
        {
            if (!delaunay.ended)
                delaunay.Run_OnePoint();
            else if (!voronoi.Ended)
                voronoi.Run_OneIteration();
            else if (!Ended)
            {
                // Select random polygon not generated
                var notGeneratedRegions = Polygons.Where(p => !regions.Contains(p));
                Polygon regionPolygon = notGeneratedRegions.PickRandom();
                
                // Select random biome
                
                PopulateRegion(regionPolygon, generationSettings.GetRandomType());
                iterations++;
            }
        }
        

        public override IEnumerator RunCoroutine()
        {
            yield return base.RunCoroutine();

            ResetRegions();
            
            DefineRegions();
            OnAllPolygonsCreated();
            
            if (_animatedPopulation)
            {
                while (!Ended)
                {
                    Run_OneIteration();
                    yield return new WaitForSecondsRealtime(DelaySeconds);
                }

                OnEndedGeneration?.Invoke(regionsData.Values.ToArray());
            }
            else
            {
                PopulateAllRegions();
            }
        }

        #endregion


        #region REGIONS CREATION

        /// <summary>
        /// Mezcla Regiones vecinas para tener mas variedad y complejidad, con poligonos concavos
        /// </summary>
        private void DefineRegions()
        {
            //TODO
            regions = Polygons;
        }

        #endregion


        #region REGION POPULATION

        private void PopulateAllRegions()
        {
            Polygons.ForEach(r => PopulateRegion(r, generationSettings.GetRandomType()));
            OnEndedGeneration?.Invoke(regionsData.Values.ToArray());
        }

        protected RegionData PopulateRegion(Polygon region, RegionType type)
        {
            // TODO Elegir un tipo de Region
            
            OliveRegionData data = OliveGroveGenerator.PopulateRegion(region, BoundsComp, oliveSettings);

            regionsData.Add(region, data);

            OnRegionPopulated?.Invoke(data);

            return data;
        }

        #endregion


        #region RENDERING

        [SerializeField] private bool _drawRegions = true;

        [SerializeField]
        private RegionsRenderer regionsRenderer;
        private RegionsRenderer Renderer => regionsRenderer ??= GetComponentInChildren<RegionsRenderer>(true);

        protected override void InitializeRenderer()
        {
            base.InitializeRenderer();
            regionsRenderer ??= Renderer ?? UnityUtils.InstantiateObject<RegionsRenderer>(transform, "Regions Renderer");
            regionsRenderer.ProjectedOnTerrain = true;
            regionsRenderer.regionGenerator = this;
        }

        protected override void UpdateRenderer()
        {
            base.UpdateRenderer();
            Renderer.UpdateAllRegions(Data);
        }


        protected override void PositionRenderer()
        {
            base.PositionRenderer();
            if (Renderer == null) return;
            BoundsComp.TransformToBounds_Local(Renderer);
            Renderer.transform.localPosition += Vector3.up * 1f;
        }


        #endregion


#region DEBUG

        #if UNITY_EDITOR

        [SerializeField] private bool drawOBBs = false;

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            if (!drawGizmos || Data.IsNullOrEmpty()) return;
            
            OliveGroveGenerator.DrawGizmos(Data, BoundsComp.LocalToWorldMatrix_WithXZrotation, drawOBBs);
        }

#endif

        #endregion
    }
}
