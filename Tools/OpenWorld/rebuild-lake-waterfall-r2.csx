// 현재 저장본의 국소 지형을 파생해 넓은 낙구·하상·소를 함께 저작한다. 전체 생성기는 사용하지 않는다.
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorUtility.scriptCompilationFailed)throw new Exception("Unity 편집 대기 상태 필요");
const string Dir="Assets/10.Datas/World/OpenWorld/LakeWaterfall/R2/";
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.name!="LakeVillageWorld")throw new Exception("대상 씬 불일치");
var terrain=Terrain.activeTerrain;var original=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/10.Datas/World/OpenWorld/WorldThemesABC/Terrain.asset");
var data=AssetDatabase.LoadAssetAtPath<TerrainData>(Dir+"Terrain.asset");
if(original==null||data==null)throw new Exception("지형 원본·사본 필요");
var root=GameObject.Find("LakeWaterfall_Northeast");
if(root.transform.Find("R2 하상과 낙구")!=null)throw new Exception("중복 저작 금지");
var beforeIds=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(v=>v.Guid).OrderBy(v=>v).ToArray();
var origin=new Vector3(1144,0,1029);var forward=new Vector3(-.45f,0,-.8930286f).normalized;var right=Vector3.Cross(Vector3.up,forward);
// s, 수면 높이, 반폭, 수심. 낙하면은 상류 수면과 소 사이를 별도로 잇는다.
var profile=new float[,]{{-33,102.0f,.1f,.2f},{-27,101.1f,2.1f,.6f},{-20,99.0f,3.7f,.7f},{-12,96.6f,5.5f,.8f},{-6,94.9f,7.5f,.9f},{0,93.5f,9.3f,1.0f},{4,78.7f,10.1f,2.6f},{11,77.7f,10.5f,2.5f},{19,77.0f,11.2f,2.7f},{28,77.0f,9.0f,2.5f}};
Func<float,int,float> sample=(s,c)=>{if(s<=profile[0,0])return profile[0,c];for(int i=1;i<profile.GetLength(0);i++)if(s<=profile[i,0])return Mathf.Lerp(profile[i-1,c],profile[i,c],Mathf.InverseLerp(profile[i-1,0],profile[i,0],s));return profile[profile.GetLength(0)-1,c];};
Func<float,float> bend=s=>s<0?1.25f*Mathf.Sin((s+33)*.13f)*Mathf.Sin(Mathf.InverseLerp(-33,0,s)*Mathf.PI):.65f*Mathf.Sin(s*.2f);
Func<float,Vector3> center=s=>origin+forward*s+right*bend(s);
Func<float,float> water=s=>sample(s,1);
Func<float,float,float> half=(s,side)=>sample(s,2)*(1+.045f*Mathf.Sin(s*.32f+side*2.1f));
Func<Vector3,Vector2> coords=p=>{var d=p-origin;float s=Vector3.Dot(d,forward);return new Vector2(s,Vector3.Dot(d,right)-bend(s));};
Func<float,float> bed=s=>{if(s<=0)return water(s)-sample(s,3);if(s<1.5f)return Mathf.Lerp(92.5f,76.1f,Mathf.SmoothStep(0,1,s/1.5f));return water(Mathf.Max(4,s))-sample(Mathf.Max(4,s),3);};
var o=terrain.transform.position;int resolution=data.heightmapResolution;float dx=data.size.x/(resolution-1),dz=data.size.z/(resolution-1);
int ax=Mathf.FloorToInt((1108-o.x)/dx),az=Mathf.FloorToInt((985-o.z)/dz),bx=Mathf.CeilToInt((1180-o.x)/dx),bz=Mathf.CeilToInt((1078-o.z)/dz);
var heights=original.GetHeights(ax,az,bx-ax+1,bz-az+1);int changed=0;float maximum=0;
for(int z=0;z<heights.GetLength(0);z++)for(int x=0;x<heights.GetLength(1);x++){
    var p=new Vector3(o.x+(ax+x)*dx,0,o.z+(az+z)*dz);var q=coords(p);float s=q.x;
    if(s< -34||s>29)continue;
    float h=half(s,Mathf.Sign(q.y));float a=Mathf.Abs(q.y);float end=Mathf.SmoothStep(0,1,Mathf.Min((s+34)/2,(29-s)/2));
    if(a>h+7||end<=0)continue;
    float old=heights[z,x]*data.size.y+o.y;float normalized=a/Mathf.Max(.2f,h);
    float floor=bed(s)+(s>0&&s<4?1.5f:sample(s,3)+.15f)*Mathf.Pow(Mathf.Clamp01(normalized),2.3f);
    float bank=water(s)+.35f;
    if(s>0&&s<4)bank=Mathf.Lerp(93.5f,79.05f,Mathf.SmoothStep(0,1,s/4));
    float target=normalized<.86f?floor:Mathf.Lerp(floor,bank,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.86f,1,normalized)));
    float weight=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(h,h+7,a));
    // 대형 경계를 평탄화하지 않고 침식 요철과 넓은 전이만 더한다.
    target+=(Mathf.PerlinNoise(p.x*.4f,p.z*.4f)-.5f)*.22f*Mathf.Clamp01(a/h);
    float next=Mathf.Lerp(old,target,weight*end);if(Mathf.Abs(next-old)>.001f){changed++;maximum=Mathf.Max(maximum,Mathf.Abs(next-old));}
    heights[z,x]=(next-o.y)/data.size.y;
}
data.SetHeights(ax,az,heights);terrain.terrainData=data;terrain.GetComponent<TerrainCollider>().terrainData=data;
// 물길에는 젖은 하상, 실제 낙구 급경사에는 암반을 칠한다.
int ares=data.alphamapResolution;int pax=Mathf.FloorToInt((1108-o.x)/data.size.x*ares),paz=Mathf.FloorToInt((985-o.z)/data.size.z*ares),pbx=Mathf.CeilToInt((1180-o.x)/data.size.x*ares),pbz=Mathf.CeilToInt((1078-o.z)/data.size.z*ares);
var paint=original.GetAlphamaps(pax,paz,pbx-pax+1,pbz-paz+1);int painted=0;
for(int z=0;z<paint.GetLength(0);z++)for(int x=0;x<paint.GetLength(1);x++){
    var p=new Vector3(o.x+(pax+x+.5f)/ares*data.size.x,0,o.z+(paz+z+.5f)/ares*data.size.z);var q=coords(p);float s=q.x;
    if(s< -34||s>29)continue;float h=half(s,Mathf.Sign(q.y));float weight=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(h-1,h+4,Mathf.Abs(q.y))))*Mathf.SmoothStep(0,1,Mathf.Min((s+34)/3,(29-s)/3));if(weight<=.001f)continue;
    int layer=s>-.6f&&s<3.5f?14:18;for(int k=0;k<data.alphamapLayers;k++)paint[z,x,k]*=1-weight;paint[z,x,layer]+=weight;painted++;
}data.SetAlphamaps(pax,paz,paint);EditorUtility.SetDirty(data);
// 이전 공중 물판과 세워 둔 새 돌을 제거한다. 구매 원본은 보존한다.
foreach(var n in new[]{"낙수와 호수 연결","침식 암반","낙구 물살"}){var child=root.transform.Find(n);if(child!=null)UnityEngine.Object.DestroyImmediate(child.gameObject);}
var part=new GameObject("R2 하상과 낙구");part.transform.SetParent(root.transform,false);
var lake=GameObject.Find("LakeWater");var lakeSurface=lake.GetComponent<UPlayGround.Components.FootstepWaterSurface>();var lakeMaterial=lake.GetComponent<MeshRenderer>().sharedMaterial;
var fallMaterial=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/OpenWorld/LakeWaterfall/Fall.mat"));fallMaterial.name="WideFall";fallMaterial.renderQueue=3010;fallMaterial.SetFloat("_FoamAmount",1.3f);fallMaterial.SetFloat("_FoamStart",.1f);fallMaterial.SetFloat("_SideFade",.045f);fallMaterial.SetFloat("_AmbientFloor",lakeMaterial.GetFloat("_AmbientFloor"));fallMaterial.SetFloat("_Smoothness",lakeMaterial.GetFloat("_Smoothness"));fallMaterial.SetVector("_NoiseTiling",new Vector4(4.4f,.8f,0,0));fallMaterial.SetVector("_NormalTiling",new Vector4(5,1,0,0));AssetDatabase.CreateAsset(fallMaterial,Dir+"Fall.mat");
Func<string,List<Vector3>,List<Vector2>,List<Color>,int,Material,GameObject> make=(name,verts,uv,colors,cols,mat)=>{
    var tris=new List<int>();int rows=verts.Count/(cols+1);for(int r=0;r<rows-1;r++)for(int c=0;c<cols;c++){int a=r*(cols+1)+c,b=a+cols+1;tris.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
    var mesh=new Mesh{name=name};mesh.SetVertices(verts);mesh.SetUVs(0,uv);if(colors!=null)mesh.SetColors(colors);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
    if(mesh.normals[0].y<0){for(int i=0;i<tris.Count;i+=3){int b=tris[i+1];tris[i+1]=tris[i+2];tris[i+2]=b;}mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateTangents();}
    AssetDatabase.CreateAsset(mesh,Dir+name+".asset");var go=new GameObject(name);go.transform.SetParent(part.transform,false);go.transform.position=origin;go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=mat;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return go;
};
Func<float,float,float> reach=(s,sign)=>{float h=half(s,sign);float d=0;while(d<h+3&&terrain.SampleHeight(center(s)+right*sign*d)+o.y<water(s)+.025f)d+=.12f;return Mathf.Max(.04f,d);};
UPlayGround.Components.FootstepWaterSurface upstream=null;
foreach(var range in new[]{new[]{-33f,0f},new[]{4f,28f}}){
    var v=new List<Vector3>();var uv=new List<Vector2>();int rows=Mathf.CeilToInt((range[1]-range[0])/.5f);const int cols=32;
    for(int r=0;r<=rows;r++){float s=Mathf.Lerp(range[0],range[1],r/(float)rows);float left=reach(s,-1),rr=reach(s,1);for(int c=0;c<=cols;c++){float u=c/(float)cols;var p=center(s)+right*Mathf.Lerp(-left,rr,u);p.y=water(s);v.Add(p-origin);uv.Add(new Vector2(p.x/6,p.z/6));}}
    var go=make(range[0]<0?"UpperStream":"PlungePool",v,uv,null,cols,lakeMaterial);var surface=go.AddComponent<UPlayGround.Components.FootstepWaterSurface>();surface.surfaceMesh=go.GetComponent<MeshFilter>();surface.profile=lakeSurface.profile;surface.useVolumeBounds=true;surface.volumeBounds=new Bounds(new Vector3(0,85,0),new Vector3(70,45,100));surface.RefreshMesh();if(range[0]<0)upstream=surface;
}
// 낙구 앞 실제 수면을 따라 붙은 뒤 짧은 투사 궤적으로 떨어진다.
var fv=new List<Vector3>();var fuv=new List<Vector2>();var fc=new List<Color>();const int C=40,R=56;
for(int row=0;row<=R;row++){
    float a=row/(float)R;float s,y,v,transition;
    if(a<.24f){float t=a/.24f;s=Mathf.Lerp(-5,0,t);y=water(s)+.008f;v=t*.16f;transition=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.65f,t));}
    else{float k=(a-.24f)/.76f;s=4*Mathf.Sqrt(k);y=93.5f-15.15f*k*k;v=Mathf.Lerp(.16f,1,k);transition=1;}
    float left=s<=0?reach(s,-1):half(0,-1);float rr=s<=0?reach(s,1):half(0,1);
    for(int col=0;col<=C;col++){float u=col/(float)C;float scallop=(Mathf.PerlinNoise(u*5.5f+2,7)-.5f)*.22f;var p=center(s)+right*Mathf.Lerp(-left,rr,u);p.y=y+scallop*(1-Mathf.InverseLerp(.7f,1,a));p+=forward*(.22f*Mathf.Sin(u*Mathf.PI)*Mathf.InverseLerp(.24f,1,a));fv.Add(p-origin);fuv.Add(new Vector2(u,v));fc.Add(new Color(transition,0,0,1));}
}make("WideFall",fv,fuv,fc,C,fallMaterial);
// 기존 호안의 세로 확장을 낮추고 땅에 묻어 암반 덩어리로 잇는다.
foreach(var name in new[]{"Lake_CliffBank_12_R2","Lake_CliffBank_13_R2","Lake_CliffBank_15_R2"}){
    var rock=GameObject.Find(name);if(rock==null)continue;var scale=rock.transform.localScale;rock.transform.localScale=new Vector3(scale.x*1.05f,Mathf.Min(scale.y,2.2f),scale.z);
    var rs=rock.GetComponentsInChildren<MeshRenderer>();var b=rs[0].bounds;foreach(var rr in rs)b.Encapsulate(rr.bounds);float ground=terrain.SampleHeight(rock.transform.position)+o.y;rock.transform.position+=Vector3.up*(ground-b.min.y-b.size.y*.55f);
}
// 상류를 가로지르던 나무 한 그루는 ID와 파괴 구성을 유지해 옆 숲에 재안착한다.
var tree=GameObject.Find("SlopeCanopy_12");tree.transform.position=new Vector3(1124,terrain.SampleHeight(new Vector3(1124,0,1054))+o.y-.25f,1054);
var systems=root.GetComponentsInChildren<ParticleSystem>();foreach(var p in systems.Where(p=>p.name.StartsWith("입수 물보라"))){int i=int.Parse(p.name.Substring(p.name.Length-1))-1;var pos=center(4)+right*((i-1)*5.2f);pos.y=79;p.transform.position=pos;p.transform.localScale=Vector3.one*.85f;}
var mist=root.transform.Find("입수 미스트");mist.position=center(5)+Vector3.up*79.4f;mist.localScale=new Vector3(1.8f,.9f,1.1f);
var impact=root.transform.Find("호수 입수 지점");impact.position=center(5)+Vector3.up*78.5f;
var link=root.GetComponent<UPlayGround.Gameplay.World.WaterfallLink>();link.source=upstream;link.target=lakeSurface;link.impact=impact;
var currentObject=new GameObject("낙구 물살");currentObject.transform.SetParent(part.transform,false);currentObject.transform.position=origin+Vector3.up*93.5f;currentObject.transform.rotation=Quaternion.LookRotation(forward);var current=currentObject.AddComponent<UPlayGround.Components.WaterfallCurrent>();var so=new SerializedObject(current);so.FindProperty("_region").boundsValue=new Bounds(new Vector3(0,-6,1),new Vector3(23,22,14));so.FindProperty("_fallStart").floatValue=.2f;so.FindProperty("_releaseHeight").floatValue=-14.2f;so.FindProperty("_flowSpeed").floatValue=4;so.FindProperty("_minimumFallSpeed").floatValue=3;so.ApplyModifiedPropertiesWithoutUndo();
var afterIds=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(v=>v.Guid).OrderBy(v=>v).ToArray();if(!beforeIds.SequenceEqual(afterIds))throw new Exception("배치 ID 변경");
Physics.SyncTransforms();EditorUtility.SetDirty(terrain);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();
var report=new{heightSamples=changed,maximumDelta=maximum,paintSamples=painted,sourceTerrain=AssetDatabase.GetAssetPath(original),derivedTerrain=AssetDatabase.GetAssetPath(data),placementIds=beforeIds.Length,idsUnchanged=true,fallWidth=half(0,-1)+half(0,1),newDecorativeRocks=0};System.IO.File.WriteAllText("output/lake-waterfall/r2/applied.txt","heights="+changed+" max="+maximum+" paint="+painted+" ids="+beforeIds.Length+" idsUnchanged=true width="+(half(0,-1)+half(0,1)));return report;
