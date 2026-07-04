#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// 주의: 전역 네임스페이스 유지.
//   UPlayGround.Object 네임스페이스가 존재하므로 UPlayGround 안에서 무자격 Object.Destroy 등을
//   쓰면 CS0234가 난다. 이 파일은 전역 네임스페이스이므로 Object == UnityEngine.Object 로 안전하지만,
//   가독성을 위해 정적 파괴 호출은 명시적으로 DestroyImmediate(그대로 EditorWindow 유틸 아님)를 쓴다.

/// <summary>
/// 무기 본(Extra Bone) 애니메이션 베이크 "정적 코어".
///
/// ■ 목적
///   단일 수동 창(WeaponBoneBakeEditorWindow)과 일괄 변환 파이프라인
///   (WeaponBoneBakePipelineWindow)이 동일한 베이크 로직을 공유하도록 핵심 처리를 여기로 모았다.
///
/// ■ 원리 (기존 창과 동일)
///   · Humanoid 리타겟은 휴먼 본만 근육값으로 변환하고, 무기 본 같은 extra bone 곡선은
///     "Animator 루트 기준 전체 경로가 일치하는 트랜스폼"에만 raw curve로 재생된다.
///   · 소스 리그(Frank)와 대상 리그(Bokusei)에 같은 클립을 AnimationMode로 동시 샘플링해,
///     매 프레임 소스 무기 본의 월드 포즈를 대상 부모 본(Hand_R) 로컬 공간으로 변환하고,
///     그 결과를 대상 경로(.../Hand_R/R_Hand_Weapon) 곡선으로 클립 사본에 기록한다.
///   · 같은 경로에 기존 베이크 결과가 있으면 CopySerialized로 덮어써 GUID를 보존한다
///     (MotionSet 등 기존 참조가 깨지지 않음 → 재베이크 안전).
/// </summary>
public static class WeaponBoneBakeCore
{
    /// <summary>소스 본 이름 → 대상 곡선 경로 매핑.</summary>
    [Serializable]
    public class BoneMapping
    {
        // 소스 리그에서 이름으로 찾을 본 (실제 칼날 포즈를 갖는 본. 예: Weapon_Sword)
        public string sourceBoneName = "Weapon_Sword";

        // 대상 Animator 루트 기준 전체 경로 = 곡선이 기록될 path.
        // 마지막 세그먼트(더미 본)는 샘플링용 대상 모델에 없어도 되지만,
        // 그 부모까지는 대상 모델에 존재해야 한다.
        public string targetBonePath =
            "Armature/Hips/Spine/Chest/UpperChest/Shoulder_R/UpperArm_R/LowerArm_R/Hand_R/R_Hand_Weapon";
    }

    /// <summary>베이크 실행에 필요한 설정 묶음.</summary>
    public class BakeOptions
    {
        public GameObject sourceModel;
        public GameObject targetModel;
        public List<BoneMapping> mappings;
        public string outputFolder = "Assets/07.Animations/WeaponBaked";
        public string suffix = "_WeaponBaked";
        public float sampleRate = 0f;      // 0 = 클립 frameRate 사용
        public float positionScale = 1f;

        // 클립 이름이 "Take 001"처럼 generic이면 FBX 파일명 기반으로 출력명을 만든다.
        public bool useFbxNameForGenericClips = true;

        // 있으면: 같은 소스 클립의 기존 산출물(이름 무관)을 조회/기록하는 데 사용.
        public WeaponBakeMap map;
    }

    /// <summary>클립 1개 베이크 결과(검증 리포트에 사용).</summary>
    public class ClipReport
    {
        public AnimationClip sourceClip;
        public AnimationClip bakedClip;
        public string sourceClipGuid;
        public long sourceClipLocalId;
        public string outputPath;

        public int keyCount;                 // 곡선 1개당 키(=샘플) 수
        public float maxPositionDeviation;   // 손 기준 최대 위치 이탈(m)
        public float maxRotationDeviation;   // 손 기준 최대 회전 이탈(도)

        public bool hadWeaponCurves;         // 소스 클립에 무기 본 곡선이 있었는가
        public bool baked;                   // 실제로 베이크를 수행했는가
        public string message;               // 상태/경고 메시지

