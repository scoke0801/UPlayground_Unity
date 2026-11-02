if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new System.Exception("편집 모드에서 실행하세요.");
string folder="Assets/07.Animations/Player/Traversal/SuperJumpGliding/";
var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Raon.prefab");
var reference=prefab.GetComponent<UPlayGround.Animation.PlayerActorAnimator>().PlayerMotionSet.GetMotionSet(UPlayGround.Data.EnumType.WeaponType.NoWeapon,UPlayGround.Data.Actor.Animation.MotionTags.Jump).motions[0].motionClip;
var launch=AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"Raon_SuperJump.anim");
string backup="output/PlayerSuperJumpGliding/review2/before/";System.IO.Directory.CreateDirectory(backup);
foreach(string name in new[]{"Raon_SuperJump","Raon_GliderCatch","Raon_GliderHang","Raon_GliderTurnLeft","Raon_GliderTurnRight"})if(!System.IO.File.Exists(backup+name+".anim"))System.IO.File.Copy(folder+name+".anim",backup+name+".anim");
var model=UnityEngine.Object.Instantiate(prefab);model.hideFlags=HideFlags.HideAndDontSave;model.SetActive(true);
var glider=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Traversal/PlayerGlider.prefab"));glider.hideFlags=HideFlags.HideAndDontSave;
HumanPoseHandler handler=null;
try{
    var animator=model.GetComponent<Animator>();animator.Rebind();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.applyRootMotion=false;
    var ac=model.GetComponent<Animancer.AnimancerComponent>();handler=new HumanPoseHandler(animator.avatar,animator.transform);
    var binding=model.GetComponent<UPlayGround.Components.PlayerGliderBinding>();
    var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
    typeof(UPlayGround.Components.PlayerGliderBinding).GetMethod("Awake",flags).Invoke(binding,null);binding.BeginTraversal();
    System.Func<AnimationClip,float,HumanPose> sample=(clip,time)=>{var state=ac.Play(clip,0f);state.Weight=1f;state.Time=time;ac.Evaluate(.00001f);var pose=new HumanPose();handler.GetHumanPose(ref pose);return pose;};
    System.Action<AnimationClip,string,float[],float[]> write=(clip,name,keyTimes,values)=>{
        var keys=new Keyframe[keyTimes.Length];for(int k=0;k<keys.Length;k++)keys[k]=new Keyframe(keyTimes[k],values[k]);
        var curve=new AnimationCurve(keys);for(int k=0;k<keys.Length;k++){AnimationUtility.SetKeyLeftTangentMode(curve,k,AnimationUtility.TangentMode.ClampedAuto);AnimationUtility.SetKeyRightTangentMode(curve,k,AnimationUtility.TangentMode.ClampedAuto);}
        AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(Animator),name),curve);
    };
    float[] times={0f,.04f,.08f,.12f,.16f,.20f,.24f,.28f,.32f,.40f,.48f,.56f,.64f,.72f,.82f,.91f,1.21f};
    var poses=new HumanPose[times.Length];
    for(int k=0;k<times.Length;k++){
        float t=times[k];var original=sample(launch,t);
        float sourceTime=t<=.32f?t*(.20f/.32f):Mathf.Min(.4666f,.20f+(t-.32f)*(.2666f/.4f));
        var source=sample(reference,sourceTime);
        float weight=Mathf.SmoothStep(0f,1f,Mathf.Clamp01(t/.08f))*(1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.64f,.91f,t)));
        for(int i=0;i<HumanTrait.MuscleCount;i++){
            string n=HumanTrait.MuscleName[i];
            if(n.Contains("Leg ")||n.Contains("Foot ")||n.Contains("Toes")||n.Contains("Spine")||n.Contains("Chest"))original.muscles[i]=Mathf.Lerp(original.muscles[i],source.muscles[i],weight);
        }
        original.bodyPosition=Vector3.Lerp(original.bodyPosition,source.bodyPosition,weight);poses[k]=original;
    }
    Undo.RecordObject(launch,"기존 점프 기반 도약 하체 재저작");
    for(int i=0;i<HumanTrait.MuscleCount;i++){
        string n=HumanTrait.MuscleName[i];if(!n.Contains("Leg ")&&!n.Contains("Foot ")&&!n.Contains("Toes")&&!n.Contains("Spine")&&!n.Contains("Chest"))continue;
        var values=new float[times.Length];for(int k=0;k<times.Length;k++)values[k]=poses[k].muscles[i];write(launch,n,times,values);
    }
    foreach(int axis in new[]{0,1,2}){var values=new float[times.Length];for(int k=0;k<times.Length;k++)values[k]=poses[k].bodyPosition[axis];write(launch,"RootT."+new[]{"x","y","z"}[axis],times,values);}
    var settings=AnimationUtility.GetAnimationClipSettings(launch);settings.loopBlendPositionY=true;settings.loopBlendPositionXZ=true;AnimationUtility.SetAnimationClipSettings(launch,settings);AssetDatabase.SaveAssetIfDirty(launch);
    var view=glider.GetComponent<UPlayGround.Components.PlayerGliderView>();binding.Attach(view);
    var solve=typeof(UPlayGround.Components.PlayerGliderBinding).GetMethod("OnAnimatorIK",flags);
    foreach(string name in new[]{"Raon_GliderCatch","Raon_GliderHang","Raon_GliderTurnLeft","Raon_GliderTurnRight"}){
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+name+".anim");Undo.RecordObject(clip,"그립 접촉 팔 포즈 베이크");
        int count=Mathf.CeilToInt(clip.length*60f)+1;var keys=new float[count];var samples=new float[count][];
        for(int k=0;k<count;k++){
            float t=clip.length*k/(count-1);keys[k]=t;var pose=sample(clip,t);
            float reach=name=="Raon_GliderCatch"?Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.12f,.23f,t)):1f;
            // 팔꿈치를 과신전하지 않고 손잡이에 닿도록 매달리는 어깨도 들어 올린다.
            foreach(string side in new[]{"Left ","Right "}){
                int index=System.Array.IndexOf(HumanTrait.MuscleName,side+"Shoulder Down-Up");
                pose.muscles[index]=Mathf.Lerp(pose.muscles[index],.35f,reach);
            }
            handler.SetHumanPose(ref pose);
            if(name!="Raon_GliderCatch"||t>=.23f){solve.Invoke(binding,new object[]{0});handler.GetHumanPose(ref pose);}
            samples[k]=(float[])pose.muscles.Clone();
        }
        for(int i=0;i<HumanTrait.MuscleCount;i++){
            string n=HumanTrait.MuscleName[i];if(!n.Contains("Shoulder")&&!n.Contains("Arm ")&&!n.Contains("Forearm")&&!n.Contains(" Hand "))continue;
            var values=new float[count];for(int k=0;k<count;k++)values[k]=samples[k][i];write(clip,n,keys,values);
        }
        EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);
    }
    binding.EndTraversal();return "기존 점프 하체·몸통 이식 및 잡기·기본·좌우 선회 팔 접촉 베이크";
}finally{if(handler!=null)handler.Dispose();UnityEngine.Object.DestroyImmediate(model);UnityEngine.Object.DestroyImmediate(glider);}
