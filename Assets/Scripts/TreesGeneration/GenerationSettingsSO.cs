using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace GENES.TreesGeneration
{
	[CreateAssetMenu(fileName = "Generation Settings", menuName = "Generation/Generation Settings")]
	public class GenerationSettingsSO : ScriptableObject
	{
		[Range(0,1)] public float oliveProbability = 1;
		[Range(0,1)] public float forestProbability = 1;

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
			if (Mathf.Approximately(regionProbabilities.Values.Sum(), 1)) return;
			
			float[] newProbs = regionProbabilities.Values.NormalizeProbabilities().ToArray();

			typeof(RegionType).GetEnumValues<RegionType>().ForEach(
				(type, i) =>
					regionProbabilities[type] = newProbs[i]);

			SincronizeProbabilities();
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
