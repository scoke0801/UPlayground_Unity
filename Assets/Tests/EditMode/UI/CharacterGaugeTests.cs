using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Party;

namespace UPlayGround.UI.Tests
{
    /// <summary>문양 표시의 자원 경계, 교체 격리, 정지 중 연출 및 프리팹 연결을 검증합니다.</summary>
    public sealed class CharacterGaugeTests
    {
        private const string Folder = "Assets/10.Datas/UI/CharacterGauge/";
        private const string Prefab = "Assets/03.Prefabs/UI/HUD/UI_HUD_PlayerInfo.prefab";

        [TestCase(30f, 100f, .3f)]
        [TestCase(-1f, 100f, 0f)]
        [TestCase(200f, 100f, 1f)]
        [TestCase(50f, 0f, 0f)]
        [TestCase(50f, -1f, 0f)]
        [TestCase(float.NaN, 100f, 0f)]
        [TestCase(50f, float.PositiveInfinity, 0f)]
        public void 자원_경계는_빈상태와_정규화값으로_안전하게_표시한다(float current, float maximum, float expected)
        {
            Assert.That(UICharacterGauge.Normalize(current, maximum), Is.EqualTo(expected).Within(.0001f));
        }

        [Test]
        public void 순환_폴백은_종료하고_유효한_문양은_찾는다()
        {
            var first = ScriptableObject.CreateInstance<CharacterGaugeVisualProfileSO>();
            var second = ScriptableObject.CreateInstance<CharacterGaugeVisualProfileSO>();
            try
            {
                first.fallbackProfile = second; second.fallbackProfile = first;
                Assert.That(first.ResolveArtwork(), Is.Null);
                second.fallbackProfile = AssetDatabase.LoadAssetAtPath<CharacterGaugeVisualProfileSO>(Folder+"Neutral.asset");
                Assert.That(first.ResolveArtwork(), Is.Not.Null);
            }
            finally { UnityEngine.Object.DestroyImmediate(first); UnityEngine.Object.DestroyImmediate(second); }
        }

