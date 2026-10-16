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

namespace UPlayGround.Tool.Editor
{
    /// <summary>보스전과 혼합전의 초기 소환 조건을 고정해 비교 플레이를 준비한다.</summary>
    public sealed class CombatTestSessionWindow : EditorWindow
    {
        private const string StartedKey = "UPlayGround.CombatTest.Started";
        [SerializeField] private ActorDefinitionSO _boss;
        [SerializeField] private ActorDefinitionSO[] _members;
        [SerializeField] private Vector2[] _positions =
        {
            new(-6, 8), new(-2, 8), new(2, 8), new(6, 8), new(-4, 16), new(4, 16)
        };
        [SerializeField] private float _bossDistance = 12f;
        [SerializeField] private string _result;

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
                // 시험장 진입 전에 프리팹을 준비해야 Awake의 HitBox 캐시에도 반영된다.
                if (!window.RepairWeaponHitboxes())
                {
                    EditorApplication.isPlaying = false;
                    Debug.LogError("[CombatTest] 무기 HitBox 준비 실패로 Play 진입을 중단했습니다. 전투 테스트 준비 창과 Console을 확인하세요.");
                }
            }
            finally
            {
                if (isTemporary) DestroyImmediate(window);
            }
        }

        private static void ResetSession(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.EnteredEditMode)
                SessionState.SetBool(StartedKey, false);
        }

        private void OnEnable()
        {
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
            minSize = new Vector2(480, 520);
        }

        private void OnGUI()
        {
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
                EditorGUILayout.LabelField("혼합 그룹: 무기별 1명 (좌우 X / 전방 Y 거리)");
                for (int i = 0; i < _members.Length; i++)
                {
                    _members[i] = (ActorDefinitionSO)EditorGUILayout.ObjectField(
                        $"멤버 {i + 1}", _members[i], typeof(ActorDefinitionSO), false);
                    _positions[i] = EditorGUILayout.Vector2Field("위치", _positions[i]);
                }
                using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || EditorApplication.isPaused))
                {
                    if (GUILayout.Button("보스전 소환")) StartEncounter(true);
                    if (GUILayout.Button("근접 + 원거리 혼합전 소환")) StartEncounter(false);
                }
            }

            if (hasStarted)
                EditorGUILayout.HelpBox(_result + "\nGame 뷰를 클릭해 전투하세요. 재도전은 Play 재실행으로 HP·쿨다운·적·투사체를 함께 초기화합니다.", MessageType.Info);
            else if (!string.IsNullOrEmpty(_result))
                EditorGUILayout.HelpBox("이전 실행: " + _result, MessageType.None);
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

        private void StartEncounter(bool isBoss)
        {
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

            if (isBoss ? !CanSpawn(_boss, cheat) : !CanSpawnGroup(cheat))
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
            int expected = isBoss ? 1 : _members.Length;
            int spawned = isBoss
                ? cheat.SpawnActorInFrontOfPlayer(_boss.actorId, 1, _bossDistance)
                : SpawnGroup(player.transform);
            _result = $"{(isBoss ? "보스전" : "혼합전")} 소환 {spawned}/{expected}. "
                      + (spawned == expected ? "전투 동작과 재미는 직접 확인해야 합니다." : "소환 실패: 이번 결과는 평가에서 제외하세요.");
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
