// 동일 메시의 일반·인스턴싱 출력과 공유 버퍼 변형을 비교한다. 씬과 원본 재질은 변경하지 않는다.
if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return "BUSY";
var renderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
    .Where(r => r.GetComponent<MeshFilter>() != null && r.sharedMaterial != null && r.sharedMaterial.shader.name.StartsWith("UPlayGround/Environment/Vegetation/"))
    .GroupBy(r => r.sharedMaterial.shader.name).Select(g => g.First()).ToArray();
var rt = RenderTexture.GetTemporary(256, 256, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
var texture = new Texture2D(256, 256, TextureFormat.RGBA32, false, true);
var previous = RenderTexture.active; bool async = ShaderUtil.allowAsyncCompilation;
string keyword = "_ENVIRONMENT_REACTION_BUFFER"; bool enabled = Shader.IsKeywordEnabled(keyword);
var buffer = new ComputeBuffer(64, 32);
var values = new Vector4[128]; for(int i=0;i<64;i++) values[i*2+1] = new Vector4(0,1,0,0);
buffer.SetData(values);
string[] globals = { "_MainLightPosition", "_MainLightColor", "unity_LightData", "_WorldSpaceCameraPos", "unity_FogParams", "unity_SHAr", "unity_SHAg", "unity_SHAb", "unity_SHBr", "unity_SHBg", "unity_SHBb", "unity_SHC", "unity_LODFade" };
var saved = globals.Select(Shader.GetGlobalVector).ToArray();
string[] fogKeywords = { "FOG_LINEAR", "FOG_EXP", "FOG_EXP2" };
var fogEnabled = fogKeywords.Select(Shader.IsKeywordEnabled).ToArray();
var rows = new System.Collections.Generic.List<object>();
try
{
    ShaderUtil.allowAsyncCompilation = false;
    foreach(var fogKeyword in fogKeywords) Shader.DisableKeyword(fogKeyword);
    Shader.SetGlobalBuffer("_EnvironmentReactionStates", buffer);
    foreach (string global in globals) Shader.SetGlobalVector(global, Vector4.zero);
    Shader.SetGlobalVector("_MainLightPosition", new Vector4(-0.4f, 0.8f, -0.3f, 0)); Shader.SetGlobalVector("_MainLightColor", Vector4.one);
    Shader.SetGlobalVector("unity_LightData", new Vector4(0, 0, 1, 0));
    Shader.SetGlobalVector("unity_SHAr", new Vector4(0,0,0,0.45f)); Shader.SetGlobalVector("unity_SHAg", new Vector4(0,0,0,0.45f)); Shader.SetGlobalVector("unity_SHAb", new Vector4(0,0,0,0.45f));
    foreach (var renderer in renderers)
    {
        var mesh = renderer.GetComponent<MeshFilter>().sharedMesh; if(mesh == null) continue;
        var material = new Material(renderer.sharedMaterial); material.enableInstancing = true;
        try
        {
            var means = new double[4]; var counts = new int[4];
            var center = mesh.bounds.center; float size = Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.y, mesh.bounds.size.z) * 0.8f;
            var eye = center + new Vector3(0,0,size*3);
            var view = Matrix4x4.Scale(new Vector3(1,1,-1))*Matrix4x4.TRS(eye,Quaternion.Euler(0,180,0),Vector3.one).inverse;
            var projection = GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(-size,size,-size,size,0.01f,size*8),true);
            Shader.SetGlobalVector("_WorldSpaceCameraPos", new Vector4(eye.x,eye.y,eye.z,1));
            for (int mode=0;mode<4;mode++)
            {
                if(mode%2==0) Shader.DisableKeyword(keyword); else Shader.EnableKeyword(keyword);
                var block = new MaterialPropertyBlock(); block.SetFloat("_EnvironmentReactionIndex",0);
                var probe = new UnityEngine.Rendering.SphericalHarmonicsL2(); probe.AddAmbientLight(new Color(0.45f,0.45f,0.45f));
                block.CopySHCoefficientArraysFrom(new[]{probe});
                block.SetVector("unity_LightData",new Vector4(0,0,1,0));
                bool passReady = material.SetPass(0);
                var cmd = new UnityEngine.Rendering.CommandBuffer();
                try
                {
                    cmd.SetRenderTarget(rt);cmd.ClearRenderTarget(true,true,Color.clear);cmd.SetViewProjectionMatrices(view,projection);
                    if(mode>=2) cmd.DrawMeshInstanced(mesh,0,material,0,new[]{Matrix4x4.identity},1,block);
                    else cmd.DrawMesh(mesh,Matrix4x4.identity,material,0,0,block);
                    Graphics.ExecuteCommandBuffer(cmd);
                    RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,256,256),0,0);texture.Apply();
                    var pixels=texture.GetPixels();int count=0;double luminance=0;
                    foreach(var pixel in pixels)if(pixel.a>0.5f){count++;luminance+=(pixel.r+pixel.g+pixel.b)/3;}
                    means[mode]=count>0?luminance/count:0; counts[mode]=count;
                    if(!passReady || count==0 || means[mode]<=0) throw new System.Exception("식생 출력 실패: "+material.shader.name+" / "+mode);
                    rows.Add(new{shader=material.shader.name,mode,passReady,pixels=count,mean=means[mode]});
                }
                finally{cmd.Release();}
            }
            if(counts[0]!=counts[1] || counts[2]!=counts[3] || System.Math.Abs(means[0]-means[1])>0.00001 || System.Math.Abs(means[2]-means[3])>0.00001)
                throw new System.Exception("공유 버퍼의 정상 상태가 원형 출력을 변경함: "+material.shader.name);
        }
        finally{UnityEngine.Object.DestroyImmediate(material);}
    }
}
finally
{
    if(enabled) Shader.EnableKeyword(keyword);else Shader.DisableKeyword(keyword);
    Shader.SetGlobalBuffer("_EnvironmentReactionStates",(ComputeBuffer)null);buffer.Dispose();
    for(int i=0;i<globals.Length;i++)Shader.SetGlobalVector(globals[i],saved[i]);
    for(int i=0;i<fogKeywords.Length;i++)if(fogEnabled[i])Shader.EnableKeyword(fogKeywords[i]);
    ShaderUtil.allowAsyncCompilation=async;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(texture);
}
return rows;
