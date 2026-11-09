// 실제 NavMesh 우회 경로를 따라 생활 흙길을 연결한다. 지지면 높이는 바꾸지 않는다.
var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var d=Terrain.activeTerrain.terrainData;
if(EditorApplication.isPlayingOrWillChangePlaymode||s.isDirty||!AssetDatabase.GetAssetPath(d).Contains("VillageConnectionR1"))throw new Exception("검토 사본 필요");
var route=new[]{new Vector2(1082,400),new Vector2(1073,407),new Vector2(1064,412),new Vector2(1049,420)};
Func<Vector2,float> distance=p=>{float nearest=9999;for(int i=1;i<route.Length;i++){var ab=route[i]-route[i-1];float f=Mathf.Clamp01(Vector2.Dot(p-route[i-1],ab)/ab.sqrMagnitude);nearest=Mathf.Min(nearest,Vector2.Distance(p,route[i-1]+f*ab));}return nearest;};
var a=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);int changed=0;
for(int z=0;z<a.GetLength(0);z++)for(int x=0;x<a.GetLength(1);x++){
float wx=x*2400f/(a.GetLength(1)-1),wz=-700+z*2800f/(a.GetLength(0)-1);if(wz<393||wz>423)continue;
float dist=distance(new Vector2(wx,wz));float width=1.6f+.7f*Mathf.PerlinNoise(wx*.06f,wz*.08f);float weight=(1-Mathf.SmoothStep(0,1,Mathf.Clamp01((dist-width)/2.2f)))*.8f;
weight*=Mathf.Clamp01((425-wz)/5);if(weight<=0)continue;for(int l=0;l<a.GetLength(2);l++)a[z,x,l]*=1-weight;a[z,x,11]+=weight;changed++;
}
d.SetAlphamaps(0,0,a);
for(int l=0;l<d.detailPrototypes.Length;l++){var map=d.GetDetailLayer(0,0,d.detailWidth,d.detailHeight,l);for(int z=0;z<d.detailHeight;z++)for(int x=0;x<d.detailWidth;x++){float wx=(x+.5f)*2400/d.detailWidth,wz=-700+(z+.5f)*2800/d.detailHeight;if(wz>393&&wz<423&&distance(new Vector2(wx,wz))<2)map[z,x]=0;}d.SetDetailLayer(0,0,l,map);}
EditorUtility.SetDirty(d);AssetDatabase.SaveAssetIfDirty(d);return changed;


