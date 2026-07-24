using System;
using System.Collections.Generic;
using System.Linq;
using UPlayGround.InputDefine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UPlayGround.Manager
{
    public partial class InputManager
    {
        private const string BindingProfilePrefsKey = "InputBindings_v1";
        private const string UserBindingGroupPrefix = "__UserBinding__";

        private InputBindingProfileData _bindingProfile = new();

        public event Action OnBindingsChanged;

        private readonly struct BindingDefinition
        {
            public readonly string Map;
            public readonly string Action;
            public readonly string DisplayName;
            public readonly InputBindingCategory Category;
            public readonly bool Required;

            public BindingDefinition(
                string map,
                string action,
                string displayName,
                InputBindingCategory category,
                bool required = false)
            {
                Map = map;
                Action = action;
                DisplayName = displayName;
                Category = category;
                Required = required;
            }
        }

        private static readonly BindingDefinition[] RebindableDefinitions =
        {
            new(InputMapNames.PlayerAction, PlayerAction.Jump, "점프", InputBindingCategory.Movement),
            new(InputMapNames.PlayerAction, PlayerAction.Sprint, "전력 질주", InputBindingCategory.Movement),
            new(InputMapNames.PlayerAction, PlayerAction.Walk, "걷기 전환", InputBindingCategory.Movement),
            new(InputMapNames.PlayerAction, PlayerAction.Dash, "대시", InputBindingCategory.Movement),
            new(InputMapNames.PlayerAction, PlayerAction.Dodge, "회피", InputBindingCategory.Movement),
            new(InputMapNames.PlayerAction, PlayerAction.Crouching, "웅크리기", InputBindingCategory.Movement),

            new(InputMapNames.PlayerAction, PlayerAction.Attack, "일반 공격", InputBindingCategory.Combat),
            new(InputMapNames.PlayerAction, PlayerAction.HeavyAttack, "강공격", InputBindingCategory.Combat),
            new(InputMapNames.PlayerAction, PlayerAction.Guard, "가드", InputBindingCategory.Combat),
            new(InputMapNames.PlayerAction, PlayerAction.SkillAbility, "스킬", InputBindingCategory.Combat),
            new(InputMapNames.PlayerAction, PlayerAction.SkillUltimate, "궁극기", InputBindingCategory.Combat),
            new(InputMapNames.PlayerAction, PlayerAction.ElementBuff, "원소 버프", InputBindingCategory.Combat),
            new(InputMapNames.PlayerAction, PlayerAction.LockOn, "락온", InputBindingCategory.Combat),
            new(InputMapNames.PlayerAction, PlayerAction.BossAssist, "보스 어시스트", InputBindingCategory.Combat),

            new(InputMapNames.PlayerAction, PlayerAction.Interact, "상호작용", InputBindingCategory.Interaction),
            new(InputMapNames.PlayerAction, PlayerAction.Equip, "무기 장착", InputBindingCategory.Interaction),
            new(InputMapNames.PlayerAction, PlayerAction.CharacterSwap_1, "캐릭터 교체 1", InputBindingCategory.Interaction),
            new(InputMapNames.PlayerAction, PlayerAction.CharacterSwap_2, "캐릭터 교체 2", InputBindingCategory.Interaction),
            new(InputMapNames.PlayerAction, PlayerAction.CharacterSwap_3, "캐릭터 교체 3", InputBindingCategory.Interaction),
            new(InputMapNames.PlayerAction, PlayerAction.CharacterSwap_4, "캐릭터 교체 4", InputBindingCategory.Interaction),
            new(InputMapNames.PlayerAction, PlayerAction.QuickSlot_Up, "퀵슬롯 위", InputBindingCategory.Interaction),
            new(InputMapNames.PlayerAction, PlayerAction.QuickSlot_Right, "퀵슬롯 오른쪽", InputBindingCategory.Interaction),
            new(InputMapNames.PlayerAction, PlayerAction.QuickSlot_Down, "퀵슬롯 아래", InputBindingCategory.Interaction),
            new(InputMapNames.PlayerAction, PlayerAction.QuickSlot_Left, "퀵슬롯 왼쪽", InputBindingCategory.Interaction),

            new(InputMapNames.PlayerAction, PlayerAction.LockOnSwitchLeft, "락온 대상 왼쪽", InputBindingCategory.Camera),
            new(InputMapNames.PlayerAction, PlayerAction.LockOnSwitchRight, "락온 대상 오른쪽", InputBindingCategory.Camera),

            new(InputMapNames.UI, UIAction.Inventory, "인벤토리", InputBindingCategory.UI),
            new(InputMapNames.UI, UIAction.Map, "지도", InputBindingCategory.UI),
            new(InputMapNames.UI, UIAction.Party, "파티", InputBindingCategory.UI),
            new(InputMapNames.UI, UIAction.MenuPanel, "메뉴", InputBindingCategory.UI, true),
            new(InputMapNames.UI, UIAction.Submit, "UI 확인", InputBindingCategory.UI, true),
            new(InputMapNames.UI, UIAction.Cancel, "UI 취소", InputBindingCategory.UI, true),
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
                ApplyBindingProfile();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[InputManager] 입력 바인딩 프로필 로드 실패. 기본값을 사용합니다.\n{exception}");
                _bindingProfile = new InputBindingProfileData();
                ApplyBindingProfile();
            }
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

            try
            {
                var restored = JsonUtility.FromJson<InputBindingProfileData>(json);
                if (restored == null)
                    return false;

                restored.entries ??= new List<InputBindingOverrideEntry>();
                _bindingProfile = restored;
                ApplyBindingProfile();
                OnBindingsChanged?.Invoke();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[InputManager] 입력 바인딩 스냅샷 복원 실패.\n{exception}");
                return false;
            }
        }

        public void SaveBindingProfile()
        {
            string json = JsonUtility.ToJson(_bindingProfile ?? new InputBindingProfileData());
            PlayerPrefs.SetString(BindingProfilePrefsKey, json);
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
                mapName = capture.Target.mapName,
                actionName = capture.Target.actionName,
                deviceGroup = capture.Target.deviceGroup,
                slot = capture.Target.slot,
                isComposite = capture.IsComposite,
                disabled = false,
                modifierPath = capture.ModifierPath,
                controlPath = capture.ControlPath,
            });

            ApplyBindingProfile();
            OnBindingsChanged?.Invoke();
            return true;
        }

        public void ResetBinding(InputBindingTarget target)
        {
            _bindingProfile.entries.RemoveAll(entry =>
                entry != null && entry.Target.Equals(target));
            ApplyBindingProfile();
            OnBindingsChanged?.Invoke();
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

            ApplyBindingProfile();
            OnBindingsChanged?.Invoke();
        }

        private void ApplyBindingProfile()
        {
            foreach (InputActionMap map in actionMapCache.Values)
            {
                bool wasEnabled = map.enabled;
                if (wasEnabled)
                    map.Disable();

                foreach (InputAction action in map.actions)
                    action.RemoveAllBindingOverrides();

                DisableRuntimeUserBindings(map);

                if (wasEnabled)
                    map.Enable();
            }

            if (_bindingProfile?.entries == null)
                return;

            foreach (InputBindingOverrideEntry entry in _bindingProfile.entries)
            {
                if (entry == null)
                    continue;

                ApplyProfileEntry(entry);
            }
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

                EnsureUserBindingSlot(
                    action,
                    entry.deviceGroup,
                    entry.slot,
                    out int singleIndex,
                    out int compositeIndex,
                    out int modifierIndex,
                    out int triggerIndex);

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
                    action.ApplyBindingOverride(compositeIndex, string.Empty);
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

        private static void DisableRuntimeUserBindings(InputActionMap map)
        {
            foreach (InputAction action in map.actions)
            {
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    if (IsUserBinding(action.bindings[i]))
                        action.ApplyBindingOverride(i, string.Empty);
                }
            }
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

                if (RootBindingMatchesDevice(bindings, i, deviceGroup))
                    action.ApplyBindingOverride(i, string.Empty);
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

        private static void EnsureUserBindingSlot(
            InputAction action,
            InputBindingDeviceGroup deviceGroup,
            InputBindingSlot slot,
            out int singleIndex,
            out int compositeIndex,
            out int modifierIndex,
            out int triggerIndex)
        {
            string baseGroup = BuildUserGroup(deviceGroup, slot);
            string singleGroup = baseGroup + "_Single";
            string chordGroup = baseGroup + "_Chord";

            singleIndex = FindBindingByGroup(action, singleGroup, composite: false);
            compositeIndex = FindBindingByGroup(action, chordGroup, composite: true);

            string placeholder = deviceGroup == InputBindingDeviceGroup.Gamepad
                ? "<Gamepad>/buttonSouth"
                : "<Keyboard>/space";
            string modifierPlaceholder = deviceGroup == InputBindingDeviceGroup.Gamepad
                ? "<Gamepad>/leftShoulder"
                : "<Keyboard>/leftCtrl";

            if (singleIndex < 0)
            {
                action.AddBinding(placeholder, groups: singleGroup);
                singleIndex = FindBindingByGroup(action, singleGroup, composite: false);
            }

            if (compositeIndex < 0)
            {
                var chordComposite = action.AddCompositeBinding("OneModifier")
                    .With("modifier", modifierPlaceholder)
                    .With("binding", placeholder);
                action.ChangeBinding(chordComposite.bindingIndex).WithGroup(chordGroup);
                compositeIndex = FindBindingByGroup(action, chordGroup, composite: true);
            }

            modifierIndex = -1;
            triggerIndex = -1;
            for (int i = compositeIndex + 1;
                 i < action.bindings.Count && action.bindings[i].isPartOfComposite;
                 i++)
            {
                if (string.Equals(action.bindings[i].name, "modifier", StringComparison.OrdinalIgnoreCase))
                    modifierIndex = i;
                else if (string.Equals(action.bindings[i].name, "binding", StringComparison.OrdinalIgnoreCase))
                    triggerIndex = i;
            }

            if (singleIndex < 0 || compositeIndex < 0 || modifierIndex < 0 || triggerIndex < 0)
                throw new InvalidOperationException($"사용자 바인딩 슬롯 생성 실패: {action.actionMap?.name}/{action.name}");
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
