// 돌출 둑의 마른 윗면과 젖은 가장자리를 연결한다. 기존 보행 높이는 바꾸지 않는다.
string dir="Assets/10.Datas/World/ThemeStudies/ShadeCreekR2/";var t=Terrain.activeTerrain;var d=t.terrainData;if(AssetDatabase.GetAssetPath(d)!=dir+"Terrain.asset")throw new Exception("전용 지형 필요");
var a=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);
for(int z=0;z<d.alphamapHeight;z++)for(int x=0;x<d.alphamapWidth;x++){float wx=x*d.size.x/(d.alphamapWidth-1),wz=-700+z*d.size.z/(d.alphamapHeight-1);float w=Mathf.Exp(-Mathf.Pow((wx-1620)/6,2)-Mathf.Pow((wz-797)/4,2))+.65f*Mathf.Exp(-Mathf.Pow((wx-1636)/5,2)-Mathf.Pow((wz-772)/3.5f,2));w=Mathf.Min(.9f,w);if(w<.02f)continue;for(int l=0;l<d.alphamapLayers;l++)a[z,x,l]*=1-w;a[z,x,19]+=w*.55f;a[z,x,15]+=w*.35f;a[z,x,18]+=w*.1f;}
d.SetAlphamaps(0,0,a);d.SetBaseMapDirty();t.Flush();EditorUtility.SetDirty(d);AssetDatabase.SaveAssetIfDirty(d);return "둑의 재료 연결";
