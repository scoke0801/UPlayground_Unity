if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new System.Exception("편집 모드에서 실행하세요.");
var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="도약 이펙트 지면 검사";floor.transform.position=new Vector3(12000,-.5f,12000);floor.transform.localScale=new Vector3(4,1,4);
var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=new Vector3(12000,.8f,12000);
var effect=new GameObject("도약 이펙트 검사 원본");
var ev=new UPlayGround.Data.Event.BeginParticleEvent{particlePrefab=effect,attachToTarget=false,projectToGround=true,groundProbeDistance=3f,destroyOnFinish=true};
var field=typeof(UPlayGround.Data.Event.BeginParticleEvent).GetField("_instance",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
GameObject instance=null;
try{
    Physics.SyncTransforms();ev.Execute(target);instance=(GameObject)field.GetValue(ev);
    if(instance==null||Mathf.Abs(instance.transform.position.y)>.001f||instance.transform.parent!=null)throw new System.Exception("지면 독립 생성 실패");
    UnityEngine.Object.DestroyImmediate(instance);instance=null;field.SetValue(ev,null);
    target.transform.position+=Vector3.right*10;Physics.SyncTransforms();ev.Execute(target);instance=(GameObject)field.GetValue(ev);
    if(instance!=null)throw new System.Exception("지면 없는 위치에 생성됨");
    ev.projectToGround=false;ev.Execute(target);instance=(GameObject)field.GetValue(ev);
    if(instance==null||Vector3.Distance(instance.transform.position,target.transform.position)>.001f)throw new System.Exception("기존 비투영 이벤트 동작 변경");
    var asset=AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Traversal/SuperJumpGliding/Raon_SuperJump.asset");
    var events=asset.motionSet.motions[0].events.OfType<UPlayGround.Data.Event.BeginParticleEvent>().Where(e=>e.projectToGround).ToArray();
    if(events.Length!=1||events[0].timeLink.markerId!="SuperJump.Launch"||events[0].attachToTarget||events[0].destroyOnFinish)throw new System.Exception("도약 이벤트 연결 오류");
    var ps=events[0].particlePrefab.GetComponent<ParticleSystem>();
    if(ps.main.loop||ps.emission.burstCount!=1||ps.main.simulationSpace!=ParticleSystemSimulationSpace.World)throw new System.Exception("일회성 월드 분출 설정 오류");
    string report="지면 고정·자기 콜라이더 제외·무지면 생성 거부·기존 비투영 동작·마커 연결·단발 파티클 검사 통과";
    System.IO.File.WriteAllText("output/PlayerSuperJumpGliding/launch-vfx-validation.txt",report);return report;
}finally{if(instance!=null)UnityEngine.Object.DestroyImmediate(instance);UnityEngine.Object.DestroyImmediate(effect);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(floor);}
