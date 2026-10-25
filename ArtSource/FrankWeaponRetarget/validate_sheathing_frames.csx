var window=Resources.FindObjectsOfTypeAll<UPlayGround.Animation.Editor.MotionSetEditorWindow>().First();
if(!EditorApplication.isPlaying||window.Subject==null)throw new System.InvalidOperationException("납도 미리보기 재생 대상을 먼저 준비하세요.");
var root=window.Subject.Animancer.transform;
var animator=root.GetComponent<Animator>();
var weapon=root.Find("Weapon/Katana");
var mouth=root.GetComponentsInChildren<Transform>().Single(t=>t.name=="Sword_Case");
var visual=mouth.Find("FrankSheathVisual").GetComponent<MeshRenderer>();
var lines=new System.Collections.Generic.List<string>{"time,grip_m,radial_m,axis_deg,docked_m,sheath_visible"};
int index=0,phase=0,frame=0;float maxGrip=0,maxRadial=0,maxAngle=0,maxDock=0;
bool visible=true;double deadline=EditorApplication.timeSinceStartup+55;
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>{
    try {
        if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup>deadline)throw new System.InvalidOperationException("검사 중단 또는 시간 초과");
        float time=index/60f;
        if(phase==0){window.SetPlaybackTime(time);frame=Time.frameCount;phase=1;return;}
        if(Time.frameCount<frame+2)return;
        Vector3 guard=weapon.TransformPoint(new Vector3(0,0.127f,0));
        Vector3 grip=(animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal).position+animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal).position)*0.5f;
        float gripError=Vector3.Distance(grip,weapon.position),radial=Vector3.Cross(mouth.position-guard,weapon.up).magnitude;
        float angle=Vector3.Angle(weapon.up,mouth.forward),dock=Vector3.Distance(guard,mouth.position);
        if(time<1.83f)maxGrip=Mathf.Max(maxGrip,gripError);
        if(time>=1.2f&&time<=1.83f){maxRadial=Mathf.Max(maxRadial,radial);maxAngle=Mathf.Max(maxAngle,angle);}
        if(time>=2f)maxDock=Mathf.Max(maxDock,dock);
        visible&=visual.enabled&&visual.gameObject.activeInHierarchy&&visual.transform.lossyScale.sqrMagnitude>0.1f;
        lines.Add(time+","+gripError+","+radial+","+angle+","+dock+","+visual.enabled);
        index++;phase=0;
        if(index<=170)return;
        EditorApplication.update-=tick;
        System.IO.File.WriteAllLines("output/FrankWeaponRetarget/SheathingCorrection/frame-measurements.csv",lines.ToArray());
        bool passed=maxGrip<0.002f&&maxRadial<0.002f&&maxAngle<0.2f&&maxDock<0.002f&&visible;
        System.IO.File.WriteAllText("output/FrankWeaponRetarget/SheathingCorrection/frame-validation.txt",
            "Passed="+passed+"\nFrames="+index+"\nGripError="+maxGrip+"\nInsertionRadialError="+maxRadial+"\nInsertionAxisError="+maxAngle+"\nDockedGuardError="+maxDock+"\nSheathVisible="+visible);
        window.SetPlaybackTime(1.55f);
    } catch(System.Exception e){EditorApplication.update-=tick;System.IO.File.WriteAllText("output/FrankWeaponRetarget/SheathingCorrection/frame-validation.txt",e.ToString());}
};
EditorApplication.update+=tick;
return "171 프레임의 실제 Constraint/메시 위치 검사 시작";
