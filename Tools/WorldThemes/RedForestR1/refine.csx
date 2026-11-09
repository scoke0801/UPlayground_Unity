// 첫 화면에서 드러난 평평한 흙판과 출구 사면의 각진 접합을 고친다.
var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(EditorApplication.isPlayingOrWillChangePlaymode||s.path!="Assets/01.Scenes/ArtReview/WorldThemes/RedForestR1.unity")throw new Exception("검토 사본 필요");
var t=Terrain.activeTerrain;var d=t.terrainData;string dir="Assets/10.Datas/World/ThemeStudies/RedForestR1";
var route=new[]{new Vector2(1150,525),new Vector2(1152.8f,558.4f),new Vector2(1152.8f,560.4f),new Vector2(1135,580)};
Func<float,float,float> distance=(x,z)=>{var p=new Vector2(x,z);float best=999;for(int i=0;i<route.Length-1;i++){var v=route[i+1]-route[i];best=Mathf.Min(best,Vector2.Distance(p,route[i]+v*Mathf.Clamp01(Vector2.Dot(p-route[i],v)/v.sqrMagnitude)));}return best;};
var all=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var fixedBounds=all.SelectMany(g=>g.GetComponents<Collider>()).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger&&!(c is TerrainCollider)&&c.bounds.size.x<70&&c.bounds.size.z<70).Select(c=>c.bounds).Where(b=>b.max.x>1100&&b.min.x<1200&&b.max.z>546&&b.min.z<586).ToArray();
var h=d.GetHeights(0,0,d.heightmapResolution,d.heightmapResolution);int edited=0;
for(int z=0;z<h.GetLength(0);z++)for(int x=0;x<h.GetLength(1);x++){
float wx=x*2400f/(h.GetLength(1)-1),wz=-700+z*2800f/(h.GetLength(0)-1);if(wx<1115||wx>1185||wz<550||wz>582)continue;
float edge=Mathf.Min(wx-1115,1185-wx,wz-550,582-wz);float w=Mathf.SmoothStep(0,1,edge/9)*Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,9,distance(wx,wz)));
foreach(var b in fixedBounds){float dx=Mathf.Max(b.min.x-wx,0,wx-b.max.x),dz=Mathf.Max(b.min.z-wz,0,wz-b.max.z);w*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1,5,Mathf.Sqrt(dx*dx+dz*dz)));}
float target=65+(wz-550)*.50f+Mathf.Abs(wx-1150)*.045f;float next=Mathf.Lerp(h[z,x]*400,Mathf.Min(h[z,x]*400,target),w*.90f);if(Mathf.Abs(next-h[z,x]*400)>.001f){h[z,x]=next/400;edited++;}
}
d.SetHeights(0,0,h);
var names=new[]{"ForestHumus","RootEarth","ClearingEarth"};var colors=new[]{new Vector4(.27f,.255f,.245f,1),new Vector4(.46f,.405f,.355f,1),new Vector4(.70f,.64f,.56f,1)};
for(int i=0;i<names.Length;i++){var l=AssetDatabase.LoadAssetAtPath<TerrainLayer>(dir+"/"+names[i]+".terrainlayer");l.diffuseRemapMax=colors[i];EditorUtility.SetDirty(l);AssetDatabase.SaveAssetIfDirty(l);}
var a=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);int n=d.terrainLayers.Length;
for(int z=0;z<a.GetLength(0);z++)for(int x=0;x<a.GetLength(1);x++){
float wx=x*2400f/(a.GetLength(1)-1),wz=-700+z*2800f/(a.GetLength(0)-1);float amount=0;for(int j=n-4;j<n;j++)amount+=a[z,x,j];if(amount<=0)continue;
float r=Mathf.Sqrt(Mathf.Pow((wx-1150)/23,2)+Mathf.Pow((wz-526)/30,2));float clear=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.30f,1,r));float noise=Mathf.PerlinNoise(wx*.13f,wz*.13f);
float path=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1,5,distance(wx,wz))))*.50f;float earth=Mathf.Max(clear*(.48f+.38f*noise),path);float rock=a[z,x,n-1]/amount;float roots=(1-earth-rock)*(.20f+.25f*noise);a[z,x,n-4]=amount*(1-earth-rock-roots);a[z,x,n-3]=amount*roots;a[z,x,n-2]=amount*earth;
}
d.SetAlphamaps(0,0,a);EditorUtility.SetDirty(d);AssetDatabase.SaveAssetIfDirty(d);
var rockTransform=all.Single(q=>q.name=="RockDebris_187_R2");var rp=rockTransform.position;rockTransform.rotation=Quaternion.FromToRotation(Vector3.up,d.GetInterpolatedNormal(rp.x/2400,(rp.z+700)/2800))*Quaternion.Euler(0,34,0);rockTransform.localScale*=.65f;rp.y=t.SampleHeight(rp)-.8f;rockTransform.position=rp;PrefabUtility.RecordPrefabInstancePropertyModifications(rockTransform);
// 큰 구성에 기여하지 않는 원색 풀 포기는 이번 사면에서만 숨긴다.
int grass=0;foreach(var q in all){if(!q.name.StartsWith("Forest_Floor_")&&!q.name.StartsWith("Path_LeafLitter_"))continue;var p=q.position;if(p.x<1100||p.x>1210||p.z<480||p.z>610)continue;string asset=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(q.gameObject);if(!asset.Contains("Grass"))continue;q.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(q.gameObject);grass++;}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(s);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s);return new{edited,hiddenGrass=grass};
