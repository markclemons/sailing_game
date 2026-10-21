using UnityEngine;

namespace Sailing
{
    public static class Wind
    {
        public static Vector3 Direction(float time)
        {
            float sway = Mathf.Sin(time * 0.15f) * 10f;
            return (Quaternion.Euler(0f, sway, 0f) * new Vector3(1f, 0f, 0.2f)).normalized;
        }
    }
}
