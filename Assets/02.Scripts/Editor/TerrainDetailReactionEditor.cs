using UnityEditor;
using UnityEngine;
using UPlayGround.Gameplay.World.ReactiveEnvironment;

namespace UPlayGround.EditorTools
{
    /// <summary>Terrain 셀의 원본 revision·구획·명시 레이어를 저작하고 검증한다.</summary>
    [CustomEditor(typeof(TerrainDetailReaction))]
    public sealed class TerrainDetailReactionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var terrain = (TerrainDetailReaction)target;
            EditorGUILayout.HelpBox("Detail 레이어와 같은 순서로 프로젝트 소유 툰 프리팹을 지정하세요. 구획 또는 원본 변경은 기존 저장과 호환되지 않습니다. Terrain Tree는 이 어댑터가 변경하지 않습니다.", MessageType.Info);
            using (new EditorGUI.DisabledScope(Application.isPlaying))
                if (GUILayout.Button("원본 revision 기록 (기존 저장 호환성 변경)"))
                {
                    Undo.RecordObject(target, "Terrain 반응 revision");
                    serializedObject.Update();
                    string path = AssetDatabase.GetAssetPath(terrain.SourceData);
                    serializedObject.FindProperty("_contentRevision").stringValue =
                        AssetDatabase.AssetPathToGUID(path) + ":" + AssetDatabase.GetAssetDependencyHash(path);
                    serializedObject.ApplyModifiedProperties();
                }
            if (GUILayout.Button("셀 구성 검사"))
            {
                string error = terrain.ValidateAuthoring();
                EditorUtility.DisplayDialog("Terrain 반응 구성", error ?? "구성 통과. 실제 풀·전파·저장·성능은 별도 확인하세요.", "확인");
            }
            EditorGUILayout.LabelField("등록 셀 / 밀도 쓰기", terrain.CellCount + " / " + terrain.DensityWrites);
        }
    }
}
