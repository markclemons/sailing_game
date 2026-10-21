using UnityEngine;

namespace Sailing
{
    /// <summary>
    /// One Gerstner sum for buoyancy and the ocean mesh.
    /// The shader only shades; it does not displace, so the drawn surface
    /// cannot drift from the height the hull samples.
    /// </summary>
    public static class GerstnerField
    {
        public struct Wave
        {
            public Vector2 direction;
            public float amplitude;
            public float wavelength;
            public float steepness;
            public float speed;
        }

        public static Wave[] DefaultWaves()
        {
            return new[]
            {
                Make(new Vector2(1f, 0.25f), 0.32f, 26f, 0.45f, 1.6f),
                Make(new Vector2(-0.35f, 1f), 0.16f, 13f, 0.5f, 1.9f),
                Make(new Vector2(0.7f, -0.55f), 0.08f, 6.5f, 0.55f, 2.2f),
                Make(new Vector2(0.15f, 1f), 0.04f, 3.2f, 0.4f, 2.6f),
            };
        }

        static Wave Make(Vector2 direction, float amplitude, float wavelength, float steepness, float speed)
        {
            direction.Normalize();
            return new Wave
            {
                direction = direction,
                amplitude = amplitude,
                wavelength = wavelength,
                steepness = steepness,
                speed = speed,
            };
        }

        public static int ActiveWaveCount
        {
            get
            {
                bool mobile = Application.platform == RuntimePlatform.Android
                    || Application.platform == RuntimePlatform.IPhonePlayer
                    || QualitySettings.GetQualityLevel() == 0;
                return mobile ? 3 : 4;
            }
        }

        public static void Sample(Vector2 xz, float time, int waveCount, out Vector3 displaced, out Vector3 normal)
        {
            Wave[] waves = DefaultWaves();
            int count = Mathf.Clamp(waveCount, 1, waves.Length);
            displaced = new Vector3(xz.x, 0f, xz.y);
            normal = Vector3.up;
            for (int i = 0; i < count; i++)
            {
                Wave wave = waves[i];
                float k = (Mathf.PI * 2f) / wave.wavelength;
                float phase = k * (Vector2.Dot(wave.direction, xz) - wave.speed * time);
                float s = Mathf.Sin(phase);
                float c = Mathf.Cos(phase);
                float q = wave.steepness / (k * count);
                displaced.x += wave.direction.x * q * c;
                displaced.z += wave.direction.y * q * c;
                displaced.y += wave.amplitude * s;
                float wa = k * wave.amplitude;
                normal.x -= wave.direction.x * wa * c;
                normal.z -= wave.direction.y * wa * c;
                normal.y -= wave.steepness * wa * s;
            }
            normal.Normalize();
        }

        public static float Height(Vector2 xz, float time, int waveCount)
        {
            Sample(xz, time, waveCount, out Vector3 displaced, out _);
            return displaced.y;
        }
    }
}
