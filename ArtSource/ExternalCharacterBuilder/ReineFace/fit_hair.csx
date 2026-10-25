// 얼굴 표면을 기준으로 뜬 앞머리만 안쪽으로 이동한다. 원본 리그·UV·가닥 연결은 보존한다.
var folder="Assets/08.Models/Characters/ReineP09";
var jsonType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var settings=(Dictionary<string,float>)jsonType.GetMethods().Single(m=>m.Name=="DeserializeObject"&&m.IsGenericMethodDefinition&&m.GetParameters().Length==1&&m.GetParameters()[0].ParameterType==typeof(string)).MakeGenericMethod(typeof(Dictionary<string,float>)).Invoke(null,new object[]{System.IO.File.ReadAllText("ArtSource/ExternalCharacterBuilder/ReineFace/hair_fit.json")});
if(settings.Values.Any(v=>float.IsNaN(v)||float.IsInfinity(v)||v<=0)||settings["front_end_m"]<=settings["front_start_m"]||settings["side_end_m"]<=settings["side_start_m"]||settings["contact_fraction"]>1)throw new Exception("헤어 피팅 수치 오류");
var profile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/Reine_P09_Face.asset");
var preview=new PreviewRenderUtility();var faceBake=new Mesh();var hairBake=new Mesh();
try{
var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics));preview.AddSingleGO(root);
profile.Apply(root);
var helper=typeof(UPlayGround.Editor.P09Builder.PrefabBuildPipeline).Assembly.GetType("UPlayGround.Editor.P09Builder.P09PosePreview");
foreach(var constraint in root.GetComponentsInChildren<UnityEngine.Animations.ParentConstraint>(true))helper.GetMethod("EvaluateConstraint",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{constraint});
var head=root.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head);
var face=root.transform.Find(profile.rendererPaths[0]).GetComponent<SkinnedMeshRenderer>();face.BakeMesh(faceBake);
faceBake.vertices=faceBake.vertices.Select(v=>head.InverseTransformPoint(face.transform.TransformPoint(v))).ToArray();
faceBake.RecalculateBounds();
var collision=new GameObject("Reine 두상 피팅 표면");preview.AddSingleGO(collision);var collider=collision.AddComponent<MeshCollider>();collider.sharedMesh=faceBake;
var skin=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Hair_03");
var native=AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics).GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name==skin.name).sharedMesh;
skin.sharedMesh=native;skin.BakeMesh(hairBake);
var vertices=native.vertices;var baked=hairBake.vertices;var weights=native.boneWeights;var binds=native.bindposes;
Func<float,float,float,float> smooth=(a,b,x)=>{var t=Mathf.Clamp01((x-a)/(b-a));return t*t*(3-2*t);};
int moved=0;float maximum=0;
for(int i=0;i<vertices.Length;i++){
var p=head.InverseTransformPoint(skin.transform.TransformPoint(baked[i]));
float amount=smooth(settings["front_start_m"],settings["front_end_m"],-p.x)*(1-smooth(settings["side_start_m"],settings["side_end_m"],Mathf.Abs(p.z)));
float move=settings["max_move_m"]*Mathf.Exp(-Mathf.Pow((p.y-settings["center_height_m"])/settings["height_falloff_m"],2))*amount;
RaycastHit hit;
if(collider.Raycast(new Ray(new Vector3(-1,p.y,p.z),Vector3.right),out hit,2))move=Mathf.Min(move,Mathf.Max(0,hit.point.x-settings["clearance_m"]-p.x)*settings["contact_fraction"]);
if(move<.00001f)continue;
var delta=skin.transform.InverseTransformVector(head.TransformVector(Vector3.right*move));
var w=weights[i];var matrix=Matrix4x4.zero;
var ids=new[]{w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3};var values=new[]{w.weight0,w.weight1,w.weight2,w.weight3};
for(int b=0;b<4;b++)if(values[b]>0){var part=skin.transform.worldToLocalMatrix*skin.bones[ids[b]].localToWorldMatrix*binds[ids[b]];for(int n=0;n<16;n++)matrix[n]+=part[n]*values[b];}
vertices[i]+=matrix.inverse.MultiplyVector(delta);maximum=Mathf.Max(maximum,move);moved++;
}
if(moved==0)throw new Exception("피팅 대상 앞머리 정점이 없습니다.");
var original=native.vertices;var triangles=native.triangles;
if(vertices.Any(v=>float.IsNaN(v.sqrMagnitude)||float.IsInfinity(v.sqrMagnitude)))throw new Exception("유효하지 않은 헤어 정점");
for(int i=0;i<triangles.Length;i+=3){int a=triangles[i],b=triangles[i+1],c=triangles[i+2];if(Vector3.Dot(Vector3.Cross(original[b]-original[a],original[c]-original[a]),Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]))<0)throw new Exception("헤어 면 뒤집힘: "+i/3);}
var mesh=UnityEngine.Object.Instantiate(native);mesh.name="Reine_Hair_03_Fitted";mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
string path=folder+"/Reine_Hair_03_Fitted.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{saved.vertices=vertices;saved.normals=mesh.normals;saved.tangents=mesh.tangents;saved.RecalculateBounds();UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}
profile.fittedMeshes=new[]{new UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO.FittedMesh{rendererPath=AnimationUtility.CalculateTransformPath(skin.transform,root.transform),sourceMesh=native,mesh=saved}};
profile.ValidateProfile();EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(saved);AssetDatabase.SaveAssetIfDirty(profile);
return new{moved=moved,maxMove=maximum,headBounds=faceBake.bounds.ToString()};
}finally{preview.Cleanup();UnityEngine.Object.DestroyImmediate(faceBake);UnityEngine.Object.DestroyImmediate(hairBake);}



