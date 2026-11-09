// 실제 LOD2 형상을 동일 격자의 전용 색·알파 아틀라스로 굽는다. 원본 에셋은 읽기만 한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
string path="Assets/10.Datas/World/ThemeStudies/ShadeCreekR3/CanopyLod2Atlas.png";
if(scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR3.unity"||scene.isDirty||EditorApplication.isPlayingOrWillChangePlaymode||System.IO.File.Exists(path))throw new Exception("저장된 R3와 최초 아틀라스 생성 필요");
var sample=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LODGroup>(true)).First(g=>g.gameObject.activeInHierarchy&&g.transform.position.x>1600&&g.transform.position.x<1650&&g.transform.position.z>770&&g.transform.position.z<830&&g.GetLODs().Length==4);
var tree=UnityEngine.Object.Instantiate(sample.gameObject);tree.hideFlags=HideFlags.HideAndDontSave;tree.transform.position=new Vector3(0,1000,0);tree.transform.rotation=Quaternion.identity;tree.transform.localScale=Vector3.one;
foreach(var tr in tree.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=31;
var group=tree.GetComponent<LODGroup>();group.ForceLOD(2);group.animateCrossFading=false;
var copies=new List<Material>();foreach(var renderer in tree.GetComponentsInChildren<Renderer>(true)){renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>{if(m==null)return null;var copy=new Material(m);copy.hideFlags=HideFlags.HideAndDontSave;copy.DisableKeyword("_USE_WPO");copies.Add(copy);return copy;}).ToArray();}
var billboard=group.GetLODs()[3].renderers[0];float span=billboard.GetComponent<MeshFilter>().sharedMesh.bounds.size.y;var center=billboard.transform.position;
var go=new GameObject("TemporaryCanopyAtlasCamera");go.hideFlags=HideFlags.HideAndDontSave;var camera=go.AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.orthographic=true;camera.orthographicSize=span*.5f;camera.aspect=1;camera.nearClipPlane=.1f;camera.farClipPlane=200;
var rt=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32);var tile=new Texture2D(256,256,TextureFormat.RGBA32,false);var atlas=new Texture2D(3072,3072,TextureFormat.RGBA32,false);atlas.SetPixels32(new Color32[3072*3072]);
var oldActive=RenderTexture.active;bool oldFog=RenderSettings.fog;var oldMode=RenderSettings.ambientMode;var oldColor=RenderSettings.ambientLight;float oldIntensity=RenderSettings.ambientIntensity;var oldProbe=RenderSettings.ambientProbe;
var lights=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).ToDictionary(l=>l,l=>l.enabled);
try{
foreach(var pair in lights)pair.Key.enabled=false;RenderSettings.fog=false;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=Color.white;RenderSettings.ambientIntensity=1;
var probe=new UnityEngine.Rendering.SphericalHarmonicsL2();probe.AddAmbientLight(Color.white);RenderSettings.ambientProbe=probe;
camera.targetTexture=rt;
for(int elevation=0;elevation<7;elevation++)for(int yaw=0;yaw<16;yaw++){
float a=yaw*22.5f*Mathf.Deg2Rad,b=(elevation-3)*15f*Mathf.Deg2Rad;
var direction=new Vector3(Mathf.Cos(a)*Mathf.Cos(b),Mathf.Sin(b),Mathf.Sin(a)*Mathf.Cos(b));
camera.transform.SetPositionAndRotation(center-direction*100,Quaternion.LookRotation(direction,Vector3.up));camera.Render();camera.Render();RenderTexture.active=rt;tile.ReadPixels(new Rect(0,0,256,256),0,0);tile.Apply();
int index=yaw+elevation*16;atlas.SetPixels(index%12*256,index/12*256,256,256,tile.GetPixels());
}
atlas.Apply();System.IO.File.WriteAllBytes(path,atlas.EncodeToPNG());
}finally{
foreach(var pair in lights)if(pair.Key!=null)pair.Key.enabled=pair.Value;RenderSettings.fog=oldFog;RenderSettings.ambientMode=oldMode;RenderSettings.ambientLight=oldColor;RenderSettings.ambientIntensity=oldIntensity;RenderSettings.ambientProbe=oldProbe;
camera.targetTexture=null;RenderTexture.active=oldActive;UnityEngine.Object.DestroyImmediate(tree);UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tile);UnityEngine.Object.DestroyImmediate(atlas);foreach(var material in copies)UnityEngine.Object.DestroyImmediate(material);
}
AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.isReadable=false;importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.ToSmaller;importer.textureCompression=TextureImporterCompression.CompressedHQ;var platform=importer.GetPlatformTextureSettings("Standalone");platform.name="Standalone";platform.overridden=true;platform.maxTextureSize=2048;platform.format=TextureImporterFormat.BC7;importer.SetPlatformTextureSettings(platform);importer.SaveAndReimport();
var target=AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/ThemeStudies/ShadeCreekR3/Canopy_2.mat");target.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(path));target.SetFloat("_Value",1);target.SetFloat("_Saturation",1);target.SetFloat("_AlphaClipThreshold",.55f);EditorUtility.SetDirty(target);AssetDatabase.SaveAssetIfDirty(target);
var result=new{views=112,tile=256,atlas=3072,sourceLOD=2,span,lighting="복제 수목·흰색 ambient SH·직접광 없음·안개 없음",normalAtlas="기존 읽기 전용 방향별 노멀 아틀라스 유지. 새로운 실루엣과 노멀 일치 여부는 검토 한계",scope="R3 전용 색·알파 아틀라스. 성능 검증 아님"};
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);System.IO.File.WriteAllText("output/ShadeCreekR3/atlas-bake.json",(string)json.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{result}));return result;

