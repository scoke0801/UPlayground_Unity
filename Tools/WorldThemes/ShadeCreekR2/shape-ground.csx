// R2 전용 지면에서 활동 공간의 밟힌 중심과 수관 아래 부식층을 연결한다.
var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
string dir="Assets/10.Datas/World/ThemeStudies/ShadeCreekR2/";
if(s.path!="Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR2.unity"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("R2 Edit Mode 필요");
var t=Terrain.activeTerrain;var d=t.terrainData;if(AssetDatabase.GetAssetPath(d)!=dir+"Terrain.asset")throw new Exception("전용 Terrain 필요");
var a=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);int n=d.alphamapLayers-4;
var trees=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LODGroup>(true)).Where(g=>g.transform.position.x>1550&&g.transform.position.x<1740&&g.transform.position.z>730&&g.transform.position.z<900).Select(g=>g.transform.position).ToArray();
int cells=0;
for(int z=0;z<d.alphamapHeight;z++)for(int x=0;x<d.alphamapWidth;x++){
float wx=x*d.size.x/(d.alphamapWidth-1),wz=-700+z*d.size.z/(d.alphamapHeight-1);
float scope=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.72f,1,Mathf.Sqrt(Mathf.Pow((wx-1640)/100,2)+Mathf.Pow((wz-810)/90,2))));if(scope<=0)continue;
float nearest=100;foreach(var p in trees)nearest=Mathf.Min(nearest,Vector2.Distance(new Vector2(wx,wz),new Vector2(p.x,p.z)));
float organic=Mathf.PerlinNoise(wx*.045f,wz*.045f);float roots=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,12+organic*7,nearest));
float weight=scope*Mathf.Clamp01(roots*.8f+organic*.35f);float shift=a[z,x,n+2]*weight;a[z,x,n+2]-=shift;a[z,x,n]+=shift*.85f;a[z,x,n+1]+=shift*.15f;cells++;
}
d.SetAlphamaps(0,0,a);d.SetBaseMapDirty();t.Flush();EditorUtility.SetDirty(d);AssetDatabase.SaveAssetIfDirty(d);
var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(dir+"CreekSurface.asset");var v=mesh.vertices;
for(int row=0;row<145;row++){int start=row*13;float z=v[start].z;float middle=(v[start].x+v[start+12].x)*.5f;float half=(v[start+12].x-v[start].x)*.5f;float spur=.65f*Mathf.Exp(-Mathf.Pow((z-789)/4.5f,2))+.55f*Mathf.Exp(-Mathf.Pow((z-811)/4,2));float opposite=.5f*Mathf.Exp(-Mathf.Pow((z-800)/4,2));for(int col=0;col<13;col++){float u=col/12f;v[start+col].x=middle+half*((u*2-1)*(1-spur-opposite)+spur-opposite);}}
mesh.vertices=v;mesh.RecalculateBounds();mesh.RecalculateNormals();mesh.RecalculateTangents();EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(s);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s);return new{cells,trees=trees.Length,stage="초기 형태·지면 후보. 단면·접지와 실제 화면 추가 검수 필요"};
