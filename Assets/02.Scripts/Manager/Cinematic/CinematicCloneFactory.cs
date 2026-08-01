using System.Collections.Generic;
using UnityEngine;
using MagicaCloth2;

namespace UPlayGround.Manager.Cinematic
{
    /// <summary>Model 서브루트를 렌더러와 본만 남긴 읽기 전용 클론으로 만든다.</summary>
    public sealed class CinematicCloneFactory
    {
        private readonly Dictionary<int, Stack<GameObject>> _pool = new();
        private readonly Dictionary<GameObject, int> _appearanceKeys = new();
        private Transform _poolRoot;

        public void Configure(Transform poolRoot)
        {
            if (_poolRoot != null || poolRoot == null)
                return;

            var root = new GameObject("Cinematic Clone Pool");
            root.transform.SetParent(poolRoot, false);
            root.SetActive(false);
            _poolRoot = root.transform;
        }

        public GameObject Acquire(Transform sourceRoot, Transform parent, int layer)
        {
            if (sourceRoot == null)
                return null;

            int key = ComputeAppearanceKey(sourceRoot);
            GameObject clone = null;
            if (_pool.TryGetValue(key, out Stack<GameObject> entries))
            {
                while (entries.Count > 0 && clone == null)
                    clone = entries.Pop();
            }

            if (clone == null)
            {
                clone = Object.Instantiate(
                    sourceRoot.gameObject,
                    _poolRoot,
                    false);
                clone.name = $"{sourceRoot.name} (Cinematic Clone)";
                Sanitize(clone, layer);
            }

            _appearanceKeys[clone] = key;

            clone.transform.SetParent(null, false);
            if (parent != null
                && clone.scene.IsValid()
                && parent.gameObject.scene.IsValid()
                && clone.scene != parent.gameObject.scene)
            {
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(
                    clone,
                    parent.gameObject.scene);
            }
            clone.transform.SetParent(parent, false);
            clone.SetActive(true);
            return clone;
        }

        public void Release(GameObject clone)
        {
            if (clone == null)
                return;

            if (!_appearanceKeys.TryGetValue(clone, out int appearanceKey))
            {
                Object.Destroy(clone);
                return;
            }

            clone.SetActive(false);
            clone.transform.SetParent(null, false);
            if (_poolRoot != null
                && clone.scene.IsValid()
                && _poolRoot.gameObject.scene.IsValid()
                && clone.scene != _poolRoot.gameObject.scene)
            {
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(
                    clone,
                    _poolRoot.gameObject.scene);
            }
            clone.transform.SetParent(_poolRoot, false);
            if (!_pool.TryGetValue(appearanceKey, out Stack<GameObject> entries))
            {
                entries = new Stack<GameObject>();
                _pool.Add(appearanceKey, entries);
            }
            entries.Push(clone);
        }

        public void Dispose()
        {
            foreach (Stack<GameObject> entries in _pool.Values)
            {
                while (entries.Count > 0)
                {
                    GameObject clone = entries.Pop();
                    if (clone != null)
                        Object.Destroy(clone);
                }
            }
            _pool.Clear();
            _appearanceKeys.Clear();
            if (_poolRoot != null)
                Object.Destroy(_poolRoot.gameObject);
            _poolRoot = null;
        }

        public static void Sanitize(GameObject clone, int layer)
        {
            if (clone == null)
                return;

            Component[] components = clone.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component is Behaviour behaviour)
                    behaviour.enabled = false;
                else if (component is Collider collider)
                    collider.enabled = false;
            }

            for (int i = components.Length - 1; i >= 0; i--)
            {
                Component component = components[i];
                if (component == null || IsAllowed(component))
                    continue;

                Object.DestroyImmediate(component);
            }

            // 본 결과를 복사할 수 없는 MeshCloth만 설계서 7.6의 예외로 독립 구동한다.
            MagicaCloth[] meshCloths = clone.GetComponentsInChildren<MagicaCloth>(true);
            for (int i = 0; i < meshCloths.Length; i++)
                meshCloths[i].enabled = true;
            ColliderComponent[] clothColliders =
                clone.GetComponentsInChildren<ColliderComponent>(true);
            for (int i = 0; i < clothColliders.Length; i++)
                clothColliders[i].enabled = true;

            SetLayerRecursively(clone.transform, layer);
        }

        private static bool IsAllowed(Component component)
        {
            return component is Transform
                   or SkinnedMeshRenderer
                   or MeshRenderer
                   or MeshFilter
                   || component is MagicaCloth cloth
                   && (int)cloth.SerializeData.clothType == 0
                   || component is ColliderComponent;
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            if (root == null)
                return;

            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++)
                SetLayerRecursively(root.GetChild(i), layer);
        }

        private static int ComputeAppearanceKey(Transform sourceRoot)
        {
            unchecked
            {
                int hash = sourceRoot.gameObject.GetInstanceID();
                Renderer[] renderers = sourceRoot.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    hash = (hash * 397) ^ renderer.GetType().GetHashCode();
                    if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
                        hash = (hash * 397) ^ skinned.sharedMesh.GetInstanceID();
                    else if (renderer.TryGetComponent(out MeshFilter filter)
                             && filter.sharedMesh != null)
                        hash = (hash * 397) ^ filter.sharedMesh.GetInstanceID();

                    Material[] materials = renderer.sharedMaterials;
                    for (int m = 0; m < materials.Length; m++)
                    {
                        if (materials[m] != null)
                            hash = (hash * 397) ^ materials[m].GetInstanceID();
                    }
                }
                return hash;
            }
        }
    }
}
