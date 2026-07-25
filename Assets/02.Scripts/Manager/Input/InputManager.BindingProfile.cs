using System;
using System.Collections.Generic;
using System.Linq;
using UPlayGround.InputDefine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UPlayGround.Manager
{
    public partial class InputManager : IInputActionIdentityLookup
    {
        private const string BindingProfilePrefsKey = "InputBindings_v1";
        private const string UserBindingGroupPrefix = "__UserBinding__";

        private InputBindingProfileData _bindingProfile = new();
        private int _bindingProfileUpdateDepth;
        private bool _bindingProfileUpdatePending;

        public event Action OnBindingsChanged;

        /// <summary>
        /// 여러 프로필 변경을 하나의 액션 맵 재적용과 변경 알림으로 묶는다.
        /// 설정 화면의 일괄 적용처럼 여러 슬롯을 동시에 바꿀 때 사용한다.
        /// </summary>
        public IDisposable BeginBindingProfileUpdate()
        {
            _bindingProfileUpdateDepth++;
            return new BindingProfileUpdateScope(this);
        }

        private sealed class BindingProfileUpdateScope : IDisposable
        {
            private InputManager _owner;

            public BindingProfileUpdateScope(InputManager owner) => _owner = owner;

            public void Dispose()
            {
                InputManager owner = _owner;
                if (owner == null)
                    return;

                _owner = null;
                owner.EndBindingProfileUpdate();
            }
        }

        private void EndBindingProfileUpdate()
        {
            if (_bindingProfileUpdateDepth <= 0)
                return;

            _bindingProfileUpdateDepth--;
            if (_bindingProfileUpdateDepth == 0 && _bindingProfileUpdatePending)
            {
                _bindingProfileUpdatePending = false;
                ApplyBindingProfile();
                OnBindingsChanged?.Invoke();
            }
        }

        private void CommitBindingProfileChange()
        {
            if (_bindingProfileUpdateDepth > 0)
            {
                _bindingProfileUpdatePending = true;
                return;
            }

            ApplyBindingProfile();
            OnBindingsChanged?.Invoke();
        }

        private readonly struct BindingDefinition
        {
            public readonly string Map;
            public readonly string Action;
            public readonly string DisplayName;
            public readonly string Description;
            public readonly InputBindingCategory Category;
            public readonly bool Required;

            public BindingDefinition(
                string map,
                string action,
                string displayName,
                string description,
                InputBindingCategory category,
                bool required = false)
            {
                Map = map;
                Action = action;
                DisplayName = displayName;
                Description = description;
                Category = category;
                Required = required;
            }
        }

        // 표시 순서 = 배열 순서. 카테고리별로 묶여 있어야 목록의 섹션 헤더가 자연스럽게 나온다.
        private static readonly BindingDefinition[] RebindableDefinitions =
        {
            new(InputMapNames.PlayerAction, PlayerAction.Jump, "점프",
                "제자리 또는 이동 중 도약합니다.", InputBindingCategory.Movement),
            new(InputMapNames.PlayerAction, PlayerAction.Sprint, "전력 질주",
                "유지하는 동안 이동 속도가 빨라집니다.", InputBindingCategory.Movement),
            new(InputMapNames.PlayerAction, PlayerAction.Walk, "걷기 전환",
                "걷기와 달리기를 번갈아 바꿉니다.", InputBindingCategory.Movement),
            new(InputMapNames.PlayerAction, PlayerAction.Dash, "대시",
                "바라보는 방향으로 짧게 돌진합니다. 쿨타임이 있습니다.", InputBindingCategory.Movement),
            new(InputMapNames.PlayerAction, PlayerAction.Dodge, "회피",
                "무적 판정으로 공격을 흘립니다. 정확한 타이밍에 반격 기회가 열립니다.", InputBindingCategory.Movement),
            new(InputMapNames.PlayerAction, PlayerAction.Crouching, "웅크리기",
                "자세를 낮춰 좁은 공간을 지나갑니다.", InputBindingCategory.Movement),

            new(InputMapNames.PlayerAction, PlayerAction.Attack, "일반 공격",
                "무기를 사용해 적을 공격합니다. 연속 입력으로 콤보가 이어집니다.", InputBindingCategory.Combat),
            new(InputMapNames.PlayerAction, PlayerAction.HeavyAttack, "강공격",
                "느리지만 강한 일격입니다. 적의 강인도를 크게 깎습니다.", InputBindingCategory.Combat),
            new(InputMapNames.PlayerAction, PlayerAction.Guard, "가드",
                "유지하는 동안 피해를 줄입니다. 피격 직전에 눌러 저스트 가드가 됩니다.", InputBindingCategory.Combat),
            new(InputMapNames.PlayerAction, PlayerAction.LockOn, "락온",
                "가장 가까운 적을 시야에 고정합니다.", InputBindingCategory.Combat),
            new(InputMapNames.PlayerAction, PlayerAction.LockOnSwitchLeft, "락온 대상 왼쪽",
                "고정 대상을 왼쪽 적으로 바꿉니다.", InputBindingCategory.Combat),
            new(InputMapNames.PlayerAction, PlayerAction.LockOnSwitchRight, "락온 대상 오른쪽",
                "고정 대상을 오른쪽 적으로 바꿉니다.", InputBindingCategory.Combat),

            new(InputMapNames.PlayerAction, PlayerAction.SkillAbility, "스킬",
                "장착한 캐릭터 스킬을 사용합니다. 스킬 게이지를 소모합니다.", InputBindingCategory.Skill),
            new(InputMapNames.PlayerAction, PlayerAction.SkillUltimate, "궁극기",
                "게이지를 모두 소모해 최대 위력의 기술을 사용합니다.", InputBindingCategory.Skill),
            new(InputMapNames.PlayerAction, PlayerAction.ElementBuff, "원소 버프",
                "무기에 원소를 부여해 속성 피해를 더합니다.", InputBindingCategory.Skill),
            new(InputMapNames.PlayerAction, PlayerAction.BossAssist, "보스 어시스트",
                "영입한 보스를 불러 지정 스킬을 1회 사용합니다.", InputBindingCategory.Skill),

            new(InputMapNames.PlayerAction, PlayerAction.Interact, "상호작용",
                "대화, 채집, 장치 조작 등 상황에 맞는 행동을 합니다.", InputBindingCategory.System),
            new(InputMapNames.PlayerAction, PlayerAction.Equip, "무기 장착",
                "무기를 뽑거나 집어넣습니다.", InputBindingCategory.System),
            new(InputMapNames.PlayerAction, PlayerAction.CharacterSwap_1, "캐릭터 교체 1",
                "파티 1번 캐릭터로 즉시 교체합니다.", InputBindingCategory.System),
            new(InputMapNames.PlayerAction, PlayerAction.CharacterSwap_2, "캐릭터 교체 2",
                "파티 2번 캐릭터로 즉시 교체합니다.", InputBindingCategory.System),
            new(InputMapNames.PlayerAction, PlayerAction.CharacterSwap_3, "캐릭터 교체 3",
                "파티 3번 캐릭터로 즉시 교체합니다.", InputBindingCategory.System),
            new(InputMapNames.PlayerAction, PlayerAction.CharacterSwap_4, "캐릭터 교체 4",
                "파티 4번 캐릭터로 즉시 교체합니다.", InputBindingCategory.System),
            new(InputMapNames.PlayerAction, PlayerAction.QuickSlot_Up, "퀵슬롯 위",
                "위쪽 퀵슬롯에 등록한 아이템을 사용합니다.", InputBindingCategory.System),
            new(InputMapNames.PlayerAction, PlayerAction.QuickSlot_Right, "퀵슬롯 오른쪽",
                "오른쪽 퀵슬롯에 등록한 아이템을 사용합니다.", InputBindingCategory.System),
            new(InputMapNames.PlayerAction, PlayerAction.QuickSlot_Down, "퀵슬롯 아래",
                "아래쪽 퀵슬롯에 등록한 아이템을 사용합니다.", InputBindingCategory.System),
            new(InputMapNames.PlayerAction, PlayerAction.QuickSlot_Left, "퀵슬롯 왼쪽",
                "왼쪽 퀵슬롯에 등록한 아이템을 사용합니다.", InputBindingCategory.System),

            new(InputMapNames.UI, UIAction.Inventory, "인벤토리",
                "소지품 창을 엽니다.", InputBindingCategory.UI),
            new(InputMapNames.UI, UIAction.Map, "지도",
                "지도를 엽니다.", InputBindingCategory.UI),
            new(InputMapNames.UI, UIAction.Party, "파티",
                "파티 구성 창을 엽니다.", InputBindingCategory.UI),
            new(InputMapNames.UI, UIAction.MenuPanel, "메뉴",
                "메인 메뉴를 엽니다. 필수 키라 비울 수 없습니다.", InputBindingCategory.UI, true),
            new(InputMapNames.UI, UIAction.Submit, "UI 확인",
                "선택한 항목을 확정합니다. 필수 키라 비울 수 없습니다.", InputBindingCategory.UI, true),
            new(InputMapNames.UI, UIAction.Cancel, "UI 취소",
                "창을 닫거나 이전으로 돌아갑니다. 필수 키라 비울 수 없습니다.", InputBindingCategory.UI, true),
        };

        private void LoadInputBindingProfile()
        {
            string json = PlayerPrefs.GetString(BindingProfilePrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                _bindingProfile = new InputBindingProfileData();
                ApplyBindingProfile();
                return;
            }

            try
            {
                _bindingProfile = JsonUtility.FromJson<InputBindingProfileData>(json)
                                  ?? new InputBindingProfileData();
                _bindingProfile.entries ??= new List<InputBindingOverrideEntry>();
                MigrateBindingProfile(_bindingProfile);
                ApplyBindingProfile();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[InputManager] 입력 바인딩 프로필 로드 실패. 기본값을 사용합니다.\n{exception}");
                _bindingProfile = new InputBindingProfileData();
                ApplyBindingProfile();
            }
        }

        /// <summary>
        /// 액션 GUID 우선 식별으로 프로필을 현재 액션 에셋에 맞춰 이전한다(스펙 §13.4).
        /// 실패한 슬롯만 기본값으로 되돌리고 프로필 전체는 유지한다.
        /// </summary>
        private void MigrateBindingProfile(InputBindingProfileData profile)
        {
            InputBindingMigrationReport report =
                InputBindingProfileMigration.Migrate(profile, this);

            if (report.HasChanges)
                Debug.Log($"[InputManager] 입력 바인딩 프로필 마이그레이션: {report}");
        }

        bool IInputActionIdentityLookup.TryResolveById(
            string actionId,
            out string mapName,
            out string actionName)
        {
            mapName = null;
            actionName = null;
            if (string.IsNullOrWhiteSpace(actionId)
                || !Guid.TryParse(actionId, out Guid guid))
            {
                return false;
            }

            foreach (InputAction action in actionCache.Values)
            {
                if (action.id != guid)
                    continue;

                mapName = action.actionMap?.name;
                actionName = action.name;
                return !string.IsNullOrEmpty(mapName);
            }

            return false;
        }

        bool IInputActionIdentityLookup.TryResolveByName(
            string mapName,
            string actionName,
            out string actionId)
        {
            actionId = null;
            InputAction action = GetAction(mapName, actionName);
            if (action == null)
                return false;

            actionId = action.id.ToString();
            return true;
        }

        private string ResolveActionId(string mapName, string actionName)
        {
            InputAction action = GetAction(mapName, actionName);
            return action == null ? null : action.id.ToString();
        }

        public IReadOnlyList<InputBindingDescriptor> GetBindingDescriptors(
            InputBindingDeviceGroup deviceGroup)
        {
            var result = new List<InputBindingDescriptor>(RebindableDefinitions.Length * 2);

            foreach (BindingDefinition definition in RebindableDefinitions)
            {
                if (!GetAction(definition.Map, definition.Action, out InputAction action) || action == null)
                    continue;

                foreach (InputBindingSlot slot in Enum.GetValues(typeof(InputBindingSlot)))
                {
                    var target = new InputBindingTarget(
                        definition.Map,
                        definition.Action,
                        deviceGroup,
                        slot);

                    bool customized = TryGetProfileEntry(target, out InputBindingOverrideEntry entry);
                    bool found = TryGetBindingShape(
                        target,
                        out string modifierPath,
                        out string controlPath,
                        out bool isComposite);

                    string display = found
                        ? FormatBindingDisplay(modifierPath, controlPath)
                        : "미지정";

                    result.Add(new InputBindingDescriptor(
                        target,
                        definition.DisplayName,
                        definition.Description,
                        definition.Category,
                        display,
                        isComposite,
                        definition.Required,
                        customized && entry != null && !entry.disabled));
                }
            }

            return result;
        }

        public string CaptureBindingProfileSnapshot() =>
            JsonUtility.ToJson(_bindingProfile?.Clone() ?? new InputBindingProfileData());

        public bool RestoreBindingProfileSnapshot(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            // 스냅샷이 현재 상태와 같으면 아무 작업도 하지 않는다.
            // 설정 창을 열었다가 그냥 닫는 가장 흔한 경우에도 전체 액션 맵 재적용과
            // OnBindingsChanged 연쇄(프롬프트 아이콘 갱신 + 키 목록 전체 재생성)가
            // 통째로 실행돼 창을 닫을 때 눈에 띄는 지연이 생겼다.
            if (string.Equals(json, CaptureBindingProfileSnapshot(), StringComparison.Ordinal))
                return true;

            try
            {
                var restored = JsonUtility.FromJson<InputBindingProfileData>(json);
                if (restored == null)
                    return false;

                restored.entries ??= new List<InputBindingOverrideEntry>();
                MigrateBindingProfile(restored);
                _bindingProfile = restored;
                CommitBindingProfileChange();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[InputManager] 입력 바인딩 스냅샷 복원 실패.\n{exception}");
                return false;
            }
        }

        public void SaveBindingProfile(bool flushPlayerPrefs = true)
        {
            string json = JsonUtility.ToJson(_bindingProfile ?? new InputBindingProfileData());
            PlayerPrefs.SetString(BindingProfilePrefsKey, json);
            if (flushPlayerPrefs)
                PlayerPrefs.Save();
        }

        public bool TryApplyBinding(
            InputRebindCaptureResult capture,
            bool replaceConflict,
            out InputBindingConflictInfo conflict)
        {
            conflict = InputBindingConflictInfo.None;
            if (!capture.IsCompleted
                || GetAction(capture.Target.mapName, capture.Target.actionName) == null)
            {
                return false;
            }

            var conflicts = FindConflicts(
                capture.Target,
                capture.ModifierPath,
                capture.ControlPath);

            if (conflicts.Count > 0)
            {
                InputBindingConflictInfo requiredConflict =
                    conflicts.FirstOrDefault(item => item.IsRequired);
                conflict = requiredConflict.HasConflict
                    ? requiredConflict
                    : conflicts[0];

                if (!replaceConflict || requiredConflict.HasConflict)
                    return false;

                foreach (InputBindingConflictInfo item in conflicts)
                    DisableBinding(item.ExistingTarget);
            }

            UpsertProfileEntry(new InputBindingOverrideEntry
            {
                actionId = ResolveActionId(capture.Target.mapName, capture.Target.actionName),
                mapName = capture.Target.mapName,
                actionName = capture.Target.actionName,
                deviceGroup = capture.Target.deviceGroup,
                slot = capture.Target.slot,
                isComposite = capture.IsComposite,
                disabled = false,
                modifierPath = capture.ModifierPath,
                controlPath = capture.ControlPath,
            });

            CommitBindingProfileChange();
            return true;
        }

        /// <summary>
        /// 사용자 슬롯을 비운다. 필수 액션의 Primary 슬롯은 제거하지 않는다.
        /// 기본 바인딩이 있는 Primary는 disabled 프로필 항목으로 저장해야 재시작 후에도
        /// 비활성 상태가 유지된다.
        /// </summary>
        public bool ClearBinding(InputBindingTarget target)
        {
            BindingDefinition? definition = FindDefinition(target.mapName, target.actionName);
            if (!definition.HasValue
                || definition.Value.Required && target.slot == InputBindingSlot.Primary)
            {
                return false;
            }

            DisableBinding(target);
            CommitBindingProfileChange();
            return true;
        }

        public void ResetBinding(InputBindingTarget target)
        {
            int removed = _bindingProfile.entries.RemoveAll(entry =>
                entry != null && entry.Target.Equals(target));
            if (removed == 0)
                return;

            CommitBindingProfileChange();
        }

        /// <summary>
        /// 한 액션의 모든 장치·슬롯을 기본값으로 되돌린다.
        /// ResetBinding을 4번 부르면 ApplyBindingProfile이 4번 돌아 액션 맵을
        /// 한 프레임에 네 번 Disable/Enable하게 되므로 한 번에 처리한다.
        /// </summary>
        public void ResetBindingsForAction(string mapName, string actionName)
        {
            if (string.IsNullOrEmpty(mapName) || string.IsNullOrEmpty(actionName))
                return;

            int removed = _bindingProfile.entries.RemoveAll(entry =>
                entry != null
                && entry.mapName == mapName
                && entry.actionName == actionName);
            if (removed == 0)
                return;

            CommitBindingProfileChange();
        }

        public void ResetBindings(InputBindingDeviceGroup? deviceGroup = null)
        {
            if (deviceGroup.HasValue)
            {
                _bindingProfile.entries.RemoveAll(entry =>
                    entry != null && entry.deviceGroup == deviceGroup.Value);
            }
            else
            {
                _bindingProfile.entries.Clear();
            }

            CommitBindingProfileChange();
        }

        /// <summary>
        /// 프로필 전체를 액션 에셋에 반영한다.
        ///
        /// 맵을 껐다 켤 때마다 InputActionState가 전부 다시 해석되므로,
        /// 초기화와 엔트리 적용을 <b>하나의 disable 구간</b>으로 묶어 맵당 1회만 토글한다.
        /// 예전에는 ApplyProfileEntry가 엔트리마다 자기 맵을 껐다 켜서
        /// 재해석이 엔트리 수만큼 반복됐고, 설정 창을 닫을 때 지연으로 체감됐다.
        ///
        /// ApplyProfileEntry는 map.enabled가 false면 스스로 토글하지 않으므로
        /// 이 구간 안에서는 override 적용만 수행한다. 구조(AddBinding)는 건드리지 않는다.
        /// </summary>
        private void ApplyBindingProfile()
        {
            var reEnableTargets = new List<InputActionMap>();

            try
            {
                foreach (InputActionMap map in actionMapCache.Values)
                {
                    // 원래 켜져 있던 맵만 기억했다가 끝에서 되돌린다.
                    if (map.enabled)
                    {
                        map.Disable();
                        reEnableTargets.Add(map);
                    }

                    foreach (InputAction action in map.actions)
                        action.RemoveAllBindingOverrides();

                    DisableRuntimeUserBindings(map);
                }

                if (_bindingProfile?.entries != null)
                {
                    foreach (InputBindingOverrideEntry entry in _bindingProfile.entries)
                    {
                        if (entry == null)
                            continue;

                        ApplyProfileEntry(entry);
                    }
                }
            }
            finally
            {
                // 엔트리 적용 중 예외가 나도 입력이 죽지 않도록 반드시 되돌린다.
                for (int i = 0; i < reEnableTargets.Count; i++)
                    reEnableTargets[i].Enable();
            }

            // effective binding이 바뀌었으므로 조합 카탈로그와 진행 중 중재 상태를 다시 만든다.
            RebuildChordCatalog();
        }

        private void ApplyProfileEntry(InputBindingOverrideEntry entry)
        {
            InputAction action = GetAction(entry.mapName, entry.actionName);
            if (action == null)
                return;

            InputActionMap map = action.actionMap;
            bool wasEnabled = map != null && map.enabled;
            if (wasEnabled)
                map.Disable();

            try
            {
                if (entry.slot == InputBindingSlot.Primary)
                    DisableDefaultBindings(action, entry.deviceGroup);

                if (entry.disabled)
                    return;

                if (!TryGetUserBindingSlot(
                        action,
                        entry.deviceGroup,
                        entry.slot,
                        out int singleIndex,
                        out int compositeIndex,
                        out int modifierIndex,
                        out int triggerIndex))
                {
                    // 슬롯은 Init에서 전부 만들어져 있어야 한다. 여기서 추가하면 에셋 구조가
                    // 바뀌어 InputActionState가 재생성되고, PlayerInputActions/UI 액션을 참조하는
                    // InputSystemUIInputModule의 캐시가 무효화된다(EventSystem.Update에서
                    // FetchMapIndices의 maps가 null이 되며 ArgumentNullException).
                    Debug.LogError(
                        $"[InputManager] 사용자 바인딩 슬롯이 없습니다: " +
                        $"{entry.mapName}/{entry.actionName}/{entry.deviceGroup}/{entry.slot}. " +
                        "EnsureAllUserBindingSlots가 Init에서 실행됐는지 확인하세요.");
                    return;
                }

                if (entry.isComposite)
                {
                    action.ApplyBindingOverride(singleIndex, string.Empty);
                    action.RemoveBindingOverride(compositeIndex);
                    action.ApplyBindingOverride(modifierIndex, entry.modifierPath);
                    action.ApplyBindingOverride(triggerIndex, entry.controlPath);
                }
                else
                {
                    action.ApplyBindingOverride(singleIndex, entry.controlPath);

                    // composite 루트의 path는 컴포지트 타입 이름("OneModifier")이다.
                    // 빈 문자열로 덮으면 InstantiateBindingComposite가
                    // "No binding composite with name '' has been registered"로 던진다.
                    // 루트는 원본으로 되돌리고, part만 비워 무력화한다.
                    action.RemoveBindingOverride(compositeIndex);
                    action.ApplyBindingOverride(modifierIndex, string.Empty);
                    action.ApplyBindingOverride(triggerIndex, string.Empty);
                }
            }
            finally
            {
                if (wasEnabled)
                    map.Enable();
            }
        }

        /// <summary>사용자 바인딩 슬롯 전체를 무력화한다.</summary>
        private static void DisableRuntimeUserBindings(InputActionMap map)
        {
            foreach (InputAction action in map.actions)
            {
                var bindings = action.bindings;
                for (int i = 0; i < bindings.Count; i++)
                {
                    if (IsUserBinding(bindings[i]))
                        DisableBindingAt(action, i);
                }
            }
        }

        /// <summary>
        /// 바인딩 하나를 무력화한다. 단일 바인딩은 경로를 비우고,
        /// composite는 <b>루트를 건드리지 않고</b> part만 비운다.
        ///
        /// composite 루트의 path는 컨트롤 경로가 아니라 컴포지트 타입 이름("OneModifier",
        /// "2DVector")이다. 이를 빈 문자열로 override하면 바인딩 재해석 중
        /// InstantiateBindingComposite가 "No binding composite with name '' has been registered"로
        /// 던진다. 그 예외는 InputBindingResolver.StartWithPreviousResolve가 state.maps를 null로
        /// 만든 뒤 ClaimDataFrom이 복구하기 전에 발생하므로, InputActionState가 maps=null인 채로
        /// 영구히 남는다. 이후 모든 action.controls 접근이 매 프레임
        /// ArgumentNullException(FetchMapIndices)으로 죽는다.
        ///
        /// part를 전부 비우면 해석되는 컨트롤이 0개가 되어 composite는 그대로 불활성이 된다.
        /// </summary>
        private static void DisableBindingAt(InputAction action, int index)
        {
            var bindings = action.bindings;
            if (index < 0 || index >= bindings.Count)
                return;

            if (!bindings[index].isComposite)
            {
                action.ApplyBindingOverride(index, string.Empty);
                return;
            }

            for (int p = index + 1; p < bindings.Count && bindings[p].isPartOfComposite; p++)
                action.ApplyBindingOverride(p, string.Empty);
        }

        private static void DisableDefaultBindings(
            InputAction action,
            InputBindingDeviceGroup deviceGroup)
        {
            var bindings = action.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                InputBinding binding = bindings[i];
                if (binding.isPartOfComposite || IsUserBinding(binding))
                    continue;

                // 기본 바인딩에도 composite가 있다(Dodge의 LB+East 조합 등).
                // 루트를 비우면 안 되므로 DisableBindingAt을 거친다.
                if (RootBindingMatchesDevice(bindings, i, deviceGroup))
                    DisableBindingAt(action, i);
            }
        }

        private static bool RootBindingMatchesDevice(
            IReadOnlyList<InputBinding> bindings,
            int rootIndex,
            InputBindingDeviceGroup deviceGroup)
        {
            InputBinding root = bindings[rootIndex];
            if (!root.isComposite)
                return PathMatchesDevice(root.path, deviceGroup);

            for (int i = rootIndex + 1;
                 i < bindings.Count && bindings[i].isPartOfComposite;
                 i++)
            {
                if (PathMatchesDevice(bindings[i].path, deviceGroup))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 모든 리바인딩 대상 액션에 사용자 바인딩 슬롯(단일 + 조합)을 미리 만든다.
        ///
        /// 반드시 Init에서 Action Map을 Enable하기 전에 1회만 호출한다.
        /// 런타임에 AddBinding/AddCompositeBinding으로 슬롯을 만들면 에셋 구조가 바뀌어
        /// InputActionState가 재생성되는데, PlayerInputActions/UI 액션을 직접 참조하는
        /// InputSystemUIInputModule은 그 사실을 모른다. 다음 EventSystem.Update의
        /// PurgeStalePointers에서 FetchMapIndices의 maps가 null이 되어
        /// ArgumentNullException으로 죽는다.
        ///
        /// 슬롯을 미리 다 깔아두면 이후 리바인딩은 ApplyBindingOverride만 쓰므로
        /// 구조 변경이 전혀 발생하지 않는다.
        /// </summary>
        private void EnsureAllUserBindingSlots()
        {
            foreach (BindingDefinition definition in RebindableDefinitions)
            {
                InputAction action = GetAction(definition.Map, definition.Action);
                if (action == null)
                    continue;

                InputActionMap map = action.actionMap;
                if (map != null && map.enabled)
                {
                    Debug.LogError(
                        $"[InputManager] {map.name}이 이미 Enable 상태입니다. " +
                        "사용자 바인딩 슬롯은 Enable 전에 만들어야 합니다.");
                    continue;
                }

                foreach (InputBindingDeviceGroup device in
                         Enum.GetValues(typeof(InputBindingDeviceGroup)))
                {
                    foreach (InputBindingSlot slot in Enum.GetValues(typeof(InputBindingSlot)))
                        CreateUserBindingSlot(action, device, slot);
                }
            }
        }

        private static void CreateUserBindingSlot(
            InputAction action,
            InputBindingDeviceGroup deviceGroup,
            InputBindingSlot slot)
        {
            string baseGroup = BuildUserGroup(deviceGroup, slot);
            string singleGroup = baseGroup + "_Single";
            string chordGroup = baseGroup + "_Chord";

            // 플레이스홀더는 즉시 빈 override로 덮으므로 실제로 입력을 받지 않는다.
            // 그래도 유효한 경로여야 바인딩 추가가 성립한다.
            string placeholder = deviceGroup == InputBindingDeviceGroup.Gamepad
                ? "<Gamepad>/buttonSouth"
                : "<Keyboard>/space";
            string modifierPlaceholder = deviceGroup == InputBindingDeviceGroup.Gamepad
                ? "<Gamepad>/leftShoulder"
                : "<Keyboard>/leftCtrl";

            if (FindBindingByGroup(action, singleGroup, composite: false) < 0)
                action.AddBinding(placeholder, groups: singleGroup);

            if (FindBindingByGroup(action, chordGroup, composite: true) < 0)
            {
                var chordComposite = action.AddCompositeBinding("OneModifier")
                    .With("modifier", modifierPlaceholder)
                    .With("binding", placeholder);
                action.ChangeBinding(chordComposite.bindingIndex).WithGroup(chordGroup);
            }
        }

        /// <summary>
        /// 미리 만들어둔 사용자 바인딩 슬롯의 인덱스를 찾는다. 구조를 바꾸지 않는다.
        /// </summary>
        private static bool TryGetUserBindingSlot(
            InputAction action,
            InputBindingDeviceGroup deviceGroup,
            InputBindingSlot slot,
            out int singleIndex,
            out int compositeIndex,
            out int modifierIndex,
            out int triggerIndex)
        {
            string baseGroup = BuildUserGroup(deviceGroup, slot);
            singleIndex = FindBindingByGroup(action, baseGroup + "_Single", composite: false);
            compositeIndex = FindBindingByGroup(action, baseGroup + "_Chord", composite: true);
            modifierIndex = -1;
            triggerIndex = -1;

            if (singleIndex < 0 || compositeIndex < 0)
                return false;

            for (int i = compositeIndex + 1;
                 i < action.bindings.Count && action.bindings[i].isPartOfComposite;
                 i++)
            {
                if (string.Equals(action.bindings[i].name, "modifier", StringComparison.OrdinalIgnoreCase))
                    modifierIndex = i;
                else if (string.Equals(action.bindings[i].name, "binding", StringComparison.OrdinalIgnoreCase))
                    triggerIndex = i;
            }

            return modifierIndex >= 0 && triggerIndex >= 0;
        }

        private static int FindBindingByGroup(
            InputAction action,
            string group,
            bool composite)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];
                if (binding.isPartOfComposite || binding.isComposite != composite)
                    continue;

                if (BindingHasGroup(binding, group))
                    return i;
            }

            return -1;
        }

        private bool TryGetBindingShape(
            InputBindingTarget target,
            out string modifierPath,
            out string controlPath,
            out bool isComposite)
        {
            modifierPath = null;
            controlPath = null;
            isComposite = false;

            if (TryGetProfileEntry(target, out InputBindingOverrideEntry entry))
            {
                if (entry == null || entry.disabled || string.IsNullOrWhiteSpace(entry.controlPath))
                    return false;

                modifierPath = entry.modifierPath;
                controlPath = entry.controlPath;
                isComposite = entry.isComposite;
                return true;
            }

            if (target.slot == InputBindingSlot.Secondary)
                return false;

            InputAction action = GetAction(target.mapName, target.actionName);
            if (action == null)
                return false;

            var bindings = action.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                InputBinding binding = bindings[i];
                if (binding.isPartOfComposite || IsUserBinding(binding))
                    continue;

                if (!RootBindingMatchesDevice(bindings, i, target.deviceGroup))
                    continue;

                if (!binding.isComposite)
                {
                    string path = binding.effectivePath;
                    if (string.IsNullOrWhiteSpace(path))
                        continue;

                    controlPath = path;
                    return true;
                }

                string modifier = null;
                string trigger = null;
                for (int p = i + 1;
                     p < bindings.Count && bindings[p].isPartOfComposite;
                     p++)
                {
                    string path = bindings[p].effectivePath;
                    if (string.Equals(bindings[p].name, "modifier", StringComparison.OrdinalIgnoreCase))
                        modifier = path;
                    else if (string.Equals(bindings[p].name, "binding", StringComparison.OrdinalIgnoreCase))
                        trigger = path;
                }

                if (!string.IsNullOrWhiteSpace(trigger))
                {
                    modifierPath = modifier;
                    controlPath = trigger;
                    isComposite = !string.IsNullOrWhiteSpace(modifier);
                    return true;
                }
            }

            return false;
        }

        private List<InputBindingConflictInfo> FindConflicts(
            InputBindingTarget target,
            string modifierPath,
            string controlPath)
        {
            var result = new List<InputBindingConflictInfo>();
            string targetModifier = NormalizePath(modifierPath);
            string targetControl = NormalizePath(controlPath);
            bool targetComposite = !string.IsNullOrEmpty(targetModifier);

            foreach (BindingDefinition definition in RebindableDefinitions)
            {
                foreach (InputBindingSlot slot in Enum.GetValues(typeof(InputBindingSlot)))
                {
                    var existingTarget = new InputBindingTarget(
                        definition.Map,
                        definition.Action,
                        target.deviceGroup,
                        slot);

                    if (existingTarget.Equals(target)
                        || !ContextsOverlap(target.mapName, existingTarget.mapName)
                        || !TryGetBindingShape(
                            existingTarget,
                            out string existingModifierPath,
                            out string existingControlPath,
                            out bool existingComposite))
                    {
                        continue;
                    }

                    string existingModifier = NormalizePath(existingModifierPath);
                    string existingControl = NormalizePath(existingControlPath);

                    bool exact = targetComposite == existingComposite
                                 && targetControl == existingControl
                                 && targetModifier == existingModifier;

                    bool subset = targetComposite != existingComposite
                                  && (targetComposite
                                      ? existingControl == targetModifier || existingControl == targetControl
                                      : targetControl == existingModifier || targetControl == existingControl);

                    if (!exact && !subset)
                        continue;

                    result.Add(new InputBindingConflictInfo(
                        true,
                        existingTarget,
                        definition.DisplayName,
                        definition.Required,
                        subset));
                }
            }

            return result;
        }

        private void DisableBinding(InputBindingTarget target)
        {
            UpsertProfileEntry(new InputBindingOverrideEntry
            {
                actionId = ResolveActionId(target.mapName, target.actionName),
                mapName = target.mapName,
                actionName = target.actionName,
                deviceGroup = target.deviceGroup,
                slot = target.slot,
                disabled = true,
            });
        }

        private void UpsertProfileEntry(InputBindingOverrideEntry entry)
        {
            _bindingProfile ??= new InputBindingProfileData();
            _bindingProfile.entries ??= new List<InputBindingOverrideEntry>();
            _bindingProfile.entries.RemoveAll(existing =>
                existing != null && existing.Target.Equals(entry.Target));
            _bindingProfile.entries.Add(entry);
        }

        private bool TryGetProfileEntry(
            InputBindingTarget target,
            out InputBindingOverrideEntry entry)
        {
            entry = _bindingProfile?.entries?.FirstOrDefault(candidate =>
                candidate != null && candidate.Target.Equals(target));
            return entry != null;
        }

        private static bool IsUserBinding(InputBinding binding) =>
            !string.IsNullOrWhiteSpace(binding.groups)
            && binding.groups.Contains(UserBindingGroupPrefix, StringComparison.Ordinal);

        private static bool BindingHasGroup(InputBinding binding, string group)
        {
            if (string.IsNullOrWhiteSpace(binding.groups))
                return false;

            string[] groups = binding.groups.Split(InputBinding.Separator);
            return groups.Any(candidate =>
                string.Equals(candidate, group, StringComparison.Ordinal));
        }

        private static string BuildUserGroup(
            InputBindingDeviceGroup deviceGroup,
            InputBindingSlot slot) =>
            $"{UserBindingGroupPrefix}{deviceGroup}_{slot}";

        private static bool PathMatchesDevice(
            string path,
            InputBindingDeviceGroup deviceGroup)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            bool gamepad = path.Contains("Gamepad", StringComparison.OrdinalIgnoreCase)
                           || path.Contains("XInputController", StringComparison.OrdinalIgnoreCase)
                           || path.Contains("DualShock", StringComparison.OrdinalIgnoreCase)
                           || path.Contains("DualSense", StringComparison.OrdinalIgnoreCase)
                           || path.Contains("SwitchPro", StringComparison.OrdinalIgnoreCase);
            return deviceGroup == InputBindingDeviceGroup.Gamepad ? gamepad : !gamepad;
        }

        private static string FormatBindingDisplay(string modifierPath, string controlPath)
        {
            string control = ToHumanReadable(controlPath);
            if (string.IsNullOrWhiteSpace(modifierPath))
                return control;

            return $"{ToHumanReadable(modifierPath)} + {control}";
        }

        private static string ToHumanReadable(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "미지정";

            return InputControlPath.ToHumanReadableString(
                path,
                InputControlPath.HumanReadableStringOptions.OmitDevice);
        }

        private static string NormalizePath(string path) =>
            string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : path.Trim().ToLowerInvariant();

        private static bool ContextsOverlap(string firstMap, string secondMap) =>
            string.Equals(firstMap, secondMap, StringComparison.Ordinal);

        private static BindingDefinition? FindDefinition(string mapName, string actionName)
        {
            foreach (BindingDefinition definition in RebindableDefinitions)
            {
                if (definition.Map == mapName && definition.Action == actionName)
                    return definition;
            }

            return null;
        }
    }
}
