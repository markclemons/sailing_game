using UnityEngine;

namespace Sailing
{
    [RequireComponent(typeof(Rigidbody))]
    public class ShipController : MonoBehaviour
    {
        public float Helm;
        public float Sail = 0.65f;
        public bool Hold;
        public ShipGrid Grid;

        public Vector3 LogicalPosition { get; private set; }
        public float Yaw { get; private set; }
        public Vector3 Velocity { get; private set; }

        public Vector3 Forward => Quaternion.Euler(0f, Yaw, 0f) * Vector3.forward;
        public Vector3 Right => Quaternion.Euler(0f, Yaw, 0f) * Vector3.right;

        const float TurnRate = 42f;
        const float BaseSpeed = 4.5f;

        SphereCollider body;
        bool placed;

        void Awake()
        {
            body = GetComponent<SphereCollider>();
            if (body == null) body = gameObject.AddComponent<SphereCollider>();
            var bodyRigid = GetComponent<Rigidbody>();
            bodyRigid.isKinematic = true;
            bodyRigid.useGravity = false;
            if (!placed)
                LogicalPosition = new Vector3(transform.position.x, 0f, transform.position.z);
            Yaw = transform.eulerAngles.y;
        }

        public void Teleport(Vector3 logical, float yaw)
        {
            LogicalPosition = new Vector3(logical.x, 0f, logical.z);
            Yaw = yaw;
            Hold = false;
            Velocity = Vector3.zero;
            placed = true;
            ApplyPose(0f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            float bonus = Grid != null && Grid.Layout != null ? Grid.Layout.MastBonus : 0f;
            if (body != null && Grid != null && Grid.Layout != null)
                body.radius = Grid.Layout.Radius;

            if (!Hold)
            {
                Yaw += Helm * TurnRate * dt;
                Vector3 wind = Wind.Direction(Time.time);
                float align = 0.2f + 0.8f * Mathf.Clamp01(Vector3.Dot(Forward, wind));
                float speed = Mathf.Clamp01(Sail) * align * BaseSpeed * (1f + bonus);
                Vector3 delta = Forward * speed * dt;
                Velocity = delta.sqrMagnitude > 0f ? delta / Mathf.Max(dt, 0.0001f) : Vector3.zero;
                LogicalPosition += delta;
                LogicalPosition = Resolve(LogicalPosition);
            }
            else
            {
                Velocity = Vector3.zero;
            }

            ApplyPose(Time.time);
        }

        void ApplyPose(float time)
        {
            int waves = GerstnerField.ActiveWaveCount;
            Vector2 xz = new Vector2(LogicalPosition.x, LogicalPosition.z);
            GerstnerField.Sample(xz, time, waves, out Vector3 center, out Vector3 normal);
            GerstnerField.Sample(xz + new Vector2(Forward.x, Forward.z) * 2.2f, time, waves, out Vector3 bow, out _);
            Vector3 up = Vector3.Slerp(Vector3.up, normal, 0.7f).normalized;
            Vector3 forward = bow - center;
            forward.y *= 0.35f;
            if (forward.sqrMagnitude < 0.001f) forward = Forward;
            transform.SetPositionAndRotation(center, Quaternion.LookRotation(forward.normalized, up));
        }

        Vector3 Resolve(Vector3 desired)
        {
            float radius = body != null ? body.radius : 1.8f;
            for (int iteration = 0; iteration < 3; iteration++)
            {
                Collider[] hits = Physics.OverlapSphere(desired + Vector3.up * 0.4f, radius, ~0, QueryTriggerInteraction.Ignore);
                bool blocked = false;
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].transform.root == transform.root) continue;
                    Vector3 closest = hits[i].ClosestPoint(desired);
                    Vector3 away = desired - closest;
                    away.y = 0f;
                    float distance = away.magnitude;
                    if (distance >= radius) continue;
                    if (distance < 0.001f)
                    {
                        away = desired - hits[i].bounds.center;
                        away.y = 0f;
                        if (away.sqrMagnitude < 0.001f) away = Right;
                        distance = 0f;
                    }
                    desired += away.normalized * (radius - distance + 0.05f);
                    blocked = true;
                }
                if (!blocked) break;
            }
            desired.y = 0f;
            return desired;
        }
    }
}
