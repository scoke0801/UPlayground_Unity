#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 소스 클립(GUID + 로컬 fileID) → 베이크 산출 클립 경로 매핑을 JSON으로 영속화한다.
///
/// ■ 용도
///   · 재베이크 시: 같은 소스 클립의 기존 산출물을 이름과 무관하게 찾아 GUID 보존 덮어쓰기.
///   · 참조 교체 시: 원본 클립 → 베이크 클립 대응표를 SerializedProperty 순회에 사용.
///
/// ■ 위치
///   출력 폴더 아래 "WeaponBakeMap.json". 에디터 전용 데이터이므로 SO/CreateAssetMenu를 만들지 않고
///   순수 JSON 파일로만 관리한다(런타임 로드 불필요).
/// </summary>
[Serializable]
public class WeaponBakeMap
{
    [Serializable]
    public class Entry
    {
        public string sourceGuid;      // 소스 클립이 속한 에셋의 GUID
        public long sourceLocalId;     // 소스 클립의 로컬 fileID (FBX 내 서브에셋 구분)
        public string sourceClipName;  // 참고용 원본 클립 이름
        public string bakedPath;       // 베이크 산출물 에셋 경로
    }

    public List<Entry> entries = new List<Entry>();

    // JSON 파일 경로 (직렬화 제외)
    [NonSerialized] private string _filePath;

    public const string DefaultFileName = "WeaponBakeMap.json";

    /// <summary>출력 폴더 기준 JSON을 로드한다. 없으면 빈 맵을 반환한다.</summary>
    public static WeaponBakeMap Load(string outputFolder)
    {
        string filePath = ToFilePath(outputFolder);
        WeaponBakeMap map;

        string sysPath = ToSystemPath(filePath);
        if (File.Exists(sysPath))
        {
            try
            {
                string json = File.ReadAllText(sysPath);
                map = JsonUtility.FromJson<WeaponBakeMap>(json) ?? new WeaponBakeMap();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[WeaponBakeMap] 로드 실패, 새 맵 생성: {e.Message}");
                map = new WeaponBakeMap();
            }
        }
        else
        {
            map = new WeaponBakeMap();
        }

        map._filePath = filePath;
        return map;
    }

    /// <summary>현재 맵을 JSON으로 저장한다.</summary>
    public void Save()
    {
        if (string.IsNullOrEmpty(_filePath))
            return;

        string sysPath = ToSystemPath(_filePath);
        string dir = Path.GetDirectoryName(sysPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(sysPath, JsonUtility.ToJson(this, true));
        AssetDatabase.ImportAsset(_filePath);
    }

    /// <summary>소스 클립 식별자로 기존 산출물 경로를 조회한다.</summary>
    public string FindBakedPath(string guid, long localId)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].sourceGuid == guid && entries[i].sourceLocalId == localId)
                return entries[i].bakedPath;
        }
        return null;
    }

    /// <summary>매핑을 추가하거나 갱신한다.</summary>
    public void Set(string guid, long localId, string clipName, string bakedPath)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].sourceGuid == guid && entries[i].sourceLocalId == localId)
            {
                entries[i].sourceClipName = clipName;
                entries[i].bakedPath = bakedPath;
                return;
            }
        }

        entries.Add(new Entry
        {
            sourceGuid = guid,
            sourceLocalId = localId,
            sourceClipName = clipName,
            bakedPath = bakedPath
        });
    }

    /// <summary>
    /// 저장된 매핑을 실제 클립 참조 딕셔너리(원본 → 베이크)로 해석한다.
    /// 존재하지 않는 에셋 항목은 건너뛴다.
    /// </summary>
    public Dictionary<AnimationClip, AnimationClip> BuildSourceToBaked()
    {
        var result = new Dictionary<AnimationClip, AnimationClip>();
        foreach (Entry e in entries)
        {
            AnimationClip source = ResolveClip(e.sourceGuid, e.sourceLocalId);
            AnimationClip baked = AssetDatabase.LoadAssetAtPath<AnimationClip>(e.bakedPath);
            if (source != null && baked != null && !result.ContainsKey(source))
                result.Add(source, baked);
        }
        return result;
    }

    /// <summary>GUID + 로컬 fileID로 소스 AnimationClip을 로드한다.</summary>
    public static AnimationClip ResolveClip(string guid, long localId)
    {
        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(assetPath))
            return null;

        UnityEngine.Object[] all = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (UnityEngine.Object obj in all)
        {
            if (obj is AnimationClip clip)
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(clip, out string g, out long id);
                if (g == guid && id == localId)
                    return clip;
            }
        }

        // 로컬 fileID가 일치하지 않으면(임포터 재생성 등) 메인 에셋으로 폴백
        return AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
    }

    // ── 경로 변환 ─────────────────────────────────────────────

    private static string ToFilePath(string outputFolder)
    {
        return outputFolder.TrimEnd('/', '\\') + "/" + DefaultFileName;
    }

    private static string ToSystemPath(string assetPath)
    {
        // "Assets/..." → 프로젝트 루트 기준 절대 경로
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
        return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
    }
}
#endif
