using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Animation.Editor
{
    [CustomEditor(typeof(MotionTestRegistrySO))]
    public class MotionTestRegistrySOEditor : UnityEditor.Editor
    {
        static readonly Color ColorDanger  = new(0.85f, 0.35f, 0.35f);
        static readonly Color ColorSync    = new(0.45f, 0.75f, 1.00f);
        static readonly Color ColorFind    = new(0.60f, 0.85f, 0.60f);

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var registry = (MotionTestRegistrySO)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("자동 동기화", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "ActorDatabase를 참조하면 항목을 자동으로 채울 수 있습니다.\n" +
                "idleClip / spawnOffset은 자동 설정되지 않으므로 필요 시 수동 입력하세요.",
                MessageType.None);

            // DB가 없으면 자동 찾기 버튼 표시
            if (registry.sourceDatabase == null)
            {
                GUI.backgroundColor = ColorFind;
                if (GUILayout.Button("프로젝트에서 ActorDatabase 자동 찾기", GUILayout.Height(28)))
                    TryAutoFindDatabase(registry);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.HelpBox("sourceDatabase 필드에 ActorDatabase를 직접 드래그하거나 위 버튼을 사용하세요.", MessageType.Warning);
                return;
            }

            // 현재 항목 수 표시
            int total   = registry.sourceDatabase.All.Count(d => d != null && d.prefab != null);
            int current = registry.entries.Count;
            EditorGUILayout.LabelField($"DB: {total}개 (prefab 있음)  /  현재 등록: {current}개", EditorStyles.miniLabel);

            EditorGUILayout.Space(4);

            // ── 타입별 추가 버튼 ──
            EditorGUILayout.LabelField("미등록 항목 추가 (중복 스킵)", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            {
                DrawSyncButton(registry, "전체",    ActorType.None);
                DrawSyncButton(registry, "Monster", ActorType.Monster);
                DrawSyncButton(registry, "Player",  ActorType.Player);
                DrawSyncButton(registry, "NPC",     ActorType.NPC);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            // ── 위험 구역 ──
            EditorGUILayout.LabelField("위험 구역", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            {
                GUI.backgroundColor = ColorDanger;
                if (GUILayout.Button("초기화 후 전체 재동기화"))
                {
                    if (EditorUtility.DisplayDialog("경고",
                        "현재 모든 항목(idleClip, spawnOffset 포함)이 삭제됩니다.\n계속하시겠습니까?",
                        "삭제 후 재동기화", "취소"))
                    {
                        registry.entries.Clear();
                        SyncFromDatabase(registry, ActorType.None);
                    }
                }
                if (GUILayout.Button("목록 전체 비우기"))
                {
                    if (EditorUtility.DisplayDialog("경고", "모든 항목을 삭제합니다.", "삭제", "취소"))
                    {
                        Undo.RecordObject(registry, "Clear MotionTestRegistry");
                        registry.entries.Clear();
                        EditorUtility.SetDirty(registry);
                    }
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawSyncButton(MotionTestRegistrySO registry, string label, ActorType filter)
        {
            GUI.backgroundColor = ColorSync;
            if (GUILayout.Button(label))
                SyncFromDatabase(registry, filter);
            GUI.backgroundColor = Color.white;
        }

        static void TryAutoFindDatabase(MotionTestRegistrySO registry)
        {
            string[] guids = AssetDatabase.FindAssets("t:ActorDatabase");
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("찾기 실패", "프로젝트에서 ActorDatabase를 찾을 수 없습니다.", "확인");
                return;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var db = AssetDatabase.LoadAssetAtPath<ActorDatabase>(path);
            if (db == null) return;

            Undo.RecordObject(registry, "Auto-find ActorDatabase");
            registry.sourceDatabase = db;
            EditorUtility.SetDirty(registry);
            Debug.Log($"[MotionTestRegistry] ActorDatabase 자동 설정: {path}");
        }

        static void SyncFromDatabase(MotionTestRegistrySO registry, ActorType filter)
        {
            if (registry.sourceDatabase == null) return;

            Undo.RecordObject(registry, "Sync MotionTestRegistry");

            int added = 0;
            foreach (var def in registry.sourceDatabase.All)
            {
                if (def == null || def.prefab == null) continue;

                // 타입 필터 (None = 전체)
                if (filter != ActorType.None && !def.actorType.HasFlag(filter)) continue;

                // 이미 등록된 항목 스킵
                if (registry.entries.Any(e => e.actorDef == def)) continue;

                registry.entries.Add(new MotionTestRegistrySO.Entry { actorDef = def });
                added++;
            }

            if (added > 0)
            {
                EditorUtility.SetDirty(registry);
                AssetDatabase.SaveAssets();
            }

            string filterLabel = filter == ActorType.None ? "전체" : filter.ToString();
            Debug.Log($"[MotionTestRegistry] [{filterLabel}] {added}개 항목 추가됨 (총 {registry.entries.Count}개)");
        }
    }
}
