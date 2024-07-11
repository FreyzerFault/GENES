using System.Collections.Generic;
using UnityEngine;

namespace TreesGeneration
{
	[CreateAssetMenu(fileName = "Generation Settings", menuName = "Generation/Generation Settings")]
	public class GenerationSettingsSO : ScriptableObject
	{
		// TODO Probabilidades de cada tipo
		private Dictionary<RegionType, TreesGenSettings> regionSettings = new();
		
		public OliveGenSettings oliveGenSettings = new(
			probTraditionalCrop: .5f,
			probIntensiveCrop: .3f,
			probSuperIntensiveCrop: .2f,
			lindeWidth: 12,
			maxSlopeAngle: 30
		);
		
		// TODO
		public ForestGenSettings forestGenSettings = new(maxSlopeAngle: 40);

		private void Awake()
		{
			oliveGenSettings.InitializeCropTypeParamsDictionary();

			regionSettings = new Dictionary<RegionType, TreesGenSettings>
			{
				{ RegionType.Olive, oliveGenSettings },
				{ RegionType.Forest, forestGenSettings }
			};
		}

		private void OnValidate()
		{
			oliveGenSettings.NormalizeProbabilities();
			oliveGenSettings.InitializeCropTypeParamsDictionary();
		}

		public TreesGenSettings GetSettingsByType(RegionType type) => regionSettings[type];
	}
}
