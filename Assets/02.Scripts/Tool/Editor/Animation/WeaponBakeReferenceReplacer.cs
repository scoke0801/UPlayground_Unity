#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// MotionSet 등 ScriptableObject의 AnimationClip 참조를 스키마 비의존적으로 교체한다.
///
/// ■ 방식
///   · 특정 클래스에 결합하지 않고 SerializedObject/SerializedProperty를 일반 순회하며
///     ObjectReference 타입 프로퍼티만 검사한다.
///   · 원본→베이크 대응표(map)에 포함된 클립 참조를 찾으면 교체 후보로 수집한다.
///   · MotionSetAsset 외에 클립을 참조하는 다른 SO가 있어도 동일하게 대응한다.
///   · 실제 적용 전 드라이런 목록을 미리 보여주고, Undo + 명시적 롤백을 모두 지원한다.
/// </summary>
public static class WeaponBakeReferenceReplacer
{
    /// <summary>교체 후보 1건.</summary>
    public class ReplacementEntry
    {
        public Object owner;          // 참조를 가진 에셋 오브젝트
        public string ownerPath;      // 에셋 경로
        public string propertyPath;   // SerializedProperty 경로 (적용 시 재조회)
        public AnimationClip fromClip;
        public AnimationClip toClip;
        public bool apply = true;     // 드라이런 목록에서 개별 선택
    }

    /// <summary>롤백용 적용 기록.</summary>
    public class AppliedRecord
    {
        public Object owner;
        public string propertyPath;
        public AnimationClip previous;
    }

    /// <summary>
    /// 폴더/타입 범위에서 map(원본→베이크)에 있는 클립 참조를 검출해 드라이런 목록을 만든다.
    /// </summary>
    public static List<ReplacementEntry> Scan(
        Dictionary<AnimationClip, AnimationClip> map, string[] folders, string typeFilter)
    {
        var entries = new List<ReplacementEntry>();
        if (map == null || map.Count == 0)
            return entries;

        foreach (string assetPath in EnumerateAssetPaths(folders, typeFilter))
        {
            Object[] all = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (Object obj in all)
            {
                if (obj == null || obj is GameObject || obj is Component)
                    continue;

                var so = new SerializedObject(obj);
                SerializedProperty it = so.GetIterator();
                while (it.NextVisible(true))
                {
                    if (it.propertyType != SerializedPropertyType.ObjectReference)
                        continue;

                    var clip = it.objectReferenceValue as AnimationClip;
                    if (clip == null || !map.TryGetValue(clip, out AnimationClip baked))
                        continue;
                    if (baked == null || baked == clip)
                        continue;

                    entries.Add(new ReplacementEntry
                    {
                        owner = obj,
                        ownerPath = assetPath,
                        propertyPath = it.propertyPath,
                        fromClip = clip,
                        toClip = baked
                    });
                }
            }
        }

        return entries;
    }

    /// <summary>드라이런 목록에서 apply=true 항목을 실제로 교체한다. Undo 등록 + 롤백 기록.</summary>
    public static int Apply(List<ReplacementEntry> entries, List<AppliedRecord> rollbackOut)
    {
        if (entries == null || entries.Count == 0)
            return 0;

        int applied = 0;
        foreach (ReplacementEntry e in entries)
        {
            if (!e.apply || e.owner == null || e.toClip == null)
                continue;

            var so = new SerializedObject(e.owner);
            SerializedProperty sp = so.FindProperty(e.propertyPath);
            if (sp == null || sp.propertyType != SerializedPropertyType.ObjectReference)
                continue;

            var previous = sp.objectReferenceValue as AnimationClip;

            Undo.RecordObject(e.owner, "Weapon Bake 참조 교체");
            sp.objectReferenceValue = e.toClip;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(e.owner);

            rollbackOut?.Add(new AppliedRecord
            {
                owner = e.owner,
                propertyPath = e.propertyPath,
                previous = previous
            });
            applied++;
        }

        AssetDatabase.SaveAssets();
        return applied;
    }

    /// <summary>적용 기록을 역방향으로 되돌린다.</summary>
    public static int Rollback(List<AppliedRecord> records)
    {
        if (records == null || records.Count == 0)
            return 0;

        int restored = 0;
        foreach (AppliedRecord r in records)
        {
            if (r.owner == null)
                continue;

            var so = new SerializedObject(r.owner);
            SerializedProperty sp = so.FindProperty(r.propertyPath);
            if (sp == null || sp.propertyType != SerializedPropertyType.ObjectReference)
                continue;

            sp.objectReferenceValue = r.previous;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(r.owner);
            restored++;
        }

        AssetDatabase.SaveAssets();
        return restored;
    }

    /// <summary>아직 원본(source) 클립을 참조 중인 에셋 경로 목록을 반환한다(중복 제거).</summary>
    public static List<string> FindStillReferencing(
        HashSet<AnimationClip> sourceClips, string[] folders, string typeFilter)
    {
        var result = new List<string>();
        if (sourceClips == null || sourceClips.Count == 0)
            return result;

        var seen = new HashSet<string>();
        foreach (string assetPath in EnumerateAssetPaths(folders, typeFilter))
        {
            Object[] all = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            bool found = false;
            foreach (Object obj in all)
            {
                if (obj == null || obj is GameObject || obj is Component)
                    continue;

                var so = new SerializedObject(obj);
                SerializedProperty it = so.GetIterator();
                while (it.NextVisible(true))
                {
                    if (it.propertyType != SerializedPropertyType.ObjectReference)
                        continue;

                    var clip = it.objectReferenceValue as AnimationClip;
                    if (clip != null && sourceClips.Contains(clip))
                    {
                        found = true;
                        break;
                    }
                }
                if (found) break;
            }

            if (found && seen.Add(assetPath))
                result.Add(assetPath);
        }

        return result;
    }

    // ── 에셋 경로 수집 ────────────────────────────────────────

    private static IEnumerable<string> EnumerateAssetPaths(string[] folders, string typeFilter)
    {
        string filter = string.IsNullOrWhiteSpace(typeFilter) ? "t:ScriptableObject" : typeFilter;

        string[] guids = (folders != null && folders.Length > 0)
            ? AssetDatabase.FindAssets(filter, folders)
            : AssetDatabase.FindAssets(filter);

        var seen = new HashSet<string>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path) && seen.Add(path))
                yield return path;
        }
    }
}
#endif