        // 무기 본 모션이 사실상 없는지 여부 (위치·회전 이탈이 모두 미미)
        public bool IsMotionNegligible =>
            baked && maxPositionDeviation < 0.005f && maxRotationDeviation < 1f;
    }

    // ── 소스 클립 무기 본 곡선 검사 ─────────────────────────────

    /// <summary>
    /// 소스 클립의 트랜스폼 곡선 중, 매핑에 지정된 소스 본(Weapon_Sword, R_Hand_Weapon 등)의
    /// path에 대응하는 곡선이 하나라도 있으면 true.
    /// </summary>
    public static bool HasWeaponBoneCurves(AnimationClip clip, List<BoneMapping> mappings)
    {
        if (clip == null || mappings == null || mappings.Count == 0)
            return false;

        var boneNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (BoneMapping m in mappings)
        {
            if (m == null) continue;
            if (!string.IsNullOrEmpty(m.sourceBoneName))
                boneNames.Add(m.sourceBoneName);

            // 대상 경로 마지막 세그먼트(예: R_Hand_Weapon)도 소스 곡선 후보에 포함.
            string leaf = LeafName(m.targetBonePath);
            if (!string.IsNullOrEmpty(leaf))
                boneNames.Add(leaf);
        }

        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
        foreach (EditorCurveBinding b in bindings)
        {
            if (b.type != typeof(Transform) || string.IsNullOrEmpty(b.path))
                continue;

            string pathLeaf = LeafName(b.path);
            if (boneNames.Contains(pathLeaf))
                return true;

            // path 중간 세그먼트에 존재하는 경우도 허용 (예: .../Weapon_Sword/Weapon_Sword_Dummy)
            foreach (string name in boneNames)
            {
                if (b.path == name ||
                    b.path.EndsWith("/" + name, StringComparison.Ordinal) ||
                    b.path.Contains("/" + name + "/"))
                    return true;
            }
        }

        return false;
    }

    // ── 일괄 베이크 ───────────────────────────────────────────

    /// <summary>
    /// 클립 목록을 베이크한다. 리그 인스턴스는 한 번만 만들어 재사용한다.
    /// null 클립은 무시하며, 실패한 클립은 리포트에 message로 표기하고 계속 진행한다.
    /// </summary>
    public static List<ClipReport> BakeClips(
        List<AnimationClip> clips,
        BakeOptions opt,
        Action<float, string> onProgress = null)
    {
        var reports = new List<ClipReport>();
        if (clips == null || clips.Count == 0 || opt == null)
            return reports;

        GameObject sourceInstance = null;
        GameObject targetInstance = null;

        try
        {
            sourceInstance = InstantiateRig(opt.sourceModel, "소스");
            targetInstance = InstantiateRig(opt.targetModel, "대상");
            if (sourceInstance == null || targetInstance == null)
                return reports;

            // 매핑 트랜스폼 사전 해석
            var resolved = new List<(BoneMapping mapping, Transform sourceBone, Transform targetParent)>();
            foreach (BoneMapping mapping in opt.mappings)
            {
                if (mapping == null) continue;

                Transform sourceBone = FindChildByName(sourceInstance.transform, mapping.sourceBoneName);
                if (sourceBone == null)
                {
                    Debug.LogError($"[WeaponBoneBake] 소스에서 본을 찾지 못함: {mapping.sourceBoneName}");
                    continue;
                }

                string parentPath = GetParentPath(mapping.targetBonePath);
                Transform targetParent = string.IsNullOrEmpty(parentPath)
                    ? targetInstance.transform
                    : targetInstance.transform.Find(parentPath);
                if (targetParent == null)
                {
                    Debug.LogError($"[WeaponBoneBake] 대상에서 부모 경로를 찾지 못함: {parentPath}");
                    continue;
                }

                resolved.Add((mapping, sourceBone, targetParent));
            }

            if (resolved.Count == 0)
            {
                Debug.LogError("[WeaponBoneBake] 유효한 본 매핑이 없어 중단합니다.");
                return reports;
            }

            EnsureFolder(opt.outputFolder);

            // 이번 실행에서 사용한 출력 경로 (동일 실행 내 파일명 충돌 방지)
            var usedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            AnimationMode.StartAnimationMode();
            try
            {
                for (int i = 0; i < clips.Count; i++)
                {
                    AnimationClip clip = clips[i];
                    if (clip == null) continue;

                    onProgress?.Invoke((float)i / clips.Count, clip.name);
                    ClipReport report = BakeOne(clip, resolved, opt, usedPaths);
                    if (report != null)
                        reports.Add(report);
                }
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }

            opt.map?.Save();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[WeaponBoneBake] 완료: {reports.Count}개 클립 → {opt.outputFolder}");
        }
        finally
        {
            if (sourceInstance != null) DestroyImmediate(sourceInstance);
            if (targetInstance != null) DestroyImmediate(targetInstance);
        }

        return reports;
    }

