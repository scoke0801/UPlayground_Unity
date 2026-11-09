// 검토본에만 얕은 비서사 수로를 시험한다. 서사 개울·수위·보행선·콘텐츠는 공유하지 않는다.
var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string dir="Assets/10.Datas/World/ThemeStudies/ShadeCreekR1/";
if(s.path!="Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR1.unity"||s.isDirty||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("저장된 검토본 필요");
var t=Terrain.activeTerrain;var d=t.terrainData;var src=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/10.Datas/World/OpenWorld/RedForestVisualR1/Terrain.asset");
if(AssetDatabase.GetAssetPath(d)!=dir+"Terrain.asset"||System.IO.File.Exists(dir+"CreekSurface.asset"))throw new Exception("전용 최초 수로 시험만 허용");
Func<float,float> center=z=>1630+8*Mathf.Sin((z-750)*.09f);
Func<float,float> width=z=>(2.7f+.7f*Mathf.Sin(z*.19f))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(761,769,z))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(825,833,z)));
Func<float,float> level=z=>src.GetInterpolatedHeight(center(z)/2400,(z+700)/2800)-.12f;
var route=new[]{new Vector2(1700,740),new Vector2(1640,750),new Vector2(1605,805),new Vector2(1650,855),new Vector2(1750,870)};
Func<float,float,float> rd=(x,z)=>{var p=new Vector2(x,z);float best=999;for(int i=0;i<route.Length-1;i++){var v=route[i+1]-route[i];best=Mathf.Min(best,Vector2.Distance(p,route[i]+v*Mathf.Clamp01(Vector2.Dot(p-route[i],v)/v.sqrMagnitude)));}return best;};
var fixedPoints=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(e=>e.transform.position).Where(p=>p.x>1570&&p.x<1680&&p.z>740&&p.z<870).ToArray();
var supports=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider>(true)).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger&&!(c is TerrainCollider)&&c.bounds.size.x<50&&c.bounds.size.z<50).Select(c=>c.bounds).Where(b=>b.max.x>1610&&b.min.x<1645&&b.max.z>761&&b.min.z<833).ToArray();
var h=d.GetHeights(0,0,d.heightmapResolution,d.heightmapResolution);int changes=0;
for(int z=0;z<h.GetLength(0);z++){float wz=-700+z*2800f/(h.GetLength(0)-1);if(wz<761||wz>833)continue;for(int x=0;x<h.GetLength(1);x++){
float wx=x*2400f/(h.GetLength(1)-1),delta=Mathf.Abs(wx-center(wz)),half=width(wz);if(half<.1f||delta>half+2)continue;
float weight=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(half*.55f,half+2,delta));weight*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(5.5f,8,rd(wx,wz)));
foreach(var p in fixedPoints)weight*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(6,10,Vector2.Distance(new Vector2(wx,wz),new Vector2(p.x,p.z))));
foreach(var b in supports){float dx=Mathf.Max(b.min.x-wx,0,wx-b.max.x),dz=Mathf.Max(b.min.z-wz,0,wz-b.max.z);weight*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1,3,Mathf.Sqrt(dx*dx+dz*dz)));}
float target=level(wz)-(.18f+.24f*(1-Mathf.Clamp01(delta/Mathf.Max(half,.1f))));float old=h[z,x]*400;float next=Mathf.Lerp(old,Mathf.Min(old,target),weight);if(Mathf.Abs(old-next)>.002f){h[z,x]=next/400;changes++;}
}}d.SetHeights(0,0,h);
var a=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);int wet=d.alphamapLayers-1;
for(int z=0;z<d.alphamapHeight;z++){float wz=-700+z*2800f/(d.alphamapHeight-1);if(wz<758||wz>836)continue;for(int x=0;x<d.alphamapWidth;x++){float wx=x*2400f/(d.alphamapWidth-1),half=width(wz);float w=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(half,half+3,Mathf.Abs(wx-center(wz)))))*.9f;if(w<.001f)continue;for(int j=0;j<a.GetLength(2);j++)a[z,x,j]*=1-w;a[z,x,wet]+=w;}}d.SetAlphamaps(0,0,a);
for(int l=0;l<d.detailPrototypes.Length;l++){var detail=d.GetDetailLayer(0,0,d.detailWidth,d.detailHeight,l);for(int z=0;z<detail.GetLength(0);z++){float wz=-700+z*2800f/(detail.GetLength(0)-1);if(wz<760||wz>834)continue;for(int x=0;x<detail.GetLength(1);x++){float wx=x*2400f/(detail.GetLength(1)-1);if(Mathf.Abs(wx-center(wz))<width(wz)+1.8f)detail[z,x]=0;}}d.SetDetailLayer(0,0,l,detail);}d.SetBaseMapDirty();EditorUtility.SetDirty(d);AssetDatabase.SaveAssetIfDirty(d);t.Flush();
const int rows=145,columns=13;var vertices=new Vector3[rows*columns];var uv=new Vector2[vertices.Length];var triangles=new int[(rows-1)*(columns-1)*6];int index=0;
for(int z=0;z<rows;z++){float wz=761+72f*z/(rows-1),half=Mathf.Max(width(wz),.01f);for(int x=0;x<columns;x++){float u=x/(float)(columns-1);vertices[z*columns+x]=new Vector3(center(wz)+(u*2-1)*half,level(wz),wz);uv[z*columns+x]=new Vector2(u,z/(float)(rows-1));}}
for(int z=0;z<rows-1;z++)for(int x=0;x<columns-1;x++){int p=z*columns+x;triangles[index++]=p;triangles[index++]=p+columns;triangles[index++]=p+1;triangles[index++]=p+1;triangles[index++]=p+columns;triangles[index++]=p+columns+1;}
var mesh=new Mesh();mesh.name="ShadeCreekSurface";mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,dir+"CreekSurface.asset");
var m=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<Material>("Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Materials/Water/EastLands_SmallRiver_Material.mat"));m.SetColor("_Color3",new Color(.38f,.54f,.49f,0));m.SetColor("_Color4",new Color(.22f,.35f,.31f,0));m.SetColor("_Color5",new Color(.63f,.74f,.66f,0));m.SetFloat("_NormalsStrength",.13f);m.SetFloat("_FoamLinesOpacity",.025f);m.SetFloat("_EdgeFoamIntensity",.13f);m.SetFloat("_Smoothness",.82f);m.SetFloat("_GlobalSpeed",.35f);AssetDatabase.CreateAsset(m,dir+"CreekWater.mat");
var go=new GameObject("ShadeCreekR1_NonNarrativeWaterStudy");go.transform.SetParent(GameObject.Find("AuthoredEnvironment").transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=m;
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(s);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s);
System.IO.File.WriteAllText("output/ShadeCreekR1/water-study.txt","검토본 한정 비서사 수로 시험. 기존 수위/서사 개울·진행·콜라이더·적 데이터 불변. 지면 함몰="+changes+". 보행 중심5.5m·콘텐츠6m 지지면 고정. 신규 상호작용·수영·이벤트 없음. 정규 환경 연결 금지.");
return new{heightChanges=changes,vertices=vertices.Length,water="독립 검토본 비서사 시험"};
