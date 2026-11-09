// 물가가 긴 띠로 읽힌 시험을 반려하고 넓어졌다 좁아지는 굽이와 진흙 선반으로 재구성한다.
var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string dir="Assets/10.Datas/World/ThemeStudies/ShadeCreekR1/";
if(s.path!="Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR1.unity"||s.isDirty||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("저장된 검토본 필요");
var t=Terrain.activeTerrain;var d=t.terrainData;var src=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/10.Datas/World/OpenWorld/RedForestVisualR1/Terrain.asset");
Func<float,float> center=z=>1630+8*Mathf.Sin((z-750)*.09f)-2*Mathf.Exp(-Mathf.Pow((z-812)/5,2));
Func<float,float> width=z=>(1.7f+3.7f*Mathf.Exp(-Mathf.Pow((z-797)/7.5f,2))+1.5f*Mathf.Exp(-Mathf.Pow((z-770)/5.5f,2)))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(761,769,z))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(825,833,z)));
Func<float,float> level=z=>src.GetInterpolatedHeight((1630+8*Mathf.Sin((z-750)*.09f))/2400,(z+700)/2800)-.12f;
var route=new[]{new Vector2(1700,740),new Vector2(1640,750),new Vector2(1605,805),new Vector2(1650,855),new Vector2(1750,870)};
Func<float,float,float> distance=(x,z)=>{var p=new Vector2(x,z);float best=999;for(int i=0;i<route.Length-1;i++){var v=route[i+1]-route[i];best=Mathf.Min(best,Vector2.Distance(p,route[i]+v*Mathf.Clamp01(Vector2.Dot(p-route[i],v)/v.sqrMagnitude)));}return best;};
var protectedPoints=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(e=>e.transform.position).Where(p=>p.x>1570&&p.x<1680&&p.z>740&&p.z<870).ToArray();
var supports=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider>(true)).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger&&!(c is TerrainCollider)&&c.bounds.size.x<50&&c.bounds.size.z<50).Select(c=>c.bounds).Where(b=>b.max.x>1610&&b.min.x<1645&&b.max.z>761&&b.min.z<833).ToArray();
var h=d.GetHeights(0,0,d.heightmapResolution,d.heightmapResolution);var original=src.GetHeights(0,0,src.heightmapResolution,src.heightmapResolution);
for(int z=0;z<h.GetLength(0);z++){float wz=-700+z*2800f/(h.GetLength(0)-1);if(wz<758||wz>836)continue;for(int x=0;x<h.GetLength(1);x++){
float wx=x*2400f/(h.GetLength(1)-1);if(wx<1610||wx>1646)continue;float edge=Mathf.Min(wx-1610,1646-wx,wz-758,836-wz);float restore=Mathf.SmoothStep(0,1,edge/2);float old=Mathf.Lerp(h[z,x],original[z,x],restore)*400;
float half=width(wz),delta=Mathf.Abs(wx-center(wz));float weight=half<.1f?0:1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(half*.6f,half+2,delta));weight*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(5.5f,8,distance(wx,wz)));
foreach(var p in protectedPoints)weight*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(6,10,Vector2.Distance(new Vector2(wx,wz),new Vector2(p.x,p.z))));
foreach(var b in supports){float dx=Mathf.Max(b.min.x-wx,0,wx-b.max.x),dz=Mathf.Max(b.min.z-wz,0,wz-b.max.z);weight*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1,3,Mathf.Sqrt(dx*dx+dz*dz)));}
float target=level(wz)-.22f-.24f*(1-Mathf.Clamp01(delta/Mathf.Max(.1f,half)));h[z,x]=Mathf.Lerp(old,Mathf.Min(old,target),weight)/400;
}}d.SetHeights(0,0,h);EditorUtility.SetDirty(d);AssetDatabase.SaveAssetIfDirty(d);t.Flush();
var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(dir+"CreekSurface.asset");var vertices=mesh.vertices;
for(int z=0;z<145;z++){float wz=761+72f*z/144,half=Mathf.Max(.01f,width(wz));for(int x=0;x<13;x++){float u=x/12f;float scallop=Mathf.Sin(wz*.85f+u*3)*.22f+Mathf.Sin(wz*.31f)*.35f;vertices[z*13+x]=new Vector3(center(wz)+(u*2-1)*(half+scallop),level(wz),wz);}}mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);
var rocks=GameObject.Find("ShadeCreekR1_BankRocks");foreach(Transform q in rocks.transform){var rs=q.GetComponentsInChildren<Renderer>(true);float bottom=rs.Min(r=>r.bounds.min.y);q.position+=Vector3.up*(t.SampleHeight(q.position)-bottom-.08f);PrefabUtility.RecordPrefabInstancePropertyModifications(q);}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(s);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s);return "물가 큰 형태 재설계·단면·기존 돌 재접지. 추가 소품 없음";
