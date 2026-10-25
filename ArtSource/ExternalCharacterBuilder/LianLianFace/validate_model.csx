var report=new List<string>();
var profile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/LianLian_P09_Face.asset");profile.ValidateProfile();
var mesh=profile.mesh;
Action<bool,string> check=(ok,message)=>{if(!ok)throw new Exception(message);};
Func<Vector3,bool> finite=v=>!float.IsNaN(v.x)&&!float.IsNaN(v.y)&&!float.IsNaN(v.z)&&!float.IsInfinity(v.x)&&!float.IsInfinity(v.y)&&!float.IsInfinity(v.z);
check(mesh.vertices.All(finite),"유효하지 않은 정점");check(mesh.uv.Length==mesh.vertexCount&&mesh.normals.Length==mesh.vertexCount&&mesh.tangents.Length==mesh.vertexCount,"정점 채널 누락");
foreach(var w in mesh.boneWeights){check(Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)<.0001f,"웨이트 합 오류");check(new[]{w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3}.All(i=>i>=0&&i<mesh.bindposes.Length),"본 인덱스 오류");}
var native=AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics).GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Female_Face_01").sharedMesh;
check(mesh.blendShapeCount==native.blendShapeCount,"표정 개수 불일치");
for(int s=0;s<mesh.blendShapeCount;s++)check(mesh.GetBlendShapeName(s)==native.GetBlendShapeName(s),"표정 순서 불일치");
var helper=typeof(UPlayGround.Editor.P09Builder.PrefabBuildPipeline).Assembly.GetType("UPlayGround.Editor.P09Builder.P09PosePreview");
int expressionBakes=0,motionBakes=0;
foreach(string name in new[]{"LianLian_P09Compatible","LianLian_P09_Alternate"}){
var root=PrefabUtility.LoadPrefabContents("Assets/03.Prefabs/Characters/ExternalModels/"+name+".prefab");var baked=new Mesh();IDisposable pose=null;
try{
var animator=root.GetComponent<Animator>();check(animator.avatar!=null&&animator.avatar.isValid&&animator.avatar.isHuman,"Avatar 오류");
check(root.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))==0,"Missing Script");
var skins=root.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled).ToArray();
foreach(var r in skins){check(r.sharedMaterials.All(m=>m!=null&&m.shader!=null)&&r.sharedMaterials.Length==r.sharedMesh.subMeshCount,"재질 슬롯 누락: "+r.name);check(r.bones.All(b=>b!=null&&(b==root.transform||b.IsChildOf(root.transform))),"외부 본 참조");}
var face=skins.Single(r=>r.name.StartsWith("Female_Face_"));check(face.sharedMesh==mesh,"프리팹 얼굴 참조 오류");
for(int s=0;s<mesh.blendShapeCount;s++){face.SetBlendShapeWeight(s,100);face.BakeMesh(baked);check(baked.vertices.All(finite),"표정 스키닝 오류");face.SetBlendShapeWeight(s,0);expressionBakes++;}
pose=(IDisposable)Activator.CreateInstance(helper,new object[]{root});
foreach(string motionName in new[]{"Npc_Female_Idle","Npc_Walk"})foreach(float phase in new[]{.2f,.5f,.8f}){
var motion=AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/"+motionName+".asset");check(motion!=null,"모션 누락");helper.GetMethod("Sample").Invoke(pose,new object[]{motion,phase,null});
face.BakeMesh(baked);check(baked.vertices.All(finite),"생활 모션 스키닝 오류");var toRoot=face.rootBone.worldToLocalMatrix*face.transform.localToWorldMatrix;check(baked.vertices.All(v=>face.localBounds.Contains(toRoot.MultiplyPoint3x4(v))),"생활 모션 얼굴 컬링 bounds 이탈");motionBakes++;
}
report.Add(name+": Missing Script/외부 본/재질 누락 0, Humanoid 유효, 활성 렌더러 "+skins.Length+", 삼각형 "+skins.Sum(r=>r.sharedMesh.triangles.Length/3)+", 재질 슬롯 "+skins.Sum(r=>r.sharedMaterials.Length));
}finally{if(pose!=null)pose.Dispose();UnityEngine.Object.DestroyImmediate(baked);PrefabUtility.UnloadPrefabContents(root);}
}
var preset=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.BuildPreset>("Assets/10.Datas/Actor/ExternalModels/LianLian_P09_BuildPreset.asset");
var config=JsonUtility.FromJson<UPlayGround.Editor.P09Builder.CharacterBuildConfig>(JsonUtility.ToJson(preset.config));check(config.FaceMeshProfile==profile&&!config.Validate().Any(),"프리셋 직렬화 오류");
var testRoot=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics));
try{
foreach(int hair in new[]{3,11})foreach(int armor in new[]{3,5}){
config.HairStyleSo=AssetDatabase.LoadAssetAtPath<ScriptableObject>(UPlayGround.Editor.P09Builder.PathConfig.CatalogRoot+"/HairStyle/HairStyleEditPartData_"+hair.ToString("00")+".asset");
foreach(var slot in new[]{UPlayGround.Editor.P09Builder.BuilderArmorSlot.Chest,UPlayGround.Editor.P09Builder.BuilderArmorSlot.Arm,UPlayGround.Editor.P09Builder.BuilderArmorSlot.Waist,UPlayGround.Editor.P09Builder.BuilderArmorSlot.Leg})config.ArmorSelections.Set(slot,AssetDatabase.LoadAssetAtPath<ScriptableObject>(UPlayGround.Editor.P09Builder.PathConfig.CatalogRoot+"/"+slot+"/"+slot+"EditPartData_"+armor.ToString("00")+".asset"));
var ctx=new UPlayGround.Editor.P09Builder.BuildContext(config);ctx.RootInstance=testRoot;new UPlayGround.Editor.P09Builder.ApplyAppearanceStep().Execute(ctx);
check(testRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r=>r.name.StartsWith("Female_Face_")).All(r=>r.sharedMesh==mesh),"슬롯 변경 후 얼굴 유실");
}
var invalid=UnityEngine.Object.Instantiate(profile);try{var first=testRoot.transform.Find(profile.rendererPaths[0]).GetComponent<SkinnedMeshRenderer>();var before=first.sharedMesh;invalid.rendererPaths=new[]{profile.rendererPaths[0],"Missing/Face"};bool rejected=false;try{invalid.Apply(testRoot);}catch(InvalidOperationException){rejected=true;}check(rejected&&first.sharedMesh==before,"무효 프로파일 부분 적용");}finally{UnityEngine.Object.DestroyImmediate(invalid);}
}finally{UnityEngine.Object.DestroyImmediate(testRoot);}
report.Add("표정 스키닝 "+expressionBakes+"회, 생활 모션 샘플 "+motionBakes+"회, 헤어/의상 4조합, 프리셋 재로드, 무효 경로 거부: 통과");
report.Add("얼굴: 정점 "+mesh.vertexCount+", 본 "+mesh.bindposes.Length+", 표정 "+mesh.blendShapeCount+", 런타임 메시 메모리 "+UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(mesh)+" bytes");
report.Add("생활 모션 얼굴 bounds 이탈 0. Editor 자동 검사이며 런타임 컬링·게임 대화·물리·Player·군중 성능 및 사용자 미술 승인 대기. 실제 SkinnedMeshRenderer 렌더는 LiveSkin 캡처로 별도 확인.");
System.IO.Directory.CreateDirectory("output/LianLianP09");System.IO.File.WriteAllLines("output/LianLianP09/validation.txt",report);return report;

