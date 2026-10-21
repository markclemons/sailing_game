using UnityEngine;

namespace Sailing
{
    [DefaultExecutionOrder(20)]
    public class Broadside : MonoBehaviour
    {
        public ShipController ship;
        public ShipGrid grid;
        public Broadside opponent;
        public bool alwaysArmed;
        public float cooldown = 5f;

        float nextFire;

        public float CooldownLeft => Mathf.Max(0f, nextFire - Time.time);

        void Update()
        {
            if (ship == null || opponent == null || opponent.ship == null || ship.Hold)
                return;
            bool armed = alwaysArmed || (grid != null && grid.Layout != null && grid.Layout.HasCannon);
            if (!armed || Time.time < nextFire)
                return;
            if (!BroadsideMath.InArc(ship.LogicalPosition, ship.Right, opponent.ship.LogicalPosition, BroadsideMath.Range, BroadsideMath.MinAbeamDot))
                return;
            nextFire = Time.time + cooldown;
            Vector3 to = opponent.ship.LogicalPosition - ship.LogicalPosition;
            float side = Mathf.Sign(Vector3.Dot(ship.Right, to));
            if (Mathf.Abs(side) < 0.01f) side = 1f;
            Vector3 from = ship.LogicalPosition + Vector3.up * 1.3f + ship.Right * side * 1.2f;
            Vector3 aim = opponent.ship.LogicalPosition + Vector3.up * 0.8f + opponent.ship.Velocity * 0.45f;
            var go = new GameObject("Cannonball");
            go.AddComponent<Cannonball>().Begin(from, aim, opponent);
        }
    }
}
