var target=Terrain.activeTerrain.terrainData;var baseline=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/10.Datas/World/OpenWorld/ShadeCreekVisualR3/Terrain.asset");
var jt=AppDomain.CurrentDomain.GetAssemblies().Select(asm=>asm.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var pixels=target.alphamapTextures.Select((t,i)=>i<baseline.alphamapTextures.Length?baseline.alphamapTextures[i].GetPixels32():new Color32[1024*1024]).ToArray();
foreach(string n in new[]{"QuarryPassR1","CloudridgeR1","HarborReedR1"}){
var cells=(Dictionary<string,float[][]>)jt.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)}).Invoke(null,new object[]{System.IO.File.ReadAllText("output/WorldThemesABCIntegration/"+n+"-cells.json"),typeof(Dictionary<string,float[][]>)});
int beforeCount=System.IO.File.ReadAllLines("output/WorldThemesABCIntegration/"+n+"-before-layers.txt").Length;var after=System.IO.File.ReadAllLines("output/WorldThemesABCIntegration/"+n+"-after-layers.txt");
var map=after.Select(g=>Array.FindIndex(target.terrainLayers,l=>AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(l))==g)).ToArray();
foreach(var r in cells["alpha"]){int index=(int)r[1]*1024+(int)r[0];for(int t=0;t<pixels.Length;t++)pixels[t][index]=new Color32(0,0,0,0);for(int l=0;l<map.Length;l++){int layer=map[l];if(layer<0)throw new Exception("레이어 GUID 누락");var p=pixels[layer/4][index];byte value=(byte)Mathf.RoundToInt(r[2+beforeCount+l]*255);switch(layer%4){case 0:p.r=value;break;case 1:p.g=value;break;case 2:p.b=value;break;case 3:p.a=value;break;}pixels[layer/4][index]=p;}}
}
for(int i=0;i<pixels.Length;i++){var tex=target.alphamapTextures[i];tex.SetPixels32(pixels[i]);tex.Apply(false);EditorUtility.SetDirty(tex);AssetDatabase.SaveAssetIfDirty(tex);}
target.SetBaseMapDirty();EditorUtility.SetDirty(target);AssetDatabase.SaveAssetIfDirty(target);
return target.GetAlphamaps(151,642,1,1).Cast<float>().ToArray();
