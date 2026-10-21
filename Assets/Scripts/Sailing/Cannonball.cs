using UnityEngine;

namespace Sailing
{
    public class Cannonball : MonoBehaviour
    {
        public float duration = 1.6f;
        public float arc = 4.5f;

        Vector3 start;
        Vector3 end;
        Broadside target;
        float age;

        public void Begin(Vector3 from, Vector3 to, Broadside broadside)
        {
            start = from;
            end = to;
            target = broadside;
            transform.position = from;
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Shot";
            body.transform.SetParent(transform, false);
            body.transform.localScale = Vector3.one * 0.35f;
            Object.Destroy(body.GetComponent<Collider>());
            Paint(body.GetComponent<Renderer>(), new Color(0.16f, 0.13f, 0.1f));
        }

        void Update()
        {
            age += Time.deltaTime;
            float u = Mathf.Clamp01(age / duration);
            Vector3 position = Vector3.Lerp(start, end, u);
            position.y += Mathf.Sin(u * Mathf.PI) * arc;
            transform.position = position;
            if (u >= 1f) Impact();
        }

        void Impact()
        {
            if (target != null && target.ship != null && target.grid != null)
            {
                Vector3 shipPosition = target.ship.LogicalPosition;
                float distance = Vector2.Distance(new Vector2(shipPosition.x, shipPosition.z), new Vector2(end.x, end.z));
                if (distance <= 6f)
                    target.grid.ApplyDamage(end);
            }
            var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(puff.GetComponent<Collider>());
            float height = GerstnerField.Height(new Vector2(end.x, end.z), Time.time, GerstnerField.ActiveWaveCount);
            puff.transform.position = new Vector3(end.x, height + 0.3f, end.z);
            puff.transform.localScale = Vector3.one * 1.5f;
            Paint(puff.GetComponent<Renderer>(), new Color(0.85f, 0.92f, 0.95f));
            Object.Destroy(puff, 0.4f);
            Object.Destroy(gameObject);
        }

        static void Paint(Renderer renderer, Color color)
        {
            if (renderer == null) return;
            renderer.material.SetColor("_BaseColor", color);
            renderer.material.SetColor("_Color", color);
        }
    }
}
