import fs from 'node:fs';
let s=fs.readFileSync('Tools/WorldThemes/ShadeCreekR3/shape-floor.csx','utf8');s=s.slice(0,s.indexOf('var heights='));
s+=`
var original=AssetDatabase.LoadAssetAtPath<Mesh>(dir.Replace("R3/","R2/")+"CreekSurface.asset");var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(dir+"CreekSurface.asset");var vertices=original.vertices;
if(vertices.Length!=145*13)throw new Exception("수면 격자 변경");
var centers=new Vector3[145];var widths=new float[145];
for(int row=0;row<145;row++){
 float z=vertices[row*13+6].z;
 float x=1630+7*Mathf.Sin((z-760)*.092f+.6f);
 float end=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,8,Mathf.Min(z-761,833-z)));
 float width=(3.1f+1.15f*Mathf.Sin((z-761)*.15f+.8f))*end+.12f;
 float y=source.GetInterpolatedHeight(x/2400,(z+700)/2800)+.06f;
 centers[row]=new Vector3(x,y,z);widths[row]=width;
 for(int c=0;c<13;c++)vertices[row*13+c]=new Vector3(x+(c/12f-.5f)*width,y,z);
}
var heights=d.GetHeights(0,0,d.heightmapResolution,d.heightmapResolution);int changed=0;
for(int z=0;z<d.heightmapResolution;z++){
 float wz=-700+z*d.size.z/(d.heightmapResolution-1);if(wz<761||wz>833)continue;
 int row=Mathf.Clamp(Mathf.RoundToInt((wz-761)*2),0,144);var center=centers[row];float half=widths[row]*.5f;
 for(int x=0;x<d.heightmapResolution;x++){
 float wx=x*d.size.x/(d.heightmapResolution-1);float distance=Mathf.Abs(wx-center.x);if(distance>half+3)continue;
 float weight=protection(new Vector2(wx,wz));if(weight<=0)continue;
 float cross=distance/Mathf.Max(.1f,half);float targetHeight=center.y-.42f+.6f*Mathf.SmoothStep(0,1,Mathf.Clamp01(cross));
 float edge=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(half,half+3,distance));
 float old=heights[z,x]*d.size.y;heights[z,x]=Mathf.Lerp(old,targetHeight,weight*edge)/d.size.y;changed++;
 }
}
d.SetHeights(0,0,heights);mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);EditorUtility.SetDirty(d);AssetDatabase.SaveAssetIfDirty(d);terrain.Flush();
var water=AssetDatabase.LoadAssetAtPath<Material>(dir+"CreekWater.mat");water.SetFloat("_AmbientFloor",.38f);water.SetFloat("_SurfaceOpacity",.4f);water.SetFloat("_DepthDistance",.45f);water.SetFloat("_ShoreFade",.35f);water.SetFloat("_ReflectionStrength",.25f);EditorUtility.SetDirty(water);AssetDatabase.SaveAssetIfDirty(water);
var rows=new List<object>();for(int i=0;i<145;i++){var p=centers[i];rows.Add(new{row=i,x=p.x,z=p.z,y=p.y,width=widths[i],depth=p.y-d.GetInterpolatedHeight(p.x/2400,(p.z+700)/2800)});}
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);System.IO.File.WriteAllText("output/ShadeCreekR3/water-section-candidate.json",(string)json.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{new{changed,rows,scope="보호 마스크 밖 하상·단면과 수면 메시 후보. 화면·물가 복귀 미검증"}}));return new{changed,rows=rows.Count};
`;
fs.writeFileSync('Tools/WorldThemes/ShadeCreekR3/shape-water.csx',s);