    private static ClipReport BakeOne(
        AnimationClip clip,
        List<(BoneMapping mapping, Transform sourceBone, Transform targetParent)> resolved,
        BakeOptions opt,
        HashSet<string> usedPaths)
    {
        var report = new ClipReport { sourceClip = clip, hadWeaponCurves = HasWeaponBoneCurves(clip, opt.mappings) };
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(clip, out string guid, out long localId);
        report.sourceClipGuid = guid;
        report.sourceClipLocalId = localId;

        float rate = opt.sampleRate > 0f ? opt.sampleRate : clip.frameRate;
        if (rate <= 0f) rate = 30f;

        int sampleCount = Mathf.Max(2, Mathf.CeilToInt(clip.length * rate) + 1);
        var times = new float[sampleCount];
        var positions = new Vector3[resolved.Count][];
        var rotations = new Quaternion[resolved.Count][];
        for (int m = 0; m < resolved.Count; m++)
        {
            positions[m] = new Vector3[sampleCount];
            rotations[m] = new Quaternion[sampleCount];
        }

        // 두 리그에 같은 클립을 동시 샘플링하여 프레임별로
        // "소스 무기 본 월드 포즈 → 대상 부모 본 로컬 포즈" 변환을 실측한다.
        for (int s = 0; s < sampleCount; s++)
        {
            float time = Mathf.Min(s / rate, clip.length);
            times[s] = time;

            AnimationMode.BeginSampling();
            // 소스/대상 리그 루트 모두에 동일 클립을 적용 (root 게임오브젝트 기준)
            SampleBothRigs(resolved, clip, time);
            AnimationMode.EndSampling();

            for (int m = 0; m < resolved.Count; m++)
            {
                Transform sourceBone = resolved[m].sourceBone;
                Transform targetParent = resolved[m].targetParent;

                Vector3 localPos = targetParent.InverseTransformPoint(sourceBone.position) * opt.positionScale;
                Quaternion localRot = Quaternion.Inverse(targetParent.rotation) * sourceBone.rotation;

                // 쿼터니언 부호 연속성 유지 (보간 뒤집힘 방지)
                if (s > 0 && Quaternion.Dot(rotations[m][s - 1], localRot) < 0f)
                    localRot = new Quaternion(-localRot.x, -localRot.y, -localRot.z, -localRot.w);

                positions[m][s] = localPos;
                rotations[m][s] = localRot;
            }
        }

        // 손 기준 최대 이탈 계산 (프레임 0 대비)
        float maxPos = 0f, maxRot = 0f;
        for (int m = 0; m < resolved.Count; m++)
        {
            for (int s = 1; s < sampleCount; s++)
            {
                maxPos = Mathf.Max(maxPos, Vector3.Distance(positions[m][s], positions[m][0]));
                maxRot = Mathf.Max(maxRot, Quaternion.Angle(rotations[m][0], rotations[m][s]));
            }
        }
        report.maxPositionDeviation = maxPos;
        report.maxRotationDeviation = maxRot;
        report.keyCount = sampleCount;

        // 원본 클립 통째 복사 (근육 곡선, 클립 설정, 이벤트 유지) 후 무기 본 곡선 추가
        AnimationClip baked = Instantiate(clip);

        for (int m = 0; m < resolved.Count; m++)
        {
            string path = resolved[m].mapping.targetBonePath;
            WriteVector3Curves(baked, path, "m_LocalPosition", times, positions[m]);
            WriteQuaternionCurves(baked, path, times, rotations[m]);
        }

        baked.EnsureQuaternionContinuity();

        // 출력 경로 결정 (기존 산출물 조회 → GUID 보존 덮어쓰기)
        string outPath = ResolveOutputPath(clip, guid, localId, opt, usedPaths);
        baked.name = Path.GetFileNameWithoutExtension(outPath);

        AnimationClip saved = SaveClipAsset(baked, outPath);
        report.bakedClip = saved;
        report.outputPath = outPath;
        report.baked = true;

        // 매핑 영속화
        opt.map?.Set(guid, localId, clip.name, outPath);

        report.message = report.IsMotionNegligible
            ? "무기 본 모션 미미 (위치·회전 이탈이 거의 0)"
            : "정상 베이크";
        return report;
    }

