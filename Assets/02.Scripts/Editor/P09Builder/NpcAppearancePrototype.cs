using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UPlayGround.Animation;
using UPlayGround.EditorTools;

namespace UPlayGround.Editor.P09Builder
{
    /// <summary>고정 외형 NPC 사본에서 참조받지 않는 비활성 메시 잎만 제거하는 시제품이다.</summary>
    public static class NpcAppearancePrototype
    {
        public const string Staging = "Assets/03.Prefabs/Characters/NpcAppearanceImprovementStaging";
        private const string Output = NpcAppearanceImprovement.Output + "/prototype";
        private static Queue<string> _pending;

        [Serializable]
        private sealed class Metrics
        {
            public string path;
            public int transforms, renderers, inactiveTransforms, inactiveRenderers, visibleRenderers;
            public long fileBytes, hierarchyNativeBytes, sharedResourceNativeBytes;
            public double[] warmLoadMs, instantiateMs;
        }

        /// <summary>남녀 각 한 종만 스테이징에 만들고 원본과 같은 조건에서 비교한다.</summary>
        [UPlaygroundTool("p09.npc-appearance.prototype-pair")]
        public static void BuildPair()
        {
            NpcAppearanceImprovement.RequireEditMode();
            Directory.CreateDirectory(Output);
            foreach (string id in new[] { "NPC_Gen_S01_M_01", "NPC_Gen_S01_F_01" }) BuildOne(id);
        }

        /// <summary>두 시제품의 참조·렌더·파일 감소가 확인된 뒤 CSV의 대표 24종으로 확장한다.</summary>
        [UPlaygroundTool("p09.npc-appearance.prototype-representatives")]
        public static void StartRepresentatives()
        {
            NpcAppearanceImprovement.RequireEditMode();
            if (_pending != null) throw new InvalidOperationException("시제품 확장 중입니다.");
            foreach (string id in new[] { "NPC_Gen_S01_M_01", "NPC_Gen_S01_F_01" })
            {
                if (!File.Exists(Output + "/" + id + "-preservation.txt")) throw new InvalidOperationException("두 시제품 참조 검사를 먼저 완료하세요.");
                var original = JsonUtility.FromJson<Metrics>(File.ReadAllText(Output + "/" + id + "-original-metrics.json"));
                var compact = JsonUtility.FromJson<Metrics>(File.ReadAllText(Output + "/" + id + "-compact-metrics.json"));
                if (compact.fileBytes >= original.fileBytes || compact.transforms >= original.transforms)
                    throw new InvalidOperationException("시제품 효과가 확인되지 않았습니다.");
                foreach (string image in Directory.GetFiles(Output, id + "-original-*.png"))
                    if (!File.ReadAllBytes(image).SequenceEqual(File.ReadAllBytes(image.Replace("-original-", "-compact-"))))
                        throw new InvalidOperationException("두 시제품의 렌더가 다릅니다.");
            }
            var ids = File.ReadAllLines("output/NpcAppearanceReview/representatives.csv").Skip(1)
                .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Split(',')[0]).ToArray();
            if (ids.Length != 24 || ids.Distinct().Count() != 24) throw new InvalidOperationException("대표 CSV 불일치");
            var remaining = ids.Where(id => id != "NPC_Gen_S01_M_01" && id != "NPC_Gen_S01_F_01").ToArray();
            if (remaining.Any(id => File.Exists(Staging + "/" + id + "_Compact.prefab") || File.Exists(Staging + "/" + id + "_Compact.prefab.meta")))
                throw new InvalidOperationException("기존 시제품은 덮어쓰지 않습니다.");
            _pending = new Queue<string>(remaining);
            EditorApplication.update += BuildNext;
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
            File.WriteAllText(Output + "/pair-gate.txt", "2종 참조 보존·12쌍 렌더 동일·Transform/파일 감소 확인. 대표 24종으로 확장. 자연 동작과 사용자 판단은 대기.\n");
        }

