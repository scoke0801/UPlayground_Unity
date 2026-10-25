// 실제 월드에서 두 캐릭터의 얼굴을 겹쳐 스텐실 충돌을 재현한다. 모든 변경은 런타임 사본이다.
if (!EditorApplication.isPlaying || !EditorApplication.isPaused) throw new Exception("일시정지한 Play Mode가 필요합니다.");
var root = GameObject.Find("Npc_Daon");
var player = UnityEngine.Object.FindFirstObjectByType<UPlayGround.PlayerActor>();
var camera = Camera.main;
var face = root.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r => r.name.StartsWith("Female_Face_") && r.gameObject.activeInHierarchy);
var hair = root.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r => r.name.StartsWith("Hair_") && r.gameObject.activeInHierarchy);
var original = face.sharedMaterials;
var material = new Material(original[0]);
var fake = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/ExternalAssets/Character/ROKO SHOP/Bokusei/03_Materials/Mat_Character/Mat_Bokusei_Hair_FakeShadow.mat"));
var shadowObject = new GameObject("P09_FakeShadow_OverlapTrial");
shadowObject.transform.SetParent(hair.transform, false);
var shadow = shadowObject.AddComponent<SkinnedMeshRenderer>();
shadow.sharedMesh = hair.sharedMesh;
shadow.bones = hair.bones;
shadow.rootBone = hair.rootBone;
shadow.localBounds = hair.localBounds;
shadow.updateWhenOffscreen = true;
shadow.sharedMaterial = fake;
shadow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
var position = player.transform.position;
var rotation = player.transform.rotation;
var rt = new RenderTexture(1024,1280,24);
var previousTarget = camera.targetTexture;
var previousActive = RenderTexture.active;
var report = new List<string>();
System.IO.Directory.CreateDirectory("output/P09ToonLook/WorldFake");
try
{
    camera.targetTexture = rt;
    var slots = (Material[])original.Clone(); slots[0] = material; face.sharedMaterials = slots;
    foreach (bool overlap in new[] { false, true })
    {
        if (overlap)
        {
            player.transform.rotation = root.transform.rotation;
            var playerHead = player.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Head);
            var npcHead = root.GetComponentsInChildren<Transform>().First(t => t.name == "Head");
            player.transform.position += npcHead.position - playerHead.position + camera.transform.right * .2f + camera.transform.forward * .08f;
        }
        foreach (string variant in new[] { "Off51", "Off52", "Bokusei51", "Bokusei52", "Silhouette51", "Silhouette52" })
        {
            int stencil = variant.EndsWith("52") ? 52 : 51;
            shadow.enabled = !variant.StartsWith("Off");
            material.SetFloat("_StencilRef", stencil);
            material.SetFloat("_StencilPass", 2);
            fake.SetFloat("_StencilRef", stencil);
            var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/ExternalAssets/Character/ROKO SHOP/Bokusei/03_Materials/Mat_Character/Mat_Bokusei_Hair_FakeShadow.mat");
            fake.SetTexture("_MainTex", variant.StartsWith("Silhouette") ? Texture2D.whiteTexture : source.GetTexture("_MainTex"));
            fake.SetColor("_Color", variant.StartsWith("Silhouette") ? new Color(.6f,.5f,.5f,1) : Color.white);
            camera.Render(); camera.Render(); RenderTexture.active = rt;
            var image = new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
            try
            {
                image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0); image.Apply();
                string path = "output/P09ToonLook/WorldFake/" + (overlap ? "Overlap_" : "Solo_") + variant + ".png";
                System.IO.File.WriteAllBytes(path,image.EncodeToPNG()); report.Add(path);
            }
            finally { UnityEngine.Object.DestroyImmediate(image); }
        }
    }
}
finally
{
    player.transform.SetPositionAndRotation(position,rotation);
    face.sharedMaterials = original;
    camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
    UnityEngine.Object.DestroyImmediate(shadowObject); UnityEngine.Object.DestroyImmediate(material);
    UnityEngine.Object.DestroyImmediate(fake); UnityEngine.Object.DestroyImmediate(rt);
}
System.IO.File.WriteAllLines("output/P09ToonLook/WorldFake/captures.txt",report.ToArray());
return report;

