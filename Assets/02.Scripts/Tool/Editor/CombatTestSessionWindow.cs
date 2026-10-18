using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.Animations;
using UPlayGround.Combat;
using UPlayGround.Components;
using UPlayGround.Tool.Editor.Combat;
using Object = UnityEngine.Object;
using SceneManager = UnityEngine.SceneManagement.SceneManager;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround.EditorTools;
using UPlayGround.Manager;
using UPlayGround.Group;
using UPlayGround.AI.BehaviorTree;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Party;
using UPlayGround.UI;
using Cysharp.Threading.Tasks;

namespace UPlayGround.Tool.Editor
{
    /// <summary>보스전과 혼합전의 초기 소환 조건을 고정해 비교 플레이를 준비한다.</summary>
    public sealed class CombatTestSessionWindow : EditorWindow
    {
        private const string StartedKey = "UPlayGround.CombatTest.Started";
        private const string PartyPendingKey = "UPlayGround.CombatTest.PartyPending";
        private const string PartyResultKey = "UPlayGround.CombatTest.PartyResult";
        [Serializable] private sealed class StartingParty { public CharacterActorType[] members; }
        [SerializeField] private bool _useStartingParty;
        [SerializeField] private CharacterActorType[] _startingParty = Array.Empty<CharacterActorType>();
        private PlayerCharacterCatalogSO _partyCatalog;
        private PartyConfigSO _partyConfig;
        private static bool _isPreparingParty;
        private const string ProgressionRoot = "Assets/10.Datas/Actor/P09Progression";
        private static readonly string[] Weapons = { "SwordShield", "GreatSword", "DualSword", "DualAxe", "Bow", "Staff_M" };
        [Serializable] private sealed class Progression { public Tier[] tiers; }
        [Serializable] private sealed class Tier { public int armor; public int level; public string label; }
        private Tier[] _tiers = Array.Empty<Tier>();
        private string[] _tierLabels = Array.Empty<string>();
        [SerializeField] private int _tierIndex;
        [SerializeField] private bool _isPlayerInvincible;
        [SerializeField] private bool _isEnemyAIStopped;
        private Vector2 _scroll;
        private double _nextCheatRefresh;
        private readonly Dictionary<MonsterActor, Action> _stoppedEnemies = new();
        [SerializeField] private ActorDefinitionSO _boss;
        [SerializeField] private ActorDefinitionSO[] _members;
        [SerializeField] private Vector2[] _positions =
        {
            new(-6, 8), new(-2, 8), new(2, 8), new(6, 8), new(-4, 16), new(4, 16)
        };
        [SerializeField] private float _bossDistance = 12f;
        [SerializeField] private string _result;
        private enum EncounterKind { Boss, Mixed, Single }
        [SerializeField] private int _singleMemberIndex;
        [SerializeField] private float _singleDistance = 5f;
        private MonsterActor _practiceActor;
        private EnemyCombat _restrictedCombat;
        private GameplayAbilitySO _previousRestriction;
        private readonly List<GameplayAbilitySO> _practiceAbilities = new();
        private string[] _practiceLabels = Array.Empty<string>();
        private int _practiceIndex;

        /// <summary>전투 시험장의 소환 설정과 실행 창을 연다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/전투 테스트 준비")]
        public static void Open()
        {
            GetWindow<CombatTestSessionWindow>("전투 테스트 준비").Show();
        }

        [InitializeOnLoadMethod]
        private static void RegisterSessionReset()
        {
            EditorApplication.playModeStateChanged -= ResetSession;
            EditorApplication.playModeStateChanged += ResetSession;
            EditorApplication.playModeStateChanged -= PrepareSession;
            EditorApplication.playModeStateChanged += PrepareSession;
            EditorApplication.update -= ApplyPendingParty;
            EditorApplication.update += ApplyPendingParty;
        }

        private static void PrepareSession(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode
                || !EditorApplication.isPlayingOrWillChangePlaymode
                || SceneManager.GetActiveScene().path != CombatTestMapTool.ScenePath)
                return;

