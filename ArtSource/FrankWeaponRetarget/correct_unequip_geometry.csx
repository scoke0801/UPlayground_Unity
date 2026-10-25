// Humanoid 기준 자세의 본 축을 비교해 납도 포즈를 굽는다. 런타임 보정 코드는 추가하지 않는다.
const string sourcePath = "Assets/ExternalAssets/AnimationOnly/Frank_Slash_Pack/Assets/Animations/Frank_SlashPack_Katana/FBX_Animation/In_Place/Frank_RPG_Katana_Unequip.FBX";
const string clipPath = "Assets/07.Animations/Player/Raon/FrankWeapon/Raon_Frank_RPG_Katana_Unequip.anim";
const string prefabPath = "Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Raon.prefab";
if (EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed) throw new System.InvalidOperationException("컴파일 오류");
System.IO.Directory.CreateDirectory("output/FrankWeaponRetarget/SheathingCorrection");
foreach (string p in new[]{clipPath,prefabPath}) {
    string backup="output/FrankWeaponRetarget/SheathingCorrection/"+System.IO.Path.GetFileName(p)+".backup";
    if(!System.IO.File.Exists(backup)) System.IO.File.Copy(p,backup);
}
var source = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath));
source.hideFlags=HideFlags.HideAndDontSave;
var avatar=AvatarBuilder.BuildHumanAvatar(source,((ModelImporter)AssetImporter.GetAtPath(sourcePath)).humanDescription);
var sa=source.AddComponent<Animator>(); sa.avatar=avatar;sa.cullingMode=AnimatorCullingMode.AlwaysAnimate;
var target=PrefabUtility.LoadPrefabContents(prefabPath);
var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
var sample=UnityEngine.Object.Instantiate(clip);
var targetGraph=UnityEngine.Playables.PlayableGraph.Create("Raon target bake");
var original=AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<AnimationClip>().Single(c=>c.name.StartsWith("__"));
System.Func<GameObject,string,Transform> find=(r,n)=>r.GetComponentsInChildren<Transform>(true).Single(t=>t.name==n);
try {
    foreach(var b in target.GetComponentsInChildren<MonoBehaviour>(true)) if(b!=null)b.enabled=false;
    foreach(var c in target.GetComponentsInChildren<UnityEngine.Animations.ParentConstraint>(true))c.enabled=false;
    target.SetActive(true);
    var ta=target.GetComponent<Animator>();ta.enabled=true;ta.cullingMode=AnimatorCullingMode.AlwaysAnimate;
    var sr=sa.GetBoneTransform(HumanBodyBones.RightHand);var sl=sa.GetBoneTransform(HumanBodyBones.LeftHand);var sh=sa.GetBoneTransform(HumanBodyBones.Hips);
    var tr=ta.GetBoneTransform(HumanBodyBones.RightHand);var tl=ta.GetBoneTransform(HumanBodyBones.LeftHand);var th=ta.GetBoneTransform(HumanBodyBones.Hips);
    using(var a=new HumanPoseHandler(avatar,source.transform)) using(var b=new HumanPoseHandler(ta.avatar,target.transform)) {
        var pose=new HumanPose{bodyPosition=Vector3.zero,bodyRotation=Quaternion.identity,muscles=new float[HumanTrait.MuscleCount]};
        a.SetHumanPose(ref pose);b.SetHumanPose(ref pose);
    }
    Quaternion rightBasis=Quaternion.Inverse(tr.rotation)*sr.rotation;
    Quaternion leftBasis=Quaternion.Inverse(tl.rotation)*sl.rotation;
    Quaternion hipBasis=Quaternion.Inverse(th.rotation)*sh.rotation;
    float ratio=ta.humanScale/sa.humanScale;
    var sourceBlade=find(source,"Weapon_Blade");var sourceSword=find(source,"Weapon_Sword");var sourceCase=find(source,"Sword_Case");
    var blade=find(target,"Weapon_Blade");var sword=find(target,"Weapon_Sword");var sheath=find(target,"Sword_Case");
    var oldRenderer=target.transform.Find("Weapon_back/Katana_Holder/School_Katana_Weapon/Weapon_Katana_sheath").GetComponent<SkinnedMeshRenderer>();
    var visual=sheath.Find("FrankSheathVisual");
    if(visual==null) {visual=new GameObject("FrankSheathVisual").transform;visual.SetParent(sheath,false);visual.gameObject.AddComponent<MeshFilter>();visual.gameObject.AddComponent<MeshRenderer>();}
    visual.GetComponent<MeshFilter>().sharedMesh=oldRenderer.sharedMesh;
    visual.GetComponent<MeshRenderer>().sharedMaterials=oldRenderer.sharedMaterials;
    visual.GetComponent<MeshRenderer>().enabled=true;
    Bounds bounds=oldRenderer.sharedMesh.bounds;
    visual.localPosition=new Vector3(-bounds.center.x,-bounds.center.y,-bounds.min.z);
    visual.localRotation=Quaternion.identity;visual.localScale=Vector3.zero;
    string bp=AnimationUtility.CalculateTransformPath(blade,target.transform),cp=AnimationUtility.CalculateTransformPath(sheath,target.transform),sp=AnimationUtility.CalculateTransformPath(sword,target.transform);
    string[] props={"m_LocalPosition.x","m_LocalPosition.y","m_LocalPosition.z","m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z","m_LocalRotation.w"};
    var bladeCurves=props.Select(_=>new AnimationCurve()).ToArray();var caseCurves=props.Select(_=>new AnimationCurve()).ToArray();
    foreach(string axis in new[]{"x","y","z"}) AnimationUtility.SetEditorCurve(sample,EditorCurveBinding.FloatCurve(sp,typeof(Transform),"m_LocalScale."+axis),AnimationCurve.Constant(0,clip.length,1));
    sa.Rebind();ta.Rebind();
    targetGraph.SetTimeUpdateMode(UnityEngine.Playables.DirectorUpdateMode.Manual);
    var targetPlayable=UnityEngine.Animations.AnimationClipPlayable.Create(targetGraph,sample);
    targetPlayable.SetApplyFootIK(false);
    UnityEngine.Playables.PlayableExtensions.SetDuration(targetPlayable,sample.length);
    var targetOutput=UnityEngine.Animations.AnimationPlayableOutput.Create(targetGraph,"target",ta);
    UnityEngine.Playables.PlayableOutputExtensions.SetSourcePlayable(targetOutput,targetPlayable);
    targetGraph.Play();
    System.Action<float> evaluate=time=>{
        UnityEngine.Playables.PlayableExtensions.SetTime(targetPlayable,time);
        UnityEngine.Playables.PlayableExtensions.SetDone(targetPlayable,false);
        original.SampleAnimation(source,time);targetGraph.Evaluate(0);
    };
    System.Func<HumanBodyBones,HumanBodyBones,Vector3> palm=(a,b)=>(ta.GetBoneTransform(a).position+ta.GetBoneTransform(b).position)*0.5f;
    evaluate(2.05f);
    Quaternion restHipMap=th.rotation*hipBasis*Quaternion.Inverse(sh.rotation);
    Vector3 restCaseAxis=restHipMap*(-sourceCase.right);
    Vector3 heldMouth=palm(HumanBodyBones.LeftMiddleProximal,HumanBodyBones.LeftMiddleDistal)-restCaseAxis*0.035f;
    Vector3 nativeMouth=th.position+restHipMap*(sourceCase.position-sh.position)*ratio;
    Vector3 sheathOffset=Quaternion.Inverse(restHipMap*sourceCase.rotation)*(heldMouth-nativeMouth);
    float maxAxisError=0;float maxRadialError=0;
    for(int frame=0;frame<=Mathf.CeilToInt(clip.length*60);frame++) {
        float time=Mathf.Min(frame/60f,clip.length);
        evaluate(time);
        Quaternion rm=tr.rotation*rightBasis*Quaternion.Inverse(sr.rotation);
        Quaternion lm=tl.rotation*leftBasis*Quaternion.Inverse(sl.rotation);
        Quaternion hm=th.rotation*hipBasis*Quaternion.Inverse(sh.rotation);
        Vector3 handMouth=palm(HumanBodyBones.LeftMiddleProximal,HumanBodyBones.LeftMiddleDistal)-lm*(-sourceCase.right)*0.035f;
        Vector3 hipMouth=th.position+hm*(sourceCase.position-sh.position)*ratio+hm*sourceCase.rotation*sheathOffset;
        float release=Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.05f,2.35f,time));
        float hold=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0.25f,0.6f,time))*(1-release);
        Vector3 mouth=Vector3.Lerp(hipMouth,handMouth,hold);
        Quaternion cm=Quaternion.Slerp(lm,hm,release);
        Vector3 caseAxis=cm*(-sourceCase.right);
        Vector3 grip=palm(HumanBodyBones.RightMiddleProximal,HumanBodyBones.RightMiddleDistal);
        Vector3 bladeAxis=rm*(-sourceBlade.right);
        float dock=Mathf.Clamp01((1-sourceSword.lossyScale.x)/0.99f);
        dock=Mathf.SmoothStep(0,1,dock);
        Vector3 towards=mouth-grip;
        float align=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0.85f,1.2f,time));
        if(towards.sqrMagnitude>0.0001f) bladeAxis=Vector3.Slerp(bladeAxis,towards.normalized,align);
        bladeAxis=Vector3.Slerp(bladeAxis,caseAxis,dock).normalized;
        Vector3 guard=Vector3.Lerp(grip+bladeAxis*0.127f,mouth,dock);
        Vector3 sheathAxis=Vector3.Slerp(caseAxis,bladeAxis,align*(1-dock)).normalized;
        var swordRotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(rm*sourceBlade.up,bladeAxis).normalized,bladeAxis);
        var caseRotation=Quaternion.LookRotation(sheathAxis,cm*sourceCase.up);
        Vector3 swordPosition=guard-swordRotation*new Vector3(0,0.127f,0);
        var bm=blade.parent.worldToLocalMatrix*Matrix4x4.TRS(swordPosition,swordRotation,Vector3.one);
        var sm=sheath.parent.worldToLocalMatrix*Matrix4x4.TRS(mouth,caseRotation,Vector3.one);
        var positions=new[]{(Vector3)bm.GetColumn(3),(Vector3)sm.GetColumn(3)};var rotations=new[]{bm.rotation,sm.rotation};
        for(int j=0;j<2;j++) {var curves=j==0?bladeCurves:caseCurves;for(int i=0;i<7;i++)curves[i].AddKey(time,i<3?positions[j][i]:rotations[j][i-3]);}
        if(time>=1.2f&&time<=1.83f){maxAxisError=Mathf.Max(maxAxisError,Vector3.Angle(bladeAxis,sheathAxis));maxRadialError=Mathf.Max(maxRadialError,Vector3.Cross(mouth-guard,bladeAxis).magnitude);}
    }
    for(int i=0;i<7;i++) foreach(int j in new[]{0,1}) {
        var c=(j==0?bladeCurves:caseCurves)[i];for(int k=0;k<c.length;k++){AnimationUtility.SetKeyLeftTangentMode(c,k,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(c,k,AnimationUtility.TangentMode.Linear);}
        AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(j==0?bp:cp,typeof(Transform),props[i]),c);
    }
    foreach(string p in new[]{sp,bp,"Weapon/Katana"})foreach(string axis in new[]{"x","y","z"})AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(p,typeof(Transform),"m_LocalScale."+axis),AnimationCurve.Constant(0,clip.length,1));
    string visualPath=AnimationUtility.CalculateTransformPath(visual,target.transform);
    AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(visualPath,typeof(MeshRenderer),"m_Enabled"),null);
    // Transform 기본값은 다른 클립으로 전환할 때 복원된다. Renderer bool은 그래프에 마지막 값이 남는다.
    foreach(string axis in new[]{"x","y","z"})AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(visualPath,typeof(Transform),"m_LocalScale."+axis),AnimationCurve.Constant(0,clip.length,1));
    clip.EnsureQuaternionContinuity();EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);
    targetGraph.Destroy();
    // 계산용으로 바꾼 활성 상태·본 포즈를 저장하지 않고, 시각물만 원래 프리팹에 추가한다.
    var savedVisualMesh=visual.GetComponent<MeshFilter>().sharedMesh;var savedMaterials=visual.GetComponent<MeshRenderer>().sharedMaterials;var visualPosition=visual.localPosition;
    PrefabUtility.UnloadPrefabContents(target);target=PrefabUtility.LoadPrefabContents(prefabPath);
    sheath=find(target,"Sword_Case");visual=sheath.Find("FrankSheathVisual");
    if(visual==null){visual=new GameObject("FrankSheathVisual").transform;visual.SetParent(sheath,false);visual.gameObject.AddComponent<MeshFilter>();visual.gameObject.AddComponent<MeshRenderer>();}
    visual.GetComponent<MeshFilter>().sharedMesh=savedVisualMesh;visual.GetComponent<MeshRenderer>().sharedMaterials=savedMaterials;visual.GetComponent<MeshRenderer>().enabled=true;
    visual.localPosition=visualPosition;visual.localRotation=Quaternion.identity;visual.localScale=Vector3.zero;
    PrefabUtility.SaveAsPrefabAsset(target,prefabPath);
    string report="ratio="+ratio+"\nInsertionAxisError="+maxAxisError+"\nInsertionRadialError="+maxRadialError+"\nVisualReview=Required";
    System.IO.File.WriteAllText("output/FrankWeaponRetarget/SheathingCorrection/bake.txt",report);return report;
} finally { if(targetGraph.IsValid())targetGraph.Destroy();PrefabUtility.UnloadPrefabContents(target);UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(avatar);UnityEngine.Object.DestroyImmediate(sample); }
