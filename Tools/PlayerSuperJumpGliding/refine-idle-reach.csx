if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new System.Exception("편집 모드에서 실행하세요.");
string folder="Assets/07.Animations/Player/Traversal/SuperJumpGliding/";
var idle=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/07.Animations/Humanoid_Relaxed_Idle.anim");
var launch=AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"Raon_SuperJump.anim");
var hang=AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"Raon_GliderHang.anim");
var names=new[]{"Raon_SuperJump","Raon_GliderCatch","Raon_GliderHang","Raon_GliderTurnLeft","Raon_GliderTurnRight"};
string backup="output/PlayerSuperJumpGliding/before-idle-reach/";System.IO.Directory.CreateDirectory(backup);
foreach(string name in names){string p=folder+name+".anim";if(!System.IO.File.Exists(backup+name+".anim"))System.IO.File.Copy(p,backup+name+".anim");}
// HumanTrait의 표시 이름과 Animator의 손가락 바인딩 이름은 다르다.
System.Func<string,string> bindingName=name=>{
    var parts=name.Split(' ');
    if(parts.Length>=3&&(parts[1]=="Thumb"||parts[1]=="Index"||parts[1]=="Middle"||parts[1]=="Ring"||parts[1]=="Little"))
        return parts[0]+"Hand."+parts[1]+"."+string.Join(" ",parts.Skip(2).ToArray());
    return name;
};
System.Action<AnimationClip,string,float[],float[]> write=(clip,name,times,values)=>{
    var keys=new Keyframe[times.Length];for(int i=0;i<times.Length;i++)keys[i]=new Keyframe(times[i],values[i]);
    var curve=new AnimationCurve(keys);for(int i=0;i<times.Length;i++){
        AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);
        AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);
    }
    AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(Animator),name),curve);
};
float[] reachTimes={0f,.24f,.32f,.44f,.66f,.91f,1.21f};
float[] reach={0f,0f,.06f,.25f,.65f,.92f,1f};
Undo.RecordObject(launch,"Idle에서 손잡이로 팔 올리기");
foreach(string name in HumanTrait.MuscleName){
    bool arm=name.Contains("Shoulder")||name.Contains("Arm ")||name.Contains("Forearm")||name.Contains("Hand ");
    if(!arm)continue;
    var b=EditorCurveBinding.FloatCurve("",typeof(Animator),name);
    var start=AnimationUtility.GetEditorCurve(idle,b);var end=AnimationUtility.GetEditorCurve(hang,b);
    if(start==null||end==null)throw new System.Exception("팔 커브 누락: "+name);
    var values=new float[reachTimes.Length];for(int i=0;i<values.Length;i++)values[i]=Mathf.Lerp(start.Evaluate(0f),end.Evaluate(0f),reach[i]);
    write(launch,name,reachTimes,values);
}
foreach(string clipName in names){
    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+clipName+".anim");Undo.RecordObject(clip,"손가락 그립 바인딩 수정");
    foreach(string name in HumanTrait.MuscleName){
        string property=bindingName(name);if(property==name)continue;
        var old=EditorCurveBinding.FloatCurve("",typeof(Animator),name);
        AnimationUtility.SetEditorCurve(clip,old,null);
        bool thumb=name.Contains("Thumb");
        float grip=name.Contains("Spread")?(thumb?-.2f:0f):thumb?-.35f:name.Contains("1 Stretched")?-.45f:name.Contains("2 Stretched")?-.9f:-.8f;
        var idleCurve=AnimationUtility.GetEditorCurve(idle,EditorCurveBinding.FloatCurve("",typeof(Animator),property));
        float relaxed=idleCurve.Evaluate(0f);
        if(clipName=="Raon_SuperJump")write(clip,property,new[]{0f,.66f,.91f,1.21f},new[]{relaxed,relaxed,relaxed,relaxed});
        else if(clipName=="Raon_GliderCatch")write(clip,property,new[]{0f,.12f,.23f,.31f,.46f},new[]{relaxed,relaxed,grip,grip,grip});
        else write(clip,property,new[]{0f,clip.length},new[]{grip,grip});
    }
    EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);
}
string prefabPath="Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Raon.prefab";
if(!System.IO.File.Exists(backup+"PlayerModel_Raon.prefab"))System.IO.File.Copy(prefabPath,backup+"PlayerModel_Raon.prefab");
var model=PrefabUtility.LoadPrefabContents(prefabPath);
try{
    // 손바닥이 손잡이 중심을 관통하지 않도록 표면 두께를 반영한 모델 전용 접촉점.
    foreach(var t in model.GetComponentsInChildren<Transform>(true)){
        if(t.name=="GliderPalmLeft")t.localPosition=new Vector3(-.02305761f,.03955951f,.00231310f);
        if(t.name=="GliderPalmRight")t.localPosition=new Vector3(.02305558f,-.03956059f,-.00231445f);
    }
    PrefabUtility.SaveAsPrefabAsset(model,prefabPath);
}finally{PrefabUtility.UnloadPrefabContents(model);}
return "Idle 팔 커브 기반 도약 및 5개 클립의 손가락 바인딩 수정";
