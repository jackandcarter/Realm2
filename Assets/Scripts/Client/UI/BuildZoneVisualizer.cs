using System.Collections.Generic;
using Building.Runtime;
using UnityEngine;

namespace Client.UI
{
    public class BuildZoneVisualizer : MonoBehaviour
    {
        [SerializeField] private Material zoneMaterial;
        [SerializeField] private float verticalOffset = 0.1f;
        [SerializeField] private Color fallbackColor = new(0f, 0.5f, 1f, 0.25f);

        private readonly List<GameObject> _zoneObjects = new();
        private readonly List<Mesh> _generatedMeshes = new();
        private Material _runtimeMaterial;
        private bool _isVisible;
        private BuildZoneService _service;

        private void Awake()
        {
            _service = BuildZoneService.Instance;
        }

        private void OnDisable()
        {
            HideZones();
        }

        private void OnDestroy()
        {
            ClearVisuals();
            if (_runtimeMaterial != null)
            {
                Destroy(_runtimeMaterial);
                _runtimeMaterial = null;
            }
        }

        public void ShowZones()
        {
            _service ??= BuildZoneService.Instance;
            RebuildVisuals();
            SetActiveState(true);
            _isVisible = true;
        }

        public void HideZones()
        {
            SetActiveState(false);
            _isVisible = false;
        }

        public void Refresh()
        {
            if (_isVisible)
            {
                ShowZones();
            }
        }

        private void RebuildVisuals()
        {
            ClearVisuals();
            if (_service == null || !_service.HasActiveZones)
            {
                return;
            }

            foreach (var bounds in _service.ActiveZones)
            {
                var visual = CreateZoneVisual(bounds);
                if (visual != null)
                {
                    _zoneObjects.Add(visual);
                }
            }
        }

        private void ClearVisuals()
        {
            foreach (var obj in _zoneObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }

            _zoneObjects.Clear();

            foreach (var mesh in _generatedMeshes)
            {
                if (mesh != null)
                {
                    Destroy(mesh);
                }
            }

            _generatedMeshes.Clear();
        }

        private GameObject CreateZoneVisual(Bounds bounds)
        {
            var mesh = CreateQuadMesh(bounds.size.x, bounds.size.z);
            if (mesh == null)
            {
                return null;
            }

            var go = new GameObject("BuildZonePreview")
            {
                hideFlags = HideFlags.DontSave
            };

            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(bounds.center.x, bounds.max.y + verticalOffset, bounds.center.z);
            go.transform.localScale = Vector3.one;

            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = ResolveMaterial();

            _generatedMeshes.Add(mesh);
            return go;
        }

        private Mesh CreateQuadMesh(float width, float length)
        {
            var mesh = new Mesh
            {
                name = "BuildZoneQuad"
            };

            var halfWidth = width * 0.5f;
            var halfLength = length * 0.5f;

            var vertices = new[]
            {
                new Vector3(-halfWidth, 0f, -halfLength),
                new Vector3(-halfWidth, 0f, halfLength),
                new Vector3(halfWidth, 0f, halfLength),
                new Vector3(halfWidth, 0f, -halfLength)
            };

            var triangles = new[] { 0, 1, 2, 0, 2, 3 };
            var uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f)
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private Material ResolveMaterial()
        {
            if (zoneMaterial != null)
            {
                return zoneMaterial;
            }

            if (_runtimeMaterial == null)
            {
                var shader = Shader.Find("Unlit/Color");
                if (shader == null)
                {
                    shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
                }

                _runtimeMaterial = shader != null ? new Material(shader) : new Material(Shader.Find("Standard"));
                _runtimeMaterial.color = fallbackColor;
                _runtimeMaterial.name = "BuildZoneRuntimeMaterial";
            }

            return _runtimeMaterial;
        }

        private void SetActiveState(bool isActive)
        {
            foreach (var obj in _zoneObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(isActive);
                }
            }
        }
    }
}
