using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace TreesGeneration
{
	[CreateAssetMenu(fileName = "Generation Settings", menuName = "Generation/Generation Settings")]
	public class GenerationSettingsSO : ScriptableObject
	{
		public OliveGenParams genParams = new()
		{
			probTraditionalCrop = .5f,
			probIntensiveCrop = .3f,
			probSuperIntensiveCrop = .2f,
			lindeWidth = 12,
			maxSlopeAngle = 30
		};

		#region CROP TYPE

		public OliveTypeParams[] cropTypeParams =
		{
			new()
			{
				type = OliveType.Traditional,
				// 10x10 - 12x12
				separationMin = new Vector2(10, 10),
				separationMax = new Vector2(12, 12),
				scale = 2
			},
			new()
			{
				type = OliveType.Intesive,
				// 3x6 - 6x6
				separationMin = new Vector2(3, 6),
				separationMax = new Vector2(6, 6),
				scale = 1
			},
			new()
			{
				type = OliveType.SuperIntesive,
				// 1x4 - 2x4
				separationMin = new Vector2(1, 4),
				separationMax = new Vector2(2, 4),
				scale = .5f
			}
		};

		private Dictionary<OliveType, OliveTypeParams> _cropTypeParamsDictionary;

		private Dictionary<OliveType, OliveTypeParams> InitializeCropTypeParamsDictionary() =>
			_cropTypeParamsDictionary =
				new Dictionary<OliveType, OliveTypeParams>(
					cropTypeParams.Select(
						p =>
							new KeyValuePair<OliveType, OliveTypeParams>(p.type, p)
					)
				);

		public OliveTypeParams GetCropTypeParams(OliveType type) =>
			(_cropTypeParamsDictionary ??= InitializeCropTypeParamsDictionary())[type];

		#endregion

		private void Awake() => InitializeCropTypeParamsDictionary();

		private void OnValidate()
		{
			genParams.NormalizeProbabilities();
			InitializeCropTypeParamsDictionary();
		}
	}
}
