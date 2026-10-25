var profile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/Siuha_P09_Face.asset");
var helper=typeof(UPlayGround.Editor.P09Builder.PrefabBuildPipeline).Assembly.GetType("UPlayGround.Editor.P09Builder.P09PosePreview");
var report=new List<string>();int bakes=0;
foreach(string suffix in new[]{"Compatible","_Alternate"}){
var root=PrefabUtility.LoadPrefabContents("Assets/03.Prefabs/Characters/ExternalModels/Siuha_P09"+suffix+".prefab");var baked=new Mesh();IDisposable pose=null;
try{
var commonSkin=AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/Actor/ScenarioResidents/ToonLook/P09_Female_Skin_Toon.mat");
if(root.GetComponentsInChildren<Renderer>(true).Any(r=>r.sharedMaterials.Contains(commonSkin)))throw new Exception("저장 프리팹에 이전 공용 피부 재질 잔존");
pose=(IDisposable)Activator.CreateInstance(helper,new object[]{root});
foreach(string motionName in new[]{"Npc_Female_Idle","Npc_Walk"})foreach(float phase in new[]{.2f,.5f,.8f}){
var motion=AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/"+motionName+".asset");helper.GetMethod("Sample").Invoke(pose,new object[]{motion,phase,null});
foreach(var fit in profile.fittedMeshes){
var skin=root.transform.Find(fit.rendererPath).GetComponent<SkinnedMeshRenderer>();
if(skin.sharedMesh!=fit.mesh)throw new Exception("피팅 메시 참조 불일치");
if(!fit.sourceMesh.boneWeights.SequenceEqual(fit.mesh.boneWeights)||!fit.sourceMesh.triangles.SequenceEqual(fit.mesh.triangles)||!fit.sourceMesh.uv.SequenceEqual(fit.mesh.uv))throw new Exception("원본 웨이트·토폴로지·UV 변경");
skin.BakeMesh(baked);var matrix=skin.rootBone.worldToLocalMatrix*skin.transform.localToWorldMatrix;
if(baked.vertices.Any(v=>!skin.localBounds.Contains(matrix.MultiplyPoint3x4(v))))throw new Exception("피팅 헤어 bounds 이탈: "+skin.name+" / "+motionName+" / "+phase);
bakes++;
}}
}finally{if(pose!=null)pose.Dispose();UnityEngine.Object.DestroyImmediate(baked);PrefabUtility.UnloadPrefabContents(root);}
}
report.Add("피팅 2종 × 프리팹 2종 × Idle/Walk 6표본 = "+bakes+"회: 원본 웨이트·UV·토폴로지 유지, 피팅 메시 참조 일치, bounds 이탈 0.");
var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/08.Models/Characters/SiuhaP09/Siuha_P09_Skin.mat");
if(profile.materials[2]!=profile.materials[0]||mat.mainTexture!=profile.materials[0].mainTexture||mat.mainTextureScale!=Vector2.zero||mat.mainTextureOffset!=new Vector2(.5f,.15f))throw new Exception("목·몸체 알베도 샘플 불일치");
if(profile.mesh.GetTriangles(2).Any(i=>profile.mesh.uv[i]!=new Vector2(.5f,.15f)))throw new Exception("목·두피 UV가 피부 영역 밖을 샘플링합니다.");
report.Add("목·뒤통수 UV는 피부 픽셀로 고정. 몸체와 동일 알베도·셰이더 사용.");
System.IO.File.WriteAllLines("output/SiuhaP09RepairR02/fitted_validation.txt",report);return report;
