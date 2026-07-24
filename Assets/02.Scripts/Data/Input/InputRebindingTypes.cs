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

    public enum InputBindingCategory
    {
        Movement,
        Combat,
        Interaction,
        Camera,
        UI,
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
        public readonly InputBindingCategory Category;
        public readonly string BindingDisplay;
        public readonly bool IsComposite;
        public readonly bool IsRequired;
        public readonly bool IsCustomized;

        public InputBindingDescriptor(
            InputBindingTarget target,
            string displayName,
            InputBindingCategory category,
            string bindingDisplay,
            bool isComposite,
            bool isRequired,
            bool isCustomized)
        {
            Target = target;
            DisplayName = displayName;
            Category = category;
            BindingDisplay = bindingDisplay;
            IsComposite = isComposite;
            IsRequired = isRequired;
            IsCustomized = isCustomized;
        }
    }

    [Serializable]
    public sealed class InputBindingOverrideEntry
    {
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
        public int profileVersion = 1;
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
