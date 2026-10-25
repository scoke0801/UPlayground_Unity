var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.name!="StonegateCitadel"||Application.isPlaying)throw new System.Exception("돌등성 Edit Mode 필요");
var root=GameObject.Find("StonegateEastArt");if(root==null||root.transform.Find("KeepEnclosure")!=null)throw new System.Exception("첫 마감만 실행");
var environment=GameObject.Find("CityEnvironment");var terrain=environment.GetComponentInChildren<Terrain>();var data=terrain.terrainData;
string kit="Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Buildings/Building Kit/";
string props="Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Props/Exterior Props/";
string east="Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Prefabs/";
string roofPath="Assets/08.Models/WorldCities/EastStyle/";
var clay=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/WorldCities/EastStyle/EastRoofClay.mat");
var timber=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/WorldCities/EastStyle/EastRoofTimber.mat");
int ground=LayerMask.NameToLayer("Ground");var report=new System.Collections.Generic.List<string>();
System.Func<string,Transform,Vector3,float,Vector3,GameObject> Place=(path,parent,position,yaw,scale)=>{
 var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab==null)throw new System.Exception("에셋 없음 "+path);
 var obj=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab,scene);obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localRotation=Quaternion.Euler(0,yaw,0)*prefab.transform.localRotation;obj.transform.localScale=scale;
 foreach(var light in obj.GetComponentsInChildren<Light>(true))light.enabled=false;
 foreach(var t in obj.GetComponentsInChildren<Transform>(true))t.gameObject.layer=ground;
 return obj;
};
System.Func<string,Transform,Vector3,Vector3,float,GameObject> Fit=(path,parent,position,size,yaw)=>{
 var obj=Place(path,parent,Vector3.zero,0,Vector3.one);var renderers=obj.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
 obj.transform.localScale=new Vector3(size.x/bounds.size.x,size.y/bounds.size.y,size.z/bounds.size.z);
 bounds=obj.GetComponentsInChildren<Renderer>()[0].bounds;foreach(var r in obj.GetComponentsInChildren<Renderer>())bounds.Encapsulate(r.bounds);
 var offset=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)-obj.transform.position;
 obj.transform.localRotation=Quaternion.Euler(0,yaw,0);obj.transform.localPosition=position-Quaternion.Euler(0,yaw,0)*offset;
 return obj;
};
System.Action<GameObject> DisableCollision=o=>{foreach(var c in o.GetComponentsInChildren<Collider>(true))c.enabled=false;};
System.Action<Transform,float,float,float> Roof=(parent,width,height,depthScale)=>{
 var group=new GameObject("TiledRoof").transform;group.SetParent(parent,false);group.localPosition=new Vector3(0,height,0);group.localScale=new Vector3(1,1,depthScale);
 System.Action<string,float,float> Module=(name,x,yaw)=>{var obj=Place(roofPath+name+".fbx",group,new Vector3(x,name.Contains("Ridge")?1.89f:0,0),yaw+180,Vector3.one);foreach(var r in obj.GetComponentsInChildren<Renderer>()){var slots=r.sharedMaterials;for(int i=0;i<slots.Length;i++)slots[i]=i==0?clay:timber;r.sharedMaterials=slots;}DisableCollision(obj);};
 float end=(width-4)/2;
 for(float x=-end;x<end-.1f;x+=2){Module("EastRoof_Straight_2m",x,0);Module("EastRoof_Straight_2m",x+2,180);Module("EastRoof_Ridge_2m",x,0);}
 Module("EastRoof_Corner_3p2m",end,0);Module("EastRoof_Corner_3p2m",end,90);Module("EastRoof_Corner_3p2m",-end,180);Module("EastRoof_Corner_3p2m",-end,270);
};
System.Func<Transform,string,Vector3,Vector3,float,GameObject> Beam=(parent,name,pos,size,yaw)=>{var obj=Fit(kit+"TGV_Beam_01A.prefab",parent,pos,size,yaw);obj.name=name;return obj;};
System.Func<string,Vector3,float,int,GameObject> House=(name,pos,yaw,variant)=>{
 int bays=variant==0?3:variant==1?4:5;int floors=variant==1?2:1;float width=bays*2;
 var house=new GameObject(name);house.transform.SetParent(root.transform,false);
 int windows=0;
 for(int f=0;f<floors;f++)for(int side=0;side<2;side++)for(int bay=0;bay<bays;bay++){
  bool door=f==0&&side==0&&bay==(variant==2?3:1);
  bool window=!door&&(side==0?(variant==0?bay==2:variant==1?(f==1||bay==3):(bay==0||bay==2)):(bay+f+variant)%3==0);
  var point=new Vector3(-width/2+1+bay*2,.35f+f*3,side==0?-2:2);
  Place(kit+(door?"TGV_Wall_06A":window?"TGV_Wall_04A":"TGV_Wall_02A")+".prefab",house.transform,point,90,Vector3.one);
  if(window){windows++;Place(kit+"TGV_Window_01A.prefab",house.transform,point,90,Vector3.one);}
  if(door)Fit(kit+"TGV_Door_01A.prefab",house.transform,point+new Vector3(0,0,-.03f),new Vector3(.25f,2.1f,1.1f),90);
 }
 for(int f=0;f<floors;f++)for(int side=0;side<2;side++)for(int bay=0;bay<2;bay++){
  bool window=side==0&&bay==variant%2&&f==floors-1;var point=new Vector3(side==0?-width/2:width/2,.35f+f*3,-1+bay*2);
  Place(kit+(window?"TGV_Wall_04A":"TGV_Wall_02A")+".prefab",house.transform,point,0,Vector3.one);
  if(window){windows++;Place(kit+"TGV_Window_01A.prefab",house.transform,point,0,Vector3.one);}
 }
 for(int x=0;x<bays;x++)for(int z=0;z<2;z++)Place(kit+"TGV_Foundation_01A.prefab",house.transform,new Vector3(-width/2+1+x*2,-.15f,-1+z*2),0,Vector3.one);
 Roof(house.transform,width,.35f+floors*3,1);
 house.transform.SetPositionAndRotation(pos,Quaternion.Euler(0,yaw,0));
 report.Add(name+" variant="+variant+" windows="+windows+" floors="+floors+" position="+pos);
 return house;
};
var keep=new GameObject("KeepEnclosure");keep.transform.SetParent(root.transform,false);
for(int floor=0;floor<3;floor++){
 int bays=10-floor*2;int sideBays=4-floor;float width=bays*2,depth=sideBays*2;
 for(int side=0;side<2;side++)for(int bay=0;bay<bays;bay++){
  bool door=floor==0&&side==0&&bay==bays/2;
  bool window=!door&&(floor==0?bay%3==0:floor==1?bay%2==0:bay==1||bay==bays-2);
  var p=new Vector3(180-width/2+1+bay*2,18+floor*4.2f,246+(side==0?-depth/2:depth/2));
  Place(kit+(door?"TGV_Wall_06A":window?"TGV_Wall_04A":"TGV_Wall_02A")+".prefab",keep.transform,p,90,new Vector3(1,1.1f,1));
  if(window)Place(kit+"TGV_Window_01A.prefab",keep.transform,p,90,Vector3.one);
 }
 for(int side=0;side<2;side++)for(int bay=0;bay<sideBays;bay++){
  bool window=bay==(floor+side)%sideBays;var p=new Vector3(180+(side==0?-width/2:width/2),18+floor*4.2f,246-depth/2+1+bay*2);
  Place(kit+(window?"TGV_Wall_04A":"TGV_Wall_02A")+".prefab",keep.transform,p,0,new Vector3(1,1.1f,1));if(window)Place(kit+"TGV_Window_01A.prefab",keep.transform,p,0,Vector3.one);
 }
}
// 기존 성 부착 장식과 중복 문을 제거하지 않고 비활성화한다.
foreach(var t in environment.GetComponentsInChildren<Transform>(true)){
 if(!t.IsChildOf(root.transform)&&t.name.StartsWith("PF_EastLands_Banner")&&t.position.z>220)t.gameObject.SetActive(false);
 if(t.IsChildOf(root.transform)&&t.name=="TGV_Door_01A")t.gameObject.SetActive(false);
}
for(int i=0;i<5;i++){
 House("RearLaneWest_"+i,new Vector3(134+(i%2)*2,10,143+i*15),90,i%3);
 House("RearLaneEast_"+i,new Vector3(231-(i%2)*2,10,146+i*15),270,(i+1)%3);
}
// 원경의 낙엽수는 남기고 도시 안 식생을 기존 동양풍 침엽수로 전환한다.
var trees=environment.GetComponentsInChildren<Transform>().Where(t=>!t.IsChildOf(root.transform)&&t.name.StartsWith("BroadleafTree_02")&&t.position.z>120&&t.position.z<250).ToArray();
int treeIndex=0;
foreach(var t in trees){var p=t.position;t.gameObject.SetActive(false);var obj=Fit(east+"Foliage/Trees/Conifers/PF_EastLands_Conifer_Multi_01.prefab",root.transform,p,new Vector3(5.5f,8+(treeIndex%3),5.5f),treeIndex*47);obj.name="CourtyardPine_"+treeIndex++;DisableCollision(obj);}
foreach(Transform t in environment.transform){if(t.name=="WoodlandTree"&&t.gameObject.activeSelf){var p=t.position;float h=terrain.SampleHeight(p);if(Mathf.Abs(h-p.y)>.1f){p.y=h;t.position=p;UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(t);}}}
// 외부 진입부는 완만하게 내려온 뒤 성문으로 오르는 굽은 길이다. 도착점과 출구 좌표는 고정한다.
System.Func<float,float> RoadX=z=>180+9*Mathf.Sin(Mathf.PI*Mathf.InverseLerp(57,118,z));
int res=data.heightmapResolution;var heights=data.GetHeights(0,0,res,res);
for(int z=0;z<res;z++)for(int x=0;x<res;x++){float wx=x*data.size.x/(res-1),wz=z*data.size.z/(res-1);if(wz<59||wz>116)continue;float width=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(6,14,Mathf.Abs(wx-RoadX(wz))));float valley=Mathf.Sin(Mathf.PI*Mathf.InverseLerp(59,116,wz));heights[z,x]-=4*valley*valley*width/data.size.y;}
data.SetHeights(0,0,heights);
int ar=data.alphamapResolution;var paints=data.GetAlphamaps(0,0,ar,ar);
for(int z=0;z<ar;z++)for(int x=0;x<ar;x++){
 float wx=x*data.size.x/(ar-1),wz=z*data.size.z/(ar-1);if(wx<158||wx>202||wz<59||wz>116)continue;
 float road=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,5,Mathf.Abs(wx-RoadX(wz))));paints[z,x,0]=1-road;paints[z,x,1]=road;for(int layer=2;layer<paints.GetLength(2);layer++)paints[z,x,layer]=0;
}data.SetAlphamaps(0,0,paints);UnityEditor.EditorUtility.SetDirty(data);UnityEditor.AssetDatabase.SaveAssetIfDirty(data);
// 오래된 평면 포장은 새 오르막 아래에 남지 않도록 해당 인스턴스만 숨긴다.
foreach(var t in environment.GetComponentsInChildren<Transform>().Where(t=>!t.IsChildOf(root.transform)&&t.name.Contains("Paving")&&t.position.z>58&&t.position.z<116).ToArray()){t.gameObject.SetActive(false);UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);}
// 상점과 쉼터를 잇는 생활 공간은 이동 계약을 바꾸지 않고 차양/의자/짐으로 묶는다.
foreach(var p in new[]{new Vector3(164,10,145),new Vector3(164,10,159),new Vector3(197,10,150),new Vector3(197,10,166)}){var canopy=new GameObject("MarketTileAwning");canopy.transform.SetParent(root.transform,false);Roof(canopy.transform,6,3.4f,.8f);for(int side=0;side<2;side++)Beam(canopy.transform,"AwningPost",new Vector3(side==0?-2:2,0,1.5f),new Vector3(.2f,3.3f,.2f),0);canopy.transform.position=p;}
for(int i=0;i<3;i++)Place("Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Buildings/Interior Props/TGV_Bench_01A.prefab",root.transform,new Vector3(166+i*3,10,209),180,Vector3.one);
// 기단의 실제 돌 메시가 본성 언덕의 수직 전환을 가린다. 중앙 경사 통로는 비운다.
string stone=east+"Props/StoneStairsWalls/PF_EastLands_StoneWall_01.prefab";
for(int side=0;side<2;side++)for(int step=0;step<4;step++){
 float z=229+step*3;float h=terrain.SampleHeight(new Vector3(180,0,z));
 for(int layer=0;layer<Mathf.CeilToInt((h-10)/1.5f);layer++)Fit(stone,root.transform,new Vector3(side==0?165:195,10+layer*1.5f,z),new Vector3(3.2f,1.6f,2),90);
}
foreach(var c in environment.GetComponentsInChildren<Component>(true)){if(c==null)continue;if(c is Transform||c is Renderer||c is Collider||c is LODGroup)UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(c);if(c is Transform)UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(c.gameObject);}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
System.IO.File.WriteAllLines("output/StonegateEast/FINISH.txt",report.ToArray());
return "본성 벽/창, 연립 골목, 침엽수, 굽은 오르막 및 생활 소품 반영";
