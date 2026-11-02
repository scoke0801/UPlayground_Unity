if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new System.Exception("편집 모드에서 실행하세요.");
string folder="Assets/03.Prefabs/Traversal/Effects";System.IO.Directory.CreateDirectory(folder);AssetDatabase.Refresh();
string path=folder+"/PlayerSuperJumpDust.prefab";
var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalAssets/FX/Magic effects pack/Prefabs/Smoke effects/Dust puff.prefab");
var shader=Shader.Find("Universal Render Pipeline/Particles/Unlit");if(shader==null)throw new System.Exception("URP 파티클 셰이더 없음");
if(AssetDatabase.LoadAssetAtPath<GameObject>(path)==null){
    var go=UnityEngine.Object.Instantiate(source);go.name="PlayerSuperJumpDust";
    try{
        var ps=go.GetComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main;main.loop=false;main.duration=.15f;main.startLifetime=new ParticleSystem.MinMaxCurve(.45f,.75f);main.startSpeed=new ParticleSystem.MinMaxCurve(1.2f,2.2f);main.startSize=new ParticleSystem.MinMaxCurve(.35f,.7f);main.startColor=new Color(.66f,.61f,.52f,.65f);main.simulationSpace=ParticleSystemSimulationSpace.World;main.gravityModifier=.3f;main.maxParticles=32;main.playOnAwake=true;
        var emission=ps.emission;emission.rateOverTime=0f;emission.rateOverDistance=0f;emission.SetBursts(new[]{new ParticleSystem.Burst(0f,22)});
        var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Hemisphere;shape.radius=.3f;shape.rotation=new Vector3(-90f,0f,0f);
        var renderer=ps.GetComponent<ParticleSystemRenderer>();var material=new Material(shader);material.name="PlayerSuperJumpDust";
        material.SetTexture("_BaseMap",renderer.sharedMaterial.mainTexture);material.SetColor("_BaseColor",Color.white);material.SetFloat("_Surface",1);material.SetFloat("_Blend",0);material.SetFloat("_SrcBlend",5);material.SetFloat("_DstBlend",10);material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;
        AssetDatabase.CreateAsset(material,folder+"/PlayerSuperJumpDust.mat");renderer.sharedMaterial=material;
        PrefabUtility.SaveAsPrefabAsset(go,path);
    }finally{UnityEngine.Object.DestroyImmediate(go);}
}
var asset=AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Traversal/SuperJumpGliding/Raon_SuperJump.asset");
var motion=asset.motionSet.motions[0];
var effectPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
if(!motion.events.Any(e=>e is UPlayGround.Data.Event.BeginParticleEvent&&((UPlayGround.Data.Event.BeginParticleEvent)e).particlePrefab==effectPrefab)){
    Undo.RecordObject(asset,"슈퍼 점프 지면 분출 연결");
    motion.events.Add(new UPlayGround.Data.Event.BeginParticleEvent{
        particlePrefab=effectPrefab,attachToTarget=false,detachAfterSpawn=false,useSpawnRotation=false,
        destroyOnFinish=false,particleLifeTime=1.5f,projectToGround=true,groundProbeDistance=3f,offset=new Vector3(0f,.15f,0f),
        startTime=.32f,endTime=.40f,reentryPolicy=UPlayGround.Data.Event.MotionEventReentryPolicy.OncePerPlayback,
        timeLink=new UPlayGround.Data.Event.MotionEventTimeLink{enabled=true,mode=UPlayGround.Data.Event.MotionEventLinkMode.Marker,linkedMotionId=motion.id,markerId="SuperJump.Launch",startValue=0f,endValue=.08f}
    });
    EditorUtility.SetDirty(asset);AssetDatabase.SaveAssetIfDirty(asset);
}
return path;
