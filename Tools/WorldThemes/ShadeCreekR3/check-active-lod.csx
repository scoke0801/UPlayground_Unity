// 임시 수목의 각 LOD를 고유 색으로 렌더해 실제 선택 단계를 식별한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR3.unity"||scene.isDirty||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("R3 Edit 필요");
var sample=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LODGroup>(true)).First(g=>g.gameObject.activeInHierarchy&&g.transform.position.x>1600&&g.transform.position.x<1650&&g.transform.position.z>770&&g.transform.position.z<830&&g.GetLODs().Length==4);
var tree=UnityEngine.Object.Instantiate(sample.gameObject);tree.hideFlags=HideFlags.HideAndDontSave;tree.transform.position=new Vector3(0,1000,0);
foreach(var tr in tree.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=31;
var group=tree.GetComponent<LODGroup>();group.ForceLOD(-1);group.animateCrossFading=false;
var colors=new[]{Color.red,Color.green,Color.blue,Color.magenta};var materials=new List<Material>();
var go=new GameObject("TemporaryLodIdentification");go.hideFlags=HideFlags.HideAndDontSave;var camera=go.AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.fieldOfView=60;camera.aspect=16f/9f;camera.farClipPlane=2000;
var rt=new RenderTexture(1280,720,24);var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);var old=RenderTexture.active;bool fog=RenderSettings.fog;var rows=new List<object>();
string folder="output/ShadeCreekR3/lod-identification";System.IO.Directory.CreateDirectory(folder);
try{
RenderSettings.fog=false;var lods=group.GetLODs();
for(int i=0;i<lods.Length;i++){var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mat.SetColor("_BaseColor",colors[i]);mat.SetFloat("_Cull",0);materials.Add(mat);foreach(var renderer in lods[i].renderers){renderer.enabled=true;renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>mat).ToArray();}}
camera.targetTexture=rt;var center=tree.transform.TransformPoint(group.localReferencePoint);
foreach(float distance in new[]{160f,320f,390f,400f,408f,409f,410f,412f,420f,430f,640f}){
camera.transform.SetPositionAndRotation(center+new Vector3(0,0,-distance),Quaternion.identity);camera.Render();camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();
int[] counts=new int[4];foreach(var c in texture.GetPixels32()){if(c.r>100&&c.b<50&&c.g<50)counts[0]++;if(c.g>100&&c.r<50&&c.b<50)counts[1]++;if(c.b>100&&c.r<50&&c.g<50)counts[2]++;if(c.r>100&&c.b>100&&c.g<50)counts[3]++;}
System.IO.File.WriteAllBytes(folder+"/"+distance+".png",texture.EncodeToPNG());rows.Add(new{distance,renderedPixelCountsByLOD=counts,identifiedContributingLODs=Enumerable.Range(0,4).Where(i=>counts[i]>0).ToArray()});
}
}finally{RenderSettings.fog=fog;camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(tree);UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);foreach(var mat in materials)UnityEngine.Object.DestroyImmediate(mat);}
var result=new{rows,method="임시 복제 수목의 LOD별 Unlit 고유색 렌더 픽셀 식별. 자동 LOD, 동일 크기·카메라. production shader/알파/디더 검증은 별도 원본 재질 비교 화면을 사용. GPU 디버거의 draw-call 계측이나 CPU/GPU 성능 측정은 아님.",formulaUsedForIdentification=false};
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);System.IO.File.WriteAllText("output/ShadeCreekR3/lod-active-diagnostic.json",(string)json.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{result}));return result;

