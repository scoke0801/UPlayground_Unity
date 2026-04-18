using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Actor.Animation.Editor
{
    [CustomEditor(typeof(ActorAnimationMotionSet))]
    public class ActorAnimationMotionSetEditor : UnityEditor.Editor
    {
        static readonly (string label, int min, int max)[] KEY_RANGES =
        {
            ("이동",       0,   29),
            ("공격",       100, 199),
            ("강공격",     200, 299),
            ("대시 공격",  300, 399),
            ("점프 공격",  400, 499),
            ("스킬",       500, 619),
            ("차지/피니시",620, 699),
            ("피격/사망",  700, 919),
            ("기타",       920, int.MaxValue),
        };

        static AnimKey[] _allKeys;
        static AnimKey[] AllKeys => _allKeys ??= (AnimKey[])System.Enum.GetValues(typeof(AnimKey));

        bool[] _foldouts;

        void OnEnable()
        {
            _foldouts = Enumerable.Repeat(true, KEY_RANGES.Length).ToArray();
        }

        public override void OnInspectorGUI()
        {
            var so = (ActorAnimationMotionSet)target;
            serializedObject.Update();

            // ── Fallback 필드 ──
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("공용 모션 (Fallback)", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            var fallbackProp = serializedObject.FindProperty("fallbackMotionSet");
            EditorGUILayout.PropertyField(fallbackProp, new GUIContent("Fallback MotionSet",
                "이 SO에 없는 AnimKey는 Fallback에서 탐색 (최대 8단계 체인)"));
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            DrawDivider();

            // ── 체인 전체 키 수집 ──
            var entries = CollectChainEntries(so);
            if (entries.Count == 0)
            {
                EditorGUILayout.HelpBox("등록된 모션이 없습니다. 아래 버튼으로 추가하세요.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField("모션 목록", EditorStyles.boldLabel);

                for (int g = 0; g < KEY_RANGES.Length; g++)
                {
                    var (label, min, max) = KEY_RANGES[g];
                    var group = entries
                        .Where(e => (int)e.key >= min && (int)e.key <= max)
                        .OrderBy(e => (int)e.key)
                        .ToList();

                    if (group.Count == 0) continue;

                    _foldouts[g] = EditorGUILayout.Foldout(
                        _foldouts[g], $"{label}  ({group.Count})", true, EditorStyles.foldoutHeader);

                    if (_foldouts[g])
                    {
                        foreach (var entry in group)
                            DrawEntryRow(so, entry);
                        GUILayout.Space(2);
                    }
                }
            }

            DrawDivider();
            if (GUILayout.Button("+ 모션 키 추가", GUILayout.Height(28)))
                ShowAddKeyMenu(so);

            EditorGUILayout.Space(4);
        }

        // ─────────────────────────────────────────────
        struct ChainEntry
        {
            public AnimKey key;
            public ActorAnimationMotionSet source;
            public MotionSetAsset asset;
        }

        List<ChainEntry> CollectChainEntries(ActorAnimationMotionSet root)
        {
            var result  = new List<ChainEntry>();
            var seen    = new HashSet<AnimKey>();
            var visited = new HashSet<ActorAnimationMotionSet>();
            var current = root;

            while (current != null && !visited.Contains(current))
            {
                visited.Add(current);
                if (current.motionSets != null)
                {
                    foreach (var kv in current.motionSets)
                    {
                        if (seen.Add(kv.Key))
                            result.Add(new ChainEntry { key = kv.Key, source = current, asset = kv.Value });
                    }
                }
                current = current.fallbackMotionSet;
            }

            return result;
        }

        // ─────────────────────────────────────────────
        void DrawEntryRow(ActorAnimationMotionSet so, ChainEntry entry)
        {
            bool isOwn = entry.source == so;

            Rect row = EditorGUILayout.GetControlRect(false, 22f);
            EditorGUI.DrawRect(row, isOwn
                ? new Color(0.20f, 0.35f, 0.20f, 0.35f)
                : new Color(0.22f, 0.22f, 0.22f, 0.30f));

            float x   = row.x + 4f;
            float y   = row.y + 2f;
            float rem = row.xMax - x;

            // 키 이름
            const float KEY_W = 170f;
            GUI.contentColor = isOwn ? new Color(0.75f, 1f, 0.75f) : new Color(0.60f, 0.60f, 0.60f);
            GUI.Label(new Rect(x, y, KEY_W, 18f), entry.key.ToString(), EditorStyles.miniLabel);
            GUI.contentColor = Color.white;
            x += KEY_W;

            if (isOwn)
            {
                // ObjectField
                float fieldW = rem - KEY_W - 80f;
                EditorGUI.BeginChangeCheck();
                var newAsset = (MotionSetAsset)EditorGUI.ObjectField(
                    new Rect(x, y, fieldW, 18f), entry.asset, typeof(MotionSetAsset), false);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(so, "MotionSet 변경");
                    so.motionSets[entry.key] = newAsset;
                    EditorUtility.SetDirty(so);
                }
                x += fieldW + 2f;

                // 선택 버튼
                EditorGUI.BeginDisabledGroup(entry.asset == null);
                if (GUI.Button(new Rect(x, y, 36f, 18f), "선택", EditorStyles.miniButton))
                {
                    Selection.activeObject = entry.asset;
                    EditorGUIUtility.PingObject(entry.asset);
                }
                x += 38f;

                // 열기 버튼
                if (GUI.Button(new Rect(x, y, 36f, 18f), "열기", EditorStyles.miniButton))
                    OpenInMotionEditor(entry.asset);
                EditorGUI.EndDisabledGroup();

                // 삭제 버튼 (×)
                x += 38f;
                GUI.contentColor = new Color(1f, 0.5f, 0.5f);
                if (GUI.Button(new Rect(x, y, 18f, 18f), "×", EditorStyles.miniButton))
                {
                    if (EditorUtility.DisplayDialog("키 삭제",
                        $"'{entry.key}' 항목을 삭제하시겠습니까?", "삭제", "취소"))
                    {
                        Undo.RecordObject(so, "모션 키 삭제");
                        so.motionSets.Remove(entry.key);
                        EditorUtility.SetDirty(so);
                    }
                }
                GUI.contentColor = Color.white;
            }
            else
            {
                // 상속 표시
                float labelW = rem - KEY_W - 90f;
                GUI.contentColor = new Color(0.55f, 0.55f, 0.55f);
                GUI.Label(new Rect(x, y, labelW, 18f),
                    $"↑ {entry.source.name}", EditorStyles.miniLabel);
                GUI.contentColor = Color.white;
                x += labelW + 2f;

                // Override 생성
                if (GUI.Button(new Rect(x, y, 86f, 18f), "Override 생성", EditorStyles.miniButton))
                    CreateOverride(so, entry.key, entry.asset);
            }
        }

        // ─────────────────────────────────────────────
        void CreateOverride(ActorAnimationMotionSet so, AnimKey key, MotionSetAsset original)
        {
            string soPath  = AssetDatabase.GetAssetPath(so);
            string dir     = System.IO.Path.GetDirectoryName(soPath);
            string sugName = $"{so.name}_{key}.asset";
            string path    = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{sugName}");

            path = EditorUtility.SaveFilePanelInProject(
                "Override MotionSetAsset 저장", sugName, "asset", "저장 위치를 선택하세요.", dir);
            if (string.IsNullOrEmpty(path)) return;

            var asset = CreateInstance<MotionSetAsset>();
            asset.motionSet = new MotionSet { motionSetName = System.IO.Path.GetFileNameWithoutExtension(path) };
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            Undo.RecordObject(so, "Override 생성");
            so.motionSets[key] = asset;
            EditorUtility.SetDirty(so);

            EditorGUIUtility.PingObject(asset);
            OpenInMotionEditor(asset);
        }

        void ShowAddKeyMenu(ActorAnimationMotionSet so)
        {
            var existing = so.motionSets?.Keys.ToHashSet() ?? new HashSet<AnimKey>();
            var menu = new GenericMenu();

            foreach (AnimKey key in AllKeys)
            {
                if (key == AnimKey.None || existing.Contains(key)) continue;
                AnimKey captured = key;
                menu.AddItem(new GUIContent(GroupLabel(key) + "/" + key), false, () =>
                {
                    Undo.RecordObject(so, "모션 키 추가");
                    if (so.motionSets == null)
                        so.motionSets = new AYellowpaper.SerializedCollections.SerializedDictionary<AnimKey, MotionSetAsset>();
                    so.motionSets[captured] = null;
                    EditorUtility.SetDirty(so);
                });
            }

            menu.ShowAsContext();
        }

        static void OpenInMotionEditor(MotionSetAsset asset)
        {
            var window = EditorWindow.GetWindow<UPlayGround.Animation.Editor.MotionSetEditorWindow>();
            window.titleContent = new GUIContent("애니메이션 에디터");
            window.minSize      = new Vector2(600, 400);
            window.Show();
            if (asset != null)
                Selection.activeObject = asset;
        }

        string GroupLabel(AnimKey key)
        {
            int v = (int)key;
            foreach (var (label, min, max) in KEY_RANGES)
                if (v >= min && v <= max) return label;
            return "기타";
        }

        static void DrawDivider()
        {
            EditorGUILayout.Space(4);
            Rect r = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(r, new Color(0.35f, 0.37f, 0.40f, 0.5f));
            EditorGUILayout.Space(4);
        }
    }
}
