using UnityEngine;

namespace Sailing
{
    public class WindVane : MonoBehaviour
    {
        void Update()
        {
            Vector3 direction = Wind.Direction(Time.time);
            if (direction.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }
}
