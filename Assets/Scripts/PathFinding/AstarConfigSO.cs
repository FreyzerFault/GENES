using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "AstarConfiguration", menuName = "Configurations/A* Configuration", order = 1)]
public class AstarConfigSO : ScriptableObject
{
    // Escalado de Coste y Heurística
    // Ajusta la penalización o recompensa de cada parámetro

    // TRAYECTO MÁS CORTO vs TERRENO MÁS SEGURO

    // =================== Coste ===================
    // Penaliza la Distancia Recorrida (Ruta + corta)
    public float distanceCost = 1f;

    // Penaliza cambiar la altura (evita que intente subir escalones grandes)
    public float heightCost = 1f;

    // =================== Heurística ===================
    // Recompensa acercarse al objetivo
    public float distanceHeuristic = 1f;

    // Recompensa acercarse a la altura del objetivo
    [FormerlySerializedAs("heightDiffHeuristic")]
    public float heightHeuristic = 1f;

    // Recompensa minimizar la pendiente (rodea montículos si puede)
    public float slopeHeuristic = 2f;
    public float maxSlopeAngle = 40f;

    // ===============================================================
    // Distancia entre nodos
    // A + grandes = + rápido pero + impreciso
    public float cellSize = 1f;

    // Maximas iteraciones del algoritmo
    public int maxIterations = 1000;

    // Cache para evitar recalcular el camino
    public bool useCache = true;

    // ===============================================================
    // RESTRICCIONES
    public float minHeight = 100f;
}