var folder="Assets/08.Models/Characters/NenmirP09";
var profile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/Nenmir_P09_Face.asset");
var root=AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics);
var fitted=new List<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO.FittedMesh>();
var attachments=new List<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO.HairAttachment>();
var jsonType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var settings=(Dictionary<string,Dictionary<string,float>>)jsonType.GetMethods().Single(m=>m.Name=="DeserializeObject"&&m.IsGenericMethodDefinition&&m.GetParameters().Length==1&&m.GetParameters()[0].ParameterType==typeof(string)).MakeGenericMethod(typeof(Dictionary<string,Dictionary<string,float>>)).Invoke(null,new object[]{System.IO.File.ReadAllText("ArtSource/ExternalCharacterBuilder/NenmirFace/hair_fit.json")});
using(var reader=new System.IO.BinaryReader(System.IO.File.OpenRead("ArtSource/ExternalCharacterBuilder/NenmirFace/hair_fitted.bin"))){
if(reader.ReadInt32()!=2)throw new Exception("헤어 교환 파일 수 불일치");
foreach(string name in new[]{"Hair_03","Hair_11"}){
var skin=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name==name);var source=skin.sharedMesh;
if(source.blendShapeCount!=0)throw new Exception("헤어 표정 피팅이 필요합니다.");
int count=reader.ReadInt32();if(count!=source.vertexCount)throw new Exception("헤어 정점 수 불일치");
var points=new Vector3[count];for(int i=0;i<count;i++)points[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
var path=folder+"/Nenmir_"+name+"_Fitted.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);bool create=mesh==null;if(create)mesh=UnityEngine.Object.Instantiate(source);
mesh.name="Nenmir_"+name+"_Fitted";mesh.vertices=points;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
if(create)AssetDatabase.CreateAsset(mesh,path);else EditorUtility.SetDirty(mesh);
fitted.Add(new UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO.FittedMesh{rendererPath=AnimationUtility.CalculateTransformPath(skin.transform,root.transform),sourceMesh=source,mesh=mesh});
attachments.Add(new UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO.HairAttachment{constraintPath="Mesh/Hair/"+name+"/Skeleton_P09_"+name+"/Constraint_Head",positionOffset=new Vector3(settings[name]["back_m"],settings[name]["lift_m"],0),localScale=Vector3.one});
}
if(reader.BaseStream.Position!=reader.BaseStream.Length)throw new Exception("헤어 교환 파일 길이 불일치");
}
profile.fittedMeshes=fitted.ToArray();profile.hairAttachments=attachments.ToArray();profile.ValidateProfile();EditorUtility.SetDirty(profile);
foreach(string name in new[]{"Nenmir_P09Compatible","Nenmir_P09_Alternate"}){
var path="Assets/03.Prefabs/Characters/ExternalModels/"+name+".prefab";var instance=PrefabUtility.LoadPrefabContents(path);
try{profile.Apply(instance);PrefabUtility.SaveAsPrefabAsset(instance,path);}finally{PrefabUtility.UnloadPrefabContents(instance);}
}
AssetDatabase.SaveAssets();return "기본/대체 프리팹과 얼굴 프로파일에 전용 헤어 피팅을 저장했습니다.";
