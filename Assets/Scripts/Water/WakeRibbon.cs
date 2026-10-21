using System.Collections.Generic;
using UnityEngine;

namespace Sailing
{
    [DefaultExecutionOrder(40)]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class WakeRibbon : MonoBehaviour
    {
        public ShipController ship;
        public int segments = 24;
        public float spacing = 0.85f;
        public float width = 1.7f;

        readonly List<Vector3> points = new List<Vector3>();
        Mesh mesh;
        Vector3[] vertices;
        Vector2[] uv;
        Color[] colors;
        int[] triangles;

        void Awake()
        {
            mesh = new Mesh { name = "Wake" };
            mesh.MarkDynamic();
            vertices = new Vector3[segments * 2];
            uv = new Vector2[segments * 2];
            colors = new Color[segments * 2];
            triangles = new int[(segments - 1) * 6];
            int t = 0;
            for (int i = 0; i < segments - 1; i++)
            {
                int v = i * 2;
                triangles[t++] = v;
                triangles[t++] = v + 1;
                triangles[t++] = v + 2;
                triangles[t++] = v + 2;
                triangles[t++] = v + 1;
                triangles[t++] = v + 3;
            }
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        void LateUpdate()
        {
            if (ship == null) return;
            Vector3 logical = ship.LogicalPosition;
            if (points.Count == 0 || Vector3.Distance(points[0], logical) > spacing)
                points.Insert(0, logical);
            while (points.Count > segments)
                points.RemoveAt(points.Count - 1);
            int waves = GerstnerField.ActiveWaveCount;
            float time = Time.time;
            Vector3 side = ship.Right;
            side.y = 0f;
            if (side.sqrMagnitude < 0.001f) side = Vector3.right;
            side.Normalize();
            for (int i = 0; i < segments; i++)
            {
                Vector3 sample = i < points.Count ? points[i] : logical;
                float height = GerstnerField.Height(new Vector2(sample.x, sample.z), time, waves) + 0.08f;
                float fade = 1f - (i / (float)segments);
                float half = width * Mathf.Lerp(1f, 0.25f, i / (float)segments);
                var center = new Vector3(sample.x, height, sample.z);
                vertices[i * 2] = transform.InverseTransformPoint(center - side * half);
                vertices[i * 2 + 1] = transform.InverseTransformPoint(center + side * half);
                float alpha = fade * fade * 0.55f;
                colors[i * 2] = colors[i * 2 + 1] = new Color(1f, 1f, 1f, alpha);
                uv[i * 2] = new Vector2(0f, fade);
                uv[i * 2 + 1] = new Vector2(1f, fade);
            }
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.colors = colors;
            mesh.RecalculateBounds();
        }
    }
}
