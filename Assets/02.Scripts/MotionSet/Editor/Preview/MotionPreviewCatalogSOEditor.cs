using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Animation.Editor
{
    [CustomEditor(typeof(MotionPreviewCatalogSO))]
    internal sealed class MotionPreviewCatalogSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            IMotionPreviewCatalogPopulator[] populators =
                TypeCache.GetTypesDerivedFrom<IMotionPreviewCatalogPopulator>()
                    .Where(type => !type.IsAbstract && !type.IsInterface)
                    .Select(TryCreate)
                    .Where(populator => populator != null)
                    .ToArray();
            if (populators.Length == 0)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("프로젝트 데이터 동기화", EditorStyles.boldLabel);
            foreach (IMotionPreviewCatalogPopulator populator in populators)
            {
                if (!GUILayout.Button(populator.ButtonLabel))
                    continue;

                Undo.RecordObject(target, populator.ButtonLabel);
                populator.Populate((MotionPreviewCatalogSO)target);
                EditorUtility.SetDirty(target);
            }
        }

        private static IMotionPreviewCatalogPopulator TryCreate(Type type)
        {
            try
            {
                return Activator.CreateInstance(type) as IMotionPreviewCatalogPopulator;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[MotionPreview] 카탈로그 Populator 생성 실패: {type.FullName}\n{exception}");
                return null;
            }
        }
    }
}
