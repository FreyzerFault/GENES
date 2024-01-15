using UnityEngine;

namespace Map
{
    public static class MarkerExtensionMethods
    {
        // Compara 2 markers, si estan a menos de maxDeltaRadius de distancia, devuelve true
        public static bool DistanceTo(this Marker marker, Marker other, float maxDeltaRadius = 0.1f)
        {
            return Vector2.Distance(marker.NormalizedPosition, other.NormalizedPosition) < maxDeltaRadius;
        }

        public static bool IsAtPoint(this Marker marker, Vector2 normalizedPos, float maxDeltaRadius = 0.1f)
        {
            return Vector2.Distance(marker.NormalizedPosition, normalizedPos) < maxDeltaRadius;
        }

        public static float DistanceTo(this Marker marker, Vector2 normalizedPos)
        {
            return Vector2.Distance(marker.NormalizedPosition, normalizedPos);
        }

        public static float DistanceTo(this Marker marker, Vector3 globalPos)
        {
            return Vector3.Distance(marker.WorldPosition, globalPos);
        }
    }
}