        private static void BuildNext()
        {
            try
            {
                NpcAppearanceImprovement.RequireEditMode();
                BuildOne(_pending.Dequeue());
                File.WriteAllText(Output + "/progress.txt", (24 - _pending.Count) + "/24\n");
                if (_pending.Count == 0) Stop();
            }
            catch (Exception error)
            {
                File.WriteAllText(Output + "/expansion-error.txt", error.ToString());
                Stop(); Debug.LogException(error);
            }
        }

        private static void Stop()
        {
            EditorApplication.update -= BuildNext;
            AssemblyReloadEvents.beforeAssemblyReload -= Stop;
            _pending = null;
        }

        private static void BuildOne(string id)
        {
            string source = $"{NpcAppearanceGenerator.PrefabRoot}/Npc/{id}/{id}.prefab";
            string target = Staging + "/" + id + "_Compact.prefab";
            if (File.Exists(target) || File.Exists(target + ".meta")) throw new InvalidOperationException("기존 시제품 거부: " + target);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(source);
            NpcAppearanceImprovement.RequireClean(prefab);
            var root = PrefabUtility.LoadPrefabContents(source);
            try
            {
                var components = root.GetComponentsInChildren<Component>(true);
                if (components.Any(c => c == null)) throw new InvalidOperationException("Missing Script");
                if (root.GetComponentInChildren<UPlayGround.Components.CharacterEquipmentVisuals>(true) != null)
                    throw new InvalidOperationException("런타임 외형 교체 프리팹은 제외합니다.");
                File.WriteAllLines(Output + "/" + id + "-components.txt", components.Select(c => c.GetType().FullName).Distinct().OrderBy(s => s));
                var candidates = root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Where(r => !IsActive(r.transform) && r.transform.childCount == 0
                        && r.GetComponents<Component>().All(c => c is Transform || c is SkinnedMeshRenderer))
                    .Select(r => r.transform).ToList();
                var clips = EditorUtility.CollectDependencies(new UnityEngine.Object[] { prefab })
                    .OfType<AnimationClip>().ToArray();
                var bindingPaths = new HashSet<string>(clips.SelectMany(c => AnimationUtility.GetCurveBindings(c)
                    .Concat(AnimationUtility.GetObjectReferenceCurveBindings(c))).Select(b => b.path));
                candidates.RemoveAll(t => bindingPaths.Any(p => p == GetPath(t, root) || p.StartsWith(GetPath(t, root) + "/", StringComparison.Ordinal)));
                // 유지 컴포넌트의 참조를 받는 후보는 고정점까지 되살린다. bones·rootBone·Constraint·소켓도 여기 포함된다.
                bool changed;
                do
                {
                    var owners = GetOwners(candidates);
                    var protectedRoots = new HashSet<Transform>();
                    foreach (var component in components.Where(c => !owners.ContainsKey(c)))
                        VisitReferences(component, (property, value) => { if (owners.TryGetValue(value, out var owner)) protectedRoots.Add(owner); });
                    changed = candidates.RemoveAll(t => protectedRoots.Contains(t)) > 0;
                } while (changed);
                var removed = GetOwners(candidates);
                var retained = components.Where(c => !removed.ContainsKey(c)).ToArray();
                var references = new List<Tuple<Component, string, UnityEngine.Object>>();
                foreach (var component in retained)
                    VisitReferences(component, (property, value) => references.Add(Tuple.Create(component, property, value)));
                string visuals = GetVisuals(root);
                string skeleton = GetSkeleton(root);
                string retainedReferences = GetReferenceFingerprint(root, new HashSet<Component>(retained));
                File.WriteAllLines(Output + "/" + id + "-removed.txt", candidates.Select(t => GetPath(t, root)));
                File.WriteAllText(Output + "/" + id + "-reference-audit.txt", "유지 참조 " + references.Count + "\n" + retainedReferences + "\n" + skeleton);
                foreach (var candidate in candidates) UnityEngine.Object.DestroyImmediate(candidate.gameObject);
                foreach (var reference in references)
                {
                    var property = new SerializedObject(reference.Item1).FindProperty(reference.Item2);
                    if (property == null || property.objectReferenceValue != reference.Item3) throw new InvalidOperationException("유지 참조 변경: " + reference.Item2);
                }
                if (visuals != GetVisuals(root) || skeleton != GetSkeleton(root)) throw new InvalidOperationException("외형/골격/Avatar 변경");
                if (candidates.Count == 0) throw new InvalidOperationException("안전한 제거 후보가 없어 확대하지 않습니다.");
                PathConfig.EnsureFolderExists(Staging);
                PrefabUtility.SaveAsPrefabAsset(root, target, out bool success);
                if (!success) throw new IOException("시제품 저장 실패");
                var saved = PrefabUtility.LoadPrefabContents(target);
                try
                {
                    if (saved.GetComponentsInChildren<Component>(true).Any(c => c == null) || visuals != GetVisuals(saved)
                        || skeleton != GetSkeleton(saved) || retainedReferences != GetReferenceFingerprint(saved, null))
                        throw new InvalidOperationException("시제품 재로드 참조/외형 보존 실패");
                }
                finally { PrefabUtility.UnloadPrefabContents(saved); }
                File.WriteAllText(Output + "/" + id + "-preservation.txt", "원본 불변. 저장 후 유지 ObjectReference·활성 외형·전체 골격·Avatar 동일. 제거 잎=" + candidates.Count);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            MeasureAndCapture(source, id + "-original");
            MeasureAndCapture(target, id + "-compact");
        }

        private static Dictionary<UnityEngine.Object, Transform> GetOwners(List<Transform> candidates)
        {
            var owners = new Dictionary<UnityEngine.Object, Transform>();
            foreach (var t in candidates)
            {
                owners[t.gameObject] = t;
                foreach (var component in t.GetComponents<Component>()) owners[component] = t;
            }
            return owners;
        }

        private static void VisitReferences(Component component, Action<string, UnityEngine.Object> visit)
        {
            var property = new SerializedObject(component).GetIterator();
            while (property.Next(true))
            {
                if (component is Transform && property.propertyPath.StartsWith("m_Children", StringComparison.Ordinal)) continue;
                if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null)
                    visit(property.propertyPath, property.objectReferenceValue);
            }
        }

