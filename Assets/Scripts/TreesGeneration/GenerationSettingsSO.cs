using UnityEngine;

namespace TreesGeneration
{
	[CreateAssetMenu(fileName = "Generation Settings", menuName = "Generation/Generation Settings")]
	public class GenerationSettingsSO : ScriptableObject
	{
		public OliveGenSettings oliveGenSettings = new()
		{
			probTraditionalCrop = .5f,
			probIntensiveCrop = .3f,
			probSuperIntensiveCrop = .2f,
			lindeWidth = 12,
			maxSlopeAngle = 30
		};

		private void Awake()
		{
			oliveGenSettings.InitializeCropTypeParamsDictionary();
		}

		private void OnValidate()
		{
			oliveGenSettings.NormalizeProbabilities();
			oliveGenSettings.InitializeCropTypeParamsDictionary();
		}
	}
}
