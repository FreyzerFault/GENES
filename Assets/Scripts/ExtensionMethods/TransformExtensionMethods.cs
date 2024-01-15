using UnityEngine;

namespace ExtensionMethods
{
    public static class TransformExtensionMethods
    {
        // Locks the rotation of the transform to the horizontal plane
        public static Transform LockRotationVertical(this Transform transform)
        {
            transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            return transform;
        }

        public static Transform Billboard(this Transform transform, Transform target)
        {
            transform.LookAt(target);
            transform.LockRotationVertical();
            return transform;
        }
    }
}