        private static string GetReferenceFingerprint(GameObject root, HashSet<Component> retained)
        {
            var lines = new List<string>();
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (retained != null && !retained.Contains(component)) continue;
                VisitReferences(component, (property, value) => lines.Add(GetPath(component.transform, root) + "|" + component.GetType().FullName + "|" + property + "|" + Describe(value, root)));
            }
            return string.Join("\n", lines.OrderBy(s => s, StringComparer.Ordinal));
        }

        private static string Describe(UnityEngine.Object value, GameObject root)
        {
            if (value == null) return "null";
            var component = value as Component;
            var gameObject = component != null ? component.gameObject : value as GameObject;
            if (gameObject != null && gameObject.transform.IsChildOf(root.transform))
                return GetPath(gameObject.transform, root) + ":" + value.GetType().FullName;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out string guid, out long localId);
            return guid + ":" + localId;
        }

        private static string GetSkeleton(GameObject root)
        {
            var rendererTransforms = new HashSet<Transform>(root.GetComponentsInChildren<Renderer>(true).Select(r => r.transform));
            var bones = root.GetComponentsInChildren<Transform>(true).Where(t => !rendererTransforms.Contains(t))
                .Select(t => GetPath(t, root) + "|" + t.localPosition.ToString("R") + "|" + t.localRotation.ToString("R") + "|" + t.localScale.ToString("R"));
            return string.Join("\n", bones.Concat(root.GetComponentsInChildren<Animator>(true).Select(a => "Avatar:" + Describe(a.avatar, root))).OrderBy(s => s, StringComparer.Ordinal));
        }

        private static string GetVisuals(GameObject root) => string.Join("\n", root.GetComponentsInChildren<Renderer>(true)
            .Where(r => r.enabled && IsActive(r.transform)).Select(r => GetPath(r.transform, root) + "|"
                + Describe(r is SkinnedMeshRenderer skin ? skin.sharedMesh : r.GetComponent<MeshFilter>()?.sharedMesh, root) + "|"
                + string.Join(",", r.sharedMaterials.Select(m => Describe(m, root)))).OrderBy(s => s, StringComparer.Ordinal));

        private static bool IsActive(Transform t)
        {
            for (; t != null; t = t.parent) if (!t.gameObject.activeSelf) return false;
            return true;
        }
        private static string GetPath(Transform t, GameObject root) => AnimationUtility.CalculateTransformPath(t, root.transform);

        private static void MeasureAndCapture(string path, string label)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var metrics = new Metrics { path = path, fileBytes = new FileInfo(path).Length, warmLoadMs = new double[5], instantiateMs = new double[5] };
            var preview = new PreviewRenderUtility();
            GameObject instance = null;
            try
            {
                for (int i = -1; i < 5; i++)
                {
                    var timer = System.Diagnostics.Stopwatch.StartNew();
                    AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (i >= 0) metrics.warmLoadMs[i] = timer.Elapsed.TotalMilliseconds;
                    timer.Restart();
                    instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview.camera.scene);
                    if (i >= 0) metrics.instantiateMs[i] = timer.Elapsed.TotalMilliseconds;
                    UnityEngine.Object.DestroyImmediate(instance); instance = null;
                }
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview.camera.scene);
                var transforms = instance.GetComponentsInChildren<Transform>(true);
                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                metrics.transforms = transforms.Length; metrics.renderers = renderers.Length;
                metrics.inactiveTransforms = transforms.Count(t => !IsActive(t));
                metrics.inactiveRenderers = renderers.Count(r => !IsActive(r.transform));
                metrics.visibleRenderers = renderers.Count(r => r.enabled && IsActive(r.transform));
                metrics.hierarchyNativeBytes = transforms.Sum(t => Profiler.GetRuntimeMemorySizeLong(t.gameObject))
                    + instance.GetComponentsInChildren<Component>(true).Sum(c => Profiler.GetRuntimeMemorySizeLong(c));
                var shared = EditorUtility.CollectDependencies(new UnityEngine.Object[] { prefab })
                    .Where(o => o is Mesh || o is Material || o is Texture).Distinct().ToArray();
                metrics.sharedResourceNativeBytes = shared.Sum(Profiler.GetRuntimeMemorySizeLong);
                File.WriteAllLines(Output + "/" + label + "-shared.csv", shared.Select(o => Describe(o, instance) + "," + o.GetType().Name + "," + Profiler.GetRuntimeMemorySizeLong(o) + "," + AssetDatabase.GetAssetPath(o)));
                File.WriteAllText(Output + "/" + label + "-metrics.json", JsonUtility.ToJson(metrics, true));
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var camera = preview.camera;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.22f, .23f, .25f);
                camera.fieldOfView = 30; camera.nearClipPlane = .01f; camera.farClipPlane = 20; camera.allowHDR = false;
                preview.lights[0].intensity = 1.3f; preview.lights[0].transform.rotation = Quaternion.Euler(25, 155, 0);
                preview.lights[1].intensity = .8f; preview.lights[1].transform.rotation = Quaternion.Euler(340, 205, 0);
                preview.ambientColor = new Color(.55f, .55f, .55f);
                using var pose = new P09PosePreview(instance);
                var set = prefab.GetComponentInChildren<ActorAnimator>(true).MotionSet;
                foreach (var slot in NpcAppearanceImprovement.Slots)
                {
                    pose.Sample(set.GetMotionSetAsset(slot), .35f, null);
                    foreach (float yaw in new[] { 0f, 90f })
                    {
                        var target = new Vector3(0, .82f, 0);
                        camera.transform.position = target + Quaternion.Euler(0, yaw, 0) * Vector3.forward * 3;
                        camera.transform.LookAt(target);
                        for (int pass = 0; pass < 2; pass++)
                        {
                            preview.BeginStaticPreview(new Rect(0, 0, 384, 576)); preview.Render(true);
                            var texture = preview.EndStaticPreview();
                            try { if (pass == 1) File.WriteAllBytes(Output + "/" + label + "-" + slot.ToString().Replace('.', '_') + "-" + yaw + ".png", texture.EncodeToPNG()); }
                            finally { UnityEngine.Object.DestroyImmediate(texture); }
                        }
                    }
                }
            }
            finally
            {
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
                preview.Cleanup();
            }
        }
    }
}
