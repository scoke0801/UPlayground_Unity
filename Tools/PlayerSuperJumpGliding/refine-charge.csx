if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new System.Exception("편집 모드·컴파일 완료 후 실행하세요.");
string folder="Assets/07.Animations/Player/Traversal/SuperJumpGliding/";
string motionPath="Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Traversal/SuperJumpGliding/Raon_SuperJump.asset";
var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"Raon_SuperJump.anim");
var motion=AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>(motionPath);
var settings=AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.PlayerGlidingSettingsSO>("Assets/10.Datas/Player/Traversal/PlayerGlidingSettings.asset");
string backup="output/PlayerSuperJumpGliding/before-charge/";System.IO.Directory.CreateDirectory(backup);
foreach(string path in new[]{AssetDatabase.GetAssetPath(clip),motionPath,AssetDatabase.GetAssetPath(settings)}){
    string destination=backup+System.IO.Path.GetFileName(path);
    if(!System.IO.File.Exists(destination))System.IO.File.Copy(path,destination);
}
// 준비를 늘리고, 이륙 뒤 시간 간격과 잡기 직전 포즈는 보존한다.
float[] times={0f,.14f,.24f,.32f,.44f,.66f,.91f,1.21f};
float[] sourceTimes={0f,.08f,.12f,.16f,.28f,.5f,.75f,1.05f};
var curves=new System.Collections.Generic.Dictionary<string,AnimationCurve>();
foreach(var binding in AnimationUtility.GetCurveBindings(clip))curves[binding.propertyName]=AnimationUtility.GetEditorCurve(clip,binding);
Undo.RecordObjects(new UnityEngine.Object[]{clip,motion,settings},"도약 축적·팔 뻗기 및 선회 완화");
System.Action<string,float[]> write=(name,values)=>{
    var keys=new Keyframe[times.Length];
    for(int i=0;i<times.Length;i++)keys[i]=new Keyframe(times[i],values[i]);
    var curve=new AnimationCurve(keys);
    for(int i=0;i<keys.Length;i++){
        AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);
        AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);
    }
    AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(Animator),name),curve);
};
foreach(var pair in curves){var values=new float[times.Length];for(int i=0;i<times.Length;i++)values[i]=pair.Value.Evaluate(sourceTimes[i]);write(pair.Key,values);}
// 마지막 두 키는 기존 잡기 포즈와 연결한다.
System.Action<string,float[]> pose=(name,first)=>{
    var values=new float[times.Length];System.Array.Copy(first,values,first.Length);
    values[6]=curves[name].Evaluate(.75f);values[7]=curves[name].Evaluate(1.05f);write(name,values);
};
foreach(string side in new[]{"Left ","Right "}){
    pose(side+"Upper Leg Front-Back",new[]{.05f,.48f,.58f,.08f,.10f,.02f});
    pose(side+"Lower Leg Stretch",new[]{.88f,-.35f,-.52f,.90f,.78f,.68f});
    pose(side+"Foot Up-Down",new[]{0f,.18f,.24f,-.22f,-.28f,-.20f});
    pose(side+"Arm Down-Up",new[]{-.62f,-.78f,-.72f,-.32f,.18f,.70f});
    pose(side+"Arm Front-Back",new[]{.12f,-.18f,-.12f,.55f,.75f,.28f});
    pose(side+"Forearm Stretch",new[]{.65f,.38f,.28f,-.20f,.12f,.52f});
    pose(side+"Shoulder Down-Up",new[]{0f,-.08f,-.08f,0f,.08f,.18f});
}
pose("Spine Front-Back",new[]{0f,.16f,.20f,-.06f,-.03f,0f});
float root=curves["RootT.y"].Evaluate(0f);
var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Raon.prefab");
float scale=prefab.GetComponent<Animator>().humanScale;
write("RootT.y",new[]{root,root-.14f/scale,root-.19f/scale,root,root+.025f/scale,root,curves["RootT.y"].Evaluate(.75f),curves["RootT.y"].Evaluate(1.05f)});
var marker=motion.motionSet.motions[0].markers.Find(m=>m.id=="SuperJump.Launch");
if(marker==null)throw new System.Exception("지면 밀기 마커 누락");
marker.normalizedTime=.32f/1.21f;
settings.turnPoseBlendTime=.18f;settings.turnPoseReleaseThreshold=.06f;
foreach(var asset in new UnityEngine.Object[]{clip,motion,settings}){EditorUtility.SetDirty(asset);AssetDatabase.SaveAssetIfDirty(asset);}
float launch;
if(!UPlayGround.State.PlayerSuperJumpGlidingState.TryGetMarkerTime(motion.motionSet,"SuperJump.Launch",out launch)||Mathf.Abs(launch-.32f)>.001f)throw new System.Exception("도약 마커 검증 실패");
return new {clip.length,clip.humanMotion,launch,settings.HasValidValues};
