using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Event;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// 애니메이션 에디터에서 저장한 사용자 MotionEvent 프리셋 라이브러리.
    /// </summary>
    public sealed class MotionEventPresetLibrarySO : ScriptableObject
    {
        public List<MotionEventPresetEntry> presets = new List<MotionEventPresetEntry>();
    }

    [Serializable]
    public sealed class MotionEventPresetEntry
    {
        public string id;
        public string displayName;
        public string description;
        public string aliases;

        [SerializeReference] public List<MotionEventBase> events = new List<MotionEventBase>();

        public static MotionEventPresetEntry FromEvents(string displayName, string description, string aliases,
            IEnumerable<MotionEventBase> sourceEvents)
        {
            var sourceList = sourceEvents?
                .Where(evt => evt != null)
                .OrderBy(evt => evt.startTime)
                .ToList() ?? new List<MotionEventBase>();

            float offset = sourceList.Count > 0 ? sourceList.Min(evt => evt.startTime) : 0f;

            var entry = new MotionEventPresetEntry
            {
                id = Guid.NewGuid().ToString("N"),
                displayName = displayName,
                description = description,
                aliases = aliases,
                events = new List<MotionEventBase>()
            };

            foreach (var evt in sourceList)
            {
                var clone = CloneEvent(evt);
                if (clone == null)
                    continue;

                clone.startTime = Mathf.Max(0f, clone.startTime - offset);
                clone.endTime = Mathf.Max(clone.startTime + 0.01f, clone.endTime - offset);
                entry.events.Add(clone);
            }

            return entry;
        }

        public IEnumerable<MotionEventBase> CreateEvents(float startTime)
        {
            if (events == null)
                yield break;

            foreach (var evt in events)
            {
                var clone = CloneEvent(evt);
                if (clone == null)
                    continue;

                clone.startTime += startTime;
                clone.endTime += startTime;
                yield return clone;
            }
        }

        static MotionEventBase CloneEvent(MotionEventBase evt)
        {
            return MotionEventSerializationUtility.Clone(evt);
        }
    }

    public static class MotionEventPresetLibraryUtility
    {
        const string DefaultFolder = "Assets/10.Datas/Editor";
        const string DefaultPath = DefaultFolder + "/MotionEventPresetLibrary.asset";

        public static MotionEventPresetLibrarySO Load()
        {
            string[] guids = AssetDatabase.FindAssets("t:MotionEventPresetLibrarySO");
            if (guids == null || guids.Length == 0)
                return null;

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<MotionEventPresetLibrarySO>(path);
        }

        public static MotionEventPresetLibrarySO LoadOrCreate()
        {
            var library = Load();
            if (library != null)
                return library;

            EnsureDefaultFolder();

            library = ScriptableObject.CreateInstance<MotionEventPresetLibrarySO>();
            AssetDatabase.CreateAsset(library, DefaultPath);
            AssetDatabase.SaveAssets();
            return library;
        }

        public static void Save(MotionEventPresetLibrarySO library)
        {
            if (library == null)
                return;

            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
        }

        static void EnsureDefaultFolder()
        {
            if (AssetDatabase.IsValidFolder(DefaultFolder))
                return;

            if (!AssetDatabase.IsValidFolder("Assets/10.Datas"))
                AssetDatabase.CreateFolder("Assets", "10.Datas");

            AssetDatabase.CreateFolder("Assets/10.Datas", "Editor");
        }
    }
}
