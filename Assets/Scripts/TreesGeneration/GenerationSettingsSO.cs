using DavidUtils.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils;
using DavidUtils.Collections;
using UnityEngine;

namespace TreesGeneration
{
	[CreateAssetMenu(fileName = "Generation Settings", menuName = "Generation/Generation Settings")]
	public class GenerationSettingsSO : ScriptableObject
	{
		// TODO Probabilidades de cada tipo
		
		// RegionType Enum as keys
		[SerializeField] private DictionarySerializable<RegionType, TreesGenSettings> regionSettings = 
			new(typeof(RegionType).GetEnumValues<RegionType>());
		
		private DictionarySerializable<RegionType, float> regionTypeProbabilities = 
			new(typeof(RegionType).GetEnumValues<RegionType>(), new []{1f});

		public TreesGenSettings GenSettings(RegionType type) => regionSettings.GetValue(type);
		public OliveGenSettings OliveGenSettings => (OliveGenSettings) GenSettings(RegionType.Olive);
		public ForestGenSettings ForestGenSettings => (ForestGenSettings) GenSettings(RegionType.Forest);
		

		private void OnValidate()
		{
			NormalizeProbabilities();
			
			// OLIVE SubType Probabilities
			OliveGenSettings.NormalizeProbabilities();
		}

		public RegionType GetRandomType() => 
            regionTypeProbabilities.Keys.PickByProbability(regionTypeProbabilities.Values);

		private void NormalizeProbabilities()
		{
			if (regionTypeProbabilities.Values.Sum() > 1)
				regionTypeProbabilities.Values = regionTypeProbabilities.Values.NormalizeProbabilities().ToArray();
		}
	}
}
