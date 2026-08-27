using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Merchant;

namespace UPlayGround.Data.Editor.Merchant
{
    /// <summary>상인 카탈로그의 저장 키·중복 품목·가격 누락을 편집 시점에 알린다.</summary>
    [CustomEditor(typeof(MerchantCatalogSO))]
    public sealed class MerchantCatalogSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            var catalog = (MerchantCatalogSO)target;
            if (catalog.TryValidate(out string error))
            {
                EditorGUILayout.HelpBox(
                    $"거래 품목 {catalog.Offers.Count}개 · 저장 ID {catalog.MerchantId}",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }
        }
    }
}
