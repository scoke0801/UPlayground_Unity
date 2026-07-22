#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Components;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Tool.Editor.Combat
{
    public sealed class CombatHitboxSetupWindow : EditorWindow
    {
        [SerializeField] private GameObject _target;
        [SerializeField] private CombatHitboxSetupProfileSO _profile;
        [SerializeField] private CombatHitboxSetupMode _mode = CombatHitboxSetupMode.WeaponAutoFit;
        [SerializeField] private bool _useAutomaticMode = true;
        [SerializeField] private bool _forceRefit;
        [SerializeField] private bool _showAdvanced;
        [SerializeField] private Vector2 _scroll;
        [SerializeField] private bool _showHitboxList = true;

        private readonly List<CombatHitboxSetupResult> _results = new();
        private readonly List<GameObject> _resultHitboxes = new();

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/게임플레이/전투/도구/HitBox 셋업", priority = UPlayGround.Tool.Editor.UPlaygroundMenuPriority.GameplayCombatTools + 3)]
        private static void Open()
        {
            GetWindow<CombatHitboxSetupWindow>("Combat HitBox Setup");
        }

        private void OnEnable()
        {
            Selection.selectionChanged += HandleSelectionChanged;
            if (_target == null)
                _target = Selection.activeGameObject;
            CollectResultHitboxes();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= HandleSelectionChanged;
        }

        private void HandleSelectionChanged()
        {
            GameObject selected = Selection.activeGameObject;
            // 결과 목록의 HitBox를 클릭해 선택한 경우엔 타깃을 바꾸지 않는다(목록이 무너지지 않도록).
            if (selected != null && !_resultHitboxes.Contains(selected))
            {
                _target = selected;
                // 대상이 바뀌면 이전 대상의 실행 결과는 무효다. ObjectField 변경 경로와 동일하게 비워
                // "무기 분석인데 신체 결과가 보이는" 잔존 결과 혼란을 막는다.
                _results.Clear();
                CollectResultHitboxes();
            }
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("부착형 Combat HitBox 자동 설정", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Hierarchy 또는 Project에서 루트를 선택하면 하위 계층을 분석해 생성 방식을 자동 결정합니다.",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            _target = (GameObject)EditorGUILayout.ObjectField(
                "대상 루트",
                _target,
                typeof(GameObject),
                true);
            if (EditorGUI.EndChangeCheck())
            {
                _results.Clear();
                if (_target != null)
                    Selection.activeGameObject = _target;
                CollectResultHitboxes();
            }

            CombatHitboxTargetAnalysis analysis = CombatHitboxAutoFitter.Analyze(_target);
            DrawAnalysis(analysis);

            _profile = (CombatHitboxSetupProfileSO)EditorGUILayout.ObjectField(
                "생성 프로필 (선택)",
                _profile,
                typeof(CombatHitboxSetupProfileSO),
                false);

            DrawProfileGuidance();

            CombatHitboxSetupMode resolvedMode = ResolveMode(analysis);
            // 프로필의 TargetKind(의도)와 실제 선택 대상의 계층 분석 결과가 어긋나면 경고한다.
            // 예: 무기 프로필인데 휴머노이드 캐릭터 루트를 선택 → 무기 대신 신체 히트박스가 생성되는 함정.
            bool profileConflict = _target != null
                && _profile != null
                && IsGenerativeMode(resolvedMode)
                && ModesConflict(ProfileExpectedMode(_profile), resolvedMode);
            if (profileConflict)
            {
                EditorGUILayout.HelpBox(
                    $"프로필은 '{GetModeLabel(ProfileExpectedMode(_profile))}' 대상인데 선택한 '{_target.name}'은(는) "
                    + $"'{GetModeLabel(resolvedMode)}'로 분석됩니다.\n"
                    + "무기 히트박스는 캐릭터 루트가 아니라 무기 노드(예: Katana)를 선택하세요.",
                    MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(_target == null))
            {
                GUI.backgroundColor = profileConflict
                    ? new Color(0.85f, 0.6f, 0.3f)
                    : new Color(0.35f, 0.8f, 0.45f);
                if (GUILayout.Button("하위 계층 분석 후 HitBox 자동 생성", GUILayout.Height(34f)))
                {
                    if (!profileConflict || EditorUtility.DisplayDialog(
                            "대상/프로필 불일치",
                            $"프로필은 '{GetModeLabel(ProfileExpectedMode(_profile))}' 대상인데 선택 대상은 "
                            + $"'{GetModeLabel(resolvedMode)}'로 분석됩니다.\n"
                            + "이대로 진행하면 프로필 의도와 다른 히트박스가 생성됩니다. 계속할까요?",
                            "그래도 생성",
                            "취소"))
                    {
                        ExecuteSingle(_target, resolvedMode);
                    }
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_target == null))
                {
                    if (GUILayout.Button("검증"))
                        ExecuteSingle(_target, CombatHitboxSetupMode.ValidateOnly);
                    if (GUILayout.Button("통합 검증"))
                        ExecuteIntegratedValidation(_target);
                    if (GUILayout.Button("기존 항목 Refit"))
                        ExecuteSingle(_target, CombatHitboxSetupMode.RefitExisting);
                    if (GUILayout.Button("생성 항목 제거"))
                        ExecuteSingle(_target, CombatHitboxSetupMode.RemoveGenerated);
                }
            }

            EditorGUILayout.Space(4);
            using (new EditorGUI.DisabledScope(_target == null))
            {
                if (GUILayout.Button("그룹 ID 동기화 (Attack Data / MotionSet 포함)"))
                    CombatHitboxGroupSyncWindow.Open(_target, _profile);
            }

            _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "고급 설정 및 다중 선택", true);
            if (_showAdvanced)
                DrawAdvanced(analysis);

            EditorGUILayout.Space(8);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (CombatHitboxSetupResult result in _results)
            {
                EditorGUILayout.LabelField(
                    $"{result.Target}  생성 {result.Created} / 갱신 {result.Updated} / 건너뜀 {result.Skipped}",
                    EditorStyles.boldLabel);
                foreach (string message in result.Messages)
                    EditorGUILayout.LabelField($"  • {message}", EditorStyles.wordWrappedLabel);
                EditorGUILayout.Space(4);
            }
            EditorGUILayout.EndScrollView();

            DrawHitboxList();
        }

        private void DrawHitboxList()
        {
            if (_resultHitboxes.Count == 0)
                return;

            EditorGUILayout.Space(6);
            _showHitboxList = EditorGUILayout.Foldout(
                _showHitboxList, $"생성된/현재 HitBox ({_resultHitboxes.Count}) — 클릭하여 선택", true);
            if (!_showHitboxList)
                return;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("전체 선택"))
                {
                    Selection.objects = _resultHitboxes.Where(go => go != null).Cast<UnityEngine.Object>().ToArray();
                    if (Selection.objects.Length > 0)
                        EditorGUIUtility.PingObject(Selection.objects[0]);
                }
                if (GUILayout.Button("목록 새로고침"))
                    CollectResultHitboxes();
            }

            foreach (GameObject go in _resultHitboxes)
            {
                if (go == null)
                    continue;
                CombatHitbox hitbox = go.GetComponent<CombatHitbox>();
                bool noCollider = hitbox != null && hitbox.ShapeCollider == null;
                string label = hitbox != null ? $"[{hitbox.GroupId}]  {go.name}" : go.name;
                if (noCollider)
                    label += "  ⚠ Collider 없음";

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(label, EditorStyles.label))
                    {
                        Selection.activeObject = go;
                        EditorGUIUtility.PingObject(go);
                    }
                }
            }
        }

        // 생성/검증 후 대상 하위의 CombatHitbox를 다시 모아 선택 가능한 목록으로 노출한다.
        // 프리팹 에셋은 영속 루트를 다시 로드해 참조가 유효하도록 한다(생성 중의 임시 콘텐츠와 구분).
        private void CollectResultHitboxes()
        {
            _resultHitboxes.Clear();
            if (_target == null)
                return;

            GameObject queryRoot = _target;
            if (PrefabUtility.IsPartOfPrefabAsset(_target))
            {
                string path = AssetDatabase.GetAssetPath(_target);
                if (!string.IsNullOrEmpty(path))
                    queryRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path) ?? _target;
            }

            foreach (CombatHitbox hitbox in queryRoot.GetComponentsInChildren<CombatHitbox>(true))
                if (hitbox != null)
                    _resultHitboxes.Add(hitbox.gameObject);
        }

        private void DrawProfileGuidance()
        {
            if (_profile != null)
            {
                EditorGUILayout.HelpBox(
                    $"프로필 '{_profile.ProfileId}' 적용: 그룹 '{_profile.DefaultGroupId}', 형상 {_profile.PreferredShape}, "
                    + $"최소 두께 {_profile.MinimumThickness:0.###}, 스윕 {(_profile.UseSweep ? "사용" : "미사용")}.",
                    MessageType.None);
                return;
            }

            EditorGUILayout.HelpBox(
                "프로필은 선택 사항입니다. 비워두면 아래 기본값으로 생성됩니다:\n"
                + "• 그룹 ID: 무기 'MainWeapon' / 본 규칙별 그룹\n"
                + "• 형상: Auto (길쭉하면 Capsule, 아니면 Box)  · 최소 두께 0.04  · 패딩 0.02\n"
                + "• 제외 키워드: Sheath, Scabbard, Effect, Trail, VFX, FX\n"
                + "• 스윕: 사용 (step 0.15 / 최대 8단계)\n"
                + "• 체인(채찍): 단일 자식 체인을 따라 링크마다 캡슐 생성 (stride 1 / 반경 0.08m / 스윕 step 0.1·16단계)\n"
                + "그룹 ID·형상·크기·본 규칙·체인 옵션을 커스터마이즈해야 할 때만 프로필을 만들어 지정하세요.",
                MessageType.Info);

            if (GUILayout.Button("기본값 프로필 에셋 생성"))
                CreateDefaultProfileAsset();
        }

        private void CreateDefaultProfileAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Combat HitBox Setup Profile 생성",
                "CombatHitboxSetupProfile",
                "asset",
                "커스터마이즈할 프로필 에셋을 저장할 위치를 선택하세요.");
            if (string.IsNullOrEmpty(path))
                return;

            var profile = ScriptableObject.CreateInstance<CombatHitboxSetupProfileSO>();
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path);
            // 메모리 인스턴스 대신 디스크에서 다시 로드해 참조를 정규화한다(재임포트로 인스턴스/에셋 참조가 갈리는 것 방지).
            _profile = AssetDatabase.LoadAssetAtPath<CombatHitboxSetupProfileSO>(path) ?? profile;
            EditorGUIUtility.PingObject(_profile);
        }

        private void DrawAnalysis(CombatHitboxTargetAnalysis analysis)
        {
            MessageType type = _target == null
                ? MessageType.Warning
                : analysis.RendererCount == 0 && analysis.SuggestedMode == CombatHitboxSetupMode.WeaponAutoFit
                    ? MessageType.Warning
                    : MessageType.Info;
            EditorGUILayout.HelpBox(
                _target == null
                    ? "Player/Model_Bokusei/Weapon/Katana처럼 HitBox를 붙일 루트를 선택하세요."
                    : $"{analysis.Summary}\n자동 모드: {GetModeLabel(analysis.SuggestedMode)}",
                type);
        }

        private void DrawAdvanced(CombatHitboxTargetAnalysis analysis)
        {
            EditorGUI.indentLevel++;
            _useAutomaticMode = EditorGUILayout.ToggleLeft("계층 분석으로 모드 자동 선택", _useAutomaticMode);
            using (new EditorGUI.DisabledScope(_useAutomaticMode))
                _mode = (CombatHitboxSetupMode)EditorGUILayout.EnumPopup("수동 모드", _mode);
            _forceRefit = EditorGUILayout.ToggleLeft("수동 수정 마커도 강제 Refit", _forceRefit);

            GameObject[] selectedTargets = GetSelectedTargets();
            EditorGUILayout.LabelField($"현재 다중 선택 대상: {selectedTargets.Length}개");
            using (new EditorGUI.DisabledScope(selectedTargets.Length == 0))
            {
                if (GUILayout.Button("선택 대상 전체 자동 생성"))
                    Execute(selectedTargets, automaticMode: true);
            }
            EditorGUI.indentLevel--;
        }

        private CombatHitboxSetupMode ResolveMode(CombatHitboxTargetAnalysis analysis)
            => _useAutomaticMode ? analysis.SuggestedMode : _mode;

        // 프로필의 TargetKind(저작자가 선언한 의도)를 실행 모드로 환산한다. 실제 생성 모드는 선택 대상의
        // 계층 분석(ResolveMode)이 결정하므로, 이 값은 둘이 어긋나는지 검사하는 가드 용도로만 쓴다.
        private static CombatHitboxSetupMode ProfileExpectedMode(CombatHitboxSetupProfileSO profile)
            => profile.TargetKind switch
            {
                CombatHitboxSetupTargetKind.Humanoid => CombatHitboxSetupMode.HumanoidBodySetup,
                CombatHitboxSetupTargetKind.Generic => CombatHitboxSetupMode.GenericBodySetup,
                CombatHitboxSetupTargetKind.Chain => CombatHitboxSetupMode.ChainAutoFit,
                _ => CombatHitboxSetupMode.WeaponAutoFit,
            };

        private static bool IsGenerativeMode(CombatHitboxSetupMode mode)
            => mode is CombatHitboxSetupMode.WeaponAutoFit
                or CombatHitboxSetupMode.HumanoidBodySetup
                or CombatHitboxSetupMode.GenericBodySetup
                or CombatHitboxSetupMode.ChainAutoFit;

        // 무기 계열(WeaponAutoFit/ChainAutoFit)은 서로 호환으로 보고 충돌로 잡지 않는다.
        // (무기 프로필을 채찍에 쓰거나, 체인 프로필을 단순 무기에 써도 막지 않음)
        private static bool ModesConflict(CombatHitboxSetupMode a, CombatHitboxSetupMode b)
        {
            if (a == b)
                return false;
            bool aWeaponFamily = a is CombatHitboxSetupMode.WeaponAutoFit or CombatHitboxSetupMode.ChainAutoFit;
            bool bWeaponFamily = b is CombatHitboxSetupMode.WeaponAutoFit or CombatHitboxSetupMode.ChainAutoFit;
            return !(aWeaponFamily && bWeaponFamily);
        }

        private void ExecuteSingle(GameObject target, CombatHitboxSetupMode mode)
        {
            _results.Clear();
            ExecuteTarget(target, mode);
            FinishExecution();
        }

        private void ExecuteIntegratedValidation(GameObject target)
        {
            _results.Clear();
            if (target == null)
            {
                _results.Add(new CombatHitboxSetupResult("(null)", 0, 0, 1, new[] { "대상이 null입니다." }));
                FinishExecution();
                return;
            }

            List<UnityEngine.Object> assets = CollectIntegratedValidationAssets(target);
            _results.Add(new CombatHitboxSetupResult(
                target.name,
                0,
                0,
                0,
                CombatHitboxSetupValidator.Validate(target, assets)));
            FinishExecution();
        }

        private void Execute(GameObject[] targets, bool automaticMode)
        {
            _results.Clear();
            foreach (GameObject target in targets)
            {
                CombatHitboxSetupMode mode = automaticMode
                    ? CombatHitboxAutoFitter.Analyze(target).SuggestedMode
                    : _mode;
                ExecuteTarget(target, mode);
            }
            FinishExecution();
        }

        private void FinishExecution()
        {
            AssetDatabase.SaveAssets();
            SceneView.RepaintAll();
            CollectResultHitboxes();
        }

        private static List<UnityEngine.Object> CollectIntegratedValidationAssets(GameObject target)
        {
            var assets = new List<UnityEngine.Object>();
            if (target == null)
                return assets;

            if (!CombatHitboxGroupSyncUtility.TryResolveContext(
                    target,
                    out CharacterModelData model,
                    out PlayerActorAnimationMotionSet container))
            {
                return assets;
            }

            CombatHitboxGroupSyncUtility.CollectAttackData(model, assets);
            if (container == null)
                return assets;

            List<WeaponType> weaponTypes = CombatHitboxGroupSyncUtility.GetWeaponTypes(container);
            if (model != null && weaponTypes.Contains(model.defaultWeaponType))
            {
                CombatHitboxGroupSyncUtility.CollectMotionSetsForWeapon(container, model.defaultWeaponType, assets);
            }
            else
            {
                foreach (WeaponType weaponType in weaponTypes)
                    CombatHitboxGroupSyncUtility.CollectMotionSetsForWeapon(container, weaponType, assets);
            }
            return assets.Distinct().ToList();
        }

        private void ExecuteTarget(GameObject target, CombatHitboxSetupMode mode)
        {
            if (target == null)
                return;

            string path = AssetDatabase.GetAssetPath(target);
            if (IsModelAsset(path))
            {
                _results.Add(new CombatHitboxSetupResult(
                    target.name, 0, 0, 1, new[] { "FBX 원본 수정 차단: Prefab Variant 또는 별도 Prefab에서 실행하세요." }));
                return;
            }

            if (PrefabUtility.IsPartOfPrefabAsset(target))
                ApplyToPrefab(path, mode);
            else
                _results.Add(CombatHitboxAutoFitter.Apply(target, mode, _profile, _forceRefit));
        }

        private void ApplyToPrefab(string path, CombatHitboxSetupMode mode)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                CombatHitboxSetupResult result =
                    CombatHitboxAutoFitter.Apply(contents, mode, _profile, _forceRefit);
                _results.Add(result);
                if (mode != CombatHitboxSetupMode.ValidateOnly)
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static GameObject[] GetSelectedTargets()
        {
            var targets = new List<GameObject>();
            foreach (UnityEngine.Object selected in Selection.objects)
            {
                if (selected is GameObject gameObject)
                {
                    targets.Add(gameObject);
                    continue;
                }

                string path = AssetDatabase.GetAssetPath(selected);
                if (!AssetDatabase.IsValidFolder(path))
                    continue;
                targets.AddRange(AssetDatabase.FindAssets("t:Prefab", new[] { path })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                    .Where(prefab => prefab != null));
            }
            return targets.Distinct().ToArray();
        }

        private static bool IsModelAsset(string path)
            => !string.IsNullOrWhiteSpace(path)
               && string.Equals(Path.GetExtension(path), ".fbx", System.StringComparison.OrdinalIgnoreCase);

        private static string GetModeLabel(CombatHitboxSetupMode mode)
            => mode switch
            {
                CombatHitboxSetupMode.WeaponAutoFit => "무기 Renderer Bounds",
                CombatHitboxSetupMode.HumanoidBodySetup => "Humanoid 본",
                CombatHitboxSetupMode.GenericBodySetup => "Generic 본 이름",
                CombatHitboxSetupMode.ChainAutoFit => "체인(채찍) 캡슐",
                _ => mode.ToString(),
            };
    }
}
#endif
