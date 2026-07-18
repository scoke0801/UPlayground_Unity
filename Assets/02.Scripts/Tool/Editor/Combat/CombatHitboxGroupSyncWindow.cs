#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Tool.Editor.Combat
{
    /// <summary>
    /// HitBox(CombatHitbox.groupId)와 공격 데이터/모션이벤트(hitboxGroupId)의 그룹 ID를
    /// 한 번에 rename(매핑)하는 창. 양쪽 값이 어긋나 런타임 BeginGroup이 실패하는 문제를 해소한다.
    /// </summary>
    public sealed class CombatHitboxGroupSyncWindow : EditorWindow
    {
        private const string EmptyLabel = "(비어있음 → Phase/Default 폴백)";

        [SerializeField] private GameObject _hitboxRoot;
        [SerializeField] private List<UnityEngine.Object> _assets = new();
        [SerializeField] private bool _affectHitboxes = true;
        [SerializeField] private bool _affectAssets = true;
        [SerializeField] private CombatHitboxSetupProfileSO _profile;
        [SerializeField] private bool _syncProfileGroup = true;
        [SerializeField] private bool _assetsFoldout = true;
        [SerializeField] private Vector2 _scroll;
        [SerializeField] private Vector2 _assetScroll;

        private List<CombatHitboxGroupSyncUtility.GroupUsage> _usages;
        private string _report;
        private readonly List<string> _pendingWarnings = new();

        // 자동 수집 컨텍스트(무기 타입 스코프용). 도메인 리로드 시 재수집하면 됨.
        private CharacterModelData _ctxModel;
        private PlayerActorAnimationMotionSet _ctxContainer;
        private List<WeaponType> _availableWeaponTypes = new();
        [SerializeField] private WeaponType _weaponFilter;

        [MenuItem("UPlayGround/게임플레이/전투/도구/HitBox 그룹 ID 동기화", priority = UPlayGround.Tool.Editor.UPlaygroundMenuPriority.GameplayCombatTools + 4)]
        private static void OpenMenu() => Open(Selection.activeGameObject, null);

        public static void Open(GameObject hitboxRoot, CombatHitboxSetupProfileSO profile)
        {
            var window = GetWindow<CombatHitboxGroupSyncWindow>("HitBox 그룹 동기화");
            if (hitboxRoot != null)
                window._hitboxRoot = hitboxRoot;
            if (profile != null)
                window._profile = profile;
            window._usages = null;
            window._report = null;
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("HitBox ↔ Attack Data / MotionSet 그룹 ID 동기화", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "HitBox(CombatHitbox.groupId)와 공격 데이터/모션이벤트(hitboxGroupId)를 함께 바꿉니다.\n"
                + "런타임 우선순위: BeginCollisionEvent > HitPhaseData > Default. 세 곳이 같아야 판정이 동작합니다.",
                MessageType.Info);

            _hitboxRoot = (GameObject)EditorGUILayout.ObjectField("HitBox 루트", _hitboxRoot, typeof(GameObject), true);

            using (new EditorGUI.DisabledScope(_hitboxRoot == null))
            {
                if (GUILayout.Button("부모 계층에서 자동 수집 (CharacterModelData · PlayerActorAnimator)"))
                    AutoCollect();
            }
            EditorGUILayout.HelpBox(
                    "HitBox 루트의 상위 부모에서 CharacterModelData.abilitySet과 "
                + "PlayerActorAnimator의 해당 무기 타입 MotionSet(이벤트)만 찾아 대상 에셋을 구성합니다.",
                MessageType.None);

            DrawWeaponTypeFilter();

            DrawAssetList();

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_hitboxRoot == null && _assets.All(a => a == null)))
                {
                    if (GUILayout.Button("그룹 사용 현황 분석", GUILayout.Height(24f)))
                        Analyze();
                }
            }

            if (_usages != null)
                DrawUsageTable();

            if (!string.IsNullOrEmpty(_report))
                EditorGUILayout.HelpBox(_report, MessageType.None);
        }

        private void DrawWeaponTypeFilter()
        {
            if (_availableWeaponTypes == null || _availableWeaponTypes.Count == 0)
                return;

            using (new EditorGUILayout.HorizontalScope())
            {
                string[] names = _availableWeaponTypes.Select(t => t.ToString()).ToArray();
                int current = Mathf.Max(0, _availableWeaponTypes.IndexOf(_weaponFilter));
                int picked = EditorGUILayout.Popup("무기 타입", current, names);
                if (picked != current)
                    _weaponFilter = _availableWeaponTypes[picked];

                if (GUILayout.Button("이 무기로 재수집", GUILayout.Width(120f)))
                    RebuildAutoAssets();
            }
        }

        private void DrawAssetList()
        {
            _assetsFoldout = EditorGUILayout.Foldout(
                _assetsFoldout, $"대상 에셋 (Ability Payload / MotionSet) — {_assets.Count}개", true);
            if (_assetsFoldout)
            {
                _assetScroll = EditorGUILayout.BeginScrollView(_assetScroll, GUILayout.MaxHeight(140f));
                for (int i = 0; i < _assets.Count; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _assets[i] = EditorGUILayout.ObjectField(_assets[i], typeof(UnityEngine.Object), false);
                        if (GUILayout.Button("−", GUILayout.Width(24f)))
                        {
                            _assets.RemoveAt(i);
                            i--;
                        }
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("에셋 추가"))
                    _assets.Add(null);
                if (GUILayout.Button("선택 항목 추가"))
                    foreach (UnityEngine.Object selected in Selection.objects)
                        if (selected != null && !_assets.Contains(selected) && AssetDatabase.Contains(selected))
                            _assets.Add(selected);
                if (GUILayout.Button("전체 비우기"))
                    _assets.Clear();
            }
        }

        private void AutoCollect()
        {
            if (!CombatHitboxGroupSyncUtility.TryResolveContext(_hitboxRoot, out _ctxModel, out _ctxContainer))
            {
                _report = "상위/하위에 CharacterModelData / PlayerActorAnimator를 찾지 못했습니다.";
                return;
            }

            _availableWeaponTypes = CombatHitboxGroupSyncUtility.GetWeaponTypes(_ctxContainer);
            // 기본 무기 타입: CharacterModelData.defaultWeaponType가 목록에 있으면 그것, 아니면 첫 항목.
            if (_ctxModel != null && _availableWeaponTypes.Contains(_ctxModel.defaultWeaponType))
                _weaponFilter = _ctxModel.defaultWeaponType;
            else if (_availableWeaponTypes.Count > 0)
                _weaponFilter = _availableWeaponTypes[0];

            RebuildAutoAssets();
        }

        // 선택된 무기 타입 기준으로 대상 에셋을 새로 구성한다(과수집 방지를 위해 교체 방식).
        private void RebuildAutoAssets()
        {
            _assets.Clear();
            int data = CombatHitboxGroupSyncUtility.CollectAttackData(_ctxModel, _assets);
            int motion = CombatHitboxGroupSyncUtility.CollectMotionSetsForWeapon(_ctxContainer, _weaponFilter, _assets);
            Analyze();
            _report = $"자동 수집: AttackData {data}개 · MotionSet {motion}개 (무기 타입 '{_weaponFilter}')."
                      + (motion == 0 && _ctxContainer != null
                          ? "\nMotionSet이 0개라면 해당 무기 타입에 충돌 이벤트가 없거나, SerializeReference 순회 문제일 수 있습니다."
                          : string.Empty);
        }

        private void Analyze()
        {
            _assets = _assets.Where(a => a != null).Distinct().ToList();
            _usages = CombatHitboxGroupSyncUtility.Collect(ResolveAnalysisRoot(), _assets);
            _report = null;
        }

        // 분석은 원본 그대로 읽어도 무방하다(프리팹 에셋/씬 모두 GetComponentsInChildren 가능).
        private GameObject ResolveAnalysisRoot() => _hitboxRoot;

        private void DrawUsageTable()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("그룹 사용 현황 / 새 그룹 ID", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("그룹 ID", GUILayout.Width(160f));
                EditorGUILayout.LabelField("HitBox", GUILayout.Width(55f));
                EditorGUILayout.LabelField("Data", GUILayout.Width(45f));
                EditorGUILayout.LabelField("Event", GUILayout.Width(50f));
                EditorGUILayout.LabelField("→ 새 그룹 ID");
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(220f));
            foreach (CombatHitboxGroupSyncUtility.GroupUsage usage in _usages)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    string label = string.IsNullOrEmpty(usage.GroupId) ? EmptyLabel : $"\"{usage.GroupId}\"";
                    EditorGUILayout.LabelField(label, GUILayout.Width(160f));
                    EditorGUILayout.LabelField(usage.HitboxCount.ToString(), GUILayout.Width(55f));
                    EditorGUILayout.LabelField(usage.DataPhaseCount.ToString(), GUILayout.Width(45f));
                    EditorGUILayout.LabelField(usage.EventCount.ToString(), GUILayout.Width(50f));
                    usage.NewGroupId = EditorGUILayout.TextField(usage.NewGroupId ?? string.Empty);
                }
            }
            EditorGUILayout.EndScrollView();

            DrawMismatchHint();

            _affectHitboxes = EditorGUILayout.ToggleLeft("HitBox 변경", _affectHitboxes);
            _affectAssets = EditorGUILayout.ToggleLeft("Attack Data / MotionSet 변경", _affectAssets);

            _profile = (CombatHitboxSetupProfileSO)EditorGUILayout.ObjectField(
                "프로필 (선택)", _profile, typeof(CombatHitboxSetupProfileSO), false);
            using (new EditorGUI.DisabledScope(_profile == null))
                _syncProfileGroup = EditorGUILayout.ToggleLeft(
                    "프로필 기본 그룹도 갱신 (단일 매핑일 때, 재생성 중복 방지)", _syncProfileGroup);

            GUI.backgroundColor = new Color(0.35f, 0.8f, 0.45f);
            if (GUILayout.Button("매핑 적용", GUILayout.Height(30f)))
                Apply();
            GUI.backgroundColor = Color.white;
        }

        private void DrawMismatchHint()
        {
            // HitBox에만 있고 Data/Event에 없는 그룹, 또는 그 반대 → 런타임 불일치 위험.
            var hitboxOnly = _usages.Where(u => u.HitboxCount > 0 && u.DataPhaseCount == 0 && u.EventCount == 0)
                .Select(u => Display(u.GroupId)).ToList();
            // 빈 문자열 그룹은 정상 폴백(이벤트 비움 → HitPhaseData/Default)이므로 불일치로 보지 않는다.
            // 비어 있지 않은데 HitBox에 없는 그룹만 "요구하는 공격이 판정 실패"로 경고한다.
            var dataOnly = _usages.Where(u => !string.IsNullOrEmpty(u.GroupId)
                    && u.HitboxCount == 0 && (u.DataPhaseCount > 0 || u.EventCount > 0))
                .Select(u => Display(u.GroupId)).ToList();
            if (hitboxOnly.Count == 0 && dataOnly.Count == 0)
                return;

            var sb = new System.Text.StringBuilder("그룹 불일치 가능성:");
            if (hitboxOnly.Count > 0)
                sb.Append($"\n• HitBox에만 존재: {string.Join(", ", hitboxOnly)}");
            if (dataOnly.Count > 0)
                sb.Append($"\n• 데이터/이벤트에만 존재: {string.Join(", ", dataOnly)} (이 그룹을 요구하는 공격은 판정이 실패할 수 있음)");
            EditorGUILayout.HelpBox(sb.ToString(), MessageType.Warning);
        }

        private void Apply()
        {
            var map = _usages
                .Where(u => (u.NewGroupId ?? string.Empty) != u.GroupId)
                .ToDictionary(u => u.GroupId ?? string.Empty, u => u.NewGroupId ?? string.Empty);
            if (map.Count == 0)
            {
                ShowNotification(new GUIContent("변경할 매핑이 없습니다."));
                return;
            }

            string summary = string.Join("\n", map.Select(kv =>
                $"'{Display(kv.Key)}' → '{Display(kv.Value)}'"));
            if (!EditorUtility.DisplayDialog("그룹 ID 동기화",
                $"다음 그룹 ID를 변경합니다:\n\n{summary}\n\n"
                + $"적용 대상: {(_affectHitboxes ? "HitBox " : "")}{(_affectAssets ? "Data/Event" : "")}\n계속할까요?",
                "적용", "취소"))
                return;

            int hitboxChanged = 0;
            int assetChanged = 0;
            _pendingWarnings.Clear();

            if (_affectAssets)
                foreach (UnityEngine.Object asset in _assets)
                    assetChanged += CombatHitboxGroupSyncUtility.RemapInAsset(asset, map);

            if (_affectHitboxes && _hitboxRoot != null)
                hitboxChanged = ApplyHitboxRemap(map);

            int profileChanged = SyncProfileIfRequested(map);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Analyze();

            string report = $"완료 — HitBox {hitboxChanged}개 · Data/Event {assetChanged}개"
                            + (profileChanged > 0 ? " · 프로필 기본 그룹 갱신" : "");
            if (_pendingWarnings.Count > 0)
                report += "\n\n" + string.Join("\n", _pendingWarnings);
            _report = report;
        }

        private int ApplyHitboxRemap(IReadOnlyDictionary<string, string> map)
        {
            string path = AssetDatabase.GetAssetPath(_hitboxRoot);
            if (!string.IsNullOrEmpty(path)
                && string.Equals(Path.GetExtension(path), ".fbx", System.StringComparison.OrdinalIgnoreCase))
            {
                _pendingWarnings.Add("FBX 원본은 수정할 수 없어 HitBox는 건너뜀. Prefab Variant에서 실행하세요.");
                return 0;
            }

            if (PrefabUtility.IsPartOfPrefabAsset(_hitboxRoot))
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    int changed = CombatHitboxGroupSyncUtility.RemapInHitboxes(contents, map);
                    if (changed > 0)
                        PrefabUtility.SaveAsPrefabAsset(contents, path);
                    return changed;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }

            // 씬의 프리팹 인스턴스: 변경이 인스턴스 오버라이드로만 남아 프리팹 원본/다른 인스턴스에 반영되지 않는다.
            // 무기가 런타임 장착 프리팹이면 이 인스턴스 수정은 실제 적용에 의미가 없을 수 있다.
            if (PrefabUtility.IsPartOfPrefabInstance(_hitboxRoot))
                _pendingWarnings.Add(
                    "주의: 씬의 프리팹 인스턴스라 HitBox 변경이 인스턴스 오버라이드로만 적용됩니다. "
                    + "프리팹 원본에 반영하려면 원본 프리팹을 대상으로 실행하세요.");

            Undo.RegisterFullObjectHierarchyUndo(_hitboxRoot, "HitBox 그룹 동기화");
            return CombatHitboxGroupSyncUtility.RemapInHitboxes(_hitboxRoot, map);
        }

        // 단일 그룹 매핑일 때만 프로필 기본 그룹을 새 값으로 맞춰 재생성 시 중복 생성을 막는다.
        private int SyncProfileIfRequested(Dictionary<string, string> map)
        {
            if (_profile == null || !_syncProfileGroup || map.Count != 1)
                return 0;

            string to = map.Values.First();
            if (string.IsNullOrWhiteSpace(to))
                return 0;

            var so = new SerializedObject(_profile);
            SerializedProperty prop = so.FindProperty("_defaultGroupId");
            if (prop == null || prop.stringValue == to)
                return 0;
            prop.stringValue = to;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(_profile);
            return 1;
        }

        private static string Display(string groupId) => string.IsNullOrEmpty(groupId) ? "(비어있음)" : groupId;
    }
}
#endif
