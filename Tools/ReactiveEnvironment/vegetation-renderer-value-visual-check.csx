var runState = UnityEditor.SessionState.GetString("LakeDebug.RendererGpuProbe", "");
if(runState=="done") return System.IO.File.ReadAllText("output/LakeVillagePerformance/Debug/renderer-value-gpu-comparison.txt");
if(runState=="running") return "GPU 비교 진행 중";
if(runState=="failed") throw new System.InvalidOperationException("이전 GPU 비교 실패를 먼저 확인하세요.");
UnityEditor.SessionState.SetString("LakeDebug.RendererGpuProbe","running");
try
{
if(UnityEngine.Application.isPlaying) throw new System.InvalidOperationException("Play 종료 후 실행하세요.");
var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.ReactiveEnvironment.VegetationReactionCatalogSO>("Assets/10.Datas/Environment/Vegetation/Catalog.asset");
if (catalog == null) throw new System.InvalidOperationException("전환 대응표가 필요합니다.");
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var renderers = new System.Collections.Generic.List<UnityEngine.MeshRenderer>();
foreach (var root in scene.GetRootGameObjects()) renderers.AddRange(root.GetComponentsInChildren<UnityEngine.MeshRenderer>(true));
bool asynchronous = UnityEditor.ShaderUtil.allowAsyncCompilation;
bool srpBatching = UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching;
var bufferType = typeof(UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentSurface).Assembly.GetType("UPlayGround.Gameplay.World.ReactiveEnvironment.EnvironmentSurfaceStateBuffer");
var bufferFlags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
var bufferReset = bufferType.GetMethod("Reset", bufferFlags);
if((int)bufferType.GetField("_active",bufferFlags).GetValue(null)!=0) throw new System.InvalidOperationException("활성 표면 임대가 있습니다.");
bufferReset.Invoke(null, null);
bufferType.GetMethod("Register", bufferFlags).Invoke(null, new object[] { UnityEngine.Vector4.one, UnityEngine.Vector4.zero, 1f }); int bufferIndex = (int)bufferType.GetMethod("Register", bufferFlags).Invoke(null, new object[] { UnityEngine.Vector4.zero, UnityEngine.Vector4.zero, 1f });
var report = new System.Text.StringBuilder();
int checkedCount = 0, empty = 0, failed = 0, wetChanged = 0, burnChanged = 0;
try
{
    UnityEditor.ShaderUtil.allowAsyncCompilation = false;
    UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching = false;
    foreach (var surface in catalog.surfaces)
    {
        System.IO.File.WriteAllText("output/LakeVillagePerformance/Debug/renderer-value-gpu-progress.txt", surface.original.name);
        UnityEngine.Mesh mesh = null;
        int slot = -1;
        foreach (var renderer in renderers)
        {
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
                if (materials[i] == surface.original || materials[i] == surface.reactive)
                {
                    var filter = renderer.GetComponent<UnityEngine.MeshFilter>();
                    if (filter != null && filter.sharedMesh != null && i < filter.sharedMesh.subMeshCount) { mesh = filter.sharedMesh; slot = i; break; }
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
                    if (materials[i] == surface.original && i < filter.sharedMesh.subMeshCount) { mesh = filter.sharedMesh; slot = i; break; }
            }
        }
        if (mesh == null) { report.AppendLine("메시 누락: " + surface.original.name); continue; }
        var probeRoot = new UnityEngine.GameObject("RendererValueProbe", typeof(UnityEngine.MeshFilter), typeof(UnityEngine.MeshRenderer));
var preview = new UnityEditor.PreviewRenderUtility();
preview.AddSingleGO(probeRoot);
probeRoot.GetComponent<UnityEngine.MeshFilter>().sharedMesh = mesh;
var probe = probeRoot.GetComponent<UnityEngine.MeshRenderer>();
var original = new UnityEngine.Material(surface.original);
        var reactive = new UnityEngine.Material(surface.reactive);
        foreach(var stableMaterial in new[]{original,reactive})
        for(int property=0;property<stableMaterial.shader.GetPropertyCount();property++){
            string propertyName=stableMaterial.shader.GetPropertyName(property);
            string lower=propertyName.ToLowerInvariant();
            var propertyType=stableMaterial.shader.GetPropertyType(property);
            if(propertyType!=UnityEngine.Rendering.ShaderPropertyType.Float && propertyType!=UnityEngine.Rendering.ShaderPropertyType.Range)continue;
            if((lower.Contains("wind")||lower.Contains("sway")||lower.Contains("wiggle")) &&
                (lower.Contains("intensity")||lower.Contains("strength")||lower.Contains("speed")||lower.Contains("amplitude")||lower.Contains("multiplier")))
                stableMaterial.SetFloat(propertyName,0);
        }
        UnityEngine.Color[][] pixels = new UnityEngine.Color[7][];
        try
        {
            float dimension = UnityEngine.Mathf.Max(0.001f, mesh.bounds.size.x, mesh.bounds.size.y, mesh.bounds.size.z);
            var orientation = UnityEngine.Quaternion.Euler(20,25,0);
            var cameraPosition = mesh.bounds.center - orientation * UnityEngine.Vector3.forward * (dimension * 3);
preview.camera.transform.SetPositionAndRotation(cameraPosition, orientation);
preview.camera.orthographic = true; preview.camera.orthographicSize=dimension*0.65f;
preview.camera.nearClipPlane=dimension*0.01f; preview.camera.farClipPlane=dimension*10;
preview.camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;
preview.camera.backgroundColor=UnityEngine.Color.clear;
preview.ambientColor=new UnityEngine.Color(0.25f,0.25f,0.25f);
preview.lights[0].intensity=1; preview.lights[0].transform.rotation=UnityEngine.Quaternion.Euler(50,30,0);
preview.lights[1].intensity=0;
            for (int phase = 0; phase < 7; phase++)
            {
                int statePhase = phase >= 4 ? phase - 3 : phase; var material = phase == 0 ? original : reactive;
                if (phase > 0) material.SetVector("_EnvState0", statePhase == 3 ? new UnityEngine.Vector4(0,1,0,1) : statePhase == 2 ? new UnityEngine.Vector4(1,0,0,0) : UnityEngine.Vector4.zero);
                var block = new UnityEngine.MaterialPropertyBlock();

                if (phase > 0) block.SetVector("_EnvState0", material.GetVector("_EnvState0"));
                block.SetFloat("_EnvironmentReactionIndex", phase >= 4 ? bufferIndex : -1);
if (phase >= 4)
{
    bufferType.GetMethod("Set", bufferFlags).Invoke(null, new object[] { bufferIndex, material.GetVector("_EnvState0"), UnityEngine.Vector4.zero, 1f });
    bufferType.GetMethod("Flush", bufferFlags).Invoke(null, null);
    block.SetVector("_EnvState0", UnityEngine.Vector4.zero);
}
                    material.enableInstancing=true;
var probeMaterials = new UnityEngine.Material[mesh.subMeshCount];
for(int s=0;s<probeMaterials.Length;s++) probeMaterials[s]=material;
probe.sharedMaterials=probeMaterials;
probe.SetPropertyBlock(null);
for(int s=0;s<probeMaterials.Length;s++) probe.SetPropertyBlock(null,s);
material.SetFloat("_EnvironmentReactionIndex",-1);
probe.SetShaderUserValue(phase>=4 ? 0x80000000u | (uint)bufferIndex : 0);
if(phase<4) probe.SetPropertyBlock(block);
else material.SetVector("_EnvState0",UnityEngine.Vector4.zero);
                    preview.BeginStaticPreview(new UnityEngine.Rect(0,0,64,64));
preview.Render(true);
var image = preview.EndStaticPreview();
try { pixels[phase] = image.GetPixels(); }
finally { UnityEngine.Object.DestroyImmediate(image); }
            }
            float bufferDifference = 0;
for (int p = 1; p <= 3; p++)
for (int pixel = 0; pixel < pixels[p].Length; pixel++)
{
    var delta = pixels[p][pixel] - pixels[p + 3][pixel];
    bufferDifference = UnityEngine.Mathf.Max(bufferDifference, UnityEngine.Mathf.Abs(delta.r), UnityEngine.Mathf.Abs(delta.g), UnityEngine.Mathf.Abs(delta.b), UnityEngine.Mathf.Abs(delta.a));
}
if (bufferDifference > 0.008f)
{
for(int p=0;p<pixels.Length;p++){
var debugImage=new UnityEngine.Texture2D(64,64,UnityEngine.TextureFormat.RGBA32,false);
debugImage.SetPixels(pixels[p]);debugImage.Apply();
System.IO.File.WriteAllBytes("output/LakeVillagePerformance/Debug/native-phase-"+p+".png",debugImage.EncodeToPNG());
UnityEngine.Object.DestroyImmediate(debugImage);
}
throw new System.Exception(surface.original.name + " buffer difference=" + bufferDifference);
}
report.AppendLine(surface.original.name + " bufferMax=" + bufferDifference);
int visible = 0; float dryDifference = 0, wetDifference = 0, burnDifference = 0;
            for (int i = 0; i < pixels[0].Length; i++)
            {
                var backgroundDelta = pixels[0][i] - pixels[0][0];
                if (UnityEngine.Mathf.Max(UnityEngine.Mathf.Abs(backgroundDelta.r), UnityEngine.Mathf.Abs(backgroundDelta.g), UnityEngine.Mathf.Abs(backgroundDelta.b)) > 0.008f) visible++;
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
        finally { preview.Cleanup(); UnityEngine.Object.DestroyImmediate(probeRoot); UnityEngine.Object.DestroyImmediate(original); UnityEngine.Object.DestroyImmediate(reactive); }
    }
}
finally
{
    bufferReset.Invoke(null, null); UnityEditor.ShaderUtil.allowAsyncCompilation = asynchronous;
    UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching = srpBatching;
}
report.AppendLine("checked=" + checkedCount + " empty=" + empty + " dryFailed=" + failed + " wetChanged=" + wetChanged + " burnChanged=" + burnChanged);
System.IO.File.WriteAllText("output/LakeVillagePerformance/Debug/renderer-value-gpu-comparison.txt", report.ToString());
if(checkedCount<45 || checkedCount+empty!=catalog.surfaces.Count || failed!=0 || wetChanged<45 || burnChanged<45) throw new System.InvalidOperationException(report.ToString());
UnityEditor.SessionState.SetString("LakeDebug.RendererGpuProbe","done");
return report.ToString();


}
catch(System.Exception exception)
{
System.IO.File.WriteAllText("output/LakeVillagePerformance/Debug/renderer-value-gpu-failure.txt", exception.ToString());
UnityEditor.SessionState.SetString("LakeDebug.RendererGpuProbe","failed");
throw;
}

