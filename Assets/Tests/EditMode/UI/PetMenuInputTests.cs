using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UPlayGround.UI.Tests
{
    public sealed class PetMenuInputTests
    {
        [Test]
        public void 펫_스킬은_고정_노드와_스킬트리_흐름_머티리얼을_사용한다()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/UI/Scene/Pet/UI_Scene_PetMenu.prefab");
            var menu = new SerializedObject(prefab.GetComponent<UI_Scene_PetMenu>());
            var slots = menu.FindProperty("_skillSlots");
            var paths = menu.FindProperty("_skillPaths");
            Assert.That(slots.arraySize, Is.EqualTo(5));
            Assert.That(paths.arraySize, Is.EqualTo(5));
            var flow = menu.FindProperty("_skillFlowMaterial").objectReferenceValue as Material;
            Assert.That(flow, Is.SameAs(AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/UI/SkillTreeFlow.mat")));
            Assert.That(prefab.transform.Find("V2SkillPage/SkillList").GetComponent<VerticalLayoutGroup>(), Is.Null);
            for (int i = 0; i < slots.arraySize; i++)
            {
                var slot = (RectTransform)slots.GetArrayElementAtIndex(i).objectReferenceValue;
                Assert.That(paths.GetArrayElementAtIndex(i).objectReferenceValue, Is.TypeOf<UISkillTreeCurve>());
                for (int j = i + 1; j < slots.arraySize; j++)
                {
                    var other = (RectTransform)slots.GetArrayElementAtIndex(j).objectReferenceValue;
                    var separation = Vector2.Scale(slot.anchorMin - other.anchorMin, new Vector2(1400, 900));
                    Assert.That(Mathf.Abs(separation.x) >= 200f || Mathf.Abs(separation.y) >= 200f, Is.True,
                        "노드 이름·레벨 영역 사이에 여백이 있어야 한다.");
                }
            }
            var status = prefab.transform.Find("Status").GetComponent<RectTransform>();
            var close = prefab.transform.Find("Close").GetComponent<RectTransform>();
            Assert.That(status.anchoredPosition.y - status.sizeDelta.y / 2f,
                Is.GreaterThan(close.anchoredPosition.y + close.sizeDelta.y / 2f));
        }

        [TestCase(1280, 720)]
        [TestCase(633, 425)]
        public void 펫_화면의_자체_Canvas가_닫기_버튼_클릭을_받는다(int width, int height)
        {
            var canvasObject = new GameObject("PetInputCanvas", typeof(Canvas));
            var eventsObject = new GameObject("PetInputEvents", typeof(EventSystem));
            var cameraObject = new GameObject("PetInputCamera", typeof(Camera));
            var target = new RenderTexture(width, height, 24);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/UI/Scene/Pet/UI_Scene_PetMenu.prefab");
            try
            {
                var camera = cameraObject.GetComponent<Camera>(); camera.enabled = false; camera.targetTexture = target;
                var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera; canvas.planeDistance = 1f;
                var scaler = canvasObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(2560, 1440); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                // Edit Mode에서는 Scaler.Update가 실행되지 않아 실제 UIManager의 계산값을 적용한다.
                canvas.scaleFactor = Mathf.Min(width / 2560f, height / 1440f);
                var menu = UnityEngine.Object.Instantiate(prefab, canvasObject.transform, false);
                var raycaster = menu.GetComponent<GraphicRaycaster>();
                Assert.That(raycaster, Is.Not.Null, "중첩 Canvas는 부모 Canvas의 Raycaster로 클릭되지 않는다.");
                var close = menu.transform.Find("Close").GetComponent<Button>();
                Canvas.ForceUpdateCanvases();
                var screen = ((RectTransform)menu.transform).rect;
                var page = ((RectTransform)menu.transform.Find("V2ModelPage")).rect;
                Assert.That(page.width / screen.width, Is.GreaterThan(.85f), "배경뿐 아니라 콘텐츠도 화면 폭을 사용해야 한다.");
                Assert.That(page.height / screen.height, Is.GreaterThan(.60f));
                var preview = ((RectTransform)menu.transform.Find("V2ModelPage/Preview")).rect;
                Assert.That(preview.width / page.width, Is.GreaterThan(.5f));
                // Edit Mode에서는 렌더 전 Graphic.depth가 -1이어서 모든 레이캐스트가 제외된다.
                camera.Render();
                var point = RectTransformUtility.WorldToScreenPoint(raycaster.eventCamera,
                    close.transform.TransformPoint(((RectTransform)close.transform).rect.center));
                var pointer = new PointerEventData(eventsObject.GetComponent<EventSystem>()) { position = point };
                var hits = new List<RaycastResult>();
                raycaster.Raycast(pointer, hits);
                Assert.That(hits.Exists(hit => hit.gameObject == close.gameObject), Is.True);
                int clicks = 0; close.onClick.AddListener(() => clicks++);
                ExecuteEvents.Execute(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
                Assert.That(clicks, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvasObject);
                UnityEngine.Object.DestroyImmediate(eventsObject);
                cameraObject.GetComponent<Camera>().targetTexture = null;
                UnityEngine.Object.DestroyImmediate(cameraObject);
                target.Release(); UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void 동행펫은_다른_메뉴와_같은_아이콘_타일이다()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/UI/HUD/UI_Scene_MenuPanel.prefab");
            var menu = new SerializedObject(prefab.GetComponent<UI_Scene_MenuPanel>());
            var pet = (RectTransform)((Button)menu.FindProperty("_petButton").objectReferenceValue).transform;
            var exit = (RectTransform)((Button)menu.FindProperty("_exitButton").objectReferenceValue).transform;
            var party = (Button)menu.FindProperty("_partyButton").objectReferenceValue;
            Assert.That(pet.parent.parent, Is.EqualTo(party.transform.parent.parent));
            Assert.That(pet.sizeDelta, Is.EqualTo(((RectTransform)party.transform).sizeDelta));
            Assert.That(pet.GetChild(0).GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(pet.parent, Is.Not.EqualTo(exit.parent));
        }

        [Test]
        public void 펫_화면은_전체화면을_덮고_공용_스킨을_사용한다()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/UI/Scene/Pet/UI_Scene_PetMenu.prefab");
            var rect = (RectTransform)prefab.transform;
            Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(rect.sizeDelta, Is.EqualTo(Vector2.zero));
            Assert.That(prefab.GetComponent<Image>().color.a, Is.EqualTo(1f));
            var theme = AssetDatabase.LoadAssetAtPath<UIVisualThemeSO>("Assets/10.Datas/UI/UIVisualTheme_Celadon.asset");
            Assert.That(prefab.transform.Find("SceneFrame").GetComponent<Image>().sprite, Is.SameAs(theme.PanelFrame));
            Assert.That(prefab.transform.Find("Close").GetComponent<Image>().sprite, Is.SameAs(theme.ButtonFrame));
        }
    }
}
