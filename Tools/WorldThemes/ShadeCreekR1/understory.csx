// 바람 셰이더를 가진 기존 숲풀을 전용 Detail로 연결하고 전투·통행 여백은 비운다.
var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string dir="Assets/10.Datas/World/ThemeStudies/ShadeCreekR1/";
if(s.path!="Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR1.unity"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("검토 씬 필요");
var t=Terrain.activeTerrain;var d=t.terrainData;if(AssetDatabase.GetAssetPath(d)!=dir+"Terrain.asset")throw new Exception("전용 Terrain 필요");
var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Prefabs/Foliage/PF_Farm_ForestSmallGrass.prefab");
var go=UnityEngine.Object.Instantiate(source);var m=UnityEngine.Object.Instantiate(go.GetComponentInChildren<Renderer>().sharedMaterial);
m.SetColor("_Color_Tint_Top",new Color(.40f,.57f,.43f));m.SetColor("_Color_Tint_Bottom",new Color(.16f,.29f,.20f));m.enableInstancing=true;
AssetDatabase.CreateAsset(m,dir+"ForestGrass.mat");foreach(var r in go.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=m;
var prefab=PrefabUtility.SaveAsPrefabAsset(go,dir+"ForestGrass.prefab");UnityEngine.Object.DestroyImmediate(go);
var prototypes=d.detailPrototypes.ToList();var proto=new DetailPrototype(prototypes[0]);proto.prototype=prefab;proto.usePrototypeMesh=true;proto.useInstancing=true;proto.minHeight=.85f;proto.maxHeight=1.4f;proto.minWidth=1;proto.maxWidth=1.5f;prototypes.Add(proto);d.detailPrototypes=prototypes.ToArray();
var route=new[]{new Vector2(1700,740),new Vector2(1640,750),new Vector2(1605,805),new Vector2(1650,855),new Vector2(1750,870)};
Func<float,float,float> distance=(x,z)=>{var p=new Vector2(x,z);float best=999;for(int i=0;i<route.Length-1;i++){var v=route[i+1]-route[i];best=Mathf.Min(best,Vector2.Distance(p,route[i]+v*Mathf.Clamp01(Vector2.Dot(p-route[i],v)/v.sqrMagnitude)));}return best;};
var enemies=AssetDatabase.LoadAssetAtPath<UPlayGround.Data.World.WorldPlacementDataSO>(AssetDatabase.GUIDToAssetPath("2819d64223022c84b9a62983354d6ecb")).Records.Where(r=>r.position.x>1500&&r.position.x<1810&&r.position.z>680&&r.position.z<980).Select(r=>r.position).ToArray();
var density=new int[d.detailHeight,d.detailWidth];int cells=0;
for(int z=0;z<d.detailHeight;z++)for(int x=0;x<d.detailWidth;x++){
float wx=x*d.size.x/(d.detailWidth-1),wz=-700+z*d.size.z/(d.detailHeight-1),r=Mathf.Sqrt(Mathf.Pow((wx-1655)/145,2)+Mathf.Pow((wz-810)/115,2));if(r>=1)continue;
float rd=distance(wx,wz);if(rd<3.7f)continue;
float weight=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.8f,1,r)))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(3.7f,8,rd));
foreach(var p in enemies)weight*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(12,20,Vector2.Distance(new Vector2(wx,wz),new Vector2(p.x,p.z))));
foreach(var p in new[]{new Vector2(1605,805),new Vector2(1608,814)})weight*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,6,Vector2.Distance(new Vector2(wx,wz),p)));
float patches=Mathf.PerlinNoise(wx*.105f+17,wz*.105f+9);weight*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.26f,.64f,patches));
density[z,x]=Mathf.RoundToInt(weight*22);if(density[z,x]>0)cells++;
}d.SetDetailLayer(0,0,prototypes.Count-1,density);EditorUtility.SetDirty(d);AssetDatabase.SaveAssetIfDirty(d);
return new{cells,prototype=prototypes.Count-1,height="기존 0.36m 메시 ×0.85~1.4",wind="GrassGraph 기존 바람 설정 유지",combat="적 중심 12m·보행선 3.7m 비움"};

