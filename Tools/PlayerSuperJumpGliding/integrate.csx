if(EditorApplication.isCompiling||EditorApplication.isUpdating)return "컴파일 또는 임포트 대기";
string clipFolder="Assets/07.Animations/Player/Traversal/SuperJumpGliding";
string motionFolder="Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Traversal/SuperJumpGliding";
string dataFolder="Assets/10.Datas/Player/Traversal";
string prefabFolder="Assets/03.Prefabs/Traversal";
foreach(string dir in new[]{motionFolder,dataFolder,prefabFolder,prefabFolder+"/Materials"})System.IO.Directory.CreateDirectory(dir);
string viewPath=prefabFolder+"/PlayerGlider.prefab";
string settingsPath=dataFolder+"/PlayerGlidingSettings.asset";
string playerSetPath=motionFolder+"/Raon_Traversal_PlayerAnimationSet.asset";
if(System.IO.File.Exists(playerSetPath)||System.IO.File.Exists(viewPath)||System.IO.File.Exists(settingsPath))return "기존 통합 결과를 보존합니다. 재실행 전 diff를 확인하세요.";
AssetDatabase.ImportAsset("Assets/05.Models/Player/Traversal/PlayerGlider.fbx",ImportAssetOptions.ForceSynchronousImport);
var importer=AssetImporter.GetAtPath("Assets/05.Models/Player/Traversal/PlayerGlider.fbx") as ModelImporter;
importer.bakeAxisConversion=false;importer.importAnimation=false;importer.SaveAndReimport();
var imported=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/05.Models/Player/Traversal/PlayerGlider.fbx");
var viewRoot=new GameObject("PlayerGlider");
UPlayGround.Components.PlayerGliderView viewAsset=null;
try{
    var model=UnityEngine.Object.Instantiate(imported);model.transform.SetParent(viewRoot.transform,true);
    Transform rider=null,left=null,right=null,canopy=null;
    foreach(var t in model.GetComponentsInChildren<Transform>(true)){
        if(t.name=="RiderAnchor")rider=t;if(t.name=="GripLeft")left=t;if(t.name=="GripRight")right=t;if(t.name=="Canopy")canopy=t;
    }
    if(rider==null||left==null||right==null||canopy==null||Vector3.Distance(left.position,new Vector3(-0.24f,1.54f,0.08f))>0.001f||Quaternion.Angle(rider.rotation,Quaternion.identity)>0.01f)throw new System.Exception("FBX 기준점·좌표 검사 실패");
    var bank=new GameObject("CanopyBankPivot");bank.transform.SetParent(viewRoot.transform,false);bank.transform.localPosition=new Vector3(0,1.86f,0.08f);canopy.SetParent(bank.transform,true);
    var shader=Shader.Find("Universal Render Pipeline/Lit");if(shader==null)throw new System.Exception("URP Lit Shader 없음");
    var materials=new System.Collections.Generic.Dictionary<string,Material>();
    foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>(true)){
        var slots=renderer.sharedMaterials;for(int i=0;i<slots.Length;i++){
            Material mat;var original=slots[i];if(!materials.TryGetValue(original.name,out mat)){
                string path=prefabFolder+"/Materials/"+original.name+".mat";
                mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(mat==null){mat=new Material(shader);mat.name=original.name;mat.color=original.color;mat.SetFloat("_Smoothness",original.name.Contains("Cloth")?0.15f:0.3f);AssetDatabase.CreateAsset(mat,path);}materials.Add(original.name,mat);
            }slots[i]=mat;
        }renderer.sharedMaterials=slots;
    }
    // Canopy는 재부모화했으므로 별도 순회하여 같은 재질을 연결한다.
    foreach(var renderer in canopy.GetComponentsInChildren<MeshRenderer>(true)){
        var slots=renderer.sharedMaterials;for(int i=0;i<slots.Length;i++){
            Material mat;var original=slots[i];if(!materials.TryGetValue(original.name,out mat)){
                string path=prefabFolder+"/Materials/"+original.name+".mat";
                mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(mat==null){mat=new Material(shader);mat.name=original.name;mat.color=original.color;mat.SetFloat("_Smoothness",0.15f);AssetDatabase.CreateAsset(mat,path);}materials.Add(original.name,mat);
            }slots[i]=mat;
        }renderer.sharedMaterials=slots;
    }
    var view=viewRoot.AddComponent<UPlayGround.Components.PlayerGliderView>();var so=new SerializedObject(view);
    so.FindProperty("_riderAnchor").objectReferenceValue=rider;so.FindProperty("_gripLeft").objectReferenceValue=left;so.FindProperty("_gripRight").objectReferenceValue=right;so.FindProperty("_canopy").objectReferenceValue=bank.transform;
    so.FindProperty("_collisionCenter").vector3Value=new Vector3(0,1.77f,0.03f);so.FindProperty("_collisionHalfExtents").vector3Value=new Vector3(1.36f,0.5f,0.65f);so.ApplyModifiedPropertiesWithoutUndo();
    viewAsset=PrefabUtility.SaveAsPrefabAsset(viewRoot,viewPath).GetComponent<UPlayGround.Components.PlayerGliderView>();
}finally{UnityEngine.Object.DestroyImmediate(viewRoot);}
var settings=ScriptableObject.CreateInstance<UPlayGround.Data.Actor.PlayerGlidingSettingsSO>();AssetDatabase.CreateAsset(settings,settingsPath);
var tags=new[]{UPlayGround.Data.Actor.Animation.MotionTags.SuperJump,UPlayGround.Data.Actor.Animation.MotionTags.GliderCatch,UPlayGround.Data.Actor.Animation.MotionTags.GliderHang,UPlayGround.Data.Actor.Animation.MotionTags.GliderTurnLeft,UPlayGround.Data.Actor.Animation.MotionTags.GliderTurnRight};
var names=new[]{"Raon_SuperJump","Raon_GliderCatch","Raon_GliderHang","Raon_GliderTurnLeft","Raon_GliderTurnRight"};
var motions=new UPlayGround.Animation.MotionSetAsset[5];
for(int i=0;i<5;i++){
    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipFolder+"/"+names[i]+".anim");if(clip==null||!clip.humanMotion)throw new System.Exception("제작 클립 없음: "+names[i]);
    var asset=ScriptableObject.CreateInstance<UPlayGround.Animation.MotionSetAsset>();asset.motionSet=UPlayGround.Animation.MotionSet.CreateAuthored(names[i],i>=2?UPlayGround.Animation.MotionSectionEndPolicy.LoopSelf:UPlayGround.Animation.MotionSectionEndPolicy.Stop);
    if(i<2)asset.motionSet.sections[0].endPolicy=UPlayGround.Animation.MotionSectionEndPolicy.Hold;
    var motion=new UPlayGround.Animation.Motion{ id=names[i],motionName=names[i],motionClip=clip};
    if(i<2)motion.markers.Add(new UPlayGround.Animation.MotionMarker{id=i==0?UPlayGround.State.PlayerSuperJumpGlidingState.LaunchMarker:UPlayGround.State.PlayerSuperJumpGlidingState.ContactMarker,displayName=i==0?"지면 밀기":"양손 접촉",normalizedTime=(i==0?0.16f:0.23f)/clip.length});
    if(i==1)motion.markers.Add(new UPlayGround.Animation.MotionMarker{id="Glider.ContactEnd",displayName="접촉 창 종료",normalizedTime=0.31f/clip.length});
    if(i==1)motion.markers.Add(new UPlayGround.Animation.MotionMarker{id="Glider.HangStart",displayName="매달리기 루프 연결",normalizedTime=0.42f/clip.length});
    asset.motionSet.motions.Add(motion);AssetDatabase.CreateAsset(asset,motionFolder+"/"+names[i]+".asset");motions[i]=asset;
}
var playerPath="Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Raon.prefab";
var player=PrefabUtility.LoadPrefabContents(playerPath);
try{
    var actorAnimator=player.GetComponent<UPlayGround.Animation.PlayerActorAnimator>();
    var original=actorAnimator.PlayerMotionSet;
    var common=ScriptableObject.CreateInstance<UPlayGround.Data.Actor.Animation.ActorAnimationMotionSet>();common.fallbackMotionSet=original.GetDefaultMotionSet();common.motionSlots=new AYellowpaper.SerializedCollections.SerializedDictionary<UPlayGround.Gameplay.Tag.GameplayTag,UPlayGround.Animation.MotionSetAsset>();
    for(int i=0;i<5;i++)common.motionSlots[tags[i]]=motions[i];AssetDatabase.CreateAsset(common,motionFolder+"/Raon_Traversal_CommonAnimationSet.asset");
    var playerSet=ScriptableObject.CreateInstance<UPlayGround.Data.Actor.Animation.PlayerActorAnimationMotionSet>();EditorUtility.CopySerialized(original,playerSet);playerSet.motionSets[UPlayGround.Data.EnumType.WeaponType.NoWeapon]=common;AssetDatabase.CreateAsset(playerSet,playerSetPath);
    var actorSerialized=new SerializedObject(actorAnimator);actorSerialized.FindProperty("_playerActorAnimationMotionSet").objectReferenceValue=playerSet;actorSerialized.ApplyModifiedPropertiesWithoutUndo();
    bool originalActive=player.activeSelf;
    var animator=player.GetComponent<Animator>();var transforms=player.GetComponentsInChildren<Transform>(true);var positions=new Vector3[transforms.Length];var rotations=new Quaternion[transforms.Length];var scales=new Vector3[transforms.Length];
    for(int i=0;i<transforms.Length;i++){positions[i]=transforms[i].localPosition;rotations[i]=transforms[i].localRotation;scales[i]=transforms[i].localScale;}
    player.SetActive(true);animator.Rebind();
    var animancer=player.GetComponent<Animancer.AnimancerComponent>();var state=animancer.Play(AssetDatabase.LoadAssetAtPath<AnimationClip>(clipFolder+"/Raon_GliderHang.anim"));state.Time=0f;animancer.Evaluate(0f);
    var palms=new Transform[2];var hints=new Transform[2];
    for(int side=0;side<2;side++){
        var hand=animator.GetBoneTransform(side==0?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);var middle=animator.GetBoneTransform(side==0?HumanBodyBones.LeftMiddleProximal:HumanBodyBones.RightMiddleProximal);
        var palm=new GameObject(side==0?"GliderPalmLeft":"GliderPalmRight").transform;palm.position=Vector3.Lerp(hand.position,middle.position,0.55f);palm.rotation=Quaternion.identity;palm.SetParent(hand,true);palms[side]=palm;
        var hint=new GameObject(side==0?"GliderElbowHintLeft":"GliderElbowHintRight").transform;hint.SetParent(player.transform,false);hint.localPosition=new Vector3(side==0?-0.44f:0.44f,1.31f,-0.1f);hints[side]=hint;
    }
    animancer.Stop();for(int i=0;i<transforms.Length;i++){transforms[i].localPosition=positions[i];transforms[i].localRotation=rotations[i];transforms[i].localScale=scales[i];}
    var binding=player.AddComponent<UPlayGround.Components.PlayerGliderBinding>();var serialized=new SerializedObject(binding);serialized.FindProperty("_settings").objectReferenceValue=settings;serialized.FindProperty("_prefab").objectReferenceValue=viewAsset;serialized.FindProperty("_animator").objectReferenceValue=animator;serialized.FindProperty("_palmLeft").objectReferenceValue=palms[0];serialized.FindProperty("_palmRight").objectReferenceValue=palms[1];serialized.FindProperty("_elbowHintLeft").objectReferenceValue=hints[0];serialized.FindProperty("_elbowHintRight").objectReferenceValue=hints[1];serialized.ApplyModifiedPropertiesWithoutUndo();
    player.SetActive(originalActive);
    PrefabUtility.SaveAsPrefabAsset(player,playerPath);
}finally{PrefabUtility.UnloadPrefabContents(player);}
System.IO.File.WriteAllText("output/PlayerSuperJumpGliding/integration.txt","model="+playerPath+"\nview="+viewPath+"\nsettings="+settingsPath+"\nplayerSet="+playerSetPath+"\nlaunch=0.16s\ncontact=0.23s\nsource common preserved through fallback\n");
return "활공 데이터·프리팹·모델 바인딩 생성 완료";
