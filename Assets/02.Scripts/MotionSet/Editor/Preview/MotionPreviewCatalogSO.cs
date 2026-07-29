using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Animation.Editor
{
    [CreateAssetMenu(
        fileName = "MotionPreviewCatalog",
        menuName = "UPlayGround/Motion/Preview Catalog")]
    public sealed class MotionPreviewCatalogSO : ScriptableObject
    {
        [Serializable]
        public sealed class SubjectEntry
        {
            [Tooltip("EditorPrefs 선택 상태에 사용하는 안정적인 식별자입니다. 비워 두면 자동 생성됩니다.")]
            public string id;
            [Tooltip("애니메이션 에디터에 표시할 이름입니다.")]
            public string displayName;
            [Tooltip("프리팹 스폰 또는 프리뷰 씬에 이미 존재하는 대상을 선택합니다.")]
            public SubjectSource source;
            [Tooltip("ScenePrefab일 때 Play Mode에서 생성할 액터 프리팹입니다.")]
            public GameObject prefab;
            [Tooltip("ScenePresent일 때 프리뷰 씬에서 찾을 GameObject 이름입니다.")]
            public string sceneObjectName;
            [Tooltip("모션 정지 시 재생할 선택적 Idle 클립입니다.")]
            public AnimationClip idleClip;
            [Tooltip("ScenePresent 기준 대상 위치에 더할 로컬 스폰 오프셋입니다. 기준 대상이 없으면 카메라 전방 위치에 적용됩니다.")]
            public Vector3 spawnOffset;
        }

        public enum SubjectSource
        {
            ScenePrefab,
            ScenePresent,
        }

        [Tooltip("애니메이션 에디터의 '씬 열기/씬에서 Play'가 사용할 프리뷰 씬입니다.")]
        public SceneAsset previewScene;
        public List<SubjectEntry> subjects = new();

        private void OnValidate()
        {
            subjects ??= new List<SubjectEntry>();
            HashSet<string> ids = new(StringComparer.Ordinal);
            foreach (SubjectEntry entry in subjects)
            {
                if (entry == null)
                    continue;
                if (string.IsNullOrWhiteSpace(entry.id) || !ids.Add(entry.id))
                {
                    entry.id = Guid.NewGuid().ToString("N");
                    ids.Add(entry.id);
                }
            }
        }
    }

    public interface IMotionPreviewCatalogPopulator
    {
        string ButtonLabel { get; }
        void Populate(MotionPreviewCatalogSO catalog);
    }
}
