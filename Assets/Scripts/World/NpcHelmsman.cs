using UnityEngine;

namespace Sailing
{
    [DefaultExecutionOrder(-40)]
    public class NpcHelmsman : MonoBehaviour
    {
        public ShipController ship;
        public Vector3[] points;
        int index;

        void Update()
        {
            if (ship == null || points == null || points.Length == 0) return;
            Vector3 to = points[index] - ship.LogicalPosition;
            to.y = 0f;
            if (to.magnitude < 6f)
                index = (index + 1) % points.Length;
            float heading = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            float delta = Mathf.DeltaAngle(ship.Yaw, heading);
            ship.Helm = Mathf.Clamp(delta / 35f, -1f, 1f);
            ship.Sail = 0.8f;
        }
    }
}
