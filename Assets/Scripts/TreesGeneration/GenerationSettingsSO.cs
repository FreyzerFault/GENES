using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace GENES.TreesGeneration
{
	[CreateAssetMenu(fileName = "Generation Settings", menuName = "Generation/Generation Settings")]
	public class GenerationSettingsSO : ScriptableObject
	{
		// PROBABILITIES
		[Range(0,1)] public float oliveProbability = 1;
		[Range(0,1)] public float forestProbability = 1;
		
		private int NumRegionTypes => Enum.GetValues(typeof(RegionType)).Length;
		public float[] Probabilities
		{
			get => new[] { oliveProbability, forestProbability };
			set
			{
				if (value.Length != NumRegionTypes) throw new ArgumentException($"Probabilities must have {NumRegionTypes} values");
				oliveProbability = value[0];
				forestProbability = value[1];
			}
		}
		private float[] lastNormalizedProbs = null;

		public OliveGenSettings oliveSettings;
		public ForestGenSettings forestSettings;
		
		public Dictionary<RegionType, float> regionProbabilities = new();
		public Dictionary<RegionType, TreesGenSettings> regionSettings = new();

		public float GetProbability(RegionType type) => regionProbabilities[type];
		public TreesGenSettings this[RegionType type] => regionSettings[type];

		public OliveGenSettings OliveSettings => this[RegionType.Olive] as OliveGenSettings;
		public ForestGenSettings ForestSettings => this[RegionType.Forest] as ForestGenSettings;


		private void OnValidate()
		{
			SincronizeDictionaries();
			NormalizeProbabilities();
			
			OliveSettings.NormalizeProbabilities();
		}
		

		public RegionType GetRandomType() => 
            regionProbabilities.Keys.PickByProbability(regionProbabilities.Values.ToArray());

		
		public void NormalizeProbabilities()
		{
			// Si no se normalizó nunca, se normaliza y se guarda como referencia para proximos cambios
			if (lastNormalizedProbs == null)
			{
				Probabilities = Probabilities.NormalizeProbabilities().ToArray();
				lastNormalizedProbs = Probabilities;
				return;
			}
			
			if (Mathf.Approximately(Probabilities.Sum(), 1)) return;
			
			// Si dejó de estar normalizado, se normaliza manteniendo el valor modificado
			// Buscamos el valor modificado para ignorarlo
			for (var i = 0; i < lastNormalizedProbs.Length; i++)
			{
				if (Mathf.Approximately(lastNormalizedProbs[i], Probabilities[i])) continue;
				
				Probabilities = Probabilities.NormalizeProbabilities(i).ToArray();
				lastNormalizedProbs = Probabilities;
				return;
			}
			
			SincronizeDictionaries();
		}

		private void SincronizeDictionaries()
		{
			regionProbabilities = new Dictionary<RegionType, float>
			{
				{RegionType.Olive, oliveProbability},
				{RegionType.Forest, forestProbability}
			};

			regionSettings = new Dictionary<RegionType, TreesGenSettings>
			{
				{ RegionType.Olive, oliveSettings },
				{ RegionType.Forest, forestSettings }
			};
		}

		private void SincronizeProbabilities()
		{
			oliveProbability = regionProbabilities[RegionType.Olive];
			forestProbability = regionProbabilities[RegionType.Forest];
		}
	}
}
