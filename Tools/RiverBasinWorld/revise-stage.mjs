import fs from 'node:fs';
import assert from 'node:assert/strict';
// 같은 저작 루틴을 사용하되 이미 만든 전용 지형·메시만 갱신한다. 공용 자산은 쓰지 않는다.
const source=fs.readFileSync('Tools/RiverBasinWorld/build-world.csx','utf8');
function part(a,b){const start=source.indexOf(a),end=source.indexOf(b,start);assert(start>=0&&end>start);return source.slice(start,end);}
let code=`var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||scene.isDirty||scene.path!="Assets/01.Scenes/Ingame/Worlds/RiverBasinWorld.unity")throw new Exception("저장된 전용 씬 필요");
string data="Assets/10.Datas/World/RiverBasinWorld",input="Tools/RiverBasinWorld";
`+part('var json=','var placements=')+`
var generated=GameObject.Find("GeneratedEnvironment");var ts=generated.GetComponentsInChildren<Terrain>();var terrains=new Terrain[3,3];
foreach(var t in ts){int tx=Mathf.RoundToInt((t.transform.position.x+768)/512),tz=Mathf.RoundToInt((t.transform.position.z+768)/512);terrains[tx,tz]=t;
var bytes=System.IO.File.ReadAllBytes(input+"/Generated/terrain-"+tx+"-"+tz+".f32");var heights=new float[513,513];Buffer.BlockCopy(bytes,0,heights,0,bytes.Length);t.terrainData.SetHeights(0,0,heights);
bytes=System.IO.File.ReadAllBytes(input+"/Generated/alpha-"+tx+"-"+tz+".f32");var alpha=new float[256,256,7];Buffer.BlockCopy(bytes,0,alpha,0,bytes.Length);t.terrainData.SetAlphamaps(0,0,alpha);AssetDatabase.SaveAssetIfDirty(t.terrainData);}
`+part('Func<float,float,float> ground=','var stone=')+`
var waterMaterial=AssetDatabase.LoadAssetAtPath<Material>(data+"/WaterBlockout.mat");var waterRoot=GameObject.Find("수계 · 평탄 소와 낙차");
foreach(Transform t in waterRoot.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
`+part('Action<string,Vector3[],int[]> meshObject=','var bridge=')
.replace('var m=new Mesh();m.name=name;','var m=AssetDatabase.LoadAssetAtPath<Mesh>(data+"/"+name+".asset");m.Clear();m.name=name;')
.replace('AssetDatabase.CreateAsset(m,data+"/"+name+".asset");','EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);')+`
var placements=rows(plan["placements"]);var art=GameObject.Find("B1 대표 구간 · 생활과 군락");var village=GameObject.Find("호수 마을 · 블록아웃");
foreach(var parent in new[]{art.transform,village.transform})foreach(Transform t in parent.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
`+part('var materialCopies=','Action<string,Vector3,Vector3,Transform,Material> cube=')
.replace('AssetDatabase.CreateAsset(copy,data+"/AutumnLeaves_"+materialCopies.Count+".mat");','var existing=AssetDatabase.LoadAssetAtPath<Material>(data+"/AutumnLeaves_"+materialCopies.Count+".mat");if(existing!=null){UnityEngine.Object.DestroyImmediate(copy);copy=existing;}else AssetDatabase.CreateAsset(copy,data+"/AutumnLeaves_"+materialCopies.Count+".mat");')+`
var stone=AssetDatabase.LoadAssetAtPath<Material>(data+"/BridgeStone.mat");
`+part('Action<string,Vector3,Vector3,Transform,Material> cube=','var landmarks=')
+part('var fields=','var context=')+`
foreach(var b in rows(plan["bridges"])){var g=GameObject.Find("Castle_Moat_Bridge (RiverBasinWorld "+b["id"]+")");g.transform.rotation=Quaternion.Euler(0,Convert.ToSingle(b["yaw"]),0);}
Physics.SyncTransforms();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);return "전용 제작본 지형·수계·군락 수정";
`;
fs.writeFileSync('Tools/RiverBasinWorld/revise-stage.csx',code);
