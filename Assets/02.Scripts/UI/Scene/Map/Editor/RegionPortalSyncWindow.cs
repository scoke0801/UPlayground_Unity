using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround;
using UPlayGround.Data.UI;

namespace UPlayGround.UI.Map.EditorTools
{
    /// <summary>
    /// 활성 씬에 배치된 <see cref="PortalActor"/>를 스캔해 지역 메타데이터(<see cref="MapRegionInfoSO"/>)의
    /// portals 목록에 자동 동기화하는 에디터 툴.
    ///
    /// ■ 동작
    ///   · 씬의 각 PortalActor → PortalEntry{label, worldPosition, targetSceneName, arrivalId}
    ///   · 대상 RegionInfo는 씬의 SceneContext.MapID + MapConfigDatabaseSO로 자동 해석(수동 지정도 가능).
    ///
    /// ■ 결과
    ///   브라우즈 모드에서 이 지역 지도를 열면 동기화된 포탈들이 아이콘으로 표시되고,
    ///   클릭하면 targetSceneName 씬으로 파스트트래블(도착 = arrivalId)한다.
    /// </summary>
    public class RegionPortalSyncWindow : EditorWindow
    {
        private MapRegionInfoSO     _target;
        private MapConfigDatabaseSO _database;
        private bool _includeInMapTeleport;   // 씬 내 텔레포트(대상 씬 없음)도 포함할지
        private bool _preserveArrivalIds = true;   // 포탈에 arrivalId가 없으면 기존 항목 값 유지
        private Vector2 _scroll;
        private readonly List<PortalActor> _found = new();

        [MenuItem("UPlayGround/Map/씬 포탈 → RegionInfo 동기화")]
        public static void Open()
        {
            var win = GetWindow<RegionPortalSyncWindow>("포탈 동기화");
            win.minSize = new Vector2(420, 360);
            win.TryAutoResolveTarget();
            win.Rescan();
        }

        private void OnFocus()
        {
            TryAutoResolveTarget();
            Rescan();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "활성 씬의 PortalActor를 스캔해 대상 RegionInfo의 portals에 기록합니다.\n" +
                "포탈 위치/대상 씬/도착 지점을 매번 손으로 옮길 필요가 없습니다.",
                MessageType.Info);

            EditorGUILayout.Space();
            _database = (MapConfigDatabaseSO)EditorGUILayout.ObjectField(
                new GUIContent("Map Config Database", "SceneContext.MapID로 RegionInfo를 자동 해석할 때 사용"),
                _database, typeof(MapConfigDatabaseSO), false);

            using (new EditorGUILayout.HorizontalScope())
            {
                _target = (MapRegionInfoSO)EditorGUILayout.ObjectField(
                    "대상 RegionInfo", _target, typeof(MapRegionInfoSO), false);
                if (GUILayout.Button("씬에서 자동 해석", GUILayout.Width(120)))
                    TryAutoResolveTarget();
            }

            _includeInMapTeleport = EditorGUILayout.Toggle(
                new GUIContent("맵 내 텔레포트 포함", "대상 씬이 없는 InMapTeleport 포탈도 포함(보통 불필요)"),
                _includeInMapTeleport);
            _preserveArrivalIds = EditorGUILayout.Toggle(
                new GUIContent("빈 arrivalId는 기존값 유지", "포탈에 도착 지점이 비어 있으면 기존 항목의 arrivalId를 보존"),
                _preserveArrivalIds);

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("다시 스캔"))
                    Rescan();

                using (new EditorGUI.DisabledScope(_target == null))
                {
                    GUI.backgroundColor = new Color(0.5f, 0.85f, 0.6f);
                    if (GUILayout.Button("스캔 & 동기화", GUILayout.Height(24)))
                        Sync();
                    GUI.backgroundColor = Color.white;
                }
            }

            if (_target == null)
                EditorGUILayout.HelpBox("대상 RegionInfo를 지정하세요. (Database + 씬 SceneContext가 있으면 자동 해석됩니다.)",
                    MessageType.Warning);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"발견된 포탈: {CountEligible()} / {_found.Count}", EditorStyles.boldLabel);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var p in _found)
            {
                if (p == null) continue;
                bool eligible = IsEligible(p);
                using (new EditorGUI.DisabledScope(!eligible))
                using (new EditorGUILayout.HorizontalScope("box"))
                {
                    EditorGUILayout.ObjectField(p, typeof(PortalActor), true);
                    string tgt = p.Type == PortalType.SceneTransition
                        ? (string.IsNullOrEmpty(p.TargetSceneName) ? "<대상 씬 없음>" : p.TargetSceneName)
                        : "(맵 내 텔레포트)";
                    EditorGUILayout.LabelField($"→ {tgt}", GUILayout.Width(160));
                }
            }
            EditorGUILayout.EndScrollView();
        }

        // ── 로직 ────────────────────────────────────────────────

        private void TryAutoResolveTarget()
        {
            if (_database == null) return;
            var ctx = FindFirstObjectByType<SceneContext>();
            if (ctx == null || string.IsNullOrEmpty(ctx.MapID)) return;

            var ri = _database.GetRegionInfo(ctx.MapID);
            if (ri != null) _target = ri;
        }

        private void Rescan()
        {
            _found.Clear();
            _found.AddRange(FindObjectsByType<PortalActor>(FindObjectsSortMode.InstanceID));
        }

        private bool IsEligible(PortalActor p)
        {
            if (p == null || !p.ShowOnMap) return false;
            if (p.Type != PortalType.SceneTransition && !_includeInMapTeleport) return false;
            return true;
        }

        private int CountEligible()
        {
            int n = 0;
            foreach (var p in _found) if (IsEligible(p)) n++;
            return n;
        }

        private void Sync()
        {
            if (_target == null) return;
            Rescan();

            // 기존 항목의 arrivalId 보존 맵 (label 기준)
            var oldArrival = new Dictionary<string, string>();
            if (_target.portals != null)
                foreach (var e in _target.portals)
                    if (!string.IsNullOrEmpty(e.label) && !oldArrival.ContainsKey(e.label))
                        oldArrival[e.label] = e.arrivalId;

            var result = new List<MapRegionInfoSO.PortalEntry>();
            foreach (var p in _found)
            {
                if (!IsEligible(p)) continue;

                string arrivalId = p.TargetArrivalId;
                if (string.IsNullOrEmpty(arrivalId) && _preserveArrivalIds)
                    oldArrival.TryGetValue(p.MapLabel, out arrivalId);

                result.Add(new MapRegionInfoSO.PortalEntry
                {
                    label           = p.MapLabel,
                    worldPosition   = p.transform.position,
                    targetSceneName = p.Type == PortalType.SceneTransition ? p.TargetSceneName : string.Empty,
                    arrivalId       = arrivalId,
                });
            }

            Undo.RecordObject(_target, "Sync Scene Portals → RegionInfo");
            _target.portals = result;
            EditorUtility.SetDirty(_target);
            AssetDatabase.SaveAssets();

            Debug.Log($"[RegionPortalSync] '{_target.name}'에 포탈 {result.Count}개 동기화 완료.", _target);
            EditorUtility.DisplayDialog("포탈 동기화",
                $"'{_target.name}'에 포탈 {result.Count}개를 동기화했습니다.", "확인");
        }
    }
}
