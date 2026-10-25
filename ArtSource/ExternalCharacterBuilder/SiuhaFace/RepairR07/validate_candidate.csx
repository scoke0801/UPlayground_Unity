// 전체 헤어를 같은 빌더 경로로 교체하며 리그·표정·재적용 계약을 확인한다.
var profile=Resources.FindObjectsOfTypeAll<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>().Single(p=>p.name=="Siuha_R07_Candidate");
var preset=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.BuildPreset>("Assets/10.Datas/Actor/ExternalModels/Siuha_P09_BuildPreset.asset");
var config=JsonUtility.FromJson<UPlayGround.Editor.P09Builder.CharacterBuildConfig>(JsonUtility.ToJson(preset.config));config.FaceMeshProfile=profile;
var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics));
var helper=typeof(UPlayGround.Editor.P09Builder.PrefabBuildPipeline).Assembly.GetType("UPlayGround.Editor.P09Builder.P09PosePreview");
var baked=new Mesh();int combinations=0,poses=0;
Action<bool,string> check=(ok,message)=>{if(!ok)throw new Exception(message);};
try{
profile.ValidateProfile();check(profile.fittedMeshes.Length==13,"전체 헤어 연결 누락");
foreach(var fitted in profile.fittedMeshes){
check(fitted.mesh.uv.SequenceEqual(fitted.sourceMesh.uv),"헤어 UV 변경");
check(fitted.mesh.boneWeights.SequenceEqual(fitted.sourceMesh.boneWeights),"헤어 웨이트 변경");
check(fitted.mesh.bindposes.SequenceEqual(fitted.sourceMesh.bindposes),"헤어 bindpose 변경");
for(int s=0;s<fitted.mesh.subMeshCount;s++)check(fitted.mesh.GetTriangles(s).SequenceEqual(fitted.sourceMesh.GetTriangles(s)),"헤어 토폴로지 변경");
}
foreach(int hair in Enumerable.Range(1,13))foreach(int armor in new[]{3,5}){
config.HairStyleSo=AssetDatabase.LoadAssetAtPath<ScriptableObject>(UPlayGround.Editor.P09Builder.PathConfig.CatalogRoot+"/HairStyle/HairStyleEditPartData_"+hair.ToString("00")+".asset");
// 실제 프리셋 경로에서 의상 카탈로그를 읽어 선택한다.
var catalog=new UPlayGround.Editor.P09Builder.P09AssetCatalog();catalog.Refresh();
config.ArmorSelections.Set(UPlayGround.Editor.P09Builder.BuilderArmorSlot.Chest,catalog.Chests.Single(c=>((P09.Modular.Humanoid.Data.IEditPartData)c).ContentId==armor));
var context=new UPlayGround.Editor.P09Builder.BuildContext(config);context.RootInstance=root;new UPlayGround.Editor.P09Builder.ApplyAppearanceStep().Execute(context);profile.Apply(root);
var skin=root.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name.StartsWith("Hair_"));
check(skin.name=="Hair_"+hair.ToString("00"),"선택 헤어 불일치");check(profile.fittedMeshes.Any(f=>f.mesh==skin.sharedMesh),"원본 헤어로 되돌아감");
var animator=root.GetComponent<Animator>();check(animator.avatar.isValid&&animator.avatar.isHuman,"Humanoid 오류");var head=animator.GetBoneTransform(HumanBodyBones.Head);check(head.localScale==Vector3.one,"머리 크기 변경");var savedRotation=head.localRotation;
foreach(float pitch in new[]{-30f,0f,30f})foreach(float yaw in new[]{-40f,0f,40f}){
head.localRotation=savedRotation*Quaternion.Euler(pitch,yaw,0);
foreach(var c in root.GetComponentsInChildren<UnityEngine.Animations.ParentConstraint>(true))helper.GetMethod("EvaluateConstraint",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{c});
skin.BakeMesh(baked);check(baked.vertices.All(v=>!float.IsNaN(v.sqrMagnitude)&&!float.IsInfinity(v.sqrMagnitude)),"헤어 스킨 유한값 오류");poses++;
}
head.localRotation=savedRotation;combinations++;
}
var report="전체 헤어 13 × 의상 2 = "+combinations+"조합, 반복 적용·원본 UV/토폴로지/웨이트/bindpose 유지. 머리 배율 1. 헤어 스킨 "+poses+"자세 유한 좌표 확인.";
System.IO.File.WriteAllText("output/SiuhaP09RepairR07/catalog_validation.txt",report);return report;
}finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(baked);}
