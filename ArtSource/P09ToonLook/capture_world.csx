// 격리 Play Mode에서 조명·카메라·포즈를 고정하고 런타임 사본만 비교한다.
if (!EditorApplication.isPlaying || !EditorApplication.isPaused) throw new Exception("Play Mode가 필요합니다.");
var root = GameObject.Find("Npc_Daon");
var camera = Camera.main;
var renderers = root.GetComponentsInChildren<Renderer>(true);
var originals = renderers.ToDictionary(r => r, r => r.sharedMaterials);
var face = renderers.Single(r => r.name.StartsWith("Female_Face_") && r.gameObject.activeInHierarchy);
var skin = face.sharedMaterials[0];
var diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/10.Datas/Actor/ScenarioResidents/ToonLook/Textures/P09_Female_Body_Diff_4K.png");
var lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.type == LightType.Directional).ToArray();
var intensities = lights.Select(l => l.intensity).ToArray();
var ambientSky = RenderSettings.ambientSkyColor;
var ambientEquator = RenderSettings.ambientEquatorColor;
var ambientGround = RenderSettings.ambientGroundColor;
var ambientProbe = RenderSettings.ambientProbe;
var report = new List<string>();
System.IO.Directory.CreateDirectory("output/P09ToonLook/WorldPlay");
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
        foreach (string variant in new[] { "Before", "After" })
        {
            var material = new Material(skin);
            if (variant == "Before") { material.SetFloat("_ShadowBorder", .3f); material.SetFloat("_ShadowBlur", .6f); material.SetFloat("_LightMaxLimit", 1f); material.SetColor("_ShadowColor", new Color(1,.856f,.825f,1)); var oldDiffuse=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ExternalAssets/Character/P09_Modular_Humanoid/Model_DATA/Textures/Female_Body/P09_Female_Body_Diff.png"); foreach(var prop in material.GetTexturePropertyNames()) if(material.GetTexture(prop)==diffuse) material.SetTexture(prop,oldDiffuse); }
            try
            {
                var faceMaterials = (Material[])originals[face].Clone();
                faceMaterials[0] = material;
                face.sharedMaterials = faceMaterials;
                camera.Render();
                camera.Render();
                RenderTexture.active = rt;
                var image = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                try
                {
                    image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    image.Apply();
                    string file = "output/P09ToonLook/WorldPlay/" + (dark ? "Dark_" : "World_") + variant + ".png";
                    System.IO.File.WriteAllBytes(file, image.EncodeToPNG());
                    report.Add(file + "|play=" + EditorApplication.isPlaying + "|camera=" + camera.transform.position + "|fov=" + camera.fieldOfView);
                }
                finally { UnityEngine.Object.DestroyImmediate(image); }
            }
            finally
            {
                face.sharedMaterials = originals[face];
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
System.IO.File.WriteAllLines("output/P09ToonLook/WorldPlay/captures.txt", report.ToArray());
return report;

