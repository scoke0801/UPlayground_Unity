var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.name!="SaltwindHarbor"||Application.isPlaying)throw new System.Exception("항구 Edit Mode 필요");
if(GameObject.Find("HarborEastArt")!=null)throw new System.Exception("이미 적용됨");
var environment=GameObject.Find("CityEnvironment");var terrain=environment.GetComponentInChildren<Terrain>();var data=terrain.terrainData;
var root=new GameObject("HarborEastArt");root.transform.SetParent(environment.transform,false);
string kit="Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Buildings/Building Kit/";
string props="Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Props/Exterior Props/";
string east="Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Prefabs/";
string roofPath="Assets/08.Models/WorldCities/EastStyle/";
string directory="Assets/10.Datas/World/WorldCities/SaltwindHarbor/EastArt";System.IO.Directory.CreateDirectory(directory);
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
var originalBuildings=environment.transform.Cast<Transform>().Where(t=>t.name=="HarborHouse"||t.name=="HarborWarehouse").ToArray();
foreach(var t in originalBuildings){report.Add("교체 인스턴스 보존: "+t.name+" "+t.position);t.gameObject.SetActive(false);UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);}
int hr=data.heightmapResolution;var heights=data.GetHeights(0,0,hr,hr);
for(int z=0;z<hr;z++)for(int x=0;x<hr;x++){
 float wx=x*data.size.x/(hr-1),wz=z*data.size.z/(hr-1),h=heights[z,x]*data.size.y;
 if(wx>108&&wx<151&&wz>125&&wz<215){float hill=Mathf.SmoothStep(0,1,Mathf.InverseLerp(108,121,wx))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(151,137,wx))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(125,145,wz))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(215,197,wz));h=Mathf.Max(h,10+7*hill);}
 if(wx>215&&wx<265&&wz>156&&wz<221){float saltWeight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(215,226,wx))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(265,253,wx))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(156,170,wz))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(221,210,wz));h=Mathf.Lerp(h,6,saltWeight);}
 heights[z,x]=h/data.size.y;
}data.SetHeights(0,0,heights);
int houseIndex=0;
foreach(var old in originalBuildings){
 var p=old.position;int variant=houseIndex%2==0?0:2;
 if(p.x<145)p.y=terrain.SampleHeight(p);
 if(houseIndex%3==0){var obj=Place("Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Buildings/Presets/House_0"+(houseIndex%2+1)+".prefab",root.transform,p,old.position.x<180?90:270,Vector3.one*.85f);obj.name="CoastalThatchedHouse_"+houseIndex;}
 else{var house=House("CoastalTileHouse_"+houseIndex,p,old.position.x<180?90:270,variant);house.transform.localScale=new Vector3(1.1f,.95f,1.15f);}
 houseIndex++;
}
foreach(var p0 in new[]{new Vector3(124,0,154),new Vector3(124,0,178),new Vector3(127,0,199)}){var p=p0;p.y=terrain.SampleHeight(p);House("HillHouse_"+houseIndex++,p,90,houseIndex%2==0?0:2);}
foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name=="TGV_Door_01A")t.gameObject.SetActive(false);
// 지형 변경 지점의 기존 시각 소품만 접지한다. 주민과 진행 오브젝트는 이동하지 않는다.
foreach(var group in new[]{environment.transform,environment.transform.Find("CityDetails"),environment.transform.Find("HarborLife")})foreach(Transform t in group){if(!t.gameObject.activeSelf||t==root.transform||t.name.Contains("Boundary")||t.GetComponent<Terrain>()!=null||t.position.y>13)continue;var p=t.position;if((p.x>108&&p.x<151&&p.z>125&&p.z<215)||(p.x>215&&p.x<265&&p.z>156&&p.z<221)){p.y=terrain.SampleHeight(p);t.position=p;UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(t);}}
string stone=east+"Props/StoneStairsWalls/PF_EastLands_StoneWall_01.prefab";
for(int i=0;i<10;i++){var p=new Vector3(147,0,142+i*6);p.y=terrain.SampleHeight(p)-1.1f;Fit(stone,root.transform,p,new Vector3(6.1f,1.4f,1),90);}
// 기존 길 오른쪽의 절벽이 시야를 가리고 왼쪽 굽이를 지나면 포구가 열린다.
for(int i=0;i<3;i++){var obj=Fit(east+"Stones/Cliffs/PF_EastLands_Cliff_01.prefab",root.transform,new Vector3(196+i*3,8,87+i*10),new Vector3(18,15+i*2,22),15+i*20);DisableCollision(obj);}
int ar=data.alphamapResolution;var paint=data.GetAlphamaps(0,0,ar,ar);
for(int z=0;z<ar;z++)for(int x=0;x<ar;x++){
 float wx=x*data.size.x/(ar-1),wz=z*data.size.z/(ar-1);
 if(wz>74&&wz<128&&wx>158&&wx<199){float center=180-9*Mathf.Sin(Mathf.PI*Mathf.InverseLerp(74,128,wz));float road=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,5,Mathf.Abs(wx-center)));paint[z,x,0]=1-road;paint[z,x,1]=road;for(int l=2;l<paint.GetLength(2);l++)paint[z,x,l]=0;}
 if(wx>118&&wx<147&&wz>135&&wz<211){float lane=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(2,3.5f,Mathf.Abs(wx-135)));float loop=Mathf.Max(1-Mathf.Clamp01(Mathf.Abs(wz-140)/3),1-Mathf.Clamp01(Mathf.Abs(wz-207)/3));float road=Mathf.Max(lane,loop);paint[z,x,0]=1-road;paint[z,x,1]=road;for(int l=2;l<paint.GetLength(2);l++)paint[z,x,l]=0;}
}data.SetAlphamaps(0,0,paint);UnityEditor.EditorUtility.SetDirty(data);UnityEditor.AssetDatabase.SaveAssetIfDirty(data);
var salt=new Material(Shader.Find("Universal Render Pipeline/Lit"));salt.name="SaltCrust";salt.color=new Color(.88f,.87f,.76f);salt.SetFloat("_Smoothness",.08f);UnityEditor.AssetDatabase.CreateAsset(salt,directory+"/SaltCrust.mat");
var sea=environment.transform.Find("CoastalWater").GetComponent<Renderer>().sharedMaterial;
var brine=new Material(sea);brine.name="SaltPanBrine";brine.SetColor("_ShallowColor",new Color(.42f,.58f,.48f));brine.SetColor("_DeepColor",new Color(.34f,.46f,.39f));brine.SetFloat("_SurfaceOpacity",.8f);brine.SetFloat("_WaveStrength",.02f);brine.SetFloat("_FoamStrength",.05f);UnityEditor.AssetDatabase.CreateAsset(brine,directory+"/SaltPanBrine.mat");
var planeMesh=environment.transform.Find("CoastalWater").GetComponent<MeshFilter>().sharedMesh;
string rock="Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Rocks/TGV_Rock_Large_01A.prefab";
for(int i=0;i<6;i++){
 var p=new Vector3(232+(i%2)*12,6.05f,176+(i/2)*12);var pan=new GameObject("SaltPan_"+i);pan.transform.SetParent(root.transform,false);pan.transform.position=p;
 pan.AddComponent<MeshFilter>().sharedMesh=planeMesh;pan.AddComponent<MeshRenderer>().sharedMaterial=i<4?brine:salt;pan.transform.localScale=new Vector3(.95f,1,.9f);
 for(int edge=0;edge<4;edge++)Fit(stone,root.transform,p+new Vector3(edge<2?(edge==0?-5.2f:5.2f):0,-.15f,edge>=2?(edge==2?-5:5):0),new Vector3(edge<2?10.4f:10.8f,.5f,.6f),edge<2?90:0);
 if(i>=4){var pile=Fit(rock,root.transform,p+new Vector3(1,.1f,0),new Vector3(2.2f,.9f,2),i*33);foreach(var renderer in pile.GetComponentsInChildren<Renderer>())renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>salt).ToArray();DisableCollision(pile);}
}
// 염전 물목은 바다와 시각적으로 이어지는 좁은 수로이며 새 낚시/수영 규칙은 추가하지 않는다.
var channel=new GameObject("BrineInlet");channel.transform.SetParent(root.transform,false);channel.transform.position=new Vector3(251,5.95f,218);channel.transform.localScale=new Vector3(.35f,1,2.2f);channel.AddComponent<MeshFilter>().sharedMesh=planeMesh;channel.AddComponent<MeshRenderer>().sharedMaterial=brine;
// 수선 배와 실제 밧줄을 쓰며 수선 주민 옆1.5m는 비운다.
var boat=Fit(east+"Props/Boat/PF_EastLands_Boat.prefab",root.transform,new Vector3(141,10,213),new Vector3(7,2.8f,3.5f),15);boat.name="HauledRepairBoat";
for(int i=0;i<4;i++)Fit(east+"Props/Ropes/PF_EastLands_Rope_0"+(i%3+1)+".prefab",root.transform,new Vector3(137+i*3,10.08f,219),new Vector3(1.2f,.12f,1),i*40);
var rack=new GameObject("NetDryingRack");rack.transform.SetParent(root.transform,false);rack.transform.position=new Vector3(217,10,219);
for(int i=0;i<3;i++)Beam(rack.transform,"NetPost",new Vector3(i*3,0,0),new Vector3(.18f,3,.18f),0);
Beam(rack.transform,"NetCrossbar",new Vector3(3,2.8f,0),new Vector3(6.4f,.17f,.17f),0);
for(int i=0;i<=14;i++){var cord=Fit(props+"TGV_Rope_01A.prefab",root.transform,new Vector3(217+i*.42f,10.55f,219.08f),new Vector3(.025f,2.1f,.025f),0);DisableCollision(cord);}
for(int i=0;i<=7;i++){var cord=Fit(props+"TGV_Rope_01A.prefab",root.transform,new Vector3(220,10.55f+i*.3f,219.08f),new Vector3(6,.025f,.025f),0);DisableCollision(cord);}
var dryRack=new GameObject("FishDryingFrame");dryRack.transform.SetParent(root.transform,false);dryRack.transform.position=new Vector3(226,10,225);for(int i=0;i<2;i++)Beam(dryRack.transform,"DryingPost",new Vector3(i*4,0,0),new Vector3(.16f,2.3f,.16f),0);Beam(dryRack.transform,"DryingBeam",new Vector3(2,2.2f,0),new Vector3(4.4f,.14f,.14f),0);
report.Add("마른 생선 신규 모델: Blender MCP 연결 실패로 미수행. 빈 덕장 프레임만 배치.");
// 높이를 달리한 주거 구역의 바람막이와 누름돌.
foreach(var house in root.transform.Cast<Transform>().Where(t=>t.name.StartsWith("CoastalTileHouse")||t.name.StartsWith("HillHouse"))){for(int i=0;i<4;i++){var p=house.TransformPoint(new Vector3(-2+i*1.3f,4.25f,i%2==0?1.4f:-1.4f));var stoneWeight=Fit(rock,root.transform,p,new Vector3(.5f,.22f,.4f),i*42);stoneWeight.name="RoofWeight";DisableCollision(stoneWeight);}}
for(int i=0;i<8;i++){var p=new Vector3(i<4?113:262,0,135+(i%4)*27);p.y=terrain.SampleHeight(p);var tree=Fit(east+"Foliage/Trees/Conifers/PF_EastLands_Conifer_Multi_01.prefab",root.transform,p,new Vector3(6,10,6),i*41);DisableCollision(tree);}
// 전용 바다의 색/흡수 거리만 조정해 해저 끝에서 갑자기 바뀌는 색을 줄인다.
sea.SetFloat("_DepthDistance",1.5f);sea.SetFloat("_Absorption",1.2f);sea.SetFloat("_SurfaceOpacity",.75f);UnityEditor.EditorUtility.SetDirty(sea);UnityEditor.AssetDatabase.SaveAssetIfDirty(sea);
foreach(var c in environment.GetComponentsInChildren<Component>(true)){if(c==null)continue;if(c is Transform||c is Renderer||c is Collider||c is LODGroup)UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(c);if(c is Transform)UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(c.gameObject);}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);System.IO.File.WriteAllLines("output/HarborEast/BUILD.txt",report.ToArray());
return "항구 주거 언덕/해안 가옥/염전/수선 배/건조대 반영. 경로와 시각 검수 필요";
