// 승인된 초가/기와 문법으로 씬 인스턴스만 교체한다. 원본 에셋은 보존한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.name!="StonegateCitadel"||Application.isPlaying)throw new System.Exception("돌등성 Edit Mode 필요");
if(GameObject.Find("StonegateEastArt")!=null)throw new System.Exception("이미 생성됨. 재실행 금지");
string output="output/StonegateEast";
string assets="Assets/10.Datas/World/WorldCities/StonegateCitadel/EastArt";
string kit="Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Buildings/Building Kit/";
string props="Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Props/Exterior Props/";
string east="Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Prefabs/";
string roofPath="Assets/08.Models/WorldCities/EastStyle/";
System.IO.Directory.CreateDirectory(assets);
var environment=scene.GetRootGameObjects().First(r=>r.name=="CityEnvironment");
var terrain=environment.GetComponentInChildren<Terrain>();var data=terrain.terrainData;
var root=new GameObject("StonegateEastArt");root.transform.SetParent(environment.transform,false);
int ground=LayerMask.NameToLayer("Ground");
var clay=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/WorldCities/EastStyle/EastRoofClay.mat");
var timber=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/WorldCities/EastStyle/EastRoofTimber.mat");
var report=new System.Collections.Generic.List<string>();
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
// 폐기 후보는 비활성 인스턴스로 보존하고 공유 원본은 삭제하지 않는다.
var oldBuildings=environment.transform.Cast<Transform>().Where(t=>t.name.StartsWith("Castle")||t.name.StartsWith("GateFlankWall")||t.name=="HarborWarehouse").ToArray();
foreach(var t in oldBuildings){report.Add("기존 외형 비활성: "+t.name+" "+t.position+" "+UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject));t.gameObject.SetActive(false);UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);}
var oldHouses=oldBuildings.Where(t=>t.name=="CastleTownHouse").ToArray();
for(int i=0;i<oldHouses.Length;i++){
 var old=oldHouses[i];
 if(old.position.z<110){var obj=Place("Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Buildings/Presets/House_0"+(i%2+1)+".prefab",root.transform,old.position,old.position.x<180?90:270,Vector3.one);obj.name="OuterThatchedHouse_"+i;}
 else House("CourtyardHouse_"+i,old.position,old.position.x<180?90:270,i%3);
}
House("JoinedHouse_WestA",new Vector3(146,10,181),90,0);
House("JoinedHouse_WestB",new Vector3(150,10,207),90,2);
House("JoinedHouse_EastA",new Vector3(216,10,177),270,0);
House("JoinedHouse_EastB",new Vector3(215,10,203),270,2);
House("Storehouse_West",new Vector3(136,10,224),90,2);
House("Storehouse_East",new Vector3(232,10,225),270,1);
// 누각은 실제 기둥/보와 다층 처마로 구성하고 중앙 통로는 비운다.
System.Action<string,Vector3,float,float,int> Pavilion=(name,pos,width,depth,floors)=>{
 var p=new GameObject(name);p.transform.SetParent(root.transform,false);
 for(int floor=0;floor<floors;floor++){
  float level=floor*4.2f;float w=width-floor*4;float d=depth-floor*1.5f;
  for(int side=0;side<2;side++)for(float x=-w/2+1;x<=w/2-1;x+=4)Beam(p.transform,"TimberColumn",new Vector3(x,level,side==0?-d/2:d/2),new Vector3(.42f,3.7f,.42f),0);
  Beam(p.transform,"FrontLintel",new Vector3(0,level+3.25f,-d/2),new Vector3(w,.45f,.45f),0);
  Beam(p.transform,"RearLintel",new Vector3(0,level+3.25f,d/2),new Vector3(w,.45f,.45f),0);
  for(int side=0;side<2;side++)Beam(p.transform,"SideLintel",new Vector3(side==0?-w/2+1:w/2-1,level+3.25f,0),new Vector3(.45f,.45f,d+1),0);
  Roof(p.transform,w+2,level+3.7f,(d+2)/6.4f);
  if(floor>0){Beam(p.transform,"Floor",new Vector3(0,level-.3f,0),new Vector3(w,.3f,d),0);for(int side=0;side<2;side++){Beam(p.transform,"BalconyRail",new Vector3(0,level+1,side==0?-d/2:d/2),new Vector3(w,.15f,.15f),0);for(float x=-w/2;x<=w/2;x+=1.2f)Beam(p.transform,"Baluster",new Vector3(x,level,side==0?-d/2:d/2),new Vector3(.12f,1,.12f),0);}}
 }
 p.transform.position=pos;
};
Pavilion("GatePavilion",new Vector3(180,15,120),20,5,1);
// 성문 좌우 받침은 양쪽10m, 가운데12m는 통행용으로 남긴다.
string stone=east+"Props/StoneStairsWalls/PF_EastLands_StoneWall_01.prefab";
for(int side=0;side<2;side++)for(int layer=0;layer<3;layer++)for(int x=0;x<3;x++)Fit(stone,root.transform,new Vector3((side==0?162:186)+x*4,10+layer*1.7f,120),new Vector3(4.1f,1.8f,5),0);
Pavilion("HighKeep",new Vector3(180,18,246),22,9,3);
// 단구 위의 성벽은 작은 굴곡으로 지형을 따르며 직사각형 외곽을 해체한다.
var perimeter=new[]{new Vector2(168,119),new Vector2(145,115),new Vector2(125,125),new Vector2(119,157),new Vector2(124,189),new Vector2(117,223),new Vector2(133,252),new Vector2(158,263),new Vector2(205,263),new Vector2(232,252),new Vector2(244,225),new Vector2(239,193),new Vector2(247,159),new Vector2(238,129),new Vector2(216,116),new Vector2(192,119)};
for(int segment=0;segment<perimeter.Length-1;segment++){
 var a=perimeter[segment];var b=perimeter[segment+1];int count=Mathf.CeilToInt(Vector2.Distance(a,b)/4);float yaw=-Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg;
 for(int step=0;step<count;step++){var point=Vector2.Lerp(a,b,(step+.5f)/count);for(int layer=0;layer<3;layer++)Fit(stone,root.transform,new Vector3(point.x,9.5f+layer*1.75f,point.y),new Vector3(Vector2.Distance(a,b)/count+.15f,1.85f,layer==0?3.2f:2.5f),yaw);
  if(step%2==0)Fit(stone,root.transform,new Vector3(point.x,14.8f,point.y),new Vector3(1.2f,.9f,1.1f),yaw);
 }
}
foreach(var p in new[]{new Vector3(125,15,125),new Vector3(238,15,129),new Vector3(133,15,252),new Vector3(232,15,252)})Pavilion("WatchPavilion",p,8,4,1);
// 기존 NPC/출구/도착점은 평탄한 생활층에 그대로 두고, 바깥 암반과 북쪽 본성 단구만 바꾼다.
int resolution=data.heightmapResolution;var heights=data.GetHeights(0,0,resolution,resolution);
for(int z=0;z<resolution;z++)for(int x=0;x<resolution;x++){
 float wx=x*data.size.x/(resolution-1),wz=z*data.size.z/(resolution-1);float h=heights[z,x]*data.size.y;
 if(wz>226&&wz<270&&wx>157&&wx<203){float across=Mathf.SmoothStep(0,1,Mathf.InverseLerp(157,165,wx))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(203,195,wx));float rise=Mathf.SmoothStep(0,1,Mathf.InverseLerp(226,242,wz));h=Mathf.Lerp(h,10+8*rise,across);}
 if(wz>112&&wz<264&&((wx>98&&wx<117)||(wx>249&&wx<266))){float edge=wx<180?Mathf.InverseLerp(117,103,wx):Mathf.InverseLerp(249,263,wx);h=Mathf.Lerp(h,2,Mathf.SmoothStep(0,1,edge));}
 heights[z,x]=h/data.size.y;
}
data.SetHeights(0,0,heights);UnityEditor.EditorUtility.SetDirty(data);
for(int side=0;side<2;side++)for(int i=0;i<9;i++){
 var obj=Fit(east+"Stones/Cliffs/PF_EastLands_Cliff_01.prefab",root.transform,new Vector3(side==0?114:250,-1,123+i*16),new Vector3(17,12,23),side==0?90:270);DisableCollision(obj);
}
// 성문 밖은 기다리는 수레와 검문·하역 자리로 나누고 출구 근처를 비운다.
for(int i=0;i<3;i++)Fit(east+"Props/WoodenProps/PF_EastLands_Carriage_01.prefab",root.transform,new Vector3(157+i*9,10,101-i%2*5),new Vector3(3.3f,1.7f,1.8f),i==1?15:0);
for(int i=0;i<8;i++)Place(props+(i%3==0?"TGV_Wood_Barrel_01A":"TGV_Wood_Crate_01A")+".prefab",root.transform,new Vector3(146+i%4*1.3f,10,105+i/4*1.4f),i*37,Vector3.one);
for(int i=0;i<4;i++)Place(east+"Props/Banners/PF_EastLands_Banner_01.prefab",root.transform,new Vector3(i<2?171:189,15,117+(i%2)*6),i<2?90:270,Vector3.one);
// 새 건물의 모든 인스턴스 변경을 기록해 재로드 때 축/재질이 되돌아가지 않게 한다.
foreach(var component in root.GetComponentsInChildren<Component>(true)){if(component==null)continue;UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(component);if(component is Transform)UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(component.gameObject);}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.AssetDatabase.SaveAssetIfDirty(data);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
System.IO.File.WriteAllLines(output+"/BUILD.txt",report.ToArray());
return "주택/누각/곡선 석축/암반 단구 반영. 후속 시각·경로 검수 필요";
