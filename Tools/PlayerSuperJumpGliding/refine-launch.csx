if(EditorApplication.isPlaying||EditorApplication.isCompiling) throw new System.Exception("편집 모드에서 실행하세요.");
string path="Assets/07.Animations/Player/Traversal/SuperJumpGliding/Raon_SuperJump.anim";
var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
System.IO.Directory.CreateDirectory("output/PlayerSuperJumpGliding");
string backup="output/PlayerSuperJumpGliding/Raon_SuperJump.before-arm-refinement.anim";
if(!System.IO.File.Exists(backup))System.IO.File.Copy(path,backup);
var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Raon.prefab"));
model.hideFlags=HideFlags.HideAndDontSave;
var hint=new GameObject("도약 팔꿈치 저작 기준");hint.hideFlags=HideFlags.HideAndDontSave;
HumanPoseHandler handler=null;
try{
    var animator=model.GetComponent<Animator>();animator.Rebind();
    var animancer=model.GetComponent<Animancer.AnimancerComponent>();
    handler=new HumanPoseHandler(animator.avatar,animator.transform);
    var state=animancer.Play(clip);
    float[] times={0f,0.08f,0.16f,0.28f,0.5f};
    // 몸 옆 준비 → 뒤로 당기기 → 가슴 앞으로 추진 → 위로 뻗기.
    Vector3[] hands={new Vector3(.23f,.94f,.04f),new Vector3(.24f,.86f,-.19f),new Vector3(.23f,1.18f,.30f),new Vector3(.24f,1.43f,.29f),new Vector3(.24f,1.53f,.16f)};
    var poses=new float[times.Length][];
    for(int k=0;k<times.Length;k++){
        state.Time=times[k];animancer.Evaluate(0f);
        for(int side=0;side<2;side++){
            bool left=side==0;float sign=left?-1f:1f;
            var upper=animator.GetBoneTransform(left?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm);
            var lower=animator.GetBoneTransform(left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);
            var hand=animator.GetBoneTransform(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
            var target=hands[k];target.x*=sign;
            hint.transform.position=model.transform.TransformPoint(new Vector3(sign*.34f,k<2?.99f:1.16f,-.12f));
            KINEMATION.MotionWarping.Runtime.Core.TwoBoneIk.SolveTwoBoneIK(upper,lower,hand,new System.ValueTuple<Vector3,Quaternion>(model.transform.TransformPoint(target),hand.rotation),hint.transform,1f,0f);
        }
        var pose=new HumanPose();handler.GetHumanPose(ref pose);poses[k]=(float[])pose.muscles.Clone();
    }
    Undo.RecordObject(clip,"슈퍼 점프 팔 동작 개선");
    int changed=0;
    for(int i=0;i<HumanTrait.MuscleCount;i++){
        string name=HumanTrait.MuscleName[i];
        if(!name.Contains("Arm ")&&!name.Contains("Forearm")&&!name.Contains("Shoulder"))continue;
        var binding=EditorCurveBinding.FloatCurve("",typeof(Animator),name);
        var curve=AnimationUtility.GetEditorCurve(clip,binding);var keys=curve.keys;
        for(int k=0;k<times.Length;k++){
            if(Mathf.Abs(keys[k].time-times[k])>.0001f)throw new System.Exception("도약 키 시각 불일치");
            keys[k].value=poses[k][i];
        }
        curve.keys=keys;
        for(int k=0;k<keys.Length;k++){
            AnimationUtility.SetKeyLeftTangentMode(curve,k,AnimationUtility.TangentMode.ClampedAuto);
            AnimationUtility.SetKeyRightTangentMode(curve,k,AnimationUtility.TangentMode.ClampedAuto);
        }
        AnimationUtility.SetEditorCurve(clip,binding,curve);changed++;
    }
    EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);
    return new {changed,clip.length,clip.humanMotion};
}finally{if(handler!=null)handler.Dispose();UnityEngine.Object.DestroyImmediate(hint);UnityEngine.Object.DestroyImmediate(model);}
