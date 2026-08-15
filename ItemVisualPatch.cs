using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace Scream100.Client
{
    internal sealed class Scream100VisualMarker : MonoBehaviour
    {
    }

    internal sealed class ScreamOverlayVisibility : MonoBehaviour
    {
        private Renderer _source;
        private Renderer _overlay;

        internal void Bind(Renderer source, Renderer overlay)
        {
            _source = source;
            _overlay = overlay;
            Sync();
        }

        private void LateUpdate()
        {
            Sync();
        }

        private void Sync()
        {
            if (_source != null && _overlay != null)
            {
                _overlay.enabled = _source.enabled && _source.gameObject.activeInHierarchy;
            }
        }
    }

    internal static class Scream100Visuals
    {
        private static readonly Color BodyBlack = new Color(0.025f, 0.03f, 0.035f, 1f);
        private static readonly Color PinGreen = new Color(0.18f, 0.58f, 0.20f, 1f);
        private static Texture2D _wordTexture;

        internal static void Apply(Item item, GameObject itemObject)
        {
            if (item == null || item.TemplateId.ToString() != Scream100Constants.TemplateId)
            {
                return;
            }

            Apply(itemObject);
        }

        internal static void Apply(GameObject itemObject)
        {
            if (itemObject == null || itemObject.GetComponent<Scream100VisualMarker>() != null)
            {
                return;
            }

            itemObject.AddComponent<Scream100VisualMarker>();
            Renderer[] renderers = itemObject.GetComponentsInChildren<Renderer>(true);

            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                if (renderer == null || renderer is ParticleSystemRenderer)
                {
                    continue;
                }

                Material[] originals = renderer.sharedMaterials;
                Material[] painted = new Material[originals.Length];
                for (int materialIndex = 0; materialIndex < originals.Length; materialIndex++)
                {
                    Material source = originals[materialIndex];
                    if (source == null)
                    {
                        painted[materialIndex] = null;
                        continue;
                    }

                    string partName = (renderer.name + " " + source.name).ToLowerInvariant();
                    bool pinPart = partName.Contains("pin") ||
                                   partName.Contains("ring") ||
                                   partName.Contains("safety") ||
                                   partName.Contains("lever") ||
                                   partName.Contains("spoon") ||
                                   partName.Contains("check");
                    Material material = new Material(source);
                    material.name = source.name + (pinPart ? " (Scream 100 green pin)" : " (Scream 100 black body)");
                    SetColour(material, pinPart ? PinGreen : BodyBlack);
                    if (!pinPart)
                    {
                        SetFloat(material, "_Metallic", 0.22f);
                        SetFloat(material, "_Glossiness", 0.34f);
                        SetFloat(material, "_Smoothness", 0.34f);
                    }

                    painted[materialIndex] = material;
                }

                renderer.sharedMaterials = painted;
            }
        }

        private static bool IsBodyShaped(Mesh mesh)
        {
            if (mesh == null || mesh.vertexCount < 80)
            {
                return false;
            }

            Vector3 size = mesh.bounds.size;
            int axis = LongestAxis(size);
            float length = Axis(size, axis);
            float radialA = Axis(size, (axis + 1) % 3);
            float radialB = Axis(size, (axis + 2) % 3);
            return length > Mathf.Max(radialA, radialB) * 1.2f;
        }

        private static void AddWord(Renderer renderer)
        {
            Mesh mesh = GetMesh(renderer);
            if (mesh == null)
            {
                return;
            }

            Bounds bounds = mesh.bounds;
            int axis = LongestAxis(bounds.size);
            int radialA = (axis + 1) % 3;
            int radialB = (axis + 2) % 3;
            float length = Axis(bounds.size, axis);
            float radiusA = Axis(bounds.size, radialA) * 0.5f;
            float radiusB = Axis(bounds.size, radialB) * 0.5f;

            GameObject root = new GameObject("Scream 100 markings");
            root.layer = renderer.gameObject.layer;
            root.transform.SetParent(renderer.transform, false);

            GameObject word = new GameObject("SCREAM label");
            word.layer = root.layer;
            word.transform.SetParent(root.transform, false);
            MeshFilter wordFilter = word.AddComponent<MeshFilter>();
            wordFilter.sharedMesh = CreateWordQuad(bounds, axis, radialA, radialB, length, radiusA, radiusB);
            MeshRenderer wordRenderer = word.AddComponent<MeshRenderer>();
            wordRenderer.sharedMaterial = CreateUnlitMaterial("Scream 100 white word", GetWordTexture(), Color.white);
            wordRenderer.shadowCastingMode = ShadowCastingMode.Off;
            wordRenderer.receiveShadows = false;
            ScreamOverlayVisibility visibility = root.AddComponent<ScreamOverlayVisibility>();
            visibility.Bind(renderer, wordRenderer);
        }

        private static Mesh CreateWordQuad(
            Bounds bounds,
            int axis,
            int radialA,
            int radialB,
            float length,
            float radiusA,
            float radiusB)
        {
            Vector3 center = bounds.center;
            SetAxis(ref center, radialA, Axis(bounds.center, radialA) + radiusA * 1.018f);
            Vector3 along = AxisVector(axis) * (length * 0.31f);
            Vector3 tall = AxisVector(radialB) * (radiusB * 0.55f);
            Mesh mesh = new Mesh { name = "Scream 100 SCREAM label" };
            mesh.vertices = new[]
            {
                center - along - tall,
                center + along - tall,
                center + along + tall,
                center - along + tall,
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3, 2, 1, 0, 3, 2, 0 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreatePinRing(
            Bounds bounds,
            int axis,
            int radialA,
            int radialB,
            float length,
            float radiusA,
            float radiusB)
        {
            const int majorSegments = 30;
            const int minorSegments = 8;
            float majorRadius = Mathf.Max(0.001f, Mathf.Min(radiusA, radiusB) * 0.48f);
            float tubeRadius = majorRadius * 0.14f;
            Vector3 center = bounds.center;
            SetAxis(ref center, axis, Axis(bounds.max, axis) + length * 0.035f);
            SetAxis(ref center, radialA, Axis(bounds.center, radialA) + radiusA * 0.72f);

            Vector3[] vertices = new Vector3[majorSegments * minorSegments];
            Vector3[] normals = new Vector3[vertices.Length];
            int[] triangles = new int[majorSegments * minorSegments * 6];
            int triangle = 0;

            for (int major = 0; major < majorSegments; major++)
            {
                float majorAngle = major * Mathf.PI * 2f / majorSegments;
                Vector3 radial = AxisVector(axis) * Mathf.Cos(majorAngle) +
                                 AxisVector(radialB) * Mathf.Sin(majorAngle);
                for (int minor = 0; minor < minorSegments; minor++)
                {
                    float minorAngle = minor * Mathf.PI * 2f / minorSegments;
                    Vector3 normal = radial * Mathf.Cos(minorAngle) +
                                     AxisVector(radialA) * Mathf.Sin(minorAngle);
                    int index = major * minorSegments + minor;
                    vertices[index] = center + radial * majorRadius + normal * tubeRadius;
                    normals[index] = normal.normalized;

                    int nextMajor = ((major + 1) % majorSegments) * minorSegments + minor;
                    int nextMinor = major * minorSegments + (minor + 1) % minorSegments;
                    int diagonal = ((major + 1) % majorSegments) * minorSegments + (minor + 1) % minorSegments;
                    triangles[triangle++] = index;
                    triangles[triangle++] = nextMajor;
                    triangles[triangle++] = nextMinor;
                    triangles[triangle++] = nextMinor;
                    triangles[triangle++] = nextMajor;
                    triangles[triangle++] = diagonal;
                }
            }

            Mesh mesh = new Mesh { name = "Scream 100 green pin ring" };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Texture2D GetWordTexture()
        {
            if (_wordTexture != null)
            {
                return _wordTexture;
            }

            const int width = 384;
            const int height = 96;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];
            string[] letters =
            {
                "111100111001111", "111100100100111", "110101110101101",
                "111100110100111", "010101111101101", "101111111101101"
            };
            int[] letterWidths = { 3, 3, 3, 3, 3, 3 };
            int cell = 10;
            int totalCells = 3 * 6 + 5;
            int startX = (width - totalCells * cell) / 2;
            int startY = (height - 5 * cell) / 2;
            int cursor = startX;

            for (int letter = 0; letter < letters.Length; letter++)
            {
                string glyph = letters[letter];
                int glyphWidth = letterWidths[letter];
                for (int y = 0; y < 5; y++)
                {
                    for (int x = 0; x < glyphWidth; x++)
                    {
                        if (glyph[y * glyphWidth + x] != '1')
                        {
                            continue;
                        }

                        for (int py = 1; py < cell - 1; py++)
                        {
                            for (int px = 1; px < cell - 1; px++)
                            {
                                int targetX = cursor + x * cell + px;
                                int targetY = startY + (4 - y) * cell + py;
                                pixels[targetY * width + targetX] = Color.white;
                            }
                        }
                    }
                }

                cursor += (glyphWidth + 1) * cell;
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            texture.name = "Scream 100 SCREAM word";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            _wordTexture = texture;
            return texture;
        }

        private static Material CreateUnlitMaterial(string name, Texture texture, Color colour)
        {
            Shader shader = Shader.Find(texture == null ? "Unlit/Color" : "Unlit/Transparent");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            Material material = new Material(shader);
            material.name = name;
            material.color = colour;
            material.mainTexture = texture;
            if (texture != null)
            {
                material.renderQueue = 3000;
            }
            return material;
        }

        private static Mesh GetMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned)
            {
                return skinned.sharedMesh;
            }

            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            return filter == null ? null : filter.sharedMesh;
        }

        private static int LongestAxis(Vector3 value)
        {
            if (value.x >= value.y && value.x >= value.z)
            {
                return 0;
            }
            return value.y >= value.z ? 1 : 2;
        }

        private static Vector3 AxisVector(int axis)
        {
            return axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;
        }

        private static float Axis(Vector3 value, int axis)
        {
            return axis == 0 ? value.x : axis == 1 ? value.y : value.z;
        }

        private static void SetAxis(ref Vector3 value, int axis, float component)
        {
            if (axis == 0) value.x = component;
            else if (axis == 1) value.y = component;
            else value.z = component;
        }

        private static void SetColour(Material material, Color colour)
        {
            if (material.HasProperty("_Color")) material.SetColor("_Color", colour);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", colour);
        }

        private static void SetFloat(Material material, string property, float value)
        {
            if (material.HasProperty(property)) material.SetFloat(property, value);
        }
    }

    [HarmonyPatch]
    internal static class SynchronousItemVisualPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(typeof(ObjectsFactory)))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.ReturnType == typeof(GameObject) &&
                    parameters.Length > 0 && parameters[0].ParameterType == typeof(Item))
                {
                    yield return method;
                }
            }
        }

        private static void Postfix(Item __0, GameObject __result)
        {
            Scream100Visuals.Apply(__0, __result);
        }
    }

    [HarmonyPatch]
    internal static class AsynchronousItemVisualPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(typeof(ObjectsFactory)))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.ReturnType == typeof(Task<GameObject>) &&
                    parameters.Length > 0 && parameters[0].ParameterType == typeof(Item))
                {
                    yield return method;
                }
            }
        }

        private static void Postfix(Item __0, ref Task<GameObject> __result)
        {
            if (__0 == null || __0.TemplateId.ToString() != Scream100Constants.TemplateId || __result == null)
            {
                return;
            }

            __result = ApplyWhenReady(__0, __result);
        }

        private static async Task<GameObject> ApplyWhenReady(Item item, Task<GameObject> itemTask)
        {
            GameObject itemObject = await itemTask;
            Scream100Visuals.Apply(item, itemObject);
            return itemObject;
        }
    }

    [HarmonyPatch(typeof(IconsHash), nameof(IconsHash.GetItemHash))]
    internal static class ItemIconHashPatch
    {
        private const int VisualRevision = 0x53433102;

        private static void Postfix(Item item, ref int __result)
        {
            if (item != null && item.TemplateId.ToString() == Scream100Constants.TemplateId)
            {
                __result ^= VisualRevision;
            }
        }
    }
}