    // AnimationMode.BeginSampling/EndSampling 은 코어 호출부에서 감싸며,
    // 소스/대상 리그 모두에 동일 클립을 적용한다.
    private static void SampleBothRigs(
        List<(BoneMapping mapping, Transform sourceBone, Transform targetParent)> resolved,
        AnimationClip clip, float time)
    {
        // 소스/대상 루트 게임오브젝트 수집 (중복 제거)
        var roots = new HashSet<GameObject>();
        foreach (var r in resolved)
        {
            roots.Add(r.sourceBone.root.gameObject);
            roots.Add(r.targetParent.root.gameObject);
        }
        foreach (GameObject go in roots)
            AnimationMode.SampleAnimationClip(go, clip, time);
    }

    // ── 출력 경로 / 파일명 ─────────────────────────────────────

    private static string ResolveOutputPath(
        AnimationClip clip, string guid, long localId, BakeOptions opt, HashSet<string> usedPaths)
    {
        // 1) 매핑에 기록된 기존 산출물이 실제로 존재하면 이름 무관하게 그 경로에 덮어쓴다.
        if (opt.map != null)
        {
            string existing = opt.map.FindBakedPath(guid, localId);
            if (!string.IsNullOrEmpty(existing) &&
                AssetDatabase.LoadAssetAtPath<AnimationClip>(existing) != null)
            {
                usedPaths.Add(existing);
                return existing;
            }
        }

        // 2) 이름 결정: generic 이름이면 FBX 파일명 사용
        string baseName = ResolveBaseName(clip, opt.useFbxNameForGenericClips);
        string desired = SanitizeFileName(baseName + opt.suffix);
        string folder = opt.outputFolder.TrimEnd('/', '\\');
        string path = folder + "/" + desired + ".anim";

        // 3) 동일 실행 내 파일명 충돌 회피 (기존 파일 덮어쓰기는 허용)
        int n = 2;
        while (usedPaths.Contains(path))
        {
            path = folder + "/" + desired + "_" + n + ".anim";
            n++;
        }
        usedPaths.Add(path);
        return path;
    }

    private static string ResolveBaseName(AnimationClip clip, bool useFbx)
    {
        if (useFbx && IsGenericClipName(clip.name))
        {
            string assetPath = AssetDatabase.GetAssetPath(clip);
            if (!string.IsNullOrEmpty(assetPath))
            {
                string ext = Path.GetExtension(assetPath).ToLowerInvariant();
                if (ext == ".fbx")
                    return Path.GetFileNameWithoutExtension(assetPath);
            }
        }
        return clip.name;
    }

