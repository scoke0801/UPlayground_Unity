if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new System.Exception("편집 모드에서 실행하세요.");
string folder="Assets/07.Animations/Player/Traversal/SuperJumpGliding/";
var launch=AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"Raon_SuperJump.anim");
var catching=AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"Raon_GliderCatch.anim");
var idle=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/07.Animations/Humanoid_Relaxed_Idle.anim");
string backup="output/PlayerSuperJumpGliding/video-review/before/";System.IO.Directory.CreateDirectory(backup);
foreach(var c in new[]{launch,catching})if(!System.IO.File.Exists(backup+c.name+".anim"))System.IO.File.Copy(AssetDatabase.GetAssetPath(c),backup+c.name+".anim");
var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Raon.prefab"));
model.hideFlags=HideFlags.HideAndDontSave;model.SetActive(true);
var hint=new GameObject("도약 무릎 저작 기준");hint.hideFlags=HideFlags.HideAndDontSave;
HumanPoseHandler handler=null;
try{
    var animator=model.GetComponent<Animator>();animator.Rebind();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
    var animancer=model.GetComponent<Animancer.AnimancerComponent>();
    var idleState=animancer.Play(idle,0f);idleState.Weight=1f;idleState.Time=0f;animancer.Evaluate(.00001f);
    handler=new HumanPoseHandler(animator.avatar,animator.transform);
    var idlePose=new HumanPose();handler.GetHumanPose(ref idlePose);
    var feet=new[]{animator.GetBoneTransform(HumanBodyBones.LeftFoot),animator.GetBoneTransform(HumanBodyBones.RightFoot)};
    var footPositions=new[]{feet[0].position,feet[1].position};var footRotations=new[]{feet[0].rotation,feet[1].rotation};
    float[] times={0f,.08f,.18f,.24f,.28f,.32f,.40f,.55f,.70f,.91f,1.21f};
    float[] depth={0f,.035f,.13f,.17f,.09f,0f,0f,0f,0f,0f,0f};
    var poses=new HumanPose[times.Length];
    var state=animancer.Play(launch,0f);state.Weight=1f;
    for(int k=0;k<times.Length;k++){
        state.Time=times[k];animancer.Evaluate(.00001f);
        var pose=new HumanPose();handler.GetHumanPose(ref pose);
        if(times[k]<=.32f){
            for(int i=0;i<HumanTrait.MuscleCount;i++){
                string name=HumanTrait.MuscleName[i];
                if(name.Contains("Leg ")||name.Contains("Foot ")||name.Contains("Toes"))pose.muscles[i]=idlePose.muscles[i];
            }
            pose.bodyPosition=idlePose.bodyPosition-Vector3.up*(depth[k]/animator.humanScale);
            handler.SetHumanPose(ref pose);
            for(int side=0;side<2;side++){
                bool left=side==0;
                var upper=animator.GetBoneTransform(left?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg);
                var lower=animator.GetBoneTransform(left?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg);
                hint.transform.position=footPositions[side]+new Vector3(0f,.35f,.6f);
                KINEMATION.MotionWarping.Runtime.Core.TwoBoneIk.SolveTwoBoneIK(upper,lower,feet[side],new System.ValueTuple<Vector3,Quaternion>(footPositions[side],footRotations[side]),hint.transform,1f,1f);
            }
            handler.GetHumanPose(ref pose);
        }
        poses[k]=pose;
    }
    Undo.RecordObjects(new UnityEngine.Object[]{launch,catching},"영상 기준 도약 접지와 잡기 연결 수정");
    System.Action<AnimationClip,string,float[],float[]> write=(clip,name,keys,values)=>{
        var frames=new Keyframe[keys.Length];for(int k=0;k<keys.Length;k++)frames[k]=new Keyframe(keys[k],values[k]);
        var curve=new AnimationCurve(frames);for(int k=0;k<keys.Length;k++){
            AnimationUtility.SetKeyLeftTangentMode(curve,k,AnimationUtility.TangentMode.ClampedAuto);
            AnimationUtility.SetKeyRightTangentMode(curve,k,AnimationUtility.TangentMode.ClampedAuto);
        }
        AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(Animator),name),curve);
    };
    for(int i=0;i<HumanTrait.MuscleCount;i++){
        string name=HumanTrait.MuscleName[i];
        if(!name.Contains("Leg ")&&!name.Contains("Foot ")&&!name.Contains("Toes"))continue;
        var values=new float[times.Length];for(int k=0;k<times.Length;k++)values[k]=poses[k].muscles[i];
        write(launch,name,times,values);
    }
    var bodyY=new float[times.Length];
    var originalY=AnimationUtility.GetEditorCurve(launch,EditorCurveBinding.FloatCurve("",typeof(Animator),"RootT.y"));
    for(int k=0;k<times.Length;k++)bodyY[k]=times[k]<=.32f?poses[k].bodyPosition.y:originalY.Evaluate(times[k]);
    write(launch,"RootT.y",times,bodyY);
    var settings=AnimationUtility.GetAnimationClipSettings(launch);settings.loopBlendPositionY=true;AnimationUtility.SetAnimationClipSettings(launch,settings);
    // 잡기 시작 포즈는 실제 도약 종료 구간에서 가져와 팔이 반대로 꺾이지 않게 한다.
    foreach(var b in AnimationUtility.GetCurveBindings(catching)){
        string n=b.propertyName;
        if(!n.Contains("Shoulder")&&!n.Contains("Arm ")&&!n.Contains("Forearm")&&!n.Contains(" Hand "))continue;
        var source=AnimationUtility.GetEditorCurve(launch,b);if(source==null)continue;
        var target=AnimationUtility.GetEditorCurve(catching,b);
        float start=source.Evaluate(.91f);float end=target.Evaluate(.23f);
        write(catching,n,new[]{0f,.12f,.23f,.31f,.46f},new[]{start,Mathf.Lerp(start,end,.6f),end,end,end});
    }
    foreach(var c in new[]{launch,catching}){EditorUtility.SetDirty(c);AssetDatabase.SaveAssetIfDirty(c);}
    return "도약 준비 발 고정·골반 하강 베이크 및 잡기 시작 팔 연결 수정";
}finally{if(handler!=null)handler.Dispose();UnityEngine.Object.DestroyImmediate(hint);UnityEngine.Object.DestroyImmediate(model);}
