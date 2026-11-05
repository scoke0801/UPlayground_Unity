var preview = new UnityEditor.PreviewRenderUtility();
UnityEngine.Texture2D image = null;
UnityEngine.Material floorMaterial = null;
try
{
    preview.camera.orthographic = true;
    preview.camera.orthographicSize = 1.55f;
    preview.camera.nearClipPlane = 0.05f;
    preview.camera.farClipPlane = 30f;
    preview.camera.transform.position = new UnityEngine.Vector3(2.2f, 3f, -4f);
    preview.camera.transform.LookAt(new UnityEngine.Vector3(1.6f, 0f, 0f));
    preview.camera.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
    preview.camera.backgroundColor = new UnityEngine.Color(0.1f, 0.13f, 0.16f);
    var floor = UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube);
    floor.transform.position = new UnityEngine.Vector3(1.6f, -0.06f, 0f);
    floor.transform.localScale = new UnityEngine.Vector3(5f, 0.1f, 2.5f);
    floorMaterial = new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit"));
    floorMaterial.SetColor("_BaseColor", new UnityEngine.Color(0.22f, 0.28f, 0.3f));
    floor.GetComponent<UnityEngine.Renderer>().sharedMaterial = floorMaterial;
    preview.AddSingleGO(floor);
    string[] names = { "FootstepDust", "FootstepWaterSplash", "FootstepWaterRipple" };
    for (int i = 0; i < names.Length; i++)
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(
            "Assets/10.Datas/Actor/Footsteps/Effects/" + names[i] + ".prefab");
        var instance = UnityEngine.Object.Instantiate(prefab);
        instance.transform.position = new UnityEngine.Vector3(i * 1.6f, 0.02f, 0f);
        preview.AddSingleGO(instance);
        instance.GetComponent<UnityEngine.ParticleSystem>().Simulate(0.14f, true, true, true);
    }
    preview.lights[0].intensity = 1.3f;
    preview.lights[0].transform.rotation = UnityEngine.Quaternion.Euler(45f, 35f, 0f);
    preview.lights[1].intensity = 0.6f;
    preview.BeginStaticPreview(new UnityEngine.Rect(0, 0, 1200, 650));
    preview.camera.Render();
    image = preview.EndStaticPreview();
    System.IO.Directory.CreateDirectory("output/Footsteps");
    System.IO.File.WriteAllBytes("output/Footsteps/effects-preview.png", image.EncodeToPNG());
    return "output/Footsteps/effects-preview.png";
}
finally
{
    if (image != null) UnityEngine.Object.DestroyImmediate(image);
    if (floorMaterial != null) UnityEngine.Object.DestroyImmediate(floorMaterial);
    preview.Cleanup();
}
