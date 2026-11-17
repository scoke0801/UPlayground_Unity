// 씬을 바꾸지 않고, 빌드에서 강제된 DBuffer 변형이 입력 없는 표면을 검게 만드는지 비교한다.
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.EditorApplication.isCompiling) return "BUSY";
var shader = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Shader>("Assets/06.Shaders/Environment/Vegetation/933532a4fcc9baf4fa0491de14d08ed7.shader");
var material = new UnityEngine.Material(shader);
var mesh = new UnityEngine.Mesh();
mesh.vertices = new[]{new UnityEngine.Vector3(-1,-1,0),new UnityEngine.Vector3(-1,1,0),new UnityEngine.Vector3(1,1,0),new UnityEngine.Vector3(1,-1,0)};
mesh.normals = Enumerable.Repeat(UnityEngine.Vector3.forward,4).ToArray();
mesh.uv = new[]{new UnityEngine.Vector2(0,0),new UnityEngine.Vector2(0,1),new UnityEngine.Vector2(1,1),new UnityEngine.Vector2(1,0)};
mesh.triangles = new[]{0,1,2,0,2,3};
var target = UnityEngine.RenderTexture.GetTemporary(128,128,24,UnityEngine.RenderTextureFormat.ARGB32,UnityEngine.RenderTextureReadWrite.Linear);
var pixels = new UnityEngine.Texture2D(128,128,UnityEngine.TextureFormat.RGBA32,false,true);
var unbound = new UnityEngine.Texture2D(1,1,UnityEngine.TextureFormat.RGBA32,false,true);
unbound.SetPixel(0,0,UnityEngine.Color.clear); unbound.Apply();
string[] keywords = {"_DBUFFER_MRT1","_DBUFFER_MRT2","_DBUFFER_MRT3","_SCREEN_SPACE_OCCLUSION","_ENVIRONMENT_REACTION_BUFFER","_CLUSTER_LIGHT_LOOP","_MAIN_LIGHT_SHADOWS","_MAIN_LIGHT_SHADOWS_CASCADE","_MAIN_LIGHT_SHADOWS_SCREEN","FOG_LINEAR","FOG_EXP","FOG_EXP2","EVALUATE_SH_VERTEX","EVALUATE_SH_MIXED"};
var enabled = keywords.Select(UnityEngine.Shader.IsKeywordEnabled).ToArray();
string[] vectors = {"_MainLightPosition","_MainLightColor","unity_LightData","_WorldSpaceCameraPos","unity_SHAr","unity_SHAg","unity_SHAb","unity_SHBr","unity_SHBg","unity_SHBb","unity_SHC","_AmbientOcclusionParam"};
var savedVectors = vectors.Select(UnityEngine.Shader.GetGlobalVector).ToArray();
string[] textures = {"_DBufferTexture0","_DBufferTexture1","_DBufferTexture2"};
var savedTextures = textures.Select(UnityEngine.Shader.GetGlobalTexture).ToArray();
var previous = UnityEngine.RenderTexture.active;
bool asynchronous = UnityEditor.ShaderUtil.allowAsyncCompilation;
var rows = new System.Collections.Generic.List<object>();
try
{
    UnityEditor.ShaderUtil.allowAsyncCompilation = false;
    foreach(var keyword in keywords) UnityEngine.Shader.DisableKeyword(keyword);
    foreach(var vector in vectors) UnityEngine.Shader.SetGlobalVector(vector,UnityEngine.Vector4.zero);
    foreach(var texture in textures) UnityEngine.Shader.SetGlobalTexture(texture,unbound);
    foreach(var sh in new[]{"unity_SHAr","unity_SHAg","unity_SHAb"}) UnityEngine.Shader.SetGlobalVector(sh,new UnityEngine.Vector4(0,0,0,0.45f));
    UnityEngine.Shader.SetGlobalVector("_WorldSpaceCameraPos",new UnityEngine.Vector4(0,0,3,1));
    material.SetColor("_BaseColor",new UnityEngine.Color(0.3f,0.7f,0.1f,1));material.SetTexture("_BaseMap",UnityEngine.Texture2D.whiteTexture);material.SetFloat("_Cull",0);material.SetFloat("_Smoothness",0);
    var block = new UnityEngine.MaterialPropertyBlock();
    var shProbe = new UnityEngine.Rendering.SphericalHarmonicsL2();shProbe.AddAmbientLight(new UnityEngine.Color(0.45f,0.45f,0.45f));block.CopySHCoefficientArraysFrom(new[]{shProbe});
    var view = UnityEngine.Matrix4x4.Scale(new UnityEngine.Vector3(1,1,-1))*UnityEngine.Matrix4x4.TRS(new UnityEngine.Vector3(0,0,3),UnityEngine.Quaternion.Euler(0,180,0),UnityEngine.Vector3.one).inverse;
    var projection = UnityEngine.GL.GetGPUProjectionMatrix(UnityEngine.Matrix4x4.Ortho(-1.5f,1.5f,-1.5f,1.5f,0.01f,10),true);
    for(int mode=0;mode<2;mode++)
    {
        if(mode==1) UnityEngine.Shader.EnableKeyword("_DBUFFER_MRT3");
        var command = new UnityEngine.Rendering.CommandBuffer();
        try
        {
            command.SetRenderTarget(target);command.ClearRenderTarget(true,true,UnityEngine.Color.clear);command.SetViewProjectionMatrices(view,projection);command.DrawMesh(mesh,UnityEngine.Matrix4x4.identity,material,0,0,block);UnityEngine.Graphics.ExecuteCommandBuffer(command);
            UnityEngine.RenderTexture.active=target;pixels.ReadPixels(new UnityEngine.Rect(0,0,128,128),0,0);pixels.Apply();
            var center=pixels.GetPixel(64,64);rows.Add(new{dbuffer=mode==1,r=center.r,g=center.g,b=center.b,a=center.a});
            System.IO.Directory.CreateDirectory("Logs/VegetationBuildInvestigation");System.IO.File.WriteAllBytes("Logs/VegetationBuildInvestigation/dbuffer-"+mode+".png",pixels.EncodeToPNG());
            if(center.a<0.9f || (mode==0 && center.g<0.05f) || (mode==1 && center.r+center.g+center.b>0.005f)) throw new System.Exception("DBuffer 비교의 기대 출력과 다릅니다: "+center);
        }
        finally{command.Release();}
    }
    return rows;
}
finally
{
    for(int i=0;i<keywords.Length;i++){if(enabled[i])UnityEngine.Shader.EnableKeyword(keywords[i]);else UnityEngine.Shader.DisableKeyword(keywords[i]);}
    for(int i=0;i<vectors.Length;i++)UnityEngine.Shader.SetGlobalVector(vectors[i],savedVectors[i]);
    for(int i=0;i<textures.Length;i++)UnityEngine.Shader.SetGlobalTexture(textures[i],savedTextures[i]);
    UnityEngine.RenderTexture.active=previous;UnityEditor.ShaderUtil.allowAsyncCompilation=asynchronous;UnityEngine.RenderTexture.ReleaseTemporary(target);
    UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(unbound);
}
