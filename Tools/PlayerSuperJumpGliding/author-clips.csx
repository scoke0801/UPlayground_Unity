if(EditorApplication.isCompiling||EditorApplication.isUpdating) return "컴파일 또는 임포트 대기";
var folder="Assets/07.Animations/Player/Traversal/SuperJumpGliding";
var proof=AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"/Raon_Hang_Proof.anim");
if(proof==null||!proof.humanMotion)throw new System.Exception("Humanoid 저장·재로드 증명이 먼저 필요합니다.");
foreach(string name in new[]{"Raon_SuperJump","Raon_GliderCatch","Raon_GliderHang","Raon_GliderTurnLeft","Raon_GliderTurnRight"})
    if(System.IO.File.Exists(folder+"/"+name+".anim"))return "기존 제작 클립을 보존합니다: "+name;
var names=HumanTrait.MuscleName;
var hang=new float[names.Length];
for(int i=0;i<names.Length;i++)hang[i]=AnimationUtility.GetEditorCurve(proof,EditorCurveBinding.FloatCurve("",typeof(Animator),names[i])).keys[0].value;
string[] rootNames={"RootT.x","RootT.y","RootT.z","RootQ.x","RootQ.y","RootQ.z","RootQ.w"};
var body=new float[7];for(int i=0;i<7;i++)body[i]=AnimationUtility.GetEditorCurve(proof,EditorCurveBinding.FloatCurve("",typeof(Animator),rootNames[i])).keys[0].value;
var sourcePath=AssetDatabase.GUIDToAssetPath("6bc467d915821294f81fb94c490ac48e");
AnimationClip source=null;
foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(sourcePath)){
    var c=asset as AnimationClip;string g;long id;
    if(c!=null&&AssetDatabase.TryGetGUIDAndLocalFileIdentifier(c,out g,out id)&&id==1827226128182048838L)source=c;
}
if(source==null)throw new System.Exception("조사한 점프 원본 GUID/local ID를 찾지 못했습니다.");
var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Raon.prefab");
var model=UnityEngine.Object.Instantiate(prefab);model.hideFlags=HideFlags.HideAndDontSave;
HumanPoseHandler handler=null;
try{
    var animator=model.GetComponent<Animator>();animator.Rebind();
    var animancer=model.GetComponent<Animancer.AnimancerComponent>();
    handler=new HumanPoseHandler(animator.avatar,animator.transform);
    float[] times={0f,0.08f,0.16f,0.28f,0.5f,0.75f,1.05f};
    float[] sourceTimes={0f,0.1f,0.25f,0.4f,0.55f,0.7f,0.8f};
    float[] reach={0f,0f,0.08f,0.28f,0.6f,0.85f,1f};
    var launch=new float[times.Length][];
    var state=animancer.Play(source);
    for(int key=0;key<times.Length;key++){
        state.Time=sourceTimes[key];animancer.Evaluate(0f);var pose=new HumanPose();handler.GetHumanPose(ref pose);
        launch[key]=(float[])pose.muscles.Clone();
        for(int i=0;i<names.Length;i++){
            bool arm=names[i].Contains("Shoulder")||names[i].Contains("Arm ")||names[i].Contains("Forearm")||names[i].Contains("Hand ")||names[i].Contains("Stretched")||names[i].Contains("Spread");
            if(arm)launch[key][i]=Mathf.Lerp(launch[key][i],hang[i],reach[key]);
        }
    }
    System.Action<float[],string,float> setMuscle=(values,name,value)=>{int i=System.Array.IndexOf(names,name);if(i>=0)values[i]=value;};
    for(int side=0;side<2;side++){
        string prefix=side==0?"Left ":"Right ";
        setMuscle(launch[1],prefix+"Upper Leg Front-Back",0.55f);
        setMuscle(launch[1],prefix+"Lower Leg Stretch",-0.5f);
        setMuscle(launch[2],prefix+"Upper Leg Front-Back",0.08f);
        setMuscle(launch[2],prefix+"Lower Leg Stretch",0.9f);
        setMuscle(hang,prefix+"Upper Leg Front-Back",-0.14f);
        setMuscle(hang,prefix+"Lower Leg Stretch",0.65f);
        setMuscle(hang,prefix+"Foot Up-Down",-0.2f);
    }
    var created=new System.Collections.Generic.List<string>();
    System.Action<string,float[],float[][],float[],bool> write=(name,keys,poses,rootY,loop)=>{
        var clip=new AnimationClip();clip.name=name;clip.frameRate=60f;
        for(int i=0;i<names.Length;i++){
            var frames=new Keyframe[keys.Length];for(int k=0;k<keys.Length;k++)frames[k]=new Keyframe(keys[k],poses[k][i]);
            var curve=new AnimationCurve(frames);for(int k=0;k<keys.Length;k++){AnimationUtility.SetKeyLeftTangentMode(curve,k,AnimationUtility.TangentMode.ClampedAuto);AnimationUtility.SetKeyRightTangentMode(curve,k,AnimationUtility.TangentMode.ClampedAuto);}
            string property=names[i];
            if(property.Contains("Stretched")||property.Contains("Spread")){
                var parts=property.Split(' ');
                property=parts[0]+"Hand."+parts[1]+"."+string.Join(" ",parts.Skip(2).ToArray());
            }
            AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(Animator),property),curve);
        }
        for(int i=0;i<7;i++){
            var frames=new Keyframe[keys.Length];for(int k=0;k<keys.Length;k++)frames[k]=new Keyframe(keys[k],body[i]+(i==1?rootY[k]/animator.humanScale:0f));
            AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(Animator),rootNames[i]),new AnimationCurve(frames));
        }
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;AnimationUtility.SetAnimationClipSettings(clip,settings);clip.EnsureQuaternionContinuity();
        string path=folder+"/"+name+".anim";AssetDatabase.CreateAsset(clip,path);AssetDatabase.SaveAssetIfDirty(clip);created.Add(path);
    };
    write("Raon_SuperJump",times,launch,new[]{0f,-0.08f,0f,0.025f,0f,0f,0f},false);
    var catchPoses=new float[5][];
    catchPoses[0]=(float[])launch[5].Clone();catchPoses[1]=(float[])hang.Clone();catchPoses[2]=(float[])hang.Clone();catchPoses[3]=(float[])hang.Clone();catchPoses[4]=(float[])hang.Clone();
    for(int i=0;i<names.Length;i++)catchPoses[1][i]=Mathf.Lerp(catchPoses[0][i],hang[i],0.82f);
    write("Raon_GliderCatch",new[]{0f,0.12f,0.23f,0.31f,0.46f},catchPoses,new[]{0f,0f,0f,-0.01f,0f},false);
    var loopMiddle=(float[])hang.Clone();
    setMuscle(loopMiddle,"Left Lower Leg Stretch",0.56f);setMuscle(loopMiddle,"Right Lower Leg Stretch",0.7f);
    write("Raon_GliderHang",new[]{0f,0.6f,1.2f},new[]{hang,loopMiddle,hang},new[]{0f,-0.008f,0f},true);
    for(int side=0;side<2;side++){
        var turn=(float[])hang.Clone();var middle=(float[])loopMiddle.Clone();float sign=side==0?-1f:1f;
        setMuscle(turn,"Spine Left-Right",sign*0.14f);setMuscle(middle,"Spine Left-Right",sign*0.16f);
        setMuscle(turn,"Left Upper Leg In-Out",sign*0.1f);setMuscle(turn,"Right Upper Leg In-Out",sign*0.1f);
        write(side==0?"Raon_GliderTurnLeft":"Raon_GliderTurnRight",new[]{0f,0.6f,1.2f},new[]{turn,middle,turn},new[]{0f,-0.008f,0f},true);
    }
    System.IO.File.WriteAllText("output/PlayerSuperJumpGliding/created-clips.txt",string.Join("\n",created.ToArray())+"\nsource="+sourcePath+"\nsourceGuid=6bc467d915821294f81fb94c490ac48e\nsourceLocalId=1827226128182048838\nUnity MCP Humanoid muscle curves only. 외부 원본을 변경하지 않음.");
    return created.ToArray();
}finally{if(handler!=null)handler.Dispose();UnityEngine.Object.DestroyImmediate(model);}
