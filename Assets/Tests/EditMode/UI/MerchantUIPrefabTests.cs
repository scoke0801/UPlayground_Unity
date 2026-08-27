using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Path;
using UPlayGround.Manager;

namespace UPlayGround.UI.Tests
{
    /// <summary>상점 UI 프리팹과 UI 데이터베이스의 필수 배선을 검증한다.</summary>
    public sealed class MerchantUIPrefabTests
    {
        private const string PrefabPath =
            "Assets/03.Prefabs/UI/Scene/Merchant/UI_Scene_Merchant.prefab";
        private const string DatabasePath =
            "Assets/10.Datas/Path/UIPrefabDatabase.asset";

        [Test]
        public void 상점_UI는_거래와_게임패드_조작에_필요한_참조를_모두_가진다()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                UI_Scene_Merchant merchant = root.GetComponent<UI_Scene_Merchant>();
                Assert.That(merchant, Is.Not.Null);
                Assert.That(root.GetComponent<Canvas>(), Is.Not.Null);
                Assert.That(root.GetComponent<GraphicRaycaster>(), Is.Not.Null);

                var serializedMerchant = new SerializedObject(merchant);
                AssertObjectReference(serializedMerchant, "_merchantName");
                AssertObjectReference(serializedMerchant, "_goldText");
                AssertObjectReference(serializedMerchant, "_goldPanel");
                AssertObjectReference(serializedMerchant, "_closeButton");
                AssertObjectReference(serializedMerchant, "_tradeTabs");
                AssertObjectReference(serializedMerchant, "_listScroll");
                AssertObjectReference(serializedMerchant, "_listContent");
                AssertObjectReference(serializedMerchant, "_itemSlotPrefab");
                AssertObjectReference(serializedMerchant, "_emptyState");
                AssertObjectReference(serializedMerchant, "_detailPanel");
                AssertObjectReference(serializedMerchant, "_quantityMinusButton");
                AssertObjectReference(serializedMerchant, "_quantityPlusButton");
                AssertObjectReference(serializedMerchant, "_quantityMaxButton");
                AssertObjectReference(serializedMerchant, "_tradeButton");
                AssertObjectReference(serializedMerchant, "_statusCanvas");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void 상점_UI는_Scene_레이어로_데이터베이스에_등록된다()
        {
            UIPrefabDatabase database =
                AssetDatabase.LoadAssetAtPath<UIPrefabDatabase>(DatabasePath);
            Assert.That(database, Is.Not.Null);

            database.Initialize();
            UIPrefabDatabase.UIPrefabEntry entry = database.GetPrefabEntry("Merchant");
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.prefab, Is.Not.Null);
            Assert.That(entry.prefab.GetComponent<UI_Scene_Merchant>(), Is.Not.Null);
            Assert.That(entry.defaultLayer, Is.EqualTo(CanvasLayer.Scene));
        }

        private static void AssertObjectReference(SerializedObject target, string propertyName)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, propertyName);
            Assert.That(property.objectReferenceValue, Is.Not.Null, propertyName);
        }
    }
}