        [Test]
        public void 캐릭터_12종의_표시_연결과_HUD_참조가_유효하다()
        {
            string[] ids = AssetDatabase.FindAssets("t:CharacterGaugeVisualProfileSO", new[]{Folder.TrimEnd('/')});
            Assert.That(ids.Length, Is.EqualTo(13));
            int count = 0;
            foreach(string id in AssetDatabase.FindAssets("t:PlayerCharacterDefinitionSO"))
            {
                var definition = AssetDatabase.LoadAssetAtPath<PlayerCharacterDefinitionSO>(AssetDatabase.GUIDToAssetPath(id));
                if(definition.gaugeVisualProfile == null) continue;
                count++;
                Assert.That(definition.gaugeVisualProfile.ResolveArtwork(), Is.Not.Null, definition.name);
            }
            Assert.That(count, Is.EqualTo(12));
            var root=PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                foreach(var transform in root.GetComponentsInChildren<Transform>(true))
                    Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), Is.Zero);
                Assert.That(root.transform.Find("LevelPanel").gameObject.activeSelf, Is.False);
                Assert.That(root.transform.Find("SkillPanel").gameObject.activeSelf, Is.False);
                Assert.That(root.GetComponentInChildren<UICharacterGauge>(true), Is.Not.Null);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        [UnityTest]
        public IEnumerator 교체_제한_잔상_일시정지_렌더링을_PlayMode에서_검증한다()
        {
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
            yield return new EnterPlayMode();
            var canvasObject = new GameObject("GaugeVerification",typeof(RectTransform),typeof(Canvas));
            var canvas=canvasObject.GetComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceCamera;
            var cameraObject = new GameObject("GaugeCamera",typeof(UnityEngine.Camera));
            var camera=cameraObject.GetComponent<UnityEngine.Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.07f,.1f,.08f,1f);
            camera.orthographic=true; camera.transform.position=new Vector3(0,0,-10);
            canvas.worldCamera=camera; canvas.planeDistance=1f;
            var texture=new RenderTexture(1280,720,24);
            camera.targetTexture=texture;
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            var template=prefab.transform.Find("CharacterGauge").gameObject;
            var first=UnityEngine.Object.Instantiate(template,canvas.transform).GetComponent<UICharacterGauge>();
            var second=UnityEngine.Object.Instantiate(template,canvas.transform).GetComponent<UICharacterGauge>();
            var neutral=AssetDatabase.LoadAssetAtPath<CharacterGaugeVisualProfileSO>(Folder+"Neutral.asset");
            try
            {
                second.Bind(neutral,reduceMotion:true);
                var other=second.GetComponentInChildren<RawImage>().material;
                float[] amounts={0f,30f,60f,100f};
                var conditions=new[]{AbilityActivationResult.Success,AbilityActivationResult.CooldownActive,AbilityActivationResult.BlockedByTag};
                foreach(string id in AssetDatabase.FindAssets("t:CharacterGaugeVisualProfileSO",new[]{Folder.TrimEnd('/')}))
                {
                    var profile=AssetDatabase.LoadAssetAtPath<CharacterGaugeVisualProfileSO>(AssetDatabase.GUIDToAssetPath(id));
                    if(profile==neutral) continue;
                    first.Bind(profile,reduceMotion:true);
                    foreach(float amount in amounts)
                    foreach(var condition in conditions)
                    {
                        first.SetResource(amount,100f,immediate:true);
                        bool ready=amount==100f && condition==AbilityActivationResult.Success;
                        var reason=condition==AbilityActivationResult.Success && !ready ? AbilityActivationResult.InsufficientResource : condition;
                        first.SetAbilityState(true,State(ready,reason));
                        Assert.That(first.transform.Find("Ready").gameObject.activeSelf,Is.EqualTo(ready));
                        Assert.That(first.transform.Find("Locked").gameObject.activeSelf,Is.EqualTo(condition!=AbilityActivationResult.Success));
                        Assert.That(first.GetComponentInChildren<RawImage>().material.GetFloat("_Resource"),Is.EqualTo(amount/100f).Within(.001f));
                    }
                }
                Assert.That(other.GetFloat("_Resource"),Is.Zero,"다른 HUD의 공유 머티리얼 오염");
                first.Bind(neutral,reduceMotion:false);
                Time.timeScale=0f;
                first.SetResource(100,100,immediate:true);
                first.SetResource(20,100);
                first.SetResource(60,100);
                float deadline=Time.realtimeSinceStartup+5f;
                while(Mathf.Abs(first.GetComponentInChildren<RawImage>().material.GetFloat("_Trail")-.6f)>.01f
                    && Time.realtimeSinceStartup<deadline) yield return null;
                Assert.That(first.GetComponentInChildren<RawImage>().material.GetFloat("_Trail"),Is.EqualTo(.6f).Within(.01f));
                first.SetResource(0,0,immediate:true);
                first.SetAbilityState(false,default);
                Assert.That(first.transform.Find("Ready").gameObject.activeSelf,Is.False);
                first.SetResource(100,100); first.gameObject.SetActive(false);
                yield return new WaitForSecondsRealtime(.4f);
                Assert.That(first.GetComponentInChildren<RawImage>(true).material.GetFloat("_Trail"),Is.Zero);
                first.gameObject.SetActive(true); first.Bind(neutral,reduceMotion:true);
                UnityEngine.Object.Destroy(second.gameObject);
                for(int row=0;row<3;row++)
                for(int column=0;column<4;column++)
                {
                    var gauge=UnityEngine.Object.Instantiate(template,canvas.transform).GetComponent<UICharacterGauge>();
                    gauge.Bind(neutral,reduceMotion:true);
                    ((RectTransform)gauge.transform).anchoredPosition=new Vector2(-450+column*300,220-row*220);
                    gauge.SetResource(amounts[column],100,immediate:true);
                    bool ready=row==0 && column==3;
                    gauge.SetAbilityState(true,State(ready,row==0 ? (ready ? AbilityActivationResult.Success:AbilityActivationResult.InsufficientResource) : conditions[row]));
                }
                first.gameObject.SetActive(false);
                yield return null;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active=texture;
                var capture=new Texture2D(texture.width,texture.height,TextureFormat.RGB24,false);
                capture.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0); capture.Apply();
                Directory.CreateDirectory("Logs/CharacterGauge");
                File.WriteAllBytes("Logs/CharacterGauge/States.png",capture.EncodeToPNG());
                UnityEngine.Object.Destroy(capture); RenderTexture.active=null;
                foreach(Transform child in canvas.transform) child.gameObject.SetActive(false);
                first.gameObject.SetActive(true); first.Bind(neutral,reduceMotion:true);
                first.SetResource(60,100,immediate:true);
                ((RectTransform)first.transform).anchoredPosition=Vector2.zero;
                yield return null;
                int fullPixels=CountVisiblePixels(camera,texture);
                Assert.That(fullPixels,Is.GreaterThan(100));
                first.GetComponent<CanvasGroup>().alpha=0f;
                Assert.That(CountVisiblePixels(camera,texture),Is.Zero,"CanvasGroup 알파");
                first.GetComponent<CanvasGroup>().alpha=1f;
                var clipObject=new GameObject("Clip",typeof(RectTransform),typeof(RectMask2D));
                clipObject.transform.SetParent(canvas.transform,false);
                ((RectTransform)clipObject.transform).sizeDelta=new Vector2(80f,80f);
                first.transform.SetParent(clipObject.transform,false);
                yield return null;
                int clippedPixels=CountVisiblePixels(camera,texture);
                Assert.That(clippedPixels,Is.GreaterThan(0).And.LessThan(fullPixels),"RectMask2D 클리핑");
                UnityEngine.Object.Destroy(clipObject.GetComponent<RectMask2D>());
                var maskImage=clipObject.AddComponent<Image>(); maskImage.color=Color.white;
                var stencil=clipObject.AddComponent<Mask>(); stencil.showMaskGraphic=false;
                yield return null;
                Assert.That(CountVisiblePixels(camera,texture),Is.GreaterThan(0).And.LessThan(fullPixels),"Mask 스텐실 클리핑");
                first.Bind(AssetDatabase.LoadAssetAtPath<CharacterGaugeVisualProfileSO>(Folder+"CharacterGauge_Hwarin.asset"),reduceMotion:true);
                first.SetResource(100,100,immediate:true); first.Tick(1f,true);
                Assert.That(first.GetComponentInChildren<RawImage>().materialForRendering.GetColor("_EnergyColor").r,
                    Is.GreaterThan(.9f),"마스크 아래 캐릭터 교체 색상");
                foreach(Transform child in canvas.transform) child.gameObject.SetActive(false);
                var previewRoot=new GameObject("HudPreview",typeof(RectTransform));
                previewRoot.transform.SetParent(canvas.transform,false);
                var previewRect=(RectTransform)previewRoot.transform;
                previewRect.anchorMin=Vector2.zero; previewRect.anchorMax=Vector2.one;
                previewRect.offsetMin=previewRect.offsetMax=Vector2.zero;
                previewRoot.SetActive(false);
                var playerPreview=CreateStaticHud(Prefab,previewRoot.transform);
                CreateStaticHud("Assets/03.Prefabs/UI/HUD/Skill/UI_HUD_Skill.prefab",previewRoot.transform);
                previewRoot.SetActive(true);
                var previewGauge=playerPreview.GetComponentInChildren<UICharacterGauge>();
                previewGauge.Bind(neutral,reduceMotion:true); previewGauge.SetResource(60,100,immediate:true);
                previewGauge.SetAbilityState(true,State(false,AbilityActivationResult.InsufficientResource));
                camera.backgroundColor=new Color(.35f,.32f,.2f,1f);
                yield return null;
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active=texture;
                var hudCapture=new Texture2D(texture.width,texture.height,TextureFormat.RGB24,false);
                hudCapture.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0); hudCapture.Apply();
                File.WriteAllBytes("Logs/CharacterGauge/HudLayout.png",hudCapture.EncodeToPNG());
                UnityEngine.Object.Destroy(hudCapture); RenderTexture.active=null;

            }
            finally
            {
                Time.timeScale=1f;
                UnityEngine.Object.Destroy(canvasObject); UnityEngine.Object.Destroy(cameraObject);
                texture.Release(); UnityEngine.Object.Destroy(texture);
            }
            yield return null;
            yield return new ExitPlayMode();
        }

        private static GameObject CreateStaticHud(string path,Transform parent)
        {
            var clone=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),parent);
            foreach(var slot in clone.GetComponentsInChildren<UPlayGround.UI.InputPrompt.UISkillSlot>(true))
                UnityEngine.Object.DestroyImmediate(slot);
            foreach(var prompt in clone.GetComponentsInChildren<UPlayGround.UI.InputPrompt.UIInputPromptIcon>(true))
                UnityEngine.Object.DestroyImmediate(prompt);
            foreach(var hud in clone.GetComponentsInChildren<UI_Base>(true))
            {
                var data=new SerializedObject(hud);
                var slots=data.FindProperty("_slots");
                if(slots!=null) { slots.arraySize=0; data.ApplyModifiedPropertiesWithoutUndo(); }
                UnityEngine.Object.DestroyImmediate(hud);
            }
            return clone;
        }

        private static int CountVisiblePixels(UnityEngine.Camera camera,RenderTexture target)
        {
            Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active=target;
            var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0); pixels.Apply();
            Color32[] colors=pixels.GetPixels32(); Color32 background=colors[0]; int count=0;
            foreach(var color in colors)
                if(System.Math.Abs(color.r-background.r)+System.Math.Abs(color.g-background.g)+System.Math.Abs(color.b-background.b)>10) count++;
            UnityEngine.Object.Destroy(pixels); RenderTexture.active=null; return count;
        }

        private static AbilitySlotViewState State(bool ready,AbilityActivationResult reason) =>
            new AbilitySlotViewState("test",true,true,ready,reason,100f,100f,
                reason==AbilityActivationResult.CooldownActive ? 2.5f : 0f,5f,string.Empty);
    }
}
