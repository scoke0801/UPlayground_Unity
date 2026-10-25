var profile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/Siuha_P09_Face.asset");profile.ValidateProfile();
if(profile.fittedMeshes.Length!=0||profile.hairAttachments.Any(a=>a.localScale!=Vector3.one))throw new Exception("압축 메시 또는 헤어 배율 설정 잔존");
using(var reader=new System.IO.BinaryReader(System.IO.File.OpenRead("ArtSource/ExternalCharacterBuilder/SiuhaFace/RepairR02/repaired_face.bin"))){int count=reader.ReadInt32();reader.ReadInt32();reader.ReadInt32();var points=profile.mesh.vertices;if(count!=points.Length)throw new Exception("얼굴 정점 수 불일치");for(int i=0;i<count;i++){var p=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());if(p!=points[i])throw new Exception("R03 이전 얼굴 복구 불일치: "+i);}}
var original=AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics);
var helper=typeof(UPlayGround.Editor.P09Builder.PrefabBuildPipeline).Assembly.GetType("UPlayGround.Editor.P09Builder.P09PosePreview");int bakes=0;
foreach(string suffix in new[]{"Compatible","_Alternate"}){
var root=PrefabUtility.LoadPrefabContents("Assets/03.Prefabs/Characters/ExternalModels/Siuha_P09"+suffix+".prefab");var baked=new Mesh();IDisposable pose=null;
try{
profile.Apply(root);profile.Apply(root);
if(root.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head).localScale!=Vector3.one)throw new Exception("머리 본 배율 변경");
foreach(var a in profile.hairAttachments){var c=root.transform.Find(a.constraintPath).GetComponent<UnityEngine.Animations.ParentConstraint>();if(c.transform.localScale!=Vector3.one||c.GetTranslationOffset(0)!=a.positionOffset)throw new Exception("반복 적용 후 위치·스케일 불일치");}
pose=(IDisposable)Activator.CreateInstance(helper,new object[]{root});
foreach(string motionName in new[]{"Npc_Female_Idle","Npc_Walk"})foreach(float phase in new[]{.2f,.5f,.8f}){
var motion=AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/"+motionName+".asset");helper.GetMethod("Sample").Invoke(pose,new object[]{motion,phase,null});
foreach(string name in new[]{"Hair_03","Hair_11"}){var skin=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name==name);var source=original.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name==name);if(skin.sharedMesh!=source.sharedMesh)throw new Exception("원본 P09 메시 참조 불일치");skin.BakeMesh(baked);var transform=skin.rootBone.worldToLocalMatrix*skin.transform.localToWorldMatrix;if(baked.vertices.Any(p=>!skin.localBounds.Contains(transform.MultiplyPoint3x4(p))))throw new Exception("원본 헤어 bounds 이탈");bakes++;}
}
}finally{if(pose!=null)pose.Dispose();UnityEngine.Object.DestroyImmediate(baked);PrefabUtility.UnloadPrefabContents(root);}
}
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene("Assets/01.Scenes/Tests/Equipment/Siuha_P09_Review.unity");
try{foreach(var root in scene.GetRootGameObjects().Where(g=>g.name.StartsWith("Siuha_P09"))){foreach(var a in profile.hairAttachments){var c=root.transform.Find(a.constraintPath).GetComponent<UnityEngine.Animations.ParentConstraint>();if(c.transform.localScale!=Vector3.one||c.GetTranslationOffset(0)!=a.positionOffset)throw new Exception("저장 씬의 헤어 설정 불일치");}foreach(string name in new[]{"Hair_03","Hair_11"})if(root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name==name).sharedMesh!=original.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name==name).sharedMesh)throw new Exception("씬 원본 헤어 연결 불일치");}var body=scene.GetRootGameObjects().Single(g=>g.name=="Siuha_P09_BodyOnly");if(body.GetComponentsInChildren<Renderer>().Count(r=>r.enabled)!=3)throw new Exception("BodyOnly 표시 변경");}
finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
var report="R03 이전 얼굴 정점 11560개 정확 복구. Head/헤어 스케일 1. Hair03/11은 P09 원본 Mesh 정확 참조. 2회 재적용, 모션 "+bakes+"회 bounds, 저장 씬 재로드·BodyOnly 3개 표시 통과.";
System.IO.Directory.CreateDirectory("output/SiuhaP09RepairR04");System.IO.File.WriteAllText("output/SiuhaP09RepairR04/native_validation.txt",report);return report;
