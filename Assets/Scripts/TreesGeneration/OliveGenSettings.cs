using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.Collections;
using DavidUtils.Editor.DevTools.CustomAttributes;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace GENES.TreesGeneration
{
    [Serializable]
	public class OliveGenSettings: TreesGenSettings
	{
		// MAIN PARAMETERS
		[Range(0.1f, 30)] public float lindeWidth = 12;

		// TIPO de CULTIVO
		public OliveType defaultOliveType;
		public OliveType[] Types => new[] { OliveType.Traditional, OliveType.Intesive, OliveType.SuperIntesive };

		// % de cada tipo
		[Range(0, 1)] public float probTraditionalCrop = .5f;
		[Range(0, 1)] public float probIntensiveCrop = .3f;
		[Range(0, 1)] public float probSuperIntensiveCrop = .2f;

		public float[] Probabilities
		{
			get => new[] { probTraditionalCrop, probIntensiveCrop, probSuperIntensiveCrop };
			set
			{
				if (value.Length != 3) throw new ArgumentException("Probabilities must have 3 values");
				probTraditionalCrop = value[0];
				probIntensiveCrop = value[1];
				probSuperIntensiveCrop = value[2];
			}
		}

		public OliveType RandomizedType => Types.PickByProbability(Probabilities);

		public void NormalizeProbabilities() =>
			Probabilities = Probabilities.NormalizeProbabilities().ToArray();
		
		
		#region CROP TYPE
		
		[SerializeField] [ArrayElementTitle]
		private OliveTypeParams[] _cropTypeParams = OliveTypeParams.GetDefaultParams();
		
		public OliveGenSettings() {}

		public OliveGenSettings(float probTraditionalCrop, float probIntensiveCrop, float probSuperIntensiveCrop, float lindeWidth, float maxSlopeAngle)
		{
			this.probTraditionalCrop = probTraditionalCrop;
			this.probIntensiveCrop = probIntensiveCrop;
			this.probSuperIntensiveCrop = probSuperIntensiveCrop;
			this.lindeWidth = lindeWidth;
			this.maxSlopeAngle = maxSlopeAngle;
		}
		
		public OliveTypeParams GetCropTypeParams(OliveType type) => _cropTypeParams.First(p => p.type == type);

		#endregion
	}

	public enum OliveType
	{
		Traditional,
		Intesive,
		SuperIntesive
	}
	
	[Serializable]
	public struct OliveTypeParams: ArrayElementTitleAttribute.IArrayElementTitle
	{
		[HideInInspector]
		public OliveType type;
		// X: Separacion entre olivos, Y: Separacion entre hileras
		public Vector2 separationMin;
		public Vector2 separationMax;
		public float scale;

		public string Name => type.ToString();
		
		
		public static OliveTypeParams DefaultTraditionalParams => new()
		{
			type = OliveType.Traditional,
			// 10x10 - 12x12
			separationMin = new Vector2(10, 10),
			separationMax = new Vector2(12, 12),
			scale = 2
		};
		
		public static OliveTypeParams DefaultIntensiveParams => new()
		{
			type = OliveType.Intesive,
			// 3x6 - 6x6
			separationMin = new Vector2(3, 6),
			separationMax = new Vector2(6, 6),
			scale = 1
		};
		
		public static OliveTypeParams DefaultSuperIntensiveParams => new()
		{
			type = OliveType.SuperIntesive,
			// 1x4 - 2x4
			separationMin = new Vector2(1, 4),
			separationMax = new Vector2(2, 4),
			scale = .5f
		};
		
		public static OliveTypeParams[] GetDefaultParams() =>
			new[] { DefaultTraditionalParams, DefaultIntensiveParams, DefaultSuperIntensiveParams };
	}
}
