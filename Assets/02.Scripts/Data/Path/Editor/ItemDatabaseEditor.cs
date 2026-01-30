namespace UPlayGround.Data.Path
{
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(ItemDatabase))]
    public class ItemDatabaseEditor : Editor
    {
        [SerializeField] private string itemFolderPath = "Assets/10.Datas/Item"; // 검색할 폴더 경로

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        
            ItemDatabase database = (ItemDatabase)target;
        
            EditorGUILayout.Space(10);
        
            if (GUILayout.Button("데이터베이스 갱신", GUILayout.Height(30)))
            {
                database.RefreshDatabase(itemFolderPath);
            }
        
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                $"총 {database.AllItems.Count}개의 아이템이 등록되어 있습니다.\n" +
                "버튼을 눌러 아이템 목록을 자동으로 갱신할 수 있습니다.",
                MessageType.Info
            );
        }
    }
#endif
}