using System.Linq;
using DavidUtils.Collections;
using UnityEngine;

namespace GENES.PathFinding.Settings.Dijkstra
{
	[CreateAssetMenu(fileName = "Dijkstra Parameters", menuName = "PathFinding/Dijkstra Parameters")]
	public class DijkstraParameters : AlgorithmParams
	{
		private const float DefaultValue = 1f;
		private static readonly string[] _displayNames = { "Distance", "Height" };

		private static readonly ParamType[] _paramsKeys =
		{
			ParamType.DistanceCost,
			ParamType.HeightCost
		};

		public DijkstraParameters(DictionarySerializable<ParamType, ParamValue> parameters = null)
		{
			ParamValue[] values = _displayNames
				.Select(displayName => new ParamValue { value = DefaultValue, displayName = displayName })
				.ToArray();
			this.parameters = parameters ?? new DictionarySerializable<ParamType, ParamValue>(_paramsKeys, values);
		}

		// Escalado de Coste y Heurística
		// Ajusta la penalización o recompensa de cada parámetro

		// TRAYECTO MÁS CORTO vs TERRENO MÁS SEGURO

		// =================== Coste ===================
		// Penaliza la Distancia Recorrida (Ruta + corta)
		public float DistanceCost
		{
			get => GetValue(ParamType.DistanceCost);
			set => SetValue(ParamType.DistanceCost, value);
		}

		// Penaliza cambiar la altura (evita que intente subir escalones grandes)
		public float HeightCost
		{
			get => GetValue(ParamType.HeightCost);
			set => SetValue(ParamType.HeightCost, value);
		}

		// Penaliza giros bruscos 
		public float TurnCost
		{
			get => GetValue(ParamType.TurnCost);
			set => SetValue(ParamType.TurnCost, value);
		}

		// =================== Heurística ===================
		// Recompensa acercarse al objetivo
		public float DistanceHeuristic
		{
			get => GetValue(ParamType.DistanceHeuristic);
			set => SetValue(ParamType.DistanceHeuristic, value);
		}

		// Recompensa acercarse a la altura del objetivo
		public float HeightHeuristic
		{
			get => GetValue(ParamType.HeightHeuristic);
			set => SetValue(ParamType.HeightHeuristic, value);
		}

		// Recompensa minimizar la pendiente (rodea montículos si puede)
		public float SlopeHeuristic
		{
			get => GetValue(ParamType.SlopeHeuristic);
			set => SetValue(ParamType.SlopeHeuristic, value);
		}
	}
}
