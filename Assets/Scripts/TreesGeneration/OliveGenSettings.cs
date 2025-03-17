using System;
using System.Linq;
using DavidUtils.DevTools.CustomAttributes;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace GENES.TreesGeneration
{
    [Serializable]
	public class OliveGenSettings: TreesGenSettings
	{
		public override RegionType RegionType => RegionType.Olive;
		public override string Name => RegionType.ToString();
		
		// MAIN PARAMETERS
		[Range(0.1f, 30)] public float lindeWidth = 12;

		// TIPO de CULTIVO
		public OliveType defaultOliveType;
		public OliveType[] Types => new[] { OliveType.Traditional, OliveType.Intesive, OliveType.SuperIntesive };
		public OliveType RandomizedType => Types.PickByProbability(Probabilities);

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

		private float[] lastNormalizedProbs = null;


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
		}


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
