using UnityEngine;

namespace Sailing
{
    public static class BroadsideMath
    {
        public const float Range = 22f;
        public const float MinAbeamDot = 0.5f;

        public static bool InArc(Vector3 selfPosition, Vector3 selfRight, Vector3 targetPosition, float range, float minAbeamDot)
        {
            Vector3 to = targetPosition - selfPosition;
            to.y = 0f;
            float distance = to.magnitude;
            if (distance < 0.01f || distance > range)
                return false;
            to /= distance;
            selfRight.y = 0f;
            if (selfRight.sqrMagnitude < 0.0001f)
                return false;
            selfRight.Normalize();
            return Mathf.Abs(Vector3.Dot(selfRight, to)) >= minAbeamDot;
        }
    }
}
