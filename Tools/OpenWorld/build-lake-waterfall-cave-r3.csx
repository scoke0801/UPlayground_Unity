// 폭포 뒤의 실제 공간·상류 덮개·진입 경사·저장 가능한 보상을 증분 저작한다.
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorUtility.scriptCompilationFailed)throw new Exception("Unity 편집 대기 필요");
const string Dir="Assets/10.Datas/World/OpenWorld/LakeWaterfall/R3/";
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(GameObject.Find("LakeWaterfallCave")!=null)throw new Exception("중복 동굴 저작 금지");
var ids=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(i=>i.Guid).OrderBy(i=>i).ToArray();
var terrain=Terrain.activeTerrain;var original=terrain.terrainData;var data=AssetDatabase.LoadAssetAtPath<TerrainData>(Dir+"Terrain.asset");
var f=new Vector3(-.45f,0,-.8930286f).normalized;var right=Vector3.Cross(Vector3.up,f);var inward=-f;
var entrance=new Vector3(1144,78.15f,1029)+f*3.2f;
var root=new GameObject("LakeWaterfallCave");root.transform.SetParent(GameObject.Find("AuthoredEnvironment").transform,false);root.transform.SetPositionAndRotation(entrance,Quaternion.LookRotation(inward));
Undo.RegisterCreatedObjectUndo(root,"폭포 뒤 숨은 동굴");
Func<Vector3,Vector3> world=p=>root.transform.TransformPoint(p);
Func<float,float> floor=z=>.12f*Mathf.Clamp(z,0,9)+.06f*Mathf.Sin(z*.7f);
Func<float,float> width=z=>4.35f+.25f*Mathf.Sin(z*.7f+.3f);
Func<float,float> ceiling=z=>4.25f+.18f*Mathf.Sin(z*.85f+.7f);
var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.name="WaterfallCaveRock";
var layer=original.terrainLayers[20];material.SetTexture("_BaseMap",layer.diffuseTexture);material.SetColor("_BaseColor",new Color(.61f,.62f,.59f,1));material.SetFloat("_Smoothness",.08f);material.SetFloat("_Cull",0);AssetDatabase.CreateAsset(material,Dir+"CaveRock.mat");
Func<string,List<Vector3>,List<int>,List<Vector2>,Material,bool,GameObject> make=(name,v,tri,uv,mat,collide)=>{
    if(v.Any(p=>!float.IsFinite(p.x)||!float.IsFinite(p.y)||!float.IsFinite(p.z)))throw new Exception("동굴 정점 오류");
    var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(tri,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Dir+name+".asset");
    var g=new GameObject(name);g.transform.SetParent(root.transform,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=mat;if(collide)g.AddComponent<MeshCollider>().sharedMesh=mesh;return g;
};
// 내부 법선이 공간을 향하는 부드러운 암굴. 아래쪽은 평평해 보행할 수 있다.
const int Rings=25,Sides=32;var vtx=new List<Vector3>();var uvtx=new List<Vector2>();var tris=new List<int>();
for(int row=0;row<=Rings;row++){
    float z=Mathf.Lerp(-.3f,9.5f,row/(float)Rings);float w=width(z),top=ceiling(z),fy=floor(z);
    for(int col=0;col<=Sides;col++){
        float a=col/(float)Sides*Mathf.PI*2;float noise=(Mathf.PerlinNoise(col*.22f+2,row*.18f)-.5f)*.2f;
        float x=Mathf.Cos(a)*(w+noise);float y=Mathf.Max(0,top*.47f+Mathf.Sin(a)*(top*.53f+noise));
        vtx.Add(new Vector3(x,fy+y,z));uvtx.Add(new Vector2(col/(float)Sides*5,z/3));
    }
}
for(int row=0;row<Rings;row++)for(int col=0;col<Sides;col++){int a=row*(Sides+1)+col,b=a+Sides+1;tris.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
int end=vtx.Count;vtx.Add(new Vector3(0,floor(9.5f)+2,9.5f));uvtx.Add(new Vector2(.5f,.5f));for(int col=0;col<Sides;col++){int a=Rings*(Sides+1)+col;tris.AddRange(new[]{end,a,a+1});}
for(int i=0;i<tris.Count;i+=3){int tmp=tris[i+1];tris[i+1]=tris[i+2];tris[i+2]=tmp;}
make("LakeWaterfallCave_Interior",vtx,tris,uvtx,material,true);
// 국소 Terrain 구멍 위의 상부 지면을 원래 높이로 덮고 앞쪽만 두꺼운 암반 처마로 잇는다.
Func<float,float,float> roof=(x,z)=>{var p=world(new Vector3(x,0,z));float gy=terrain.SampleHeight(p)+terrain.transform.position.y-entrance.y;return Mathf.Max(gy,ceiling(z)+floor(z)+.85f);};
var capV=new List<Vector3>();var capUV=new List<Vector2>();var capT=new List<int>();const int NX=28,NZ=30;
for(int iz=0;iz<=NZ;iz++)for(int ix=0;ix<=NX;ix++){float x=Mathf.Lerp(-5.6f,5.6f,ix/(float)NX),z=Mathf.Lerp(-.6f,10.4f,iz/(float)NZ);var p=new Vector3(x,roof(x,z)+.015f,z);capV.Add(p);capUV.Add(new Vector2(world(p).x/7,world(p).z/7));}
for(int iz=0;iz<NZ;iz++)for(int ix=0;ix<NX;ix++){int a=iz*(NX+1)+ix,b=a+NX+1;capT.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
make("LakeWaterfallCave_Roof",capV,capT,capUV,material,true);
// 외벽은 상부 지면부터 내부보다 바깥까지 이어 공중 처마나 옆 틈을 남기지 않는다.
var outerV=new List<Vector3>();var outerUV=new List<Vector2>();var outerT=new List<int>();
foreach(float sign in new[]{-1f,1f})for(int iz=0;iz<30;iz++){
    float za=Mathf.Lerp(-.6f,10.4f,iz/30f),zb=Mathf.Lerp(-.6f,10.4f,(iz+1)/30f),x=sign*5.6f;
    int b=outerV.Count;outerV.AddRange(new[]{new Vector3(x,-.8f,za),new Vector3(x,roof(x,za),za),new Vector3(x,-.8f,zb),new Vector3(x,roof(x,zb),zb)});
    outerUV.AddRange(new[]{new Vector2(za/3,0),new Vector2(za/3,roof(x,za)/3),new Vector2(zb/3,0),new Vector2(zb/3,roof(x,zb)/3)});outerT.AddRange(new[]{b,b+1,b+2,b+2,b+1,b+3});
}
// 앞면의 아치 구멍은 안쪽 윤곽과 외곽 암반을 연결하는 띠로 남긴다.
for(int col=0;col<Sides;col++){
    int b=outerV.Count;foreach(int c in new[]{col,col+1}){
        var inner=vtx[c];inner.z=-.6f;float a=c/(float)Sides*Mathf.PI*2;float xx=Mathf.Cos(a)*5.6f;float yy=Mathf.Sin(a)>=0?roof(xx,-.6f):-.8f;
        outerV.Add(inner);outerV.Add(new Vector3(xx,yy,-.6f));outerUV.Add(new Vector2(inner.x/3,inner.y/3));outerUV.Add(new Vector2(xx/3,yy/3));
    }outerT.AddRange(new[]{b,b+1,b+2,b+2,b+1,b+3});
}
make("LakeWaterfallCave_Cliff",outerV,outerT,outerUV,material,true);
// 입구 앞 완만한 경사는 물속에서 시작해 마른 바닥에 이어진다.
var rampV=new List<Vector3>();var rampUV=new List<Vector2>();var rampT=new List<int>();
for(int iz=0;iz<=16;iz++)for(int ix=0;ix<=16;ix++){float x=Mathf.Lerp(-3.8f,3.8f,ix/16f),z=Mathf.Lerp(-5.5f,.2f,iz/16f);float y=Mathf.Lerp(-1.7f,.03f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(-5.5f,.2f,z)));rampV.Add(new Vector3(x,y,z));rampUV.Add(new Vector2(x/3,z/3));}
for(int iz=0;iz<16;iz++)for(int ix=0;ix<16;ix++){int a=iz*17+ix,b=a+17;rampT.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
make("LakeWaterfallCave_Ramp",rampV,rampT,rampUV,material,true);
// 구멍을 낸 영역 전체가 새 지면/외벽/내벽의 실물 메시 안에 있어야 한다.
int hr=data.holesResolution;float hx=data.size.x/hr,hz=data.size.z/hr;int ax=Mathf.FloorToInt(1130/hx),az=Mathf.FloorToInt((1010-terrain.transform.position.z)/hz),hw=Mathf.CeilToInt(30/hx),hh=Mathf.CeilToInt(40/hz);
var holes=data.GetHoles(ax,az,hw,hh);int cut=0;
for(int iz=0;iz<hh;iz++)for(int ix=0;ix<hw;ix++){
    var wp=new Vector3((ax+ix+.5f)*hx,0,terrain.transform.position.z+(az+iz+.5f)*hz);var lp=root.transform.InverseTransformPoint(wp);
    if(lp.z<-.1f||lp.z>10.05f||Mathf.Abs(lp.x)>5.15f)continue;holes[iz,ix]=false;cut++;
}
data.SetHoles(ax,az,holes);terrain.terrainData=data;terrain.GetComponent<TerrainCollider>().terrainData=data;EditorUtility.SetDirty(data);EditorUtility.SetDirty(terrain);
// 상자와 일회 소비 상태는 이미 사용 중인 배치 드롭 계약으로 연결한다.
var chest=new GameObject("숨겨진 보물상자");chest.transform.SetParent(root.transform,false);chest.transform.localPosition=new Vector3(1.15f,floor(7.6f)+.08f,7.6f);chest.transform.localRotation=Quaternion.Euler(0,180,0);chest.layer=LayerMask.NameToLayer("InteractableObject");
var box=chest.AddComponent<BoxCollider>();box.size=new Vector3(1.5f,1.3f,1.2f);box.center=Vector3.up*.65f;
var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Environment/Dungeon/Dng_Chest.prefab"),chest.transform);visual.transform.localPosition=Vector3.zero;
var mf=visual.GetComponentsInChildren<MeshFilter>();var bound=new Bounds();bool first=true;foreach(var m in mf){var lb=m.sharedMesh.bounds;foreach(float x in new[]{lb.min.x,lb.max.x})foreach(float y in new[]{lb.min.y,lb.max.y})foreach(float z in new[]{lb.min.z,lb.max.z}){var p=chest.transform.InverseTransformPoint(m.transform.TransformPoint(new Vector3(x,y,z)));if(first){bound=new Bounds(p,Vector3.zero);first=false;}else bound.Encapsulate(p);}}
float scale=1.4f/Mathf.Max(bound.size.x,bound.size.z);visual.transform.localScale*=scale;visual.transform.localPosition=new Vector3(-bound.center.x*scale,-bound.min.y*scale,-bound.center.z*scale);foreach(var c in visual.GetComponentsInChildren<Collider>())c.enabled=false;
var actor=chest.AddComponent<UPlayGround.DropItemActor>();actor.Init(AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Item.ItemSO>("Assets/10.Datas/Item/Consume/rare_hp_potion.asset"),3,AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.InteractableActorSO>("Assets/10.Datas/Actor/Interaction/DungeonTreasure.asset"));
var id=chest.AddComponent<UPlayGround.Components.SceneEntityId>();id.EditorSetGuid("8ce0e1fbf3384abca2ad4d0996d433b7");
var lightGo=new GameObject("동굴 안쪽의 잔광");lightGo.transform.SetParent(root.transform,false);lightGo.transform.localPosition=new Vector3(-1.4f,2.7f,5.5f);var light=lightGo.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.72f,.39f);light.intensity=2.6f;light.range=8.5f;light.shadows=LightShadows.Soft;
// 이름 규칙만 등록한다. 씬에 CameraOccluder는 붙이지 않는다.
var rules=AssetDatabase.LoadAssetAtPath<UPlayGround.Data.CameraOccluderNameRulesSO>("Assets/10.Datas/Camera/CameraOccluderNameRules.asset");System.IO.File.Copy(AssetDatabase.GetAssetPath(rules),"output/lake-waterfall/r3/backup/CameraOccluderNameRules.asset",true);
var list=rules.Rules.ToList();foreach(var name in new[]{"LakeWaterfallCave_Interior","LakeWaterfallCave_Roof","LakeWaterfallCave_Cliff","LakeWaterfallCave_Ramp"}){
    if(list.Any(rule=>rule.namePattern==name))throw new Exception("동굴 이름 규칙 중복");list.Add(new UPlayGround.Data.CameraOccluderNameRulesSO.Rule{namePattern=name,isPrefix=false,mode=name.EndsWith("Ramp")?UPlayGround.CameraSystem.OccluderMode.Solid:UPlayGround.CameraSystem.OccluderMode.Auto,coversInterior=!name.EndsWith("Ramp"),note="폭포 뒤 동굴 실물 천장·벽/진입 경사"});
}rules.SetRules(list);EditorUtility.SetDirty(rules);
var afterIds=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(i=>i.Guid).OrderBy(i=>i).ToArray();if(!ids.Concat(new[]{id.Guid}).OrderBy(i=>i).SequenceEqual(afterIds))throw new Exception("기존 진행 ID 변경");
Physics.SyncTransforms();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();return new{holeCells=cut,entrance=entrance.ToString(),chest=chest.transform.position.ToString(),chestGuid=id.Guid,idsBefore=ids.Length,idsAfter=afterIds.Length,roomWidth=8.7f,roomDepth=9.5f};
