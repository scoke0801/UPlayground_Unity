if(Application.dataPath!="C:/UsingProject/UnityProject/UPlayground/Assets"||!System.IO.File.ReadAllText("Tools/WorldThemes/AgentCoordination/editor.lock").Contains("QuarryPassR1-A-20261108"))throw new Exception("A 잠금 필요");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/QuarryPassR1.unity")throw new Exception("A 씬 필요");
string folder="Assets/10.Datas/World/ThemeStudies/QuarryPassR1";var layer=AssetDatabase.LoadAssetAtPath<TerrainLayer>(folder+"/StrataStone.terrainlayer");
var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/PoorVillage/Props/PoorProp_Stone_Field.mat");
var rows=new List<object>();
foreach(var t in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name.StartsWith("채석장_")||t.name.StartsWith("고개_"))){
var renderer=t.GetComponent<Renderer>();if(renderer==null)continue;var p=t.position;if(p.x<290||p.x>535||p.z<1080||p.z>1470)throw new Exception("범위 밖 암반 "+t.name);
string path=folder+"/Rock_"+t.name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(source);AssetDatabase.CreateAsset(mat,path);}
mat.SetTexture("_BaseMap",layer.diffuseTexture);mat.SetTexture("_BumpMap",layer.normalMapTexture);mat.SetFloat("_BumpScale",.4f);mat.SetFloat("_Smoothness",.02f);mat.SetColor("_BaseColor",new Color(.75f,.73f,.69f,1));mat.EnableKeyword("_NORMALMAP");
var mesh=t.GetComponent<MeshFilter>().sharedMesh;var uv=mesh.uv;float extent=uv.Length==0?1:Mathf.Max(uv.Max(v=>v.x)-uv.Min(v=>v.x),uv.Max(v=>v.y)-uv.Min(v=>v.y));float size=Mathf.Max(renderer.bounds.size.x,renderer.bounds.size.y,renderer.bounds.size.z);float scale=Mathf.Clamp(size/(8*Mathf.Max(.1f,extent)),.35f,10);mat.SetTextureScale("_BaseMap",Vector2.one*scale);mat.SetTextureScale("_BumpMap",Vector2.one*scale);
EditorUtility.SetDirty(mat);AssetDatabase.SaveAssetIfDirty(mat);renderer.sharedMaterial=mat;PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
rows.Add(new{name=t.name,objectId=GlobalObjectId.GetGlobalObjectIdSlow(renderer).ToString(),source=AssetDatabase.GetAssetPath(source),target=path,uvExtent=extent,worldSize=size,textureScale=scale,poseUnchanged=true,meshUnchanged=true,colliderUnchanged=true});
}
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);var jt=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);System.IO.File.WriteAllText("output/QuarryPassR1/rock-material-changes.json",(string)jt.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{rows}));return rows.Count;
