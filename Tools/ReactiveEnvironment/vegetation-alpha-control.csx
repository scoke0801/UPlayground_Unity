var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.ReactiveEnvironment.VegetationReactionCatalogSO>("Assets/10.Datas/Environment/Vegetation/Catalog.asset");
if (catalog == null) throw new System.InvalidOperationException("전환 대응표가 필요합니다.");
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var renderers = new System.Collections.Generic.List<UnityEngine.MeshRenderer>();
foreach (var root in scene.GetRootGameObjects()) renderers.AddRange(root.GetComponentsInChildren<UnityEngine.MeshRenderer>(true));
var target = UnityEngine.RenderTexture.GetTemporary(64, 64, 24, UnityEngine.RenderTextureFormat.ARGB32, UnityEngine.RenderTextureReadWrite.Linear);
var readback = new UnityEngine.Texture2D(64, 64, UnityEngine.TextureFormat.RGBA32, false, true);
var previous = UnityEngine.RenderTexture.active;
bool asynchronous = UnityEditor.ShaderUtil.allowAsyncCompilation;
string[] globals = { "_MainLightPosition", "_MainLightColor", "unity_LightData", "_WorldSpaceCameraPos", "unity_FogParams", "unity_SHAr", "unity_SHAg", "unity_SHAb", "unity_SHBr", "unity_SHBg", "unity_SHBb", "unity_SHC", "unity_LODFade" };
var savedGlobals = new UnityEngine.Vector4[globals.Length];
for (int i = 0; i < globals.Length; i++) savedGlobals[i] = UnityEngine.Shader.GetGlobalVector(globals[i]);
var report = new System.Text.StringBuilder();
int checkedCount = 0, empty = 0, failed = 0, wetChanged = 0, burnChanged = 0;
try
{
    UnityEditor.ShaderUtil.allowAsyncCompilation = false;
    foreach (var surface in catalog.surfaces)
    {
        if (surface.original.name != "MI_Farm_LeavesAtlas_01" && surface.original.name != "MI_Farm_FoliageLOD2_03") continue;
        System.IO.File.WriteAllText("output/ReactiveEnvironment/VegetationConversion-20261114/gpu-alpha-progress.txt", surface.original.name);
        UnityEngine.Mesh mesh = null;
        UnityEngine.MeshRenderer sampleRenderer = null;
        int slot = -1;
        foreach (var renderer in renderers)
        {
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
                if (materials[i] == surface.original || materials[i] == surface.reactive)
                {
                    var filter = renderer.GetComponent<UnityEngine.MeshFilter>();
                    if (filter != null && filter.sharedMesh != null && i < filter.sharedMesh.subMeshCount) { mesh = filter.sharedMesh; sampleRenderer = renderer; slot = i; break; }
                }
            if (mesh != null) break;
        }
        if (mesh == null)
        {
            foreach (var root in scene.GetRootGameObjects())
            foreach (var terrain in root.GetComponentsInChildren<UnityEngine.Terrain>(true))
            foreach (var prototype in terrain.terrainData.detailPrototypes)
            {
                if (prototype.prototype == null) continue;
                var renderer = prototype.prototype.GetComponent<UnityEngine.MeshRenderer>();
                var filter = prototype.prototype.GetComponent<UnityEngine.MeshFilter>();
                if (renderer == null || filter == null || filter.sharedMesh == null) continue;
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                    if (materials[i] == surface.original && i < filter.sharedMesh.subMeshCount) { mesh = filter.sharedMesh; sampleRenderer = renderer; slot = i; break; }
            }
        }
        if (mesh == null) { report.AppendLine("메시 누락: " + surface.original.name); continue; }
        var controlMesh = new UnityEngine.Mesh();
        controlMesh.vertices = new UnityEngine.Vector3[] { new UnityEngine.Vector3(-0.5f,0,0), new UnityEngine.Vector3(0.5f,0,0), new UnityEngine.Vector3(0.5f,1,0), new UnityEngine.Vector3(-0.5f,1,0) };
        controlMesh.triangles = new int[] {0,2,1,0,3,2,0,1,2,0,2,3};
        controlMesh.uv = new UnityEngine.Vector2[] {new UnityEngine.Vector2(0,0),new UnityEngine.Vector2(1,0),new UnityEngine.Vector2(1,1),new UnityEngine.Vector2(0,1)};
        controlMesh.colors = new UnityEngine.Color[] {UnityEngine.Color.white,UnityEngine.Color.white,UnityEngine.Color.white,UnityEngine.Color.white};
        controlMesh.RecalculateNormals(); controlMesh.RecalculateBounds();
        mesh = controlMesh; slot = 0; sampleRenderer = null;
        var original = new UnityEngine.Material(surface.original);
        var reactive = new UnityEngine.Material(surface.reactive);
        foreach (var material in new[] { original, reactive })
        {
            foreach (string property in new[] {"_Dither_Fade","_Wind_Large_Intensity","_Wind_Small_Intensity","_UV_Pivot_Lock_Strangth"})
                if (material.HasProperty(property)) material.SetFloat(property,0);
            if (material.HasProperty("_Alpha_Clip")) material.SetFloat("_Alpha_Clip",1);
            if (material.HasProperty("_Foliage_Map")) material.SetTexture("_Foliage_Map", UnityEngine.Texture2D.whiteTexture);
        }
        UnityEngine.Color[][] pixels = new UnityEngine.Color[4][];
        try
        {
            float dimension = UnityEngine.Mathf.Max(0.001f, mesh.bounds.size.x, mesh.bounds.size.y, mesh.bounds.size.z);
            var orientation = UnityEngine.Quaternion.Euler(20,25,0);
            var cameraPosition = mesh.bounds.center - orientation * UnityEngine.Vector3.forward * (dimension * 3);
            var model = UnityEngine.Matrix4x4.identity;
            for (int phase = 0; phase < 4; phase++)
            {
                var material = phase == 0 ? original : reactive;
                if (phase > 0) material.SetVector("_EnvState0", phase == 3 ? new UnityEngine.Vector4(0,1,0,1) : phase == 2 ? new UnityEngine.Vector4(1,0,0,0) : UnityEngine.Vector4.zero);
                var block = new UnityEngine.MaterialPropertyBlock();
                if (sampleRenderer != null && !UnityEditor.EditorUtility.IsPersistent(sampleRenderer))
                {
                    sampleRenderer.GetPropertyBlock(block, slot);
                    if (block.isEmpty) sampleRenderer.GetPropertyBlock(block);
                }
                if (phase > 0) block.SetVector("_EnvState0", material.GetVector("_EnvState0"));
                int pass = material.FindPass("ForwardLit");
                if (pass < 0) pass = material.FindPass("Universal Forward");
                if (pass < 0) pass = material.FindPass("Forward");
                if (pass < 0) pass = material.FindPass("Unlit");
                if (pass < 0) pass = 0;
                var commands = new UnityEngine.Rendering.CommandBuffer();
                try
                {
                    commands.SetRenderTarget(target);
                    commands.ClearRenderTarget(true, true, UnityEngine.Color.clear);
                    var view = UnityEngine.Matrix4x4.Scale(new UnityEngine.Vector3(1,1,-1)) * UnityEngine.Matrix4x4.TRS(cameraPosition, orientation, UnityEngine.Vector3.one).inverse;
                    commands.SetViewProjectionMatrices(view, UnityEngine.GL.GetGPUProjectionMatrix(UnityEngine.Matrix4x4.Ortho(-dimension * 0.65f, dimension * 0.65f, -dimension * 0.65f, dimension * 0.65f, dimension * 0.01f, dimension * 10), true));
                    commands.SetGlobalVector("_MainLightPosition", new UnityEngine.Vector4(0.3f,0.8f,-0.6f,0));
                    commands.SetGlobalVector("_MainLightColor", UnityEngine.Vector4.one);
                    commands.SetGlobalVector("unity_LightData", new UnityEngine.Vector4(0,0,1,0));
                    commands.SetGlobalVector("_WorldSpaceCameraPos", new UnityEngine.Vector4(cameraPosition.x,cameraPosition.y,cameraPosition.z,1));
                    commands.SetGlobalVector("unity_FogParams", new UnityEngine.Vector4(0,0,0,1));
                    commands.SetGlobalVector("unity_SHAr", new UnityEngine.Vector4(0,0,0,0.25f));
                    commands.SetGlobalVector("unity_SHAg", new UnityEngine.Vector4(0,0,0,0.25f));
                    commands.SetGlobalVector("unity_SHAb", new UnityEngine.Vector4(0,0,0,0.25f));
                    for (int g = 8; g < globals.Length; g++) commands.SetGlobalVector(globals[g], UnityEngine.Vector4.zero);
                    commands.SetGlobalVector("unity_LODFade", UnityEngine.Vector4.one);
                    commands.DrawMesh(mesh, model, material, slot, pass, block);
                    UnityEngine.Graphics.ExecuteCommandBuffer(commands);
                    UnityEngine.RenderTexture.active = target;
                    readback.ReadPixels(new UnityEngine.Rect(0,0,64,64),0,0); readback.Apply();
                    pixels[phase] = readback.GetPixels();
                }
                finally { commands.Release(); }
            }
            int visible = 0; float dryDifference = 0, wetDifference = 0, burnDifference = 0;
            for (int i = 0; i < pixels[0].Length; i++)
            {
                if (pixels[0][i].a > 0.1f) visible++;
                var dry = pixels[0][i] - pixels[1][i]; var wet = pixels[1][i] - pixels[2][i];
                dryDifference = UnityEngine.Mathf.Max(dryDifference, UnityEngine.Mathf.Abs(dry.r), UnityEngine.Mathf.Abs(dry.g), UnityEngine.Mathf.Abs(dry.b), UnityEngine.Mathf.Abs(dry.a));
                wetDifference = UnityEngine.Mathf.Max(wetDifference, UnityEngine.Mathf.Abs(wet.r), UnityEngine.Mathf.Abs(wet.g), UnityEngine.Mathf.Abs(wet.b));
                var burn = pixels[1][i] - pixels[3][i];
                burnDifference = UnityEngine.Mathf.Max(burnDifference, UnityEngine.Mathf.Abs(burn.r), UnityEngine.Mathf.Abs(burn.g), UnityEngine.Mathf.Abs(burn.b));
            }
            if (visible == 0) empty++;
            else { checkedCount++; if (dryDifference > 0.008f) failed++; if (wetDifference > 0.008f) wetChanged++; if (burnDifference > 0.008f) burnChanged++; }
            report.AppendLine(surface.original.name + " visible=" + visible + " dryMax=" + dryDifference + " wetMax=" + wetDifference + " burnMax=" + burnDifference);
        }
        finally { UnityEngine.Object.DestroyImmediate(controlMesh); UnityEngine.Object.DestroyImmediate(original); UnityEngine.Object.DestroyImmediate(reactive); }
    }
}
finally
{
    UnityEditor.ShaderUtil.allowAsyncCompilation = asynchronous;
    for (int i = 0; i < globals.Length; i++) UnityEngine.Shader.SetGlobalVector(globals[i], savedGlobals[i]);
    UnityEngine.RenderTexture.active = previous;
    UnityEngine.RenderTexture.ReleaseTemporary(target);
    UnityEngine.Object.DestroyImmediate(readback);
}
report.AppendLine("checked=" + checkedCount + " empty=" + empty + " dryFailed=" + failed + " wetChanged=" + wetChanged + " burnChanged=" + burnChanged);
System.IO.File.WriteAllText("output/ReactiveEnvironment/VegetationConversion-20261114/material-alpha-control.txt", report.ToString());
return report.ToString();




