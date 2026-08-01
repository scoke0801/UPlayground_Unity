using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Manager.Cinematic
{
    /// <summary>원본의 로컬 본 포즈를 읽어 렌더 전용 클론에 복사한다.</summary>
    public sealed class CinematicPoseMirror
    {
        private readonly Transform _sourceRoot;
        private readonly Transform _cloneRoot;
        private readonly Transform[] _sourceBones;
        private readonly Transform[] _cloneBones;
        private readonly Renderer[] _sourceRenderers;
        private readonly Renderer[] _cloneRenderers;
        private readonly SkinnedMeshRenderer[] _sourceSkinnedRenderers;
        private readonly SkinnedMeshRenderer[] _cloneSkinnedRenderers;

        public CinematicPoseMirror(Transform sourceRoot, Transform cloneRoot)
        {
            _sourceRoot = sourceRoot != null
                ? sourceRoot
                : throw new ArgumentNullException(nameof(sourceRoot));
            _cloneRoot = cloneRoot != null
                ? cloneRoot
                : throw new ArgumentNullException(nameof(cloneRoot));

            BuildBonePairs(sourceRoot, cloneRoot, out _sourceBones, out _cloneBones);
            BuildRendererPairs(
                sourceRoot,
                cloneRoot,
                out _sourceRenderers,
                out _cloneRenderers,
                out _sourceSkinnedRenderers,
                out _cloneSkinnedRenderers);
        }

        public int BoneCount => _sourceBones.Length;

        public void Apply(Matrix4x4 stageTransform)
        {
            if (_sourceRoot == null || _cloneRoot == null)
                return;

            for (int i = 0; i < _sourceBones.Length; i++)
            {
                Transform source = _sourceBones[i];
                Transform clone = _cloneBones[i];
                if (source == null || clone == null)
                    continue;

                clone.localPosition = source.localPosition;
                clone.localRotation = source.localRotation;
                clone.localScale = source.localScale;
            }

            for (int i = 0; i < _sourceRenderers.Length; i++)
            {
                Renderer source = _sourceRenderers[i];
                Renderer clone = _cloneRenderers[i];
                if (source != null && clone != null)
                    clone.enabled = source.enabled;
            }

            for (int i = 0; i < _sourceSkinnedRenderers.Length; i++)
            {
                SkinnedMeshRenderer source = _sourceSkinnedRenderers[i];
                SkinnedMeshRenderer clone = _cloneSkinnedRenderers[i];
                if (source == null || clone == null)
                    continue;

                clone.localBounds = source.localBounds;
                int blendShapeCount = Mathf.Min(
                    source.sharedMesh != null ? source.sharedMesh.blendShapeCount : 0,
                    clone.sharedMesh != null ? clone.sharedMesh.blendShapeCount : 0);
                for (int shapeIndex = 0; shapeIndex < blendShapeCount; shapeIndex++)
                {
                    float weight = source.GetBlendShapeWeight(shapeIndex);
                    if (!Mathf.Approximately(clone.GetBlendShapeWeight(shapeIndex), weight))
                        clone.SetBlendShapeWeight(shapeIndex, weight);
                }
            }

            _cloneRoot.SetPositionAndRotation(
                stageTransform.MultiplyPoint3x4(_sourceRoot.position),
                stageTransform.rotation * _sourceRoot.rotation);
            _cloneRoot.localScale = _sourceRoot.lossyScale;
        }

        public static bool TryValidateHierarchy(
            Transform sourceRoot,
            Transform cloneRoot,
            out string error)
        {
            if (sourceRoot == null || cloneRoot == null)
            {
                error = "원본 또는 클론 루트가 없습니다.";
                return false;
            }

            try
            {
                BuildBonePairs(sourceRoot, cloneRoot, out _, out _);
                error = string.Empty;
                return true;
            }
            catch (InvalidOperationException exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private static void BuildBonePairs(
            Transform sourceRoot,
            Transform cloneRoot,
            out Transform[] sourceBones,
            out Transform[] cloneBones)
        {
            Transform[] sources = sourceRoot.GetComponentsInChildren<Transform>(true);
            var cloneByPath = new Dictionary<string, Transform>(sources.Length);
            Transform[] clones = cloneRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < clones.Length; i++)
                cloneByPath[GetPath(cloneRoot, clones[i])] = clones[i];

            var pairedSources = new List<Transform>(sources.Length - 1);
            var pairedClones = new List<Transform>(sources.Length - 1);
            for (int i = 1; i < sources.Length; i++)
            {
                string path = GetPath(sourceRoot, sources[i]);
                if (!cloneByPath.TryGetValue(path, out Transform clone))
                    throw new InvalidOperationException($"클론 본 계층에 '{path}'가 없습니다.");

                pairedSources.Add(sources[i]);
                pairedClones.Add(clone);
            }

            sourceBones = pairedSources.ToArray();
            cloneBones = pairedClones.ToArray();
        }

        private static void BuildRendererPairs(
            Transform sourceRoot,
            Transform cloneRoot,
            out Renderer[] sourceRenderers,
            out Renderer[] cloneRenderers,
            out SkinnedMeshRenderer[] sourceSkinnedRenderers,
            out SkinnedMeshRenderer[] cloneSkinnedRenderers)
        {
            var cloneByPath = new Dictionary<string, Transform>();
            Transform[] cloneTransforms = cloneRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < cloneTransforms.Length; i++)
                cloneByPath[GetPath(cloneRoot, cloneTransforms[i])] = cloneTransforms[i];

            var pairedSources = new List<Renderer>();
            var pairedClones = new List<Renderer>();
            var pairedSkinnedSources = new List<SkinnedMeshRenderer>();
            var pairedSkinnedClones = new List<SkinnedMeshRenderer>();
            Renderer[] sources = sourceRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < sources.Length; i++)
            {
                Renderer source = sources[i];
                string path = GetPath(sourceRoot, source.transform);
                if (!cloneByPath.TryGetValue(path, out Transform cloneTransform))
                    continue;

                Renderer clone = cloneTransform.GetComponent(source.GetType()) as Renderer;
                if (clone == null)
                    continue;

                pairedSources.Add(source);
                pairedClones.Add(clone);
                if (source is SkinnedMeshRenderer sourceSkinned
                    && clone is SkinnedMeshRenderer cloneSkinned)
                {
                    pairedSkinnedSources.Add(sourceSkinned);
                    pairedSkinnedClones.Add(cloneSkinned);
                }
            }

            sourceRenderers = pairedSources.ToArray();
            cloneRenderers = pairedClones.ToArray();
            sourceSkinnedRenderers = pairedSkinnedSources.ToArray();
            cloneSkinnedRenderers = pairedSkinnedClones.ToArray();
        }

        private static string GetPath(Transform root, Transform current)
        {
            if (current == root)
                return string.Empty;

            var names = new Stack<string>();
            while (current != null && current != root)
            {
                names.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", names);
        }
    }
}
