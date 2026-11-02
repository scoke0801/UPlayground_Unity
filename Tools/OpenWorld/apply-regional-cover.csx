var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||scene.isDirty||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("저장된 월드 Edit Mode 필요");
var world=GameObject.Find("AuthoredEnvironment");if(world.transform.Find("연속 경관과 농경지")!=null)throw new Exception("이미 적용됨: 중복 실행 금지");
var folder="output/OpenWorld/RegionalCover";System.IO.Directory.CreateDirectory(folder+"/backup");var terrain=Terrain.activeTerrain;var data=terrain.terrainData;var origin=terrain.transform.position;
foreach(var p in new[]{scene.path,AssetDatabase.GetAssetPath(data)}){var dest=folder+"/backup/"+System.IO.Path.GetFileName(p);if(System.IO.File.Exists(dest))throw new Exception("백업 존재: 실행 상태 확인 필요");System.IO.File.Copy(p,dest);}
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/regional-cover-plan.json"),typeof(Dictionary<string,object>));
var regions=(Dictionary<string,object>[])read(plan["regions"].ToString(),typeof(Dictionary<string,object>[]));var rows=(Dictionary<string,object>[])read(plan["placements"].ToString(),typeof(Dictionary<string,object>[]));var fields=(Dictionary<string,object>[])read(plan["fields"].ToString(),typeof(Dictionary<string,object>[]));
var assets=rows.Select(r=>r["path"].ToString()).Distinct().ToDictionary(p=>p,p=>AssetDatabase.LoadAssetAtPath<GameObject>(p));
foreach(var kv in assets)if(kv.Value==null||kv.Value.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true).Length>0||kv.Value.GetComponentsInChildren<Transform>(true).Any(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)||kv.Value.GetComponentsInChildren<Renderer>(true).Any(r=>r.sharedMaterials.Any(m=>m==null||m.shader==null||m.shader.name.Contains("InternalError"))))throw new Exception("에셋 오류 "+kv.Key);
Func<string> identities=()=>string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")).OrderBy(s=>s));var before=identities();System.IO.File.WriteAllText(folder+"/identities-before.txt",before);
var protectedPoints=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c=>c.transform.position).ToArray();
var buildings=new List<Bounds>();var existingTrees=new List<Vector2>();
foreach(var t in world.GetComponentsInChildren<Transform>(true).Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject))){
 string path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);if(path.Contains("/Foliage/Trees/")&&!path.Contains("Bush")){existingTrees.Add(new Vector2(t.position.x,t.position.z));continue;}
 if(!(path.Contains("/Dungeon/")||path.Contains("/WorldCities/")||path.Contains("/LakeVillage/")||path.Contains("PoorHouse_")))continue;
 var rs=t.GetComponentsInChildren<MeshRenderer>(true);if(rs.Length==0)continue;var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);b.Expand(new Vector3(20,0,20));buildings.Add(b);
}
Func<float,float,bool> reserved=(x,z)=>buildings.Any(b=>x>b.min.x&&x<b.max.x&&z>b.min.z&&z<b.max.z)||protectedPoints.Any(p=>new Vector2(p.x-x,p.z-z).sqrMagnitude<225);
var root=new GameObject("연속 경관과 농경지").transform;root.SetParent(world.transform,false);var groups=new Dictionary<string,Transform>();foreach(var region in regions){var t=new GameObject(region["name"].ToString()).transform;t.SetParent(root,false);groups.Add(region["id"].ToString(),t);}
int trees=0,props=0,skipped=0,fieldCount=0,plants=0;var samples=new System.Text.StringBuilder("group,x,y,z,kind\n");
foreach(var row in rows){float x=Convert.ToSingle(row["x"]),z=Convert.ToSingle(row["z"]);string kind=row["kind"].ToString();
 if(reserved(x,z)||(kind=="tree"&&existingTrees.Any(treePoint=>Vector2.Distance(treePoint,new Vector2(x,z))<12))){skipped++;continue;}
 float slope=data.GetSteepness((x-origin.x)/data.size.x,(z-origin.z)/data.size.z);if((kind=="prop"&&slope>12)||slope>42){skipped++;continue;}
 var go=(GameObject)PrefabUtility.InstantiatePrefab(assets[row["path"].ToString()],groups[row["region"].ToString()]);var p=new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z))+origin.y,z);go.transform.position=p;go.transform.rotation=Quaternion.Euler(0,Convert.ToSingle(row["yaw"]),0);go.transform.localScale=Vector3.one*Convert.ToSingle(row["scale"]);
 if(kind=="tree") {go.transform.position+=Vector3.down*.1f;trees++;}else {var rs=go.GetComponentsInChildren<Renderer>(true);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);float y=p.y;if(kind=="rock")foreach(float px in new[]{b.min.x,b.max.x})foreach(float pz in new[]{b.min.z,b.max.z})y=Mathf.Min(y,terrain.SampleHeight(new Vector3(px,0,pz))+origin.y);go.transform.position+=Vector3.up*(y-b.min.y-(kind=="rock"?.3f:.06f));props++;}
 PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);samples.AppendLine(row["region"]+","+x+","+go.transform.position.y+","+z+","+kind);
}
string assetFolder="Assets/10.Datas/World/OpenWorld/RegionalCover";if(!AssetDatabase.IsValidFolder(assetFolder))AssetDatabase.CreateFolder("Assets/10.Datas/World/OpenWorld","RegionalCover");
var crop=AssetDatabase.LoadAssetAtPath<GameObject>(plan["cropPath"].ToString());var cropMeshes=crop.GetComponentsInChildren<MeshFilter>(true);var random=new System.Random(1102);var activeFields=new List<float[]>();
var cropRoutes=((Dictionary<string,object>[])read(plan["routes"].ToString(),typeof(Dictionary<string,object>[]))).Select(r=>(float[][])read(r["points"].ToString(),typeof(float[][]))).ToArray();
Func<float,float,bool> onRoad=(x,z)=>{var p=new Vector2(x,z);foreach(var line in cropRoutes)for(int k=1;k<line.Length;k++){var a=new Vector2(line[k-1][0],line[k-1][1]);var d=new Vector2(line[k][0],line[k][1])-a;float f=Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude);if(Vector2.Distance(p,a+d*f)<8)return true;}return false;};
foreach(var f in fields){float cx=Convert.ToSingle(f["x"]),cz=Convert.ToSingle(f["z"]),w=Convert.ToSingle(f["w"]),d=Convert.ToSingle(f["d"]),spacing=Convert.ToSingle(f["spacing"]);
 if(reserved(cx,cz)||data.GetSteepness(cx/data.size.x,(cz-origin.z)/data.size.z)>16){skipped++;continue;}
 var field=new GameObject("밀밭_"+fieldCount).transform;field.SetParent(groups[f["region"].ToString()],false);field.position=new Vector3(cx,0,cz);var lodRenderers=new List<Renderer>();int fieldPlants=0;
 foreach(int level in new[]{1,2}){var mf=cropMeshes.First(m=>m.name.Contains("LOD"+level));var combines=new List<CombineInstance>();
  for(float x=cx-w/2+2;x<cx+w/2-2;x+=spacing)for(float z=cz-d/2+2;z<cz+d/2-2;z+=spacing){if(reserved(x,z)||onRoad(x,z))continue;var p=new Vector3(x-cx,terrain.SampleHeight(new Vector3(x,0,z))+origin.y-.04f,z-cz);var combine=new CombineInstance();combine.mesh=mf.sharedMesh;combine.transform=Matrix4x4.TRS(p,Quaternion.Euler(0,(float)random.NextDouble()*360,0),Vector3.one*.9f)*mf.transform.localToWorldMatrix;combines.Add(combine);}
  if(combines.Count==0)continue;var mesh=new Mesh();mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.name="WheatField_"+fieldCount+"_LOD"+level;mesh.CombineMeshes(combines.ToArray(),true,true);mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,assetFolder+"/"+mesh.name+".asset");
  var go=new GameObject("Wheat_LOD"+level);go.transform.SetParent(field,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterials=mf.GetComponent<Renderer>().sharedMaterials;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=true;lodRenderers.Add(renderer);if(level==1)fieldPlants=combines.Count;
 }
 if(lodRenderers.Count==2){var lod=field.gameObject.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.12f,new[]{lodRenderers[0]}),new LOD(.008f,new[]{lodRenderers[1]})});lod.RecalculateBounds();}
 activeFields.Add(new[]{cx,cz,w,d});plants+=fieldPlants;fieldCount++;
}
// 넓은 지역의 기본 지표를 바꾸되 기존 길과 시설 주변의 채색은 보존한다.
var polygons=regions.Select(r=>(float[][])read(r["polygon"].ToString(),typeof(float[][]))).ToArray();
Func<Vector2,float[][],bool> inside=(p,poly)=>{bool result=false;for(int i=0,j=poly.Length-1;i<poly.Length;j=i++){var a=poly[i];var b=poly[j];if((a[1]>p.y)!=(b[1]>p.y)&&p.x<(b[0]-a[0])*(p.y-a[1])/(b[1]-a[1])+a[0])result=!result;}return result;};
var routeRows=(Dictionary<string,object>[])read(plan["routes"].ToString(),typeof(Dictionary<string,object>[]));var lines=routeRows.Select(r=>(float[][])read(r["points"].ToString(),typeof(float[][]))).ToArray();
var map=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);float sx=data.size.x/data.alphamapWidth,sz=data.size.z/data.alphamapHeight;
for(int iz=0;iz<data.alphamapHeight;iz++)for(int ix=0;ix<data.alphamapWidth;ix++){
 var p=new Vector2(origin.x+(ix+.5f)*sx,origin.z+(iz+.5f)*sz);if(!polygons.Any(poly=>inside(p,poly))||reserved(p.x,p.y))continue;bool road=false;
 foreach(var line in lines){for(int k=1;k<line.Length;k++){var a=new Vector2(line[k-1][0],line[k-1][1]);var d=new Vector2(line[k][0],line[k][1])-a;float f=Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude);if(Vector2.Distance(p,a+d*f)<8){road=true;break;}}if(road)break;}if(road)continue;
 var field=activeFields.FirstOrDefault(f=>Mathf.Abs(p.x-f[0])<f[2]/2&&Mathf.Abs(p.y-f[1])<f[3]/2);float noise=Mathf.PerlinNoise(p.x*.018f,p.y*.018f);float weight=field!=null?.86f:.24f;
 for(int l=0;l<data.alphamapLayers;l++){float target=field!=null?(l==1?.62f:l==5?.25f:l==3?.13f:0):(l==0?.55f:l==3?.3f:l==1?.15f:0);map[iz,ix,l]=Mathf.Lerp(map[iz,ix,l],target,weight*(.7f+noise*.3f));}
}
data.SetAlphamaps(0,0,map);terrain.Flush();EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);if(before!=identities())throw new Exception("진행 ID·좌표 변경");
Physics.SyncTransforms();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
System.IO.File.WriteAllText(folder+"/placements.csv",samples.ToString());System.IO.File.WriteAllText(folder+"/identities-after.txt",identities());string report="수목="+trees+" / 바위·관목·생활 소품="+props+" / 밭="+fieldCount+" / 작물 묶음="+plants+" / 제외="+skipped+" / 진행 ID·좌표 보존="+(before==identities());System.IO.File.WriteAllText(folder+"/application.txt",report);return report;
