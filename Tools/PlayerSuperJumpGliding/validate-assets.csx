if(EditorApplication.isCompiling||EditorApplication.isUpdating)return "컴파일 또는 임포트 대기";
System.IO.Directory.CreateDirectory("output/PlayerSuperJumpGliding");
var path="Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Raon.prefab";
var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
var player=UnityEngine.Object.Instantiate(prefab);player.hideFlags=HideFlags.HideAndDontSave;
var report=new System.Text.StringBuilder();
var errors=new System.Collections.Generic.List<string>();
Application.LogCallback log=(message,stack,type)=>{if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);};
Application.logMessageReceived+=log;
var view=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Traversal/PlayerGlider.prefab"));view.hideFlags=HideFlags.HideAndDontSave;
try{
    player.SetActive(true);
    var binding=player.GetComponent<UPlayGround.Components.PlayerGliderBinding>();
    var animator=player.GetComponent<Animator>();animator.Rebind();var animancer=player.GetComponent<Animancer.AnimancerComponent>();
    var glider=view.GetComponent<UPlayGround.Components.PlayerGliderView>();
    var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
    typeof(UPlayGround.Components.PlayerGliderBinding).GetMethod("Awake",flags).Invoke(binding,null);
    typeof(UPlayGround.Components.PlayerGliderView).GetMethod("Awake",flags).Invoke(glider,null);
    var applyIK=typeof(UPlayGround.Components.PlayerGliderBinding).GetMethod("OnAnimatorIK",flags);
    binding.BeginTraversal();binding.Attach(glider);
    int missing=0;foreach(var t in player.GetComponentsInChildren<Transform>(true))missing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);foreach(var t in view.GetComponentsInChildren<Transform>(true))missing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
    report.AppendLine("MissingScript="+missing+" SettingsValid="+binding.Settings.HasValidValues+" ViewValid="+glider.HasValidBinding);
    int triangles=0;int missingMeshes=0;int missingMaterials=0;
    foreach(var mesh in view.GetComponentsInChildren<MeshFilter>(true)){
        if(mesh.sharedMesh==null)missingMeshes++;else triangles+=mesh.sharedMesh.triangles.Length/3;
    }
    foreach(var renderer in view.GetComponentsInChildren<MeshRenderer>(true))foreach(var material in renderer.sharedMaterials)
        if(material==null||material.shader==null||material.shader.name=="Hidden/InternalErrorShader")missingMaterials++;
    report.AppendLine("GliderRenderers="+view.GetComponentsInChildren<MeshRenderer>(true).Length+" Triangles="+triangles+" MissingMesh="+missingMeshes+" MissingMaterial="+missingMaterials);
    if(missingMeshes!=0||missingMaterials!=0)throw new System.Exception("탈것 메시·재질 누락");
    var names=new[]{"Raon_GliderCatch","Raon_GliderHang","Raon_GliderTurnLeft","Raon_GliderTurnRight"};
    float worstPosition=0f,worstRotation=0f;
    foreach(string name in names){
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/07.Animations/Player/Traversal/SuperJumpGliding/"+name+".anim");
        var state=animancer.Play(clip);float start=name=="Raon_GliderCatch"?0.23f:0f;float maxPosition=0f,maxRotation=0f;float maxPositionTime=0f;string maxPositionHand="Left";
        for(int i=0;i<=120;i++){
            state.Time=Mathf.Lerp(start,clip.length,i/120f);animancer.Evaluate(0f);
            applyIK.Invoke(binding,new object[]{0});
            float leftError=Vector3.Distance(binding.PalmLeft.position,glider.GripLeft.position);
            float rightError=Vector3.Distance(binding.PalmRight.position,glider.GripRight.position);
            if(Mathf.Max(leftError,rightError)>maxPosition){maxPosition=Mathf.Max(leftError,rightError);maxPositionTime=(float)state.Time;maxPositionHand=leftError>=rightError?"Left":"Right";}
            maxRotation=Mathf.Max(maxRotation,Quaternion.Angle(binding.PalmLeft.rotation,glider.GripLeft.rotation),Quaternion.Angle(binding.PalmRight.rotation,glider.GripRight.rotation));
        }
        worstPosition=Mathf.Max(worstPosition,maxPosition);worstRotation=Mathf.Max(worstRotation,maxRotation);
        report.AppendLine(name+" samples=121 maxPalmErrorM="+maxPosition.ToString("F6")+" maxPalmAngleDeg="+maxRotation.ToString("F5")+" atSeconds="+maxPositionTime.ToString("F5")+" hand="+maxPositionHand);
    }
    var set=player.GetComponent<UPlayGround.Animation.PlayerActorAnimator>().PlayerMotionSet;
    var tags=new[]{UPlayGround.Data.Actor.Animation.MotionTags.SuperJump,UPlayGround.Data.Actor.Animation.MotionTags.GliderCatch,UPlayGround.Data.Actor.Animation.MotionTags.GliderHang,UPlayGround.Data.Actor.Animation.MotionTags.GliderTurnLeft,UPlayGround.Data.Actor.Animation.MotionTags.GliderTurnRight};
    foreach(var tag in tags){
        var motion=set.GetMotionSetAsset(UPlayGround.Data.EnumType.WeaponType.Katana,tag);if(motion==null||!motion.motionSet.IsValid())throw new System.Exception("모션 해석 실패: "+tag);
        report.AppendLine(tag+"="+AssetDatabase.GetAssetPath(motion));
    }
    if(missing!=0||!binding.Settings.HasValidValues||!glider.HasValidBinding||worstPosition>0.02f||worstRotation>10f)throw new System.Exception("활공 에셋 검사 실패\n"+report);
    report.AppendLine("NewConsoleErrors="+errors.Count);
    if(errors.Count!=0)throw new System.Exception(string.Join("\n",errors.ToArray()));
    System.IO.File.WriteAllText("output/PlayerSuperJumpGliding/asset-validation.txt",report.ToString()+"\nEditor 그래프 평가+직접 IK 호출. 실제 Play Mode 입력·KCC 주행 검증과 구분함.\n");
    return report.ToString();
}finally{Application.logMessageReceived-=log;if(player!=null)UnityEngine.Object.DestroyImmediate(player);if(view!=null)UnityEngine.Object.DestroyImmediate(view);}
