// 격리 Play Mode에서 조명·카메라·포즈를 고정하고 런타임 사본만 비교한다.
if (!EditorApplication.isPlaying) throw new Exception("Play Mode가 필요합니다.");
var root = GameObject.Find("P09_Review_Daon");
var camera = GameObject.Find("P09_ReviewCamera").GetComponent<Camera>();
var renderers = root.GetComponentsInChildren<Renderer>(true);
var originals = renderers.ToDictionary(r => r, r => r.sharedMaterials);
var face = renderers.Single(r => r.name.StartsWith("Female_Face_") && r.gameObject.activeInHierarchy);
var hair = renderers.OfType<SkinnedMeshRenderer>().Single(r => r.name.StartsWith("Hair_") && r.gameObject.activeInHierarchy);
var skin = face.sharedMaterials[0];
var diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/10.Datas/Actor/ScenarioResidents/ToonLook/Textures/P09_Female_Body_Diff_4K.png");
var normal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/10.Datas/Actor/ScenarioResidents/ToonLook/Textures/P09_Female_Body_Normal_4K.png");
var lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.type == LightType.Directional).ToArray();
var intensities = lights.Select(l => l.intensity).ToArray();
var ambientSky = RenderSettings.ambientSkyColor;
var ambientEquator = RenderSettings.ambientEquatorColor;
var ambientGround = RenderSettings.ambientGroundColor;
var ambientProbe = RenderSettings.ambientProbe;
var report = new List<string>();
System.IO.Directory.CreateDirectory("output/P09ToonLook/FakeSmallOffset");
var rt = new RenderTexture(1024, 1280, 24, RenderTextureFormat.ARGB32);
var previousTarget = camera.targetTexture;
var previousActive = RenderTexture.active;
try
{
    camera.targetTexture = rt;
    foreach (bool dark in new[] { false, true })
    {
        for (int i = 0; i < lights.Length; i++) lights[i].intensity = intensities[i] * (dark ? 0.12f : 1f);
        RenderSettings.ambientSkyColor = ambientSky * (dark ? 0.18f : 1f);
        RenderSettings.ambientEquatorColor = ambientEquator * (dark ? 0.18f : 1f);
        RenderSettings.ambientGroundColor = ambientGround * (dark ? 0.18f : 1f);
        var probe = ambientProbe;
        for (int channel = 0; channel < 3; channel++) for (int coefficient = 0; coefficient < 9; coefficient++) probe[channel, coefficient] *= dark ? 0.18f : 1f;
        RenderSettings.ambientProbe = probe;
        foreach (string variant in new[] { "Baseline", "FakeBokuseiMask", "FakeP09Silhouette" })
        {
            var material = new Material(skin);
            Material fake = null;
            GameObject shadowObject = null;
            try
            {
                if (variant == "FaceShape") { material.SetFloat("_ShadowBorder", 0.45f); material.SetFloat("_ShadowBlur", 0.35f); }
                if (variant == "Diff4K" || variant == "Both4K")
                    foreach (string property in material.GetTexturePropertyNames())
                        if (material.GetTexture(property) == skin.GetTexture("_MainTex")) material.SetTexture(property, diffuse);
                if (variant == "Both4K") material.SetTexture("_BumpMap", normal);
                var faceMaterials = (Material[])originals[face].Clone();
                faceMaterials[0] = material;
                face.sharedMaterials = faceMaterials;
                if (variant.StartsWith("Fake"))
                {
                    material.SetFloat("_StencilRef", 51);
                    material.SetFloat("_StencilPass", 2);
                    fake = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/ExternalAssets/Character/ROKO SHOP/Bokusei/03_Materials/Mat_Character/Mat_Bokusei_Hair_FakeShadow.mat"));
                    fake.SetFloat("_ZWrite", 1);
                    fake.SetVector("_FakeShadowVector", new Vector4(-0.6f, 0.3f, 0, 0.0035f));
                    if (variant == "FakeP09Silhouette") { fake.SetTexture("_MainTex", Texture2D.whiteTexture); fake.SetColor("_Color", new Color(0.88f, 0.77f, 0.78f, 1)); }
                    shadowObject = new GameObject("P09_FakeShadow_Trial");
                    shadowObject.transform.SetParent(hair.transform, false);
                    var shadow = shadowObject.AddComponent<SkinnedMeshRenderer>();
                    shadow.sharedMesh = hair.sharedMesh;
                    shadow.bones = hair.bones;
                    shadow.rootBone = hair.rootBone;
                    shadow.localBounds = hair.localBounds;
                    shadow.updateWhenOffscreen = true;
                    shadow.sharedMaterial = fake;
                    shadow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                camera.Render();
                camera.Render();
                RenderTexture.active = rt;
                var image = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                try
                {
                    image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    image.Apply();
                    string file = "output/P09ToonLook/FakeSmallOffset/" + (dark ? "Dark_" : "World_") + variant + ".png";
                    System.IO.File.WriteAllBytes(file, image.EncodeToPNG());
                    report.Add(file + "|play=" + EditorApplication.isPlaying + "|camera=" + camera.transform.position + "|fov=" + camera.fieldOfView);
                }
                finally { UnityEngine.Object.DestroyImmediate(image); }
            }
            finally
            {
                face.sharedMaterials = originals[face];
                if (shadowObject != null) UnityEngine.Object.DestroyImmediate(shadowObject);
                if (fake != null) UnityEngine.Object.DestroyImmediate(fake);
                UnityEngine.Object.DestroyImmediate(material);
            }
        }
    }
}
finally
{
    for (int i = 0; i < lights.Length; i++) lights[i].intensity = intensities[i];
    RenderSettings.ambientSkyColor = ambientSky;
    RenderSettings.ambientEquatorColor = ambientEquator;
    RenderSettings.ambientGroundColor = ambientGround;
    RenderSettings.ambientProbe = ambientProbe;
    camera.targetTexture = previousTarget;
    RenderTexture.active = previousActive;
    UnityEngine.Object.DestroyImmediate(rt);
}
System.IO.File.WriteAllLines("output/P09ToonLook/FakeSmallOffset/captures.txt", report.ToArray());
return report;