            var window = Resources.FindObjectsOfTypeAll<CombatTestSessionWindow>().FirstOrDefault();
            bool isTemporary = window == null;
            if (isTemporary) window = CreateInstance<CombatTestSessionWindow>();
            try
            {
                SessionState.SetString(PartyPendingKey, string.Empty);
                SessionState.SetString(PartyResultKey, string.Empty);
                if (window._useStartingParty && !window.TryGetStartingParty(out _, out string error))
                {
                    EditorApplication.isPlaying = false;
                    Debug.LogError("[CombatTest] 시작 파티 설정 오류: " + error);
                    return;
                }
                // 시험장 진입 전에 프리팹을 준비해야 Awake의 HitBox 캐시에도 반영된다.
                if (!window.RepairWeaponHitboxes())
                {
                    EditorApplication.isPlaying = false;
                    Debug.LogError("[CombatTest] 무기 HitBox 준비 실패로 Play 진입을 중단했습니다. 전투 테스트 준비 창과 Console을 확인하세요.");
                }
                else if (window._useStartingParty && window.TryGetStartingParty(out var members, out _))
                {
                    SessionState.SetString(PartyPendingKey, JsonUtility.ToJson(new StartingParty { members = members }));
                    SessionState.SetString(PartyResultKey, "시작 파티 준비 대기 중");
                }
            }
            finally
            {
                if (isTemporary) DestroyImmediate(window);
            }
        }

