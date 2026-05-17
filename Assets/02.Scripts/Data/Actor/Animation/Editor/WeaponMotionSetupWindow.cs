using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AYellowpaper.SerializedCollections;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// AnimationClip → AnimKey 매핑을 지정하고 MotionSetAsset을 일괄 생성하여
    /// ActorAnimationMotionSet에 등록하는 에디터 창.
    ///
    /// 사용 흐름:
    ///   1. AnimClip 폴더 + 출력 폴더 + 대상 SO 설정
    ///   2. [폴더 스캔] — 클립 목록 표시
    ///   3. 각 클립에 AnimKey 지정 (드롭다운)
    ///   4. [매핑 저장] — 다음 캐릭터에 재사용
    ///   5. [일괄 생성] — MotionSetAsset 생성 + AnimSet 등록
    /// </summary>
    public class WeaponMotionSetupWindow : EditorWindow
    {
        private const string DefaultScanFolder = "Assets/ExternalAssets/AnimationOnly/Frank_Slash_Pack/Assets/Animations";
        private const string DefaultPlayerMotionSetFolder = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player";
        private const string DefaultTargetMotionSetPath = DefaultPlayerMotionSetFolder + "/Player_AnimationSet.asset";
        private const string DefaultMappingConfigFolder = "Assets/10.Datas/Actor/Animation/WeaponMotionSetupConfig";
        private static readonly (string label, int min, int max)[] KeyRanges =
        {
            ("이동",       0,   29),
            ("공격",       100, 199),
            ("강공격",     200, 299),
            ("대시 공격",  300, 399),
            ("점프 공격",  400, 499),
            ("스킬",       500, 619),
            ("차지/피니시", 620, 699),
            ("피격",  700, 919),
            ("기타",       920, int.MaxValue),
        };

        // ── 설정 필드 ────────────────────────────────────────────────────────────
        private string _scanFolder   = DefaultScanFolder;
        private string _outputFolder = DefaultPlayerMotionSetFolder;
        private string _filePrefix   = "";
        private bool   _overwrite    = false;

        private ActorAnimationMotionSet   _targetSO;
        private WeaponMotionMappingConfig _mappingConfig;

        // ── 내부 데이터 ──────────────────────────────────────────────────────────
        private class RowEntry
        {
            public AnimationClip clip;
            // clip.name이 "Take 001"인 FBX 서브에셋의 경우 FBX 파일명으로 대체
            public string        displayName = "";
            public AnimKey       animKey    = AnimKey.None;
            public int           orderInSet = 0;
            public bool          skip       = false;
        }

        private List<RowEntry> _rows      = new();
        private Vector2        _scroll;
        private bool           _scanned   = false;
        private string         _statusMsg = "";

        // ── 스태틱 캐시 ──────────────────────────────────────────────────────────
        private static AnimKey[] s_AllKeys;
        private static string[]  s_AllKeyNames;
        private GUIStyle         _headerStyle;

        // ────────────────────────────────────────────────────────────────────────

        [MenuItem("UPlayGround/Util/Weapon Motion Setup")]
        public static void Open()
        {
            var w = GetWindow<WeaponMotionSetupWindow>("Weapon Motion Setup");
            w.minSize = new Vector2(760, 540);
            w.Show();
        }

        private void OnEnable()
        {
            if (s_AllKeys == null)
            {
                s_AllKeys     = (AnimKey[])Enum.GetValues(typeof(AnimKey));
                s_AllKeyNames = s_AllKeys.Select(k => k.ToString()).ToArray();
            }

            ApplyDefaults(false);
        }

        // ── GUI ─────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            InitStyles();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Weapon Motion Setup", _headerStyle);
            EditorGUILayout.LabelField(
                "AnimationClip을 AnimKey에 매핑하고 MotionSetAsset을 일괄 생성 · 등록합니다.",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(6);

            DrawSettings();
            EditorGUILayout.Space(4);
            DrawActionButtons();

            if (_scanned && _rows.Count > 0)
            {
                EditorGUILayout.Space(4);
                DrawTable();
                EditorGUILayout.Space(4);
                DrawGenerateButton();
            }

            if (!string.IsNullOrEmpty(_statusMsg))
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(_statusMsg, MessageType.Info);
            }
        }

        // ── 설정 패널 ────────────────────────────────────────────────────────────

        private void DrawSettings()
        {
            using var box = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);
            EditorGUILayout.LabelField("설정", EditorStyles.boldLabel);

            DrawFolderField("AnimClip 폴더",  ref _scanFolder);
            DrawFolderField("출력 폴더",       ref _outputFolder);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("기본 경로 적용", GUILayout.Width(110)))
                    ApplyDefaults(true);
            }

            _filePrefix = EditorGUILayout.TextField(
                new GUIContent("파일명 접두사", "예: Spear → Spear_Attack_1.asset"),
                _filePrefix);

            _targetSO = (ActorAnimationMotionSet)EditorGUILayout.ObjectField(
                "대상 AnimationMotionSet", _targetSO, typeof(ActorAnimationMotionSet), false);
            EditorGUILayout.LabelField(
                new GUIContent("기본 대상 경로", DefaultTargetMotionSetPath),
                EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                _mappingConfig = (WeaponMotionMappingConfig)EditorGUILayout.ObjectField(
                    "매핑 설정", _mappingConfig, typeof(WeaponMotionMappingConfig), false);
                if (GUILayout.Button("새로 만들기", GUILayout.Width(80)))
                    CreateNewMappingConfig();
            }

            _overwrite = EditorGUILayout.Toggle(
                new GUIContent("기존 에셋 덮어쓰기", "같은 이름의 MotionSetAsset이 이미 있으면 덮어씁니다."),
                _overwrite);
        }

        private void DrawFolderField(string label, ref string value)
        {
            using var h = new EditorGUILayout.HorizontalScope();
            value = EditorGUILayout.TextField(label, value);
            if (GUILayout.Button("…", GUILayout.Width(26)))
            {
                string picked = EditorUtility.OpenFolderPanel(label, ToAbsoluteFolderPath(value), "");
                if (!string.IsNullOrEmpty(picked))
                    value = ToProjectRelative(picked);
            }
        }

        // ── 액션 버튼 ────────────────────────────────────────────────────────────

        private void DrawActionButtons()
        {
            using var h = new EditorGUILayout.HorizontalScope();

            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_scanFolder));
            if (GUILayout.Button("폴더 스캔", GUILayout.Height(26)))
                ScanFolder();
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(_mappingConfig == null || !_scanned);
            if (GUILayout.Button("매핑 불러오기", GUILayout.Height(26)))
                LoadMapping();
            if (GUILayout.Button("매핑 저장", GUILayout.Height(26)))
                SaveMapping();
            EditorGUI.EndDisabledGroup();
        }

        // ── 테이블 ───────────────────────────────────────────────────────────────

        private void DrawTable()
        {
            // 헤더
            Rect hdr = EditorGUILayout.GetControlRect(false, 20f);
            EditorGUI.DrawRect(hdr, new Color(0.18f, 0.18f, 0.18f, 0.8f));
            float hx = hdr.x + 4f, hy = hdr.y + 2f;
            GUI.Label(new Rect(hx,        hy, 220f, 16f), "AnimationClip", EditorStyles.boldLabel);
            GUI.Label(new Rect(hx + 222f, hy, 190f, 16f), "AnimKey",       EditorStyles.boldLabel);
            GUI.Label(new Rect(hx + 414f, hy,  44f, 16f), "순서",           EditorStyles.boldLabel);
            GUI.Label(new Rect(hx + 460f, hy,  50f, 16f), "건너뜀",         EditorStyles.boldLabel);

            DrawDivider();

            // AnimKey → orderInSet 순으로 정렬 (None / skip은 하단)
            var sorted = _rows
                .OrderBy(r => r.skip ? 1 : 0)
                .ThenBy(r => r.animKey == AnimKey.None ? int.MaxValue : (int)r.animKey)
                .ThenBy(r => r.orderInSet)
                .ToList();

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(380));
            for (int i = 0; i < sorted.Count; i++)
                DrawRow(sorted[i], i);
            EditorGUILayout.EndScrollView();

            DrawDivider();

            // 요약
            int unmapped = _rows.Count(r => !r.skip && r.animKey == AnimKey.None);
            int skipped  = _rows.Count(r =>  r.skip);
            EditorGUILayout.LabelField(
                $"총 {_rows.Count}개  |  매핑됨: {_rows.Count - unmapped - skipped}  |  미매핑: {unmapped}  |  건너뜀: {skipped}",
                EditorStyles.miniLabel);
        }

        private void DrawRow(RowEntry row, int idx)
        {
            Color bg;
            if      (row.skip)                   bg = new Color(0.12f, 0.12f, 0.12f, 0.4f);
            else if (row.animKey == AnimKey.None) bg = new Color(0.50f, 0.13f, 0.13f, 0.4f);
            else                                  bg = idx % 2 == 0 ? new Color(0.22f, 0.22f, 0.22f, 0.25f) : Color.clear;

            Rect r = EditorGUILayout.GetControlRect(false, 20f);
            EditorGUI.DrawRect(r, bg);

            float x = r.x + 4f, y = r.y + 1f;

            // 클립 이름 (FBX 서브에셋은 displayName = 파일명)
            var nameStyle = row.skip
                ? new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.5f, 0.5f, 0.5f) } }
                : EditorStyles.miniLabel;
            string label = string.IsNullOrEmpty(row.displayName) ? (row.clip?.name ?? "(null)") : row.displayName;
            GUI.Label(new Rect(x, y, 218f, 18f), label, nameStyle);
            x += 222f;

            // AnimKey 선택 (애니메이션 에디터와 동일한 그룹 메뉴)
            Rect keyRect = new Rect(x, y, 188f, 18f);
            if (GUI.Button(keyRect, row.animKey.ToString(), EditorStyles.popup))
                ShowAnimKeyMenu(row, keyRect);
            x += 192f;

            // 순서 (같은 AnimKey에 여러 클립이 묶일 때 Motion 순서)
            row.orderInSet = EditorGUI.IntField(new Rect(x, y, 40f, 18f), row.orderInSet);
            x += 44f;

            // 건너뜀 토글
            row.skip = EditorGUI.Toggle(new Rect(x + 14f, y, 18f, 18f), row.skip);
        }

        // ── 생성 버튼 ────────────────────────────────────────────────────────────

        private void DrawGenerateButton()
        {
            bool canGen = !string.IsNullOrEmpty(_outputFolder)
                          && _rows.Any(r => !r.skip && r.animKey != AnimKey.None);

            EditorGUI.BeginDisabledGroup(!canGen);
            if (GUILayout.Button("MotionSetAsset 일괄 생성 & 등록", GUILayout.Height(32)))
                GenerateAssets();
            EditorGUI.EndDisabledGroup();
        }

        // ── 로직 ────────────────────────────────────────────────────────────────

        private void ScanFolder()
        {
            _scanFolder = NormalizeAssetPath(_scanFolder);
            if (!AssetDatabase.IsValidFolder(_scanFolder))
            {
                EditorUtility.DisplayDialog("오류", $"AnimClip 폴더가 존재하지 않습니다:\n{_scanFolder}", "확인");
                return;
            }

            var clips = FindClipsInFolder(_scanFolder);

            // 재스캔 시 기존 AnimKey 매핑 보존
            var prev = _rows.Where(r => r.clip != null).ToDictionary(r => r.clip);
            _rows = clips.Select(pair =>
            {
                var (clip, displayName) = pair;
                if (prev.TryGetValue(clip, out var ex))
                {
                    ex.displayName = displayName;
                    return ex;
                }
                return new RowEntry { clip = clip, displayName = displayName };
            }).ToList();

            _scanned   = true;
            _statusMsg = $"{clips.Count}개 AnimationClip 발견.";

            if (_mappingConfig != null)
                LoadMapping();
        }

        private void LoadMapping()
        {
            if (_mappingConfig == null) return;

            // 1순위: 클립 직접 참조 (같은 캐릭터 재로드)
            var byClip = new Dictionary<AnimationClip, WeaponMotionMappingConfig.ClipEntry>();
            var byName = new Dictionary<string, WeaponMotionMappingConfig.ClipEntry>();
            foreach (var e in _mappingConfig.entries)
            {
                if (e == null) continue;
                if (e.clip != null && !byClip.ContainsKey(e.clip))
                    byClip.Add(e.clip, e);

                // 이전 버전 매핑은 clipDisplayName이 비어있을 수 있으므로 clip.name까지 보조 키로 유지
                string displayName = !string.IsNullOrEmpty(e.clipDisplayName)
                    ? e.clipDisplayName
                    : e.clip != null ? e.clip.name : "";
                if (!string.IsNullOrEmpty(displayName) && !byName.ContainsKey(displayName))
                    byName.Add(displayName, e);
            }

            foreach (var row in _rows)
            {
                if (row.clip == null) continue;
                WeaponMotionMappingConfig.ClipEntry e = null;
                byClip.TryGetValue(row.clip, out e);
                if (e == null) byName.TryGetValue(row.displayName, out e);
                if (e == null) continue;

                row.animKey    = e.animKey;
                row.orderInSet = e.orderInSet;
                row.skip       = e.skip;
            }
            _statusMsg = $"매핑 불러옴: {_mappingConfig.name}";
        }

        private void SaveMapping()
        {
            if (_mappingConfig == null) return;
            Undo.RecordObject(_mappingConfig, "Save WeaponMotionMapping");
            _mappingConfig.entries = _rows
                .Where(r => r.clip != null)
                .Select(r => new WeaponMotionMappingConfig.ClipEntry
                {
                    clip            = r.clip,
                    clipDisplayName = r.displayName,
                    animKey         = r.animKey,
                    orderInSet      = r.orderInSet,
                    skip            = r.skip,
                })
                .ToList();
            EditorUtility.SetDirty(_mappingConfig);
            AssetDatabase.SaveAssetIfDirty(_mappingConfig);
            _statusMsg = $"매핑 저장됨: {_mappingConfig.name}";
        }

        private void GenerateAssets()
        {
            _outputFolder = NormalizeAssetPath(_outputFolder);
            if (string.IsNullOrEmpty(_outputFolder))
            {
                EditorUtility.DisplayDialog("오류", "출력 폴더를 지정해주세요.", "확인");
                return;
            }

            if (!EnsureFolder(_outputFolder))
            {
                EditorUtility.DisplayDialog("오류", $"출력 폴더를 만들 수 없습니다:\n{_outputFolder}", "확인");
                return;
            }

            if (_targetSO == null)
                _targetSO = EnsureTargetMotionSet();

            var groups = _rows
                .Where(r => !r.skip && r.animKey != AnimKey.None && r.clip != null)
                .GroupBy(r => r.animKey)
                .OrderBy(g => (int)g.Key)
                .ToList();

            if (groups.Count == 0)
            {
                _statusMsg = "생성할 항목이 없습니다. AnimKey를 매핑해주세요.";
                return;
            }

            int created = 0, updated = 0, skipped = 0;
            var toRegister = new List<(AnimKey key, MotionSetAsset asset)>();

            foreach (var group in groups)
            {
                AnimKey key  = group.Key;
                var motions  = group.OrderBy(r => r.orderInSet).ToList();

                string prefix    = string.IsNullOrEmpty(_filePrefix) ? "" : (_filePrefix + "_");
                string fileName  = $"{prefix}{key}.asset";
                string assetPath = $"{_outputFolder}/{fileName}";

                var existing = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(assetPath);

                MotionSetAsset asset;
                if (existing != null && !_overwrite)
                {
                    asset = existing;
                    skipped++;
                }
                else if (existing != null)
                {
                    // 덮어쓰기: 기존 에셋 내용만 교체
                    Undo.RecordObject(existing, "Update MotionSetAsset");
                    existing.motionSet = BuildMotionSet(Path.GetFileNameWithoutExtension(fileName), motions);
                    EditorUtility.SetDirty(existing);
                    asset = existing;
                    updated++;
                }
                else
                {
                    asset = CreateInstance<MotionSetAsset>();
                    asset.motionSet = BuildMotionSet(Path.GetFileNameWithoutExtension(fileName), motions);
                    AssetDatabase.CreateAsset(asset, assetPath);
                    created++;
                }

                toRegister.Add((key, asset));
            }

            AssetDatabase.SaveAssets();

            // 대상 SO에 일괄 등록
            if (_targetSO != null && toRegister.Count > 0)
            {
                foreach (var (key, asset) in toRegister)
                    AddOrAssignTargetMotionAsset(_targetSO, key, asset);
            }

            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            _statusMsg = $"완료 — 생성: {created}, 갱신: {updated}, 건너뜀(이미 존재): {skipped}";
        }

        private static MotionSet BuildMotionSet(string name, List<RowEntry> rows)
        {
            return new MotionSet
            {
                motionSetName = name,
                motions = rows.Select(r => new Motion
                {
                    motionName = string.IsNullOrEmpty(r.displayName) ? r.clip.name : r.displayName,
                    motionClip = r.clip,
                }).ToList(),
            };
        }

        // ── 유틸 ────────────────────────────────────────────────────────────────

        private void ShowAnimKeyMenu(RowEntry row, Rect dropdownRect)
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("None"), row.animKey == AnimKey.None, () =>
            {
                row.animKey = AnimKey.None;
                Repaint();
            });
            menu.AddSeparator("");

            foreach (AnimKey key in s_AllKeys)
            {
                if (key == AnimKey.None) continue;
                AnimKey captured = key;
                menu.AddItem(new GUIContent(GetKeyGroupLabel(key) + "/" + key), row.animKey == key, () =>
                {
                    row.animKey = captured;
                    Repaint();
                });
            }

            menu.DropDown(dropdownRect);
        }

        private static string GetKeyGroupLabel(AnimKey key)
        {
            int value = (int)key;
            foreach (var range in KeyRanges)
            {
                if (value >= range.min && value <= range.max)
                    return range.label;
            }
            return "기타";
        }

        private static void AddOrAssignTargetMotionAsset(ActorAnimationMotionSet target, AnimKey key, MotionSetAsset asset)
        {
            var sObj = new SerializedObject(target);
            var listProp = sObj.FindProperty("motionSets").FindPropertyRelative("_serializedList");
            int idx = FindMotionKeyIndex(listProp, key);

            if (idx < 0)
            {
                listProp.InsertArrayElementAtIndex(listProp.arraySize);
                idx = listProp.arraySize - 1;
            }

            var elem = listProp.GetArrayElementAtIndex(idx);
            elem.FindPropertyRelative("Key").intValue = (int)key;
            elem.FindPropertyRelative("Value").objectReferenceValue = asset;

            sObj.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssetIfDirty(target);
        }

        private static int FindMotionKeyIndex(SerializedProperty listProp, AnimKey key)
        {
            for (int i = 0; i < listProp.arraySize; i++)
            {
                if ((AnimKey)listProp.GetArrayElementAtIndex(i).FindPropertyRelative("Key").intValue == key)
                    return i;
            }
            return -1;
        }

        private void ApplyDefaults(bool force)
        {
            if (force || string.IsNullOrEmpty(_scanFolder))
                _scanFolder = DefaultScanFolder;
            if (force || string.IsNullOrEmpty(_outputFolder))
                _outputFolder = DefaultPlayerMotionSetFolder;

            if (force || _targetSO == null)
                _targetSO = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(DefaultTargetMotionSetPath);

            if (force)
                _statusMsg = "기본 경로를 적용했습니다.";
        }

        private static ActorAnimationMotionSet EnsureTargetMotionSet()
        {
            var target = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(DefaultTargetMotionSetPath);
            if (target != null)
                return target;

            if (!EnsureFolder(DefaultPlayerMotionSetFolder))
                return null;

            target = CreateInstance<ActorAnimationMotionSet>();
            target.motionSets = new SerializedDictionary<AnimKey, MotionSetAsset>();
            AssetDatabase.CreateAsset(target, DefaultTargetMotionSetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return target;
        }

        private static bool EnsureFolder(string folder)
        {
            folder = NormalizeAssetPath(folder);
            if (AssetDatabase.IsValidFolder(folder))
                return true;
            if (string.IsNullOrEmpty(folder) || !folder.StartsWith("Assets/"))
                return false;

            string current = "Assets";
            foreach (string part in folder["Assets/".Length..].Split('/'))
            {
                if (string.IsNullOrEmpty(part))
                    continue;

                string next = $"{current}/{part}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, part);
                current = next;
            }

            return AssetDatabase.IsValidFolder(folder);
        }

        // FBX 서브에셋의 clip.name은 "Take 001"이므로 파일명을 displayName으로 파생
        private static bool IsGenericClipName(string name) =>
            !string.IsNullOrEmpty(name)
            && (name == "Take 001" || name == "__preview__Take 001" || name.StartsWith("Take "));

        private static List<(AnimationClip clip, string displayName)> FindClipsInFolder(string folder)
        {
            var result = new Dictionary<AnimationClip, string>();

            // .anim 파일
            foreach (var g in AssetDatabase.FindAssets("t:AnimationClip", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var c = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (c == null || result.ContainsKey(c)) continue;
                string dName = IsGenericClipName(c.name)
                    ? Path.GetFileNameWithoutExtension(path)
                    : c.name;
                result[c] = dName;
            }

            // FBX 서브에셋
            foreach (var g in AssetDatabase.FindAssets("t:Model", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                string fallbackName = Path.GetFileNameWithoutExtension(path);
                var reps = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
                foreach (var r in reps)
                {
                    if (r is not AnimationClip c || result.ContainsKey(c)) continue;
                    string dName = IsGenericClipName(c.name) ? fallbackName : c.name;
                    result[c] = dName;
                }
            }

            return result.OrderBy(kv => kv.Value).Select(kv => (kv.Key, kv.Value)).ToList();
        }

        private void CreateNewMappingConfig()
        {
            EnsureFolder(DefaultMappingConfigFolder);
            string path = EditorUtility.SaveFilePanelInProject(
                "WeaponMotionMapping 저장", "WeaponMotionMapping", "asset", "저장 위치 선택", DefaultMappingConfigFolder);
            if (string.IsNullOrEmpty(path)) return;
            var cfg = CreateInstance<WeaponMotionMappingConfig>();
            AssetDatabase.CreateAsset(cfg, path);
            AssetDatabase.SaveAssets();
            _mappingConfig = cfg;
        }

        private static string ToProjectRelative(string abs)
        {
            abs = abs.Replace("\\", "/");
            string dp = Application.dataPath.Replace("\\", "/");
            return abs.StartsWith(dp) ? "Assets" + abs[dp.Length..] : abs;
        }

        private static string ToAbsoluteFolderPath(string projectRelativePath)
        {
            projectRelativePath = NormalizeAssetPath(projectRelativePath);
            if (string.IsNullOrEmpty(projectRelativePath))
                return Application.dataPath;

            if (Path.IsPathRooted(projectRelativePath))
                return Directory.Exists(projectRelativePath) ? projectRelativePath : Application.dataPath;

            if (projectRelativePath == "Assets")
                return Application.dataPath;

            if (projectRelativePath.StartsWith("Assets/"))
            {
                string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
                if (!string.IsNullOrEmpty(projectRoot))
                {
                    string abs = Path.Combine(projectRoot, projectRelativePath);
                    if (Directory.Exists(abs))
                        return abs;
                }
            }

            return Application.dataPath;
        }

        private static string NormalizeAssetPath(string path) => path.Replace("\\", "/").TrimEnd('/');

        private static void DrawDivider()
        {
            EditorGUILayout.Space(2);
            Rect r = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(r, new Color(0.35f, 0.37f, 0.40f, 0.5f));
            EditorGUILayout.Space(2);
        }

        private void InitStyles()
        {
            if (_headerStyle != null) return;
            _headerStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize  = 15,
                fontStyle = FontStyle.Bold,
            };
        }
    }
}
