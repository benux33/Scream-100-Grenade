using UnityEngine;

namespace Scream100.Client
{
    internal static class ScreamSparkEffect
    {
        private static Material _sparkMaterial;

        internal static void Spawn(Transform grenade, float duration)
        {
            if (grenade == null)
            {
                return;
            }

            if (_sparkMaterial == null)
            {
                _sparkMaterial = CreateMaterial();
            }
            if (_sparkMaterial == null)
            {
                return;
            }

            GameObject root = new GameObject("Scream 100 Dragon's Breath sparks");
            root.transform.SetParent(grenade, false);
            root.transform.localPosition = Vector3.zero;

            ParticleSystem particles = root.AddComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();

            ParticleSystem.MainModule main = particles.main;
            main.duration = Mathf.Max(0.2f, duration);
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.38f, 1.05f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(5.5f, 16f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.11f);
            main.gravityModifier = 0.65f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 8000;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 1f, 0.88f), 0f),
                    new GradientColorKey(new Color(1f, 0.68f, 0.06f), 0.28f),
                    new GradientColorKey(new Color(1f, 0.22f, 0.01f), 0.72f),
                    new GradientColorKey(new Color(0.35f, 0.025f, 0.005f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.025f),
                    new GradientAlphaKey(0.72f, 0.78f),
                    new GradientAlphaKey(0f, 1f),
                });
            ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
            color.enabled = true;
            color.color = gradient;

            ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(
                1f,
                AnimationCurve.Linear(0f, 1f, 1f, 0.08f));

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = 1350f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 600, 850),
                new ParticleSystem.Burst(2f, 280, 420),
                new ParticleSystem.Burst(4f, 320, 480),
                new ParticleSystem.Burst(6f, 360, 520),
                new ParticleSystem.Burst(8f, 400, 580),
                new ParticleSystem.Burst(10f, 450, 650),
            });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.07f;

            ParticleSystem.NoiseModule noise = particles.noise;
            noise.enabled = true;
            noise.strength = 0.72f;
            noise.frequency = 0.85f;
            noise.scrollSpeed = 2.6f;
            noise.quality = ParticleSystemNoiseQuality.Medium;

            ParticleSystem.CollisionModule collision = particles.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.bounce = new ParticleSystem.MinMaxCurve(0.08f, 0.28f);
            collision.dampen = 0.74f;
            collision.lifetimeLoss = 0.32f;
            collision.quality = ParticleSystemCollisionQuality.Medium;

            ParticleSystem.TrailModule trails = particles.trails;
            trails.enabled = true;
            trails.ratio = 0.82f;
            trails.lifetime = new ParticleSystem.MinMaxCurve(0.045f, 0.11f);
            trails.minVertexDistance = 0.08f;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(
                1f,
                AnimationCurve.Linear(0f, 1f, 1f, 0f));
            trails.inheritParticleColor = true;
            trails.dieWithParticles = true;

            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = _sparkMaterial;
            renderer.trailMaterial = _sparkMaterial;
            particles.Play(true);
            UnityEngine.Object.Destroy(root, duration + 0.35f);
        }

        private static Material CreateMaterial()
        {
            Shader shader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader == null)
            {
                shader = Shader.Find("Particles/Standard Unlit");
            }
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }
            if (shader == null)
            {
                return null;
            }

            Material material = new Material(shader);
            material.name = "Scream 100 Dragon's Breath spark material";
            material.mainTexture = CreateSparkTexture(32);
            return material;
        }

        private static Texture2D CreateSparkTexture(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Scream 100 soft spark";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            float center = size * 0.5f;
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - center;
                    float dy = y + 0.5f - center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy) / center;
                    float edge = Mathf.Clamp01(1f - distance);
                    pixels[y * size + x] = new Color(
                        1f,
                        Mathf.Lerp(0.45f, 1f, edge),
                        Mathf.Lerp(0f, 0.8f, edge),
                        edge * edge * edge);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
