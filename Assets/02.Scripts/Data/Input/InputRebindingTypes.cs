using System;
using System.Collections.Generic;

namespace UPlayGround.InputDefine
{
    public enum InputBindingDeviceGroup
    {
        KeyboardMouse,
        Gamepad,
    }

    public enum InputBindingSlot
    {
        Primary,
        Secondary,
    }

    /// <summary>
    /// 키 설정 화면 좌측 레일의 분류. 순서가 곧 표시 순서다.
    /// "모든"은 필터 해제(null)로 표현하므로 여기에 넣지 않는다.
    /// </summary>
    public enum InputBindingCategory
    {
        Movement,
        Combat,
        Skill,
        System,
        UI,
    }

    public static class InputBindingCategoryNames
    {
        public static string ToKorean(InputBindingCategory category) => category switch
        {
            InputBindingCategory.Movement => "이동",
            InputBindingCategory.Combat => "전투",
            InputBindingCategory.Skill => "스킬",
            InputBindingCategory.System => "시스템",
            InputBindingCategory.UI => "UI",
            _ => category.ToString(),
        };
    }

    public enum InputRebindCapturePhase
    {
        None,
        WaitingForNeutral,
        WaitingForFirstControl,
        WaitingForSecondControl,
        Completed,
        Canceled,
        TimedOut,
        Failed,
    }

    [Serializable]
    public struct InputBindingTarget : IEquatable<InputBindingTarget>
    {
        public string mapName;
        public string actionName;
        public InputBindingDeviceGroup deviceGroup;
        public InputBindingSlot slot;

        public InputBindingTarget(
            string mapName,
            string actionName,
            InputBindingDeviceGroup deviceGroup,
            InputBindingSlot slot)
        {
            this.mapName = mapName;
            this.actionName = actionName;
            this.deviceGroup = deviceGroup;
            this.slot = slot;
        }

        public bool Equals(InputBindingTarget other) =>
            string.Equals(mapName, other.mapName, StringComparison.Ordinal)
            && string.Equals(actionName, other.actionName, StringComparison.Ordinal)
            && deviceGroup == other.deviceGroup
            && slot == other.slot;

        public override bool Equals(object obj) => obj is InputBindingTarget other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(mapName, actionName, (int)deviceGroup, (int)slot);

        public override string ToString() => $"{mapName}/{actionName}/{deviceGroup}/{slot}";
    }

    public readonly struct InputBindingDescriptor
    {
        public readonly InputBindingTarget Target;
        public readonly string DisplayName;

        /// <summary>키 설정 상세 패널에 띄우는 한 줄 설명.</summary>
        public readonly string Description;

        public readonly InputBindingCategory Category;
        public readonly string BindingDisplay;
        public readonly bool IsComposite;
        public readonly bool IsRequired;
        public readonly bool IsCustomized;

        public InputBindingDescriptor(
            InputBindingTarget target,
            string displayName,
            string description,
            InputBindingCategory category,
            string bindingDisplay,
            bool isComposite,
            bool isRequired,
            bool isCustomized)
        {
            Target = target;
            DisplayName = displayName;
            Description = description;
            Category = category;
            BindingDisplay = bindingDisplay;
            IsComposite = isComposite;
            IsRequired = isRequired;
            IsCustomized = isCustomized;
        }

        /// <summary>바인딩이 지정돼 있는지. 표시 문자열이 폴백("미지정")이면 false.</summary>
        public bool HasBinding =>
            !string.IsNullOrWhiteSpace(BindingDisplay) && BindingDisplay != "미지정";
    }

    [Serializable]
    public sealed class InputBindingOverrideEntry
    {
        /// <summary>
        /// 액션 GUID. 이름보다 우선하는 식별자다(스펙 §13.4).
        /// v1 프로필에는 없으며 로드 시 보조 키로 채운다.
        /// </summary>
        public string actionId;

        public string mapName;
        public string actionName;
        public InputBindingDeviceGroup deviceGroup;
        public InputBindingSlot slot;
        public bool isComposite;
        public bool disabled;
        public string modifierPath;
        public string controlPath;

        public InputBindingTarget Target =>
            new(mapName, actionName, deviceGroup, slot);

        public InputBindingOverrideEntry Clone() => new()
        {
            actionId = actionId,
            mapName = mapName,
            actionName = actionName,
            deviceGroup = deviceGroup,
            slot = slot,
            isComposite = isComposite,
            disabled = disabled,
            modifierPath = modifierPath,
            controlPath = controlPath,
        };
    }

    [Serializable]
    public sealed class InputBindingProfileData
    {
        public int profileVersion = InputBindingProfileMigration.CurrentProfileVersion;
        public List<InputBindingOverrideEntry> entries = new();

        public InputBindingProfileData Clone()
        {
            var clone = new InputBindingProfileData { profileVersion = profileVersion };
            if (entries == null)
                return clone;

            foreach (InputBindingOverrideEntry entry in entries)
            {
                if (entry != null)
                    clone.entries.Add(entry.Clone());
            }

            return clone;
        }
    }

    public readonly struct InputRebindCaptureState
    {
        public readonly InputRebindCapturePhase Phase;
        public readonly string FirstControlDisplay;
        public readonly float RemainingSeconds;
        public readonly string Message;

        public InputRebindCaptureState(
            InputRebindCapturePhase phase,
            string firstControlDisplay,
            float remainingSeconds,
            string message)
        {
            Phase = phase;
            FirstControlDisplay = firstControlDisplay;
            RemainingSeconds = remainingSeconds;
            Message = message;
        }
    }

    public readonly struct InputRebindCaptureResult
    {
        public readonly InputBindingTarget Target;
        public readonly InputRebindCapturePhase Phase;
        public readonly string ModifierPath;
        public readonly string ControlPath;
        public readonly string DisplayString;

        public InputRebindCaptureResult(
            InputBindingTarget target,
            InputRebindCapturePhase phase,
            string modifierPath,
            string controlPath,
            string displayString)
        {
            Target = target;
            Phase = phase;
            ModifierPath = modifierPath;
            ControlPath = controlPath;
            DisplayString = displayString;
        }

        public bool IsCompleted =>
            Phase == InputRebindCapturePhase.Completed
            && !string.IsNullOrWhiteSpace(ControlPath);

        public bool IsComposite => !string.IsNullOrWhiteSpace(ModifierPath);
    }

    public readonly struct InputBindingConflictInfo
    {
        public readonly bool HasConflict;
        public readonly InputBindingTarget ExistingTarget;
        public readonly string ExistingDisplayName;
        public readonly bool IsRequired;
        public readonly bool IsChordSubset;

        public InputBindingConflictInfo(
            bool hasConflict,
            InputBindingTarget existingTarget,
            string existingDisplayName,
            bool isRequired,
            bool isChordSubset)
        {
            HasConflict = hasConflict;
            ExistingTarget = existingTarget;
            ExistingDisplayName = existingDisplayName;
            IsRequired = isRequired;
            IsChordSubset = isChordSubset;
        }

        public static InputBindingConflictInfo None => default;
    }
}
