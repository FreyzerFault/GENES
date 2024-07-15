using System.Collections.Generic;
using System.Linq;
using DavidUtils.Collections;
using DavidUtils.Editor.DevTools.CustomAttributes;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace GENES.TreesGeneration
{
	[CreateAssetMenu(fileName = "Generation Settings", menuName = "Generation/Generation Settings")]
	public class GenerationSettingsSO : ScriptableObject
	{
		// TODO Probabilidades de cada tipo
		
		// RegionType Enum as keys
		[SerializeField] private DictionarySerializable<RegionType, TreesGenSettings> regionSettings = 
			new(typeof(RegionType).GetEnumValues<RegionType>());
		
		[SerializeField] private DictionarySerializable<RegionType, float> regionTypeProbabilities = 
			new(typeof(RegionType).GetEnumValues<RegionType>(), new []{1f});

		public TreesGenSettings GenSettings(RegionType type) => regionSettings[type];
		public OliveGenSettings OliveGenSettings => (OliveGenSettings) GenSettings(RegionType.Olive);
		public ForestGenSettings ForestGenSettings => (ForestGenSettings) GenSettings(RegionType.Forest);

		public OliveGenSettings oliveSettings;
		public ForestGenSettings forestSettings;
		
		private void OnValidate()
		{
			NormalizeProbabilities();
			
			// OLIVE SubType Probabilities
			// TODO
			// OliveGenSettings.NormalizeProbabilities();
		}

		public RegionType GetRandomType() => 
            regionTypeProbabilities.Keys.PickByProbability(regionTypeProbabilities.Values.ToArray());

		private void NormalizeProbabilities()
		{
			if (regionTypeProbabilities.Values.Sum() <= 1) return;
			
			float[] newProbs = regionTypeProbabilities.Values.NormalizeProbabilities().ToArray();

			typeof(RegionType).GetEnumValues<RegionType>().ForEach(
				(type, i) =>
					regionTypeProbabilities[type] = newProbs[i]);
		}
	}
}
