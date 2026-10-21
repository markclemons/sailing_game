using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Sailing
{
    [DefaultExecutionOrder(50)]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class OceanSurface : MonoBehaviour
    {
        public float spacing = 2.2f;
        public int resolution = 72;

        Mesh mesh;
        Vector3[] vertices;
        Vector3[] normals;
        Camera cam;

        void Awake()
        {
            cam = Camera.main;
            if (cam != null)
            {
                UniversalAdditionalCameraData extra = cam.GetUniversalAdditionalCameraData();
                extra.requiresDepthOption = CameraOverrideOption.On;
                extra.requiresColorOption = CameraOverrideOption.Off;
            }
            if (GerstnerField.ActiveWaveCount == 3)
                resolution = 48;
            BuildMesh();
        }

        void BuildMesh()
        {
            int n = resolution;
            vertices = new Vector3[n * n];
            normals = new Vector3[n * n];
            var uv = new Vector2[n * n];
            var triangles = new int[(n - 1) * (n - 1) * 6];
            int t = 0;
            for (int z = 0; z < n - 1; z++)
            {
                for (int x = 0; x < n - 1; x++)
                {
                    int i = z * n + x;
                    triangles[t++] = i;
                    triangles[t++] = i + n;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + n;
                    triangles[t++] = i + n + 1;
                }
            }
            mesh = new Mesh { name = "Ocean" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            Vector3 focus = cam != null ? cam.transform.position : Vector3.zero;
            float span = (resolution - 1) * spacing;
            float originX = Mathf.Floor(focus.x / spacing) * spacing - span * 0.5f;
            float originZ = Mathf.Floor(focus.z / spacing) * spacing - span * 0.5f;
            transform.position = new Vector3(originX, 0f, originZ);
            int waves = GerstnerField.ActiveWaveCount;
            float time = Time.time;
            int n = resolution;
            for (int z = 0; z < n; z++)
            {
                for (int x = 0; x < n; x++)
                {
                    int i = z * n + x;
                    var xz = new Vector2(originX + x * spacing, originZ + z * spacing);
                    GerstnerField.Sample(xz, time, waves, out Vector3 displaced, out Vector3 normal);
                    vertices[i] = new Vector3(displaced.x - originX, displaced.y, displaced.z - originZ);
                    normals[i] = normal;
                }
            }
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.RecalculateBounds();
        }
    }
}