    /// <summary>"Take 001", "mixamo.com", 빈 문자열 등 정보성 없는 이름 판별.</summary>
    private static bool IsGenericClipName(string n)
    {
        if (string.IsNullOrWhiteSpace(n)) return true;
        if (Regex.IsMatch(n, @"^Take\s*\d+", RegexOptions.IgnoreCase)) return true;
        if (n.Equals("mixamo.com", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static AnimationClip SaveClipAsset(AnimationClip baked, string assetPath)
    {
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (existing != null)
        {
            // GUID 보존 덮어쓰기 — MotionSet 등 기존 참조 유지
            EditorUtility.CopySerialized(baked, existing);
            DestroyImmediate(baked);
            return existing;
        }

        AssetDatabase.CreateAsset(baked, assetPath);
        return baked;
    }

    // ── 곡선 기록 (기존 창과 동일) ─────────────────────────────

    private static void WriteVector3Curves(AnimationClip clip, string path, string property, float[] times, Vector3[] values)
    {
        var x = new float[times.Length];
        var y = new float[times.Length];
        var z = new float[times.Length];
        for (int i = 0; i < times.Length; i++)
        {
            x[i] = values[i].x;
            y[i] = values[i].y;
            z[i] = values[i].z;
        }

        SetCurve(clip, path, property + ".x", times, x);
        SetCurve(clip, path, property + ".y", times, y);
        SetCurve(clip, path, property + ".z", times, z);
    }

    private static void WriteQuaternionCurves(AnimationClip clip, string path, float[] times, Quaternion[] values)
    {
        var x = new float[times.Length];
        var y = new float[times.Length];
        var z = new float[times.Length];
        var w = new float[times.Length];
        for (int i = 0; i < times.Length; i++)
        {
            x[i] = values[i].x;
            y[i] = values[i].y;
            z[i] = values[i].z;
            w[i] = values[i].w;
        }

        SetCurve(clip, path, "m_LocalRotation.x", times, x);
        SetCurve(clip, path, "m_LocalRotation.y", times, y);
        SetCurve(clip, path, "m_LocalRotation.z", times, z);
        SetCurve(clip, path, "m_LocalRotation.w", times, w);
    }

    private static void SetCurve(AnimationClip clip, string path, string property, float[] times, float[] values)
    {
        var keys = new Keyframe[times.Length];
        for (int i = 0; i < times.Length; i++)
        {
            // 중앙차분 탄젠트로 부드러운 보간
            int prev = Mathf.Max(0, i - 1);
            int next = Mathf.Min(times.Length - 1, i + 1);
            float dt = times[next] - times[prev];
            float tangent = dt > 0f ? (values[next] - values[prev]) / dt : 0f;
            keys[i] = new Keyframe(times[i], values[i], tangent, tangent);
        }

        var binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), property);
        AnimationUtility.SetEditorCurve(clip, binding, new AnimationCurve(keys));
    }

    // ── 리그/트랜스폼 유틸 (기존 창과 동일) ────────────────────

    private static GameObject InstantiateRig(GameObject model, string label)
    {
        if (model == null)
        {
            Debug.LogError($"[WeaponBoneBake] {label} 모델이 지정되지 않았습니다.");
            return null;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        if (instance == null)
            instance = Instantiate(model);

        instance.hideFlags = HideFlags.HideAndDontSave;
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        var animator = instance.GetComponent<Animator>();
        if (animator == null || animator.avatar == null || !animator.avatar.isValid)
        {
            Debug.LogError($"[WeaponBoneBake] {label} 모델에 유효한 Animator/Avatar가 없습니다: {model.name}");
            DestroyImmediate(instance);
            return null;
        }

        return instance;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string[] parts = folder.Replace('\\', '/').Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static string SanitizeFileName(string name)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
            name = name.Replace(invalid, '_');
        return name;
    }

    private static string GetParentPath(string path)
    {
        int index = path.LastIndexOf('/');
        return index < 0 ? string.Empty : path.Substring(0, index);
    }

    private static string LeafName(string path)
    {
        if (string.IsNullOrEmpty(path)) return string.Empty;
        int index = path.LastIndexOf('/');
        return index < 0 ? path : path.Substring(index + 1);
    }

    private static Transform FindChildByName(Transform root, string name)
    {
        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildByName(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }

    // ── 정적 파괴 헬퍼 (EditorWindow 외부에서 Object.DestroyImmediate 명시) ──
    private static void DestroyImmediate(UnityEngine.Object obj)
    {
        UnityEngine.Object.DestroyImmediate(obj);
    }

    private static GameObject Instantiate(GameObject src)
    {
        return UnityEngine.Object.Instantiate(src);
    }

    private static AnimationClip Instantiate(AnimationClip src)
    {
        return UnityEngine.Object.Instantiate(src);
    }
}
#endif
