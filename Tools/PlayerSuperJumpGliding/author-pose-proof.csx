if(EditorApplication.isCompiling||EditorApplication.isUpdating)return "컴파일 또는 임포트 대기";
var folder="Assets/07.Animations/Player/Traversal/SuperJumpGliding";
System.IO.Directory.CreateDirectory(folder);
var path=folder+"/Raon_Hang_Proof.anim";
if(System.IO.File.Exists(path))return "기존 저작 증명을 보존합니다: "+path;
var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Raon.prefab");
var root=UnityEngine.Object.Instantiate(prefab);root.hideFlags=HideFlags.HideAndDontSave;
UnityEngine.HumanPoseHandler handler=null;
var hints=new System.Collections.Generic.List<GameObject>();
try {
    root.SetActive(true);
    var a=root.GetComponent<Animator>();a.Rebind();
    handler=new HumanPoseHandler(a.avatar,a.transform);
    var pose=new HumanPose();handler.GetHumanPose(ref pose);
    var report=new System.Text.StringBuilder("손바닥 기준: 손 본과 가운데손가락 첫 마디 사이 55%. 기준 루트: KCC 발밑.\n");
    for(int side=0;side<2;side++){
        bool left=side==0;float sign=left?-1f:1f;
        var upper=a.GetBoneTransform(left?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm);
        var lower=a.GetBoneTransform(left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);
        var hand=a.GetBoneTransform(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
        var middle=a.GetBoneTransform(left?HumanBodyBones.LeftMiddleProximal:HumanBodyBones.RightMiddleProximal);
        Vector3 palmLocal=hand.InverseTransformPoint(Vector3.Lerp(hand.position,middle.position,0.55f));
        Quaternion handRotation=Quaternion.FromToRotation((middle.position-hand.position).normalized,Vector3.forward)*hand.rotation;
        Vector3 grip=new Vector3(sign*0.24f,1.58f,0.08f);
        var hint=new GameObject("TraversalPoseHint");hints.Add(hint);hint.hideFlags=HideFlags.HideAndDontSave;hint.transform.position=new Vector3(sign*0.44f,1.31f,-0.1f);
        KINEMATION.MotionWarping.Runtime.Core.TwoBoneIk.SolveTwoBoneIK(upper,lower,hand,new System.ValueTuple<Vector3,Quaternion>(grip-handRotation*palmLocal,handRotation),hint.transform,1f,1f);
        report.AppendLine((left?"Left":"Right")+"\tgrip="+grip.ToString("F5")+"\tpalmLocal="+palmLocal.ToString("F5")+"\thandRotation="+hand.rotation.ToString("F5"));
    }
    handler.GetHumanPose(ref pose);
    for(int i=0;i<HumanTrait.MuscleCount;i++){
        string muscle=HumanTrait.MuscleName[i];
        if(muscle.Contains("Stretched"))pose.muscles[i]=-0.55f;
        if(muscle.Contains("Spread"))pose.muscles[i]=0f;
    }
    handler.SetHumanPose(ref pose);
    var clip=new AnimationClip();clip.name="Raon_Hang_Proof";clip.frameRate=60f;
    for(int i=0;i<HumanTrait.MuscleCount;i++)AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(Animator),HumanTrait.MuscleName[i]),AnimationCurve.Linear(0f,pose.muscles[i],1f,pose.muscles[i]));
    float[] roots={pose.bodyPosition.x,pose.bodyPosition.y,pose.bodyPosition.z,pose.bodyRotation.x,pose.bodyRotation.y,pose.bodyRotation.z,pose.bodyRotation.w};
    string[] names={"RootT.x","RootT.y","RootT.z","RootQ.x","RootQ.y","RootQ.z","RootQ.w"};
    for(int i=0;i<names.Length;i++)AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(Animator),names[i]),AnimationCurve.Linear(0f,roots[i],1f,roots[i]));
    var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,settings);
    clip.EnsureQuaternionContinuity();
    AssetDatabase.CreateAsset(clip,path);AssetDatabase.SaveAssetIfDirty(clip);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
    clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
    var animancer=root.GetComponent<Animancer.AnimancerComponent>();
    var state=animancer.Play(clip);state.Time=0.5f;animancer.Evaluate(0f);
    for(int side=0;side<2;side++){
        bool left=side==0;var hand=a.GetBoneTransform(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);var middle=a.GetBoneTransform(left?HumanBodyBones.LeftMiddleProximal:HumanBodyBones.RightMiddleProximal);
        Vector3 palm=Vector3.Lerp(hand.position,middle.position,0.55f);Vector3 grip=new Vector3(left?-0.24f:0.24f,1.58f,0.08f);
        report.AppendLine((left?"Left":"Right")+"\treloadPalm="+palm.ToString("F5")+"\terror="+Vector3.Distance(palm,grip).ToString("F6"));
    }
    report.AppendLine("humanMotion="+clip.humanMotion+"\tlength="+clip.length+"\tbindings="+AnimationUtility.GetCurveBindings(clip).Length);
    System.IO.Directory.CreateDirectory("output/PlayerSuperJumpGliding");
    System.IO.File.WriteAllText("output/PlayerSuperJumpGliding/pose-proof.txt",report.ToString());
    System.IO.File.WriteAllLines("output/PlayerSuperJumpGliding/muscles.txt",HumanTrait.MuscleName);
    return report.ToString();
}finally{if(handler!=null)handler.Dispose();foreach(var h in hints)UnityEngine.Object.DestroyImmediate(h);UnityEngine.Object.DestroyImmediate(root);}
