using System;

namespace PathFinding.A_Star
{
    [Serializable]
    public struct AstarConfig
    {
        // Escalado de Coste y Heurística
        // Ajusta la penalización o recompensa de cada parámetro

        // TRAYECTO MÁS CORTO vs TERRENO MÁS SEGURO

        // =================== Coste ===================
        // Penaliza la Distancia Recorrida (Ruta + corta)
        public float distanceCost;

        // Penaliza cambiar la altura (evita que intente subir escalones grandes)
        public float heightCost;

        // Penaliza giros bruscos 
        public float turnCost;

        // =================== Heurística ===================
        // Recompensa acercarse al objetivo
        public float distanceHeuristic;

        // Recompensa acercarse a la altura del objetivo
        public float heightHeuristic;

        // Recompensa minimizar la pendiente (rodea montículos si puede)
        public float slopeHeuristic;
        public float maxSlopeAngle;
    }
}