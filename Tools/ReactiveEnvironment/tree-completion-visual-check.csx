// 실제 본편 복제본의 중앙 수명 단계와 원래 재질/슬롯을 짧은 GPU 캡처로 대조한다. 본편 씬은 변경하지 않는다.
if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return "BUSY";
var original = UnityEngine.Object.FindObjectsByType<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(t => t.Definition != null && AssetDatabase.GetAssetPath(t.Definition) == "Assets/10.Datas/Environment/TreeDestruction/Tree_A.asset");
var fixture = new UPlayGround.EditorTools.Tests.ReactiveEnvironmentIntegrationTests(); fixture.SetUp();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
System.Func<object, string, object[], object> invoke = (o, n, a) => o.GetType().GetMethod(n, flags).Invoke(o, a);
var rt = RenderTexture.GetTemporary(960, 540, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
var texture = new Texture2D(960, 540, TextureFormat.RGBA32, false, true);
var previous = RenderTexture.active; bool async = ShaderUtil.allowAsyncCompilation;
string[] globals = { "_MainLightPosition", "_MainLightColor", "unity_LightData", "_WorldSpaceCameraPos", "unity_FogParams", "unity_SHAr", "unity_SHAg", "unity_SHAb", "unity_SHBr", "unity_SHBg", "unity_SHBb", "unity_SHC", "unity_LODFade" };
var saved = globals.Select(Shader.GetGlobalVector).ToArray();
var results = new System.Collections.Generic.List<object>();
string folder = "output/ReactiveEnvironment/TreeCompletion-20261115"; System.IO.Directory.CreateDirectory(folder);
try
{
    ShaderUtil.allowAsyncCompilation = false;
    var scene = (UnityEngine.SceneManagement.Scene)fixture.GetType().GetField("_scene", flags).GetValue(fixture);
    var world = (UPlayGround.Manager.WorldStateManager)fixture.GetType().GetField("_world", flags).GetValue(fixture);
    var baseTarget = (UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject)fixture.GetType().GetField("_target", flags).GetValue(fixture);
    var root = UnityEngine.Object.Instantiate(original.gameObject); root.SetActive(false);
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
    root.transform.SetPositionAndRotation(new Vector3(30000, 4000, 30000), Quaternion.identity); root.transform.localScale = Vector3.one * 0.5f;
    root.GetComponent<UPlayGround.Components.SceneEntityId>().RuntimeSetGuid("tree-completion-visual");
    var target = root.GetComponent<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject>();
    target.GetType().GetField("_weather", flags).SetValue(target, baseTarget.GetType().GetField("_weather", flags).GetValue(baseTarget));
    invoke(target, "Awake", new object[0]);
    if (!world.RegisterEnvironment(target)) throw new System.Exception("시각 복제본 등록 실패");
    root.SetActive(true);
    root.GetComponent<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentDestruction>().SetImpactDirection(Vector3.right);
    world.ApplyEnvironmentImpact(target, 123, 0, 100, 0, 0);
    for (int i = 0; i < 41; i++) invoke(world, "FixedTickEnvironment", new object[] { 0.05f });
    invoke(world, "FixedTickEnvironment", new object[] { 0.1f });
    var groups = root.GetComponentsInChildren<LODGroup>(true);
    var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
    var bounds = new Bounds(root.transform.position, Vector3.zero);
    foreach (var renderer in renderers) if (renderer.gameObject.activeInHierarchy) bounds.Encapsulate(renderer.bounds);
    var orientation = Quaternion.Euler(32, -25, 0);
    float extent = Mathf.Max(bounds.size.x / (960f / 540), bounds.size.y, bounds.size.z) * 0.65f;
    var eye = bounds.center - orientation * Vector3.forward * extent * 3;
    var view = Matrix4x4.Scale(new Vector3(1, 1, -1)) * Matrix4x4.TRS(eye, orientation, Vector3.one).inverse;
    var projection = GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(-extent * 960 / 540, extent * 960 / 540, -extent, extent, 0.1f, extent * 8), true);
    foreach (string global in globals) Shader.SetGlobalVector(global, Vector4.zero);
    Shader.SetGlobalVector("_MainLightPosition", new Vector4(-0.4f, 0.8f, -0.3f, 0)); Shader.SetGlobalVector("_MainLightColor", Vector4.one);
    Shader.SetGlobalVector("unity_LightData", new Vector4(0, 0, 1, 0)); Shader.SetGlobalVector("_WorldSpaceCameraPos", new Vector4(eye.x, eye.y, eye.z, 1));
    Shader.SetGlobalVector("unity_FogParams", new Vector4(0, 0, 0, 1)); Shader.SetGlobalVector("unity_SHAr", new Vector4(0, 0, 0, 0.45f)); Shader.SetGlobalVector("unity_SHAg", new Vector4(0, 0, 0, 0.45f)); Shader.SetGlobalVector("unity_SHAb", new Vector4(0, 0, 0, 0.45f));
    for (int phase = 0; phase < 3; phase++)
    {
        if (phase > 0) invoke(world, "FixedTickEnvironment", new object[] { 0.225f });
        for (int lod = 0; lod < 3; lod++)
        {
            var excluded = new System.Collections.Generic.HashSet<Renderer>();
            foreach (var group in groups)
            {
                var levels = group.GetLODs();
                for (int level = 0; level < levels.Length; level++)
                    if (level != Mathf.Min(lod, levels.Length - 1)) foreach (var renderer in levels[level].renderers) excluded.Add(renderer);
            }
            var commands = new UnityEngine.Rendering.CommandBuffer();
            try
            {
                commands.SetRenderTarget(rt); commands.ClearRenderTarget(true, true, Color.clear); commands.SetViewProjectionMatrices(view, projection);
                foreach (var renderer in renderers)
                {
                    if (!renderer.enabled || !renderer.gameObject.activeInHierarchy || excluded.Contains(renderer)) continue;
                    var mesh = renderer.GetComponent<MeshFilter>().sharedMesh; var materials = renderer.sharedMaterials;
                    for (int slot = 0; slot < materials.Length && slot < mesh.subMeshCount; slot++)
                    {
                        var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block, slot);
                        if (block.isEmpty) renderer.GetPropertyBlock(block);
                        commands.DrawMesh(mesh, renderer.localToWorldMatrix, materials[slot], slot, 0, block);
                    }
                }
                Graphics.ExecuteCommandBuffer(commands); RenderTexture.active = rt;
                texture.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); texture.Apply();
                int pixels = texture.GetPixels().Count(c => c.a > 0.01f);
                string path = folder + "/fade-" + phase + "-lod" + lod + ".png";
                System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
                var pose = (UPlayGround.Data.Save.EnvironmentDestructionSaveState)target.GetType().GetField("_appliedDestruction", flags).GetValue(target);
                var sample = new MaterialPropertyBlock(); renderers.First(r => r.transform.IsChildOf(root.transform.Find("Remnant"))).GetPropertyBlock(sample, 0);
                results.Add(new { phase = phase, lod = lod, pixels = pixels, path = path, lifetime = pose.completion.phase.ToString(), elapsed = pose.completion.elapsed, shaderClearing = sample.GetVector("_EnvState1").z });
                if (phase < 2 && pixels == 0 || phase == 2 && pixels > 0) throw new System.Exception("수명 소거 GPU 결과 오류: " + path);
            }
            finally { commands.Release(); }
        }
    }
}
finally
{
    for (int i = 0; i < globals.Length; i++) Shader.SetGlobalVector(globals[i], saved[i]);
    ShaderUtil.allowAsyncCompilation = async; RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.DestroyImmediate(texture); fixture.TearDown();
}
var serialize = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject", new[] { typeof(object) });
System.IO.File.WriteAllText(folder + "/visual-result.json", (string)serialize.Invoke(null, new object[] { results }));
return results;