        private static void ResetSession(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetString(PartyPendingKey, string.Empty);
                PlayerActor.IsCombatTestInvincible = false;
                foreach (var window in Resources.FindObjectsOfTypeAll<CombatTestSessionWindow>())
                {
                    window.ReleaseEnemyStops();
                    window.ReleasePracticeRestriction();
                }
            }
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.EnteredEditMode)
                SessionState.SetBool(StartedKey, false);
        }

        private void OnEnable()
        {
            _partyCatalog = AssetDatabase.LoadAssetAtPath<PlayerCharacterCatalogSO>("Assets/10.Datas/Party/PlayerCharacterCatalog.asset");
            _partyConfig = AssetDatabase.LoadAssetAtPath<PartyConfigSO>("Assets/10.Datas/Party/PartyConfig.asset");
            LoadProgression();
            EditorApplication.update += RefreshCheats;
            const string folder = "Assets/10.Datas/Actor/DataBase/";
            if (_boss == null) _boss = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(folder + "Humanoid/MonsterRaon.asset");
            if (_members == null || _members.Length != 6)
            {
                string[] weapons = { "SwordShield", "GreatSword", "DualSword", "DualAxe", "Bow", "Staff_M" };
                _members = new ActorDefinitionSO[weapons.Length];
                for (int i = 0; i < weapons.Length; i++)
                {
                    string id = "Enemy_P09_Armor06_" + weapons[i];
                    _members[i] = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(
                        $"Assets/10.Datas/Actor/P09Progression/Generated/{id}/{id}_ActorDef.asset");
                }
            }
            _singleMemberIndex = Mathf.Clamp(_singleMemberIndex, 0, _members.Length - 1);
            minSize = new Vector2(480, 520);
        }

        private void OnDisable()
        {
            EditorApplication.update -= RefreshCheats;
            PlayerActor.IsCombatTestInvincible = false;
            ReleaseEnemyStops();
            ReleasePracticeRestriction();
        }

        private void LoadProgression()
        {
            try
            {
                _tiers = JsonUtility.FromJson<Progression>(File.ReadAllText(ProgressionRoot + "/P09Progression.json"))?.tiers
                    ?? Array.Empty<Tier>();
                _tierLabels = _tiers.Select(tier => $"Lv.{tier.level} / {tier.label}").ToArray();
                _tierIndex = Mathf.Clamp(_tierIndex, 0, Mathf.Max(0, _tiers.Length - 1));
            }
            catch (Exception exception) { _result = "P09 제작표 읽기 실패: " + exception.Message; }
        }

        private bool ApplyProgressionTier(int index)
        {
            if (index < 0 || index >= _tiers.Length) return false;
            var members = new ActorDefinitionSO[Weapons.Length];
            for (int i = 0; i < members.Length; i++)
            {
                string id = $"Enemy_P09_Armor{_tiers[index].armor:00}_{Weapons[i]}";
                members[i] = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>($"{ProgressionRoot}/Generated/{id}/{id}_ActorDef.asset");
                if (members[i] == null || members[i].prefab == null
                    || members[i].EffectiveLevel != _tiers[index].level)
                {
                    _result = $"P09 레벨 교체 실패: {id}의 정의·프리팹·레벨을 확인하세요. 기존 선택을 유지합니다.";
                    return false;
                }
            }
            _members = members;
            _tierIndex = index;
            _result = $"혼합 그룹을 {_tierLabels[index]} 프리팹 6종으로 교체했습니다.";
            return true;
        }

        private void RefreshCheats()
        {
            bool isTest = EditorApplication.isPlaying
                && SceneManager.GetActiveScene().path == CombatTestMapTool.ScenePath;
            PlayerActor.IsCombatTestInvincible = isTest && _isPlayerInvincible;
            if (!isTest || _practiceActor == null || !_practiceActor.IsAlive())
                ReleasePracticeRestriction();
            if (!isTest || !_isEnemyAIStopped)
            {
                ReleaseEnemyStops();
                return;
            }
            // Editor 폴링은 후속 소환도 포착하되 매 프레임 씬을 탐색하지 않는다.
            if (EditorApplication.timeSinceStartup < _nextCheatRefresh) return;
            _nextCheatRefresh = EditorApplication.timeSinceStartup + 0.2;
            foreach (var enemy in Object.FindObjectsByType<MonsterActor>(FindObjectsSortMode.None))
            {
                if (!enemy.IsAlive() || _stoppedEnemies.ContainsKey(enemy)) continue;
                var detection = enemy.Detection;
                bool wasDetectionEnabled = detection != null && detection.enabled;
                var runner = enemy.GetComponent<BehaviorTreeRunner>();
                bool wasTestPaused = runner != null && runner.IsCombatTestPaused;
                bool wasPaused = runner != null && runner.IsPaused;
                if (detection != null)
                {
                    detection.enabled = false;
                    detection.ForceResetTarget();
                }
                if (runner != null) runner.IsCombatTestPaused = true;
                // 타겟을 비운 뒤 홀드를 취득해야 교전 중 대화의 전투 유지 예외를 피한다.
                var hold = enemy.BeginDialogueStage(null);
                _stoppedEnemies.Add(enemy, () =>
                {
                    if (enemy == null) return;
                    hold.Dispose();
                    if (runner != null)
                    {
                        runner.IsCombatTestPaused = wasTestPaused;
                        if (wasPaused || !enemy.IsAlive()) runner.PauseTree();
                    }
                    if (enemy.IsAlive() && detection != null) detection.enabled = wasDetectionEnabled;
                });
            }
        }

        private void ReleaseEnemyStops()
        {
            foreach (var release in _stoppedEnemies.Values) release();
            _stoppedEnemies.Clear();
        }

        private void OnInspectorUpdate()
        {
            if (EditorApplication.isPlaying) Repaint();
        }

        private void DrawStartingParty()
        {
            EditorGUILayout.LabelField("시작 출전 파티", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                _useStartingParty = EditorGUILayout.Toggle("시작 편성 지정", _useStartingParty);
                if (_useStartingParty && _partyCatalog != null && _partyConfig != null)
                {
                    int limit = Mathf.Max(1, _partyConfig.maxBattleSize);
                    if (_startingParty.Length == 0)
                        _startingParty = _partyCatalog.entries.Where(entry => entry != null
                            && entry.characterType != CharacterActorType.None).Select(entry => entry.characterType)
                            .Distinct().Take(limit).ToArray();
                    if (_startingParty.Length != limit) Array.Resize(ref _startingParty, limit);
                    var choices = new[] { CharacterActorType.None }.Concat(_partyCatalog.entries
                        .Where(entry => entry != null && _partyCatalog.TryGetDefinitionAddress(entry.characterType, out _))
                        .Select(entry => entry.characterType)).Distinct().ToArray();
                    var labels = choices.Select(type => type == CharacterActorType.None ? "비움" : type.ToString()).ToArray();
                    for (int i = 0; i < _startingParty.Length; i++)
                    {
                        int current = Array.IndexOf(choices, _startingParty[i]);
                        int selected = EditorGUILayout.Popup($"출전 슬롯 {i + 1}", current, labels);
                        if (selected >= 0 && selected < choices.Length) _startingParty[i] = choices[selected];
                    }
                }
            }
            if (_useStartingParty && !TryGetStartingParty(out _, out string error))
                EditorGUILayout.HelpBox(error, MessageType.Error);
            EditorGUILayout.HelpBox("Play 전에 선택하세요. 비운 슬롯은 제외하며 전체 보유 명단은 유지합니다. 기존 조작 캐릭터가 편성에 있으면 유지하고, 없으면 첫 슬롯 캐릭터로 전환합니다.", MessageType.None);
            string result = SessionState.GetString(PartyResultKey, string.Empty);
            if (EditorApplication.isPlaying && !string.IsNullOrEmpty(result))
                EditorGUILayout.HelpBox(result, MessageType.Info);
        }

        private bool TryGetStartingParty(out CharacterActorType[] members, out string error)
        {
            members = (_startingParty ?? Array.Empty<CharacterActorType>())
                .Where(type => type != CharacterActorType.None).ToArray();
            error = null;
            if (_partyCatalog == null || _partyConfig == null)
                error = "파티 카탈로그와 PartyConfig를 확인하세요.";
            else if (members.Length == 0 || members.Length > Mathf.Max(1, _partyConfig.maxBattleSize))
                error = $"시작 파티는 1~{Mathf.Max(1, _partyConfig.maxBattleSize)}명이어야 합니다.";
            else if (members.Distinct().Count() != members.Length)
                error = "같은 캐릭터를 여러 출전 슬롯에 지정할 수 없습니다.";
            else if (members.Any(type => !_partyCatalog.TryGetDefinitionAddress(type, out _)))
                error = "카탈로그에 정의 주소가 없는 캐릭터가 포함되어 있습니다.";
            return error == null;
        }

        private static void ApplyPendingParty()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPaused || _isPreparingParty)
                return;
            string pending = SessionState.GetString(PartyPendingKey, string.Empty);
            if (string.IsNullOrEmpty(pending)) return;
            if (SceneManager.GetActiveScene().path != CombatTestMapTool.ScenePath)
            {
                SessionState.SetString(PartyPendingKey, string.Empty);
                return;
            }
            if (!Services.TryGet<IUIPartyService>(out var party)
                || party.ActiveCharacter == null || party.ActiveCharacterType == CharacterActorType.None
                || party.BattleOrder.Count == 0)
                return;
            _isPreparingParty = true;
            ApplyStartingPartyAsync(party, JsonUtility.FromJson<StartingParty>(pending).members).Forget();
        }

        private static async UniTask ApplyStartingPartyAsync(IUIPartyService party, CharacterActorType[] members)
        {
            try
            {
                var token = party.ActiveCharacter.GetCancellationTokenOnDestroy();
                if (members.Length > party.MaxBattleSize || members.Any(type => !party.Roster.Contains(type)))
                    throw new InvalidOperationException("선택한 편성이 현재 파티의 보유 명단 또는 출전 상한과 맞지 않습니다.");
                bool applied = await party.SetBattleOrderAsync(members, token);
                token.ThrowIfCancellationRequested();
                if (!applied || !party.BattleOrder.SequenceEqual(members))
                    throw new InvalidOperationException("시작 파티 편성에 실패했습니다. 보유 명단과 모델 로드 오류를 확인하세요.");
                SessionState.SetString(PartyResultKey, "시작 파티 적용 완료: " + string.Join(", ", members));
            }
            catch (OperationCanceledException) { }
            catch (Exception exception)
            {
                SessionState.SetString(PartyResultKey, "시작 파티 적용 실패: " + exception.Message);
                Debug.LogException(exception);
            }
            finally
            {
                _isPreparingParty = false;
                SessionState.SetString(PartyPendingKey, string.Empty);
            }
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawStartingParty();
            EditorGUI.BeginChangeCheck();
            _isPlayerInvincible = EditorGUILayout.Toggle("플레이어 무적", _isPlayerInvincible);
            _isEnemyAIStopped = EditorGUILayout.Toggle("적 AI 정지", _isEnemyAIStopped);
            if (EditorGUI.EndChangeCheck())
            {
                _nextCheatRefresh = 0;
                RefreshCheats();
            }
            EditorGUILayout.HelpBox("치트는 전투 테스트 맵의 Play 중에 적용됩니다. 창을 닫으면 해제됩니다. AI 정지 중에도 적의 피격·사망은 유지됩니다.", MessageType.None);
            EditorGUILayout.HelpBox("중앙에서 같은 캐릭터·장비로 시작하세요. 한 Play 세션에 한 전투만 소환합니다. 재도전·전투 변경은 Play 종료 후 재실행하세요.", MessageType.Info);
            if (GUILayout.Button("전투 테스트 맵 열기")) CombatTestMapTool.Open();
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
            {
                if (GUILayout.Button("비활성 무기에 남은 HitBox 복구")) RepairWeaponHitboxes();
            }

            bool hasStarted = SessionState.GetBool(StartedKey, false);
            using (new EditorGUI.DisabledScope(hasStarted))
            {
                _boss = (ActorDefinitionSO)EditorGUILayout.ObjectField("보스", _boss, typeof(ActorDefinitionSO), false);
                _bossDistance = EditorGUILayout.Slider("보스 전방 거리", _bossDistance, 5f, 30f);
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || _tiers.Length == 0))
                {
                    EditorGUI.BeginChangeCheck();
                    int tier = EditorGUILayout.Popup("P09 혼합전 레벨", _tierIndex, _tierLabels);
                    if (EditorGUI.EndChangeCheck()) ApplyProgressionTier(tier);
                    if (GUILayout.Button("선택 레벨의 P09 6종 적용")) ApplyProgressionTier(_tierIndex);
                }
                EditorGUILayout.LabelField("혼합 그룹: 무기별 1명 (좌우 X / 전방 Y 거리)");
                for (int i = 0; i < _members.Length; i++)
                {
                    _members[i] = (ActorDefinitionSO)EditorGUILayout.ObjectField(
                        $"멤버 {i + 1}", _members[i], typeof(ActorDefinitionSO), false);
                    _positions[i] = EditorGUILayout.Vector2Field("위치", _positions[i]);
                }
                using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || EditorApplication.isPaused))
                {
                    if (GUILayout.Button("보스전 소환")) StartEncounter(EncounterKind.Boss);
                    if (GUILayout.Button("근접 + 원거리 혼합전 소환")) StartEncounter(EncounterKind.Mixed);
                }
                _singleMemberIndex = EditorGUILayout.Popup("단일 적 무기", _singleMemberIndex, Weapons);
                _singleDistance = EditorGUILayout.Slider("단일 적 전방 거리", _singleDistance, 1f, 30f);
                using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || EditorApplication.isPaused))
                    if (GUILayout.Button("선택한 일반 적 1체 소환")) StartEncounter(EncounterKind.Single);
            }

            DrawPracticeControls();

            if (hasStarted)
                EditorGUILayout.HelpBox(_result + "\nGame 뷰를 클릭해 전투하세요. 재도전은 Play 재실행으로 HP·쿨다운·적·투사체를 함께 초기화합니다.", MessageType.Info);
            else if (!string.IsNullOrEmpty(_result))
                EditorGUILayout.HelpBox("이전 실행: " + _result, MessageType.None);
            EditorGUILayout.EndScrollView();
        }

        private bool RepairWeaponHitboxes()
        {
            var staged = new Dictionary<string, GameObject>();
            var backups = new Dictionary<string, byte[]>();
            int repaired = 0;
            try
            {
                foreach (var definition in new[] { _boss }.Concat(_members))
                {
                    if (definition == null || definition.prefab == null)
                        throw new InvalidOperationException("몬스터 프리팹 연결을 확인하세요.");
                    string path = AssetDatabase.GetAssetPath(definition.prefab);
                    if (staged.ContainsKey(path)) continue;
                    var root = PrefabUtility.LoadPrefabContents(path);
                    staged.Add(path, root);
                    int count = RepairInactiveWeaponHitboxes(root);
                    if (count == 0) continue;
                    repaired += count;
                    backups.Add(path, File.ReadAllBytes(path));
                }

                // 모든 프리팹을 메모리에서 검증한 뒤 저장한다. 중간 저장 실패도 원본 바이트로 복구한다.
                foreach (string path in backups.Keys)
                {
                    PrefabUtility.SaveAsPrefabAsset(staged[path], path, out bool success);
                    if (!success) throw new IOException("프리팹 저장 실패: " + path);
                }
                _result = $"무기 HitBox {repaired}개 그룹 복구. Play에서 실제 무기 궤적과 판정을 확인하세요.";
                return true;
            }
            catch (Exception exception)
            {
                foreach (var backup in backups) File.WriteAllBytes(backup.Key, backup.Value);
                AssetDatabase.Refresh();
                _result = "복구 실패, 저장 대상 원본 복원: " + exception.Message;
                Debug.LogException(exception);
                return false;
            }
            finally
            {
                foreach (var root in staged.Values) PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>비활성 무기에만 존재하는 그룹을 현재 활성 무기 메시로 다시 맞춘다.</summary>
        public static int RepairInactiveWeaponHitboxes(GameObject root)
        {
            var weaponRoot = WeaponAttachmentResolver.FindWeaponRoot(root.transform);
            if (weaponRoot == null) return 0;
            var hitboxes = weaponRoot.GetComponentsInChildren<CombatHitbox>(true);
            var brokenGroups = hitboxes
                .Where(box => box.GroupId == "MainWeapon" || box.GroupId == "SubWeapon")
                .GroupBy(box => box.GroupId)
                .Where(group => !group.Any(box => box.IsSupported && box.gameObject.activeInHierarchy))
                .Select(group => group.Key).ToArray();
            int repaired = 0;
            foreach (string group in brokenGroups)
            {
                // P09의 기존 장착 슬롯 계약을 사용하며 비활성 교체용 메시를 bounds에 섞지 않는다.
                var candidates = weaponRoot.GetComponentsInChildren<ParentConstraint>(true)
                    .Where(slot => slot.gameObject.activeInHierarchy
                        && ((slot.name == "SubSword" || slot.name == "Shield") == (group == "SubWeapon")))
                    .SelectMany(slot => slot.GetComponentsInChildren<Renderer>(false))
                    .Where(renderer => renderer.enabled && (renderer is MeshRenderer || renderer is SkinnedMeshRenderer))
                    .Distinct().ToArray();
                if (candidates.Length != 1)
                    throw new InvalidOperationException($"{root.name}/{group}: 활성 무기 메시 후보 {candidates.Length}개. 자동 복구를 중단합니다.");

                var profile = ScriptableObject.CreateInstance<CombatHitboxSetupProfileSO>();
                try
                {
                    var serialized = new SerializedObject(profile);
                    serialized.FindProperty("_defaultGroupId").stringValue = group;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    var result = CombatHitboxAutoFitter.Apply(candidates[0].gameObject,
                        CombatHitboxSetupMode.WeaponAutoFit, profile, false);
                    if (result.Created + result.Updated == 0)
                        throw new InvalidOperationException($"{root.name}/{group}: HitBox 생성 실패");
                    if (!candidates[0].GetComponentsInChildren<CombatHitbox>(false)
                        .Any(box => box.GroupId == group && box.IsSupported))
                        throw new InvalidOperationException($"{root.name}/{group}: 활성 판정 검증 실패");
                    repaired++;
                }
                finally { DestroyImmediate(profile); }
            }
            return repaired;
        }

        private void StartEncounter(EncounterKind kind)
        {
            if (!string.IsNullOrEmpty(SessionState.GetString(PartyPendingKey, string.Empty)))
            {
                _result = "시작 파티 준비가 완료된 뒤 소환하세요.";
                return;
            }
            bool isBoss = kind == EncounterKind.Boss;
            bool isMixed = kind == EncounterKind.Mixed;
            ActorDefinitionSO single = _boss;
            if (kind == EncounterKind.Single)
                single = _members[_singleMemberIndex];
            if (SceneManager.GetActiveScene().path != CombatTestMapTool.ScenePath)
            {
                _result = "전투 테스트 맵에서만 소환할 수 있습니다.";
                return;
            }

            var cheat = CheatManager.Instance;
            var player = PartyManager.Instance != null ? PartyManager.Instance.ActiveCharacter : null;
            if (cheat == null || player == null || !player.IsAlive())
            {
                _result = "살아 있는 플레이어와 매니저 초기화를 먼저 확인하세요.";
                return;
            }

            if (Object.FindObjectsByType<MonsterActor>(FindObjectsSortMode.None).Length != 0)
            {
                _result = "기존 몬스터가 있습니다. Play를 재실행해 빈 맵에서 시작하세요.";
                return;
            }

            if (isMixed ? !CanSpawnGroup(cheat) : !CanSpawn(single, cheat))
            {
                _result = "선택한 몬스터의 DB 등록·프리팹·AbilitySet을 확인하세요.";
                return;
            }
            if (isBoss && _boss.EffectiveGrade != MonsterActorGrade.Boss)
            {
                _result = "보스 등급의 ActorDefinition을 선택하세요.";
                return;
            }

            // 실패 중 일부 적이 생성되어도 재시도로 전투 조건이 섞이지 않게 한다.
            SessionState.SetBool(StartedKey, true);
            _result = "소환 진행 중 오류가 발생하면 Console을 확인하고 Play를 재실행하세요.";
            int expected = isMixed ? _members.Length : 1;
            Vector3 start = player.transform.position;
            float heading = player.transform.eulerAngles.y;
            int spawned = isMixed
                ? SpawnGroup(player.transform)
                : cheat.SpawnActorInFrontOfPlayer(single.actorId, 1, isBoss ? _bossDistance : _singleDistance);
            if (!isMixed && spawned == 1)
                SelectPracticeActor(Object.FindObjectsByType<MonsterActor>(FindObjectsSortMode.None).FirstOrDefault());
            _nextCheatRefresh = 0;
            RefreshCheats();
            string label = kind switch
            {
                EncounterKind.Boss => "보스전",
                EncounterKind.Mixed => "혼합전",
                _ => "단일 적",
            };
            _result = $"{label} 소환 {spawned}/{expected}. 시작 위치 {start:F2}, 방향 {heading:F1}°. "
                      + (spawned == expected ? "전투 동작과 재미는 직접 확인해야 합니다." : "소환 실패: 이번 결과는 평가에서 제외하세요.");
        }

        private void DrawPracticeControls()
        {
            if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().path != CombatTestMapTool.ScenePath)
                return;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("공격 반복 관찰", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            var actor = (MonsterActor)EditorGUILayout.ObjectField("관찰할 적", _practiceActor, typeof(MonsterActor), true);
            if (EditorGUI.EndChangeCheck()) SelectPracticeActor(actor);
            if (_practiceActor == null || _practiceLabels.Length == 0) return;
            EditorGUI.BeginChangeCheck();
            int index = EditorGUILayout.Popup("허용할 공격", _practiceIndex, _practiceLabels);
            if (EditorGUI.EndChangeCheck())
                ApplyPracticeRestriction(index);
            EditorGUILayout.HelpBox("선택한 공격만 다음 AI 후보에 남고 동일 기술 반복 억제만 우회합니다. 현재 공격은 끝까지 실행하며 거리·쿨다운·조건·BT 분기는 유지합니다. AI 정지를 해제하고 Game 뷰에서 관찰하세요. 전체 공격 선택·창 닫기·Play 종료 시 제한을 해제합니다. 전체 전투 평가는 제한 없이 진행하세요.", MessageType.Info);
            var combat = _practiceActor.GetComponent<EnemyCombat>();
            if (combat != null)
            {
                EditorGUILayout.LabelField("현재 Ability", combat.CurrentAbility != null ? combat.CurrentAbility.abilityId : "없음");
                EditorGUILayout.LabelField("현재 Motion", combat.CurrentMotionAsset != null ? combat.CurrentMotionAsset.name : "없음");
                if (_practiceIndex > 0 && combat.CurrentSkill == null)
                    EditorGUILayout.HelpBox(combat.BuildAbilitySelectionDiagnosticSummary(), MessageType.None);
            }
        }

        private void SelectPracticeActor(MonsterActor actor)
        {
            ReleasePracticeRestriction();
            _practiceActor = actor != null && actor.gameObject.scene == SceneManager.GetActiveScene() ? actor : null;
            _practiceAbilities.Clear();
            var combat = _practiceActor != null ? _practiceActor.GetComponent<EnemyCombat>() : null;
            if (combat?.AbilitySet != null)
                _practiceAbilities.AddRange(combat.AbilitySet.GetRuntimeAbilities().Where(ability => ability != null).Distinct());
            _practiceLabels = new[] { "전체 공격 (제한 없음)" }
                .Concat(_practiceAbilities.Select(ability => ability.abilityId + " / " + ability.name)).ToArray();
        }

        private void ApplyPracticeRestriction(int index)
        {
            ReleasePracticeRestriction();
            if (_practiceActor == null || index <= 0 || index > _practiceAbilities.Count) return;
            _restrictedCombat = _practiceActor.GetComponent<EnemyCombat>();
            if (_restrictedCombat == null) return;
            _practiceIndex = index;
            _previousRestriction = _restrictedCombat.CombatTestAbility;
            _restrictedCombat.CombatTestAbility = _practiceAbilities[index - 1];
        }

        private void ReleasePracticeRestriction()
        {
            if (_restrictedCombat != null) _restrictedCombat.CombatTestAbility = _previousRestriction;
            _restrictedCombat = null;
            _previousRestriction = null;
            _practiceIndex = 0;
        }

        private bool CanSpawnGroup(CheatManager cheat)
        {
            if (_members == null || _members.Length != 6 || _positions == null
                || _positions.Length != _members.Length)
                return false;
            for (int i = 0; i < _members.Length; i++)
            {
                if (!CanSpawn(_members[i], cheat)) return false;
                for (int j = 0; j < i; j++)
                    if (_members[i].actorId == _members[j].actorId) return false;
            }
            return true;
        }

        private int SpawnGroup(Transform player)
        {
            Vector3 forward = Vector3.ProjectOnPlane(player.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            var positions = new Vector3[_members.Length];
            // 전원 배치 가능 여부를 먼저 검사해 지면 밖 소환을 방지한다.
            for (int i = 0; i < positions.Length; i++)
            {
                Vector3 point = player.position + right * _positions[i].x + forward * _positions[i].y;
                if (!Physics.Raycast(point + Vector3.up * 3f, Vector3.down, out var hit,
                        33f, LayerMask.GetMask("Ground"), QueryTriggerInteraction.Ignore))
                    return 0;
                positions[i] = hit.point;
            }

            var groupObject = new GameObject("CombatTest_MixedGroup");
            var group = groupObject.AddComponent<MonsterGroupController>();
            int spawned = 0;
            for (int i = 0; i < _members.Length; i++)
            {
                if (ActorSpawnManager.Instance.SpawnActor(_members[i].actorId, positions[i],
                        Quaternion.LookRotation(-forward), group, groupObject.transform) != null)
                    spawned++;
            }
            return spawned;
        }

        private static bool CanSpawn(ActorDefinitionSO definition, CheatManager cheat)
        {
            if (definition == null || !definition.actorType.HasFlag(ActorType.Monster)
                || string.IsNullOrEmpty(definition.actorId) || definition.prefab == null
                || definition.EffectiveAbilitySet == null)
                return false;
            foreach (var registered in cheat.GetSpawnableDefinitions())
                if (registered == definition) return true;
            return false;
        }
    }
}
