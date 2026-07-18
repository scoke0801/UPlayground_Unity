#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Tool.Editor.Combat
{
    /// <summary>
    /// 프레임 데이터 테이블 — 격투게임식 선딜/액티브/후딜 + 데미지/캔슬 정보를
    /// MotionSet 타임라인과 AttackDataSO에서 자동 합산해 한 테이블로 보여준다.
    ///
    /// 데이터 소스: 추가 입력 없음. 타이밍은 MotionSet의 Collision/ComboWindow 이벤트,
    /// 수치는 AttackDataSO의 HitPhaseData에서 읽는다.
    /// </summary>
    public class CombatFrameDataWindow : EditorWindow
    {
        [MenuItem("UPlayGround/게임플레이/전투/도구/프레임 데이터 테이블",
            priority = UPlaygroundMenuPriority.GameplayCombatTools + 2)]
        public static void Open()
        {
            var window = GetWindow<CombatFrameDataWindow>("프레임 데이터");
            window.minSize = new Vector2(900f, 420f);
            window.Show();
        }

        enum SourceMode { Player, Enemy }

        sealed class Row
        {
            public string ActorName;
            public string Source;
            public AnimKey Key;
            public float Duration;
            public float Startup;
            public float Active;
            public int HitCount;
            public float Recovery;
            public string CancelMask;
            public bool HasComboWindow;
            public float ComboStart;        // 첫 콤보 윈도우 시작 (-1 = 없음)
            public float DamageSum;
            public float PoiseSum;
            public float BreakSum;
            public string Reaction;
            public int TimelinePhases;
            public int DataPhases;
            public bool PhaseMismatch;
            public bool NoCollision;
            public MotionSetAsset Asset;
            public UnityEngine.Object Data;
        }

        SourceMode _mode = SourceMode.Player;
        AbilitySetSO _playerData;
        ActorAnimationMotionSet _playerMotionSet;
        ActorDefinitionSO _enemyActor;
        bool _includeFallback = true;
        bool _showFrames;
        int _fps = 30;

        readonly List<Row> _rows = new();
        Vector2 _scroll;
        int _sortColumn = -1;
        bool _sortAscending = true;
        string _statusMessage = "";

        // (헤더, 폭) — DrawRow와 인덱스 일치 필수
        static readonly (string Label, float Width)[] COLUMNS =
        {
            ("액터",     95f),
            ("출처",     135f),
            ("AnimKey",  150f),
            ("길이",     52f),
            ("선딜",     52f),
            ("액티브",   52f),
            ("히트",     38f),
            ("후딜",     52f),
            ("캔슬",     115f),
            ("콤보창",   52f),
            ("DMG",      52f),
            ("Poise",    52f),
            ("Break",    52f),
            ("리액션",   85f),
            ("페이즈",   70f),
        };

        void OnGUI()
        {
            DrawToolbar();
            DrawSourceFields();
            EditorGUILayout.Space(2);
            DrawTable();
        }

        // ─────────────────────────────────────────────────────────────────
        //  상단 컨트롤
        // ─────────────────────────────────────────────────────────────────
        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                var newMode = (SourceMode)GUILayout.Toolbar((int)_mode,
                    new[] { "플레이어", "몬스터" }, EditorStyles.toolbarButton, GUILayout.Width(140));
                if (newMode != _mode)
                {
                    _mode = newMode;
                    _rows.Clear();
                    _statusMessage = "";
                }

                _includeFallback = GUILayout.Toggle(_includeFallback, "Fallback 포함", EditorStyles.toolbarButton, GUILayout.Width(95));

                GUILayout.Space(8);
                _showFrames = GUILayout.Toggle(_showFrames, "F단위", EditorStyles.toolbarButton, GUILayout.Width(50));
                if (_showFrames)
                {
                    GUILayout.Label("fps", GUILayout.Width(24));
                    _fps = Mathf.Clamp(EditorGUILayout.IntField(_fps, GUILayout.Width(36)), 1, 120);
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("새로 고침", EditorStyles.toolbarButton, GUILayout.Width(70)))
                    Rebuild();

                if (_mode == SourceMode.Enemy
                    && GUILayout.Button("전체 몬스터 스캔", EditorStyles.toolbarButton, GUILayout.Width(110)))
                    RebuildAllEnemies();

                using (new EditorGUI.DisabledScope(_rows.Count == 0))
                {
                    if (GUILayout.Button("CSV 내보내기", EditorStyles.toolbarButton, GUILayout.Width(90)))
                        ExportCsv();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawSourceFields()
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUI.BeginChangeCheck();
                if (_mode == SourceMode.Player)
                {
                    _playerData = (AbilitySetSO)EditorGUILayout.ObjectField(
                        "AbilitySet", _playerData, typeof(AbilitySetSO), false);
                    _playerMotionSet = (ActorAnimationMotionSet)EditorGUILayout.ObjectField(
                        "MotionSet (무기별)", _playerMotionSet, typeof(ActorAnimationMotionSet), false);
                }
                else
                {
                    var newActor = (ActorDefinitionSO)EditorGUILayout.ObjectField(
                        "ActorDefinitionSO", _enemyActor, typeof(ActorDefinitionSO), false);
                    if (newActor != _enemyActor)
                        _enemyActor = newActor;
                }
                if (EditorGUI.EndChangeCheck())
                    Rebuild();
            }
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_statusMessage))
                EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);
        }

        // ─────────────────────────────────────────────────────────────────
        //  행 빌드
        // ─────────────────────────────────────────────────────────────────
        void Rebuild()
        {
            _rows.Clear();
            _statusMessage = "";

            if (_mode == SourceMode.Player)
            {
                if (_playerData == null || _playerMotionSet == null) return;
                AppendRows(_playerData.name.Replace("AbilitySet_", ""), _playerMotionSet, _playerData);
            }
            else if (_enemyActor != null)
            {
                AppendEnemyActorRows(_enemyActor);
            }

            ApplySort();
            _statusMessage = $"{_rows.Count}개 공격 항목.";
        }

        void RebuildAllEnemies()
        {
            _rows.Clear();
            int actorCount = 0;

            string[] guids = AssetDatabase.FindAssets("t:ActorDefinitionSO");
            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    var actor = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(AssetDatabase.GUIDToAssetPath(guids[i]));
                    if (actor == null || actor.attackData == null || actor.prefab == null) continue;

                    EditorUtility.DisplayProgressBar("프레임 데이터 스캔", actor.name, (float)i / guids.Length);
                    if (AppendEnemyActorRows(actor))
                        actorCount++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            ApplySort();
            _statusMessage = $"몬스터 {actorCount}개, 공격 {_rows.Count}개 항목.";
        }

        bool AppendEnemyActorRows(ActorDefinitionSO actor)
        {
            if (actor == null || actor.attackData == null || actor.prefab == null) return false;

            var animator = actor.prefab.GetComponentInChildren<UPlayGround.Animation.ActorAnimator>(true);
            if (animator == null || animator.MotionSet == null) return false;

            int before = _rows.Count;
            AppendRows(actor.name, animator.MotionSet, actor.attackData);
            return _rows.Count > before;
        }

        void AppendRows(string actorName, ActorAnimationMotionSet root, AttackDataSO data)
            => AppendRows(
                actorName,
                root,
                data,
                key => CombatTimelineUtility.ResolveAttacks(data, key));

        void AppendRows(string actorName, ActorAnimationMotionSet root, AbilitySetSO data)
            => AppendRows(
                actorName,
                root,
                data,
                key => CombatTimelineUtility.ResolveAttacks(data, key));

        void AppendRows(
            string actorName,
            ActorAnimationMotionSet root,
            UnityEngine.Object data,
            System.Func<AnimKey, List<CombatTimelineUtility.ResolvedAttack>> resolve)
        {
            var seen = new HashSet<AnimKey>();
            foreach (ActorAnimationMotionSet set in CombatTimelineUtility.EnumerateMotionSets(root, _includeFallback))
            {
                if (set?.motionSets == null) continue;
                foreach (KeyValuePair<AnimKey, MotionSetAsset> pair in set.motionSets)
                {
                    if (!seen.Add(pair.Key)) continue;
                    if (pair.Value == null || pair.Value.motionSet == null) continue;

                    var resolved = resolve(pair.Key);
                    foreach (CombatTimelineUtility.ResolvedAttack atk in resolved)
                        _rows.Add(BuildRow(actorName, pair.Value, data, atk));
                }
            }
        }

        Row BuildRow(
            string actorName,
            MotionSetAsset asset,
            UnityEngine.Object data,
            CombatTimelineUtility.ResolvedAttack atk)
        {
            var set = asset.motionSet;
            float total = set.TotalDuration;
            var collisions = CombatTimelineUtility.CollectCollisionSpans(set);
            var combos = CombatTimelineUtility.CollectSpans<UPlayGround.Data.Event.ComboWindowEvent>(set);

            CombatTimelineUtility.ComputeFrameMetrics(collisions, total,
                out float startup, out float active, out float recovery);

            // 데미지 합: 타임라인 Collision 스팬마다 매칭 페이즈 수치를 누적 (실제 1회 사용 기대값)
            float dmg = 0f, poise = 0f, brk = 0f;
            string reaction = "-";
            if (collisions.Count > 0)
            {
                foreach (var span in collisions)
                {
                    HitPhaseData phase = atk.GetHitPhase(span.PhaseIndex);
                    if (phase == null) continue;
                    dmg += phase.damage;
                    poise += phase.poiseDamage;
                    brk += phase.breakDamage;
                }
                HitPhaseData first = atk.GetHitPhase(collisions[0].PhaseIndex);
                if (first != null) reaction = first.reactionType.ToString();
            }
            else if (atk.HitPhases != null)
            {
                foreach (HitPhaseData phase in atk.HitPhases)
                {
                    if (phase == null) continue;
                    dmg += phase.damage;
                    poise += phase.poiseDamage;
                    brk += phase.breakDamage;
                }
                if (atk.HitPhases.Count > 0 && atk.HitPhases[0] != null)
                    reaction = atk.HitPhases[0].reactionType.ToString();
            }

            int maxIdx = -1;
            foreach (var span in collisions) maxIdx = Mathf.Max(maxIdx, span.PhaseIndex);
            int timelinePhases = maxIdx + 1;
            int dataPhases = atk.HitPhases?.Count ?? 0;

            return new Row
            {
                ActorName = actorName,
                Source = atk.SourceName,
                Key = atk.AnimKey,
                Duration = total,
                Startup = startup,
                Active = active,
                HitCount = collisions.Count,
                Recovery = recovery,
                CancelMask = CombatTimelineUtility.FormatInterruptMask(atk.InterruptActions),
                HasComboWindow = combos.Count > 0,
                ComboStart = combos.Count > 0 ? combos[0].Start : -1f,
                DamageSum = dmg,
                PoiseSum = poise,
                BreakSum = brk,
                Reaction = reaction,
                TimelinePhases = timelinePhases,
                DataPhases = dataPhases,
                PhaseMismatch = collisions.Count > 0 && timelinePhases != dataPhases,
                NoCollision = collisions.Count == 0,
                Asset = asset,
                Data = data,
            };
        }

        // ─────────────────────────────────────────────────────────────────
        //  테이블
        // ─────────────────────────────────────────────────────────────────
        void DrawTable()
        {
            if (_rows.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    _mode == SourceMode.Player
                        ? "AbilitySetSO와 무기별 MotionSet을 지정하세요."
                        : "ActorDefinitionSO를 지정하거나 [전체 몬스터 스캔]을 실행하세요.",
                    MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            // 헤더 (클릭 정렬)
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < COLUMNS.Length; i++)
            {
                string label = COLUMNS[i].Label;
                if (_sortColumn == i) label += _sortAscending ? " ▲" : " ▼";
                if (GUILayout.Button(label, EditorStyles.miniButtonMid, GUILayout.Width(COLUMNS[i].Width)))
                {
                    if (_sortColumn == i) _sortAscending = !_sortAscending;
                    else { _sortColumn = i; _sortAscending = true; }
                    ApplySort();
                }
            }
            EditorGUILayout.EndHorizontal();

            var normalStyle = new GUIStyle(EditorStyles.miniLabel) { clipping = TextClipping.Clip };
            var warnStyle = new GUIStyle(normalStyle) { normal = { textColor = new Color(1f, 0.65f, 0.2f) } };
            var dimStyle = new GUIStyle(normalStyle) { normal = { textColor = new Color(0.55f, 0.55f, 0.6f) } };

            foreach (Row row in _rows)
                DrawRow(row, normalStyle, warnStyle, dimStyle);

            EditorGUILayout.EndScrollView();
        }

        void DrawRow(Row row, GUIStyle normal, GUIStyle warn, GUIStyle dim)
        {
            GUIStyle style = row.PhaseMismatch ? warn : row.NoCollision ? dim : normal;

            Rect rowRect = EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label(row.ActorName, style, GUILayout.Width(COLUMNS[0].Width));
                GUILayout.Label(row.Source, style, GUILayout.Width(COLUMNS[1].Width));
                GUILayout.Label(row.Key.ToString(), style, GUILayout.Width(COLUMNS[2].Width));
                GUILayout.Label(FormatTime(row.Duration), style, GUILayout.Width(COLUMNS[3].Width));
                GUILayout.Label(row.NoCollision ? "-" : FormatTime(row.Startup), style, GUILayout.Width(COLUMNS[4].Width));
                GUILayout.Label(row.NoCollision ? "-" : FormatTime(row.Active), style, GUILayout.Width(COLUMNS[5].Width));
                GUILayout.Label(row.HitCount.ToString(), style, GUILayout.Width(COLUMNS[6].Width));
                GUILayout.Label(row.NoCollision ? "-" : FormatTime(row.Recovery), style, GUILayout.Width(COLUMNS[7].Width));
                GUILayout.Label(row.CancelMask, style, GUILayout.Width(COLUMNS[8].Width));
                GUILayout.Label(row.HasComboWindow ? FormatTime(row.ComboStart) : "-", style, GUILayout.Width(COLUMNS[9].Width));
                GUILayout.Label(row.DamageSum.ToString("0.#"), style, GUILayout.Width(COLUMNS[10].Width));
                GUILayout.Label(row.PoiseSum.ToString("0.#"), style, GUILayout.Width(COLUMNS[11].Width));
                GUILayout.Label(row.BreakSum.ToString("0.#"), style, GUILayout.Width(COLUMNS[12].Width));
                GUILayout.Label(row.Reaction, style, GUILayout.Width(COLUMNS[13].Width));

                string phaseText = row.NoCollision
                    ? "이벤트 없음"
                    : row.PhaseMismatch
                        ? $"{row.TimelinePhases}↔{row.DataPhases} ⚠"
                        : row.DataPhases.ToString();
                GUILayout.Label(phaseText, style, GUILayout.Width(COLUMNS[14].Width));
            }
            EditorGUILayout.EndHorizontal();

            // 행 클릭: 좌클릭 = 에셋 핑, 우클릭 = 에디터 열기 메뉴
            Event evt = Event.current;
            if (evt.type == EventType.MouseDown && rowRect.Contains(evt.mousePosition))
            {
                if (evt.button == 0)
                {
                    EditorGUIUtility.PingObject(row.Asset);
                }
                else if (evt.button == 1)
                {
                    var menu = new GenericMenu();
                    var capturedRow = row;
                    menu.AddItem(new GUIContent("애니메이션 에디터에서 열기"), false,
                        () => UPlayGround.Animation.Editor.MotionSetEditorWindow.Open(capturedRow.Asset));
                    menu.AddItem(new GUIContent("데이터 에셋 선택"), false, () =>
                    {
                        Selection.activeObject = capturedRow.Data;
                        EditorGUIUtility.PingObject(capturedRow.Data);
                    });
                    menu.ShowAsContext();
                }
                evt.Use();
            }
        }

        string FormatTime(float seconds)
        {
            if (seconds < 0f) return "-";
            return _showFrames ? $"{Mathf.RoundToInt(seconds * _fps)}F" : $"{seconds:0.00}s";
        }

        // ─────────────────────────────────────────────────────────────────
        //  정렬 / 내보내기
        // ─────────────────────────────────────────────────────────────────
        void ApplySort()
        {
            if (_sortColumn < 0) return;

            Comparison<Row> cmp;
            switch (_sortColumn)
            {
                case 0:  cmp = (a, b) => string.Compare(a.ActorName, b.ActorName, StringComparison.Ordinal); break;
                case 1:  cmp = (a, b) => string.Compare(a.Source, b.Source, StringComparison.Ordinal); break;
                case 2:  cmp = (a, b) => ((int)a.Key).CompareTo((int)b.Key); break;
                case 3:  cmp = (a, b) => a.Duration.CompareTo(b.Duration); break;
                case 4:  cmp = (a, b) => a.Startup.CompareTo(b.Startup); break;
                case 5:  cmp = (a, b) => a.Active.CompareTo(b.Active); break;
                case 6:  cmp = (a, b) => a.HitCount.CompareTo(b.HitCount); break;
                case 7:  cmp = (a, b) => a.Recovery.CompareTo(b.Recovery); break;
                case 8:  cmp = (a, b) => string.Compare(a.CancelMask, b.CancelMask, StringComparison.Ordinal); break;
                case 9:  cmp = (a, b) => a.ComboStart.CompareTo(b.ComboStart); break;
                case 10: cmp = (a, b) => a.DamageSum.CompareTo(b.DamageSum); break;
                case 11: cmp = (a, b) => a.PoiseSum.CompareTo(b.PoiseSum); break;
                case 12: cmp = (a, b) => a.BreakSum.CompareTo(b.BreakSum); break;
                case 13: cmp = (a, b) => string.Compare(a.Reaction, b.Reaction, StringComparison.Ordinal); break;
                case 14: cmp = (a, b) => a.DataPhases.CompareTo(b.DataPhases); break;
                default: return;
            }

            _rows.Sort(cmp);
            if (!_sortAscending) _rows.Reverse();
        }

        void ExportCsv()
        {
            string path = EditorUtility.SaveFilePanel(
                "프레임 데이터 CSV 저장", Application.dataPath, "FrameData.csv", "csv");
            if (string.IsNullOrWhiteSpace(path)) return;

            var sb = new StringBuilder();
            sb.AppendLine("Actor,Source,AnimKey,Duration,Startup,Active,Hits,Recovery,Cancel,ComboStart,Damage,Poise,Break,Reaction,TimelinePhases,DataPhases,Mismatch");
            foreach (Row row in _rows)
            {
                sb.AppendLine(string.Join(",",
                    Csv(row.ActorName), Csv(row.Source), row.Key,
                    row.Duration.ToString("0.###", CultureInfo.InvariantCulture),
                    row.Startup.ToString("0.###", CultureInfo.InvariantCulture),
                    row.Active.ToString("0.###", CultureInfo.InvariantCulture),
                    row.HitCount,
                    row.Recovery.ToString("0.###", CultureInfo.InvariantCulture),
                    Csv(row.CancelMask),
                    row.ComboStart.ToString("0.###", CultureInfo.InvariantCulture),
                    row.DamageSum.ToString("0.###", CultureInfo.InvariantCulture),
                    row.PoiseSum.ToString("0.###", CultureInfo.InvariantCulture),
                    row.BreakSum.ToString("0.###", CultureInfo.InvariantCulture),
                    row.Reaction,
                    row.TimelinePhases,
                    row.DataPhases,
                    row.PhaseMismatch ? "Y" : ""));
            }

            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
            EditorUtility.RevealInFinder(path);
        }

        static string Csv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Contains(',') ? $"\"{value}\"" : value;
        }
    }
}
#endif
