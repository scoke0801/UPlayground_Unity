// 수면 메시의 실제 정점을 따라 상류 연결부를 만들고, 기존 낙수 하부와 거품 UV를 보존한다.
const string Dir="Assets/10.Datas/World/RiverBasinWorld/VisualR3/";
const float ApproachLength=6f, OpaqueLead=3.5f, SurfaceOffset=0.008f;
const int RiverColumns=20, FallColumns=24;
var river=AssetDatabase.LoadAssetAtPath<Mesh>(Dir+"B1RiverUnified.asset");
var material=AssetDatabase.LoadAssetAtPath<Material>(Dir+"B1RiverFall_R3.mat");
var riverMaterial=AssetDatabase.LoadAssetAtPath<Material>(Dir+"B1RiverUnified_R3.mat");
if(river==null||material==null||riverMaterial==null)throw new InvalidOperationException("강과 폭포 에셋 누락");
var riverVertices=river.vertices;
var messages=new System.Text.StringBuilder();
for(int fallIndex=0;fallIndex<2;fallIndex++){
 var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Dir+"B1Waterfall_"+(fallIndex+1)+".asset");
 var oldVertices=mesh.vertices;var oldUV=mesh.uv;
 if(oldVertices.Length%(FallColumns+1)!=0)throw new InvalidOperationException("폭포 열 수 불일치");
 float lipZ=fallIndex==0?-109f:-147f;
 var vertices=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var triangles=new List<int>();
 var upperRows=new List<int>();
 for(int row=0;row<riverVertices.Length;row+=RiverColumns+1){float z=riverVertices[row].z;if(z>=lipZ-0.001f&&z<=lipZ+ApproachLength+0.001f)upperRows.Add(row);}
 if(upperRows.Count<5)throw new InvalidOperationException("낙구 상류 표본 부족");
 float startZ=riverVertices[upperRows[0]].z;
 foreach(int row in upperRows){float z=riverVertices[row].z;float along=(startZ-z)/(startZ-lipZ);
  float transition=Mathf.SmoothStep(0,1,Mathf.Clamp01((startZ-z)/(startZ-lipZ-OpaqueLead)));
  for(int col=0;col<=FallColumns;col++){
   float across=col/(float)FallColumns;float column=across*RiverColumns;int left=Mathf.Min((int)column,RiverColumns-1);
   var position=Vector3.Lerp(riverVertices[row+left],riverVertices[row+left+1],column-left)+Vector3.up*SurfaceOffset;
   vertices.Add(position);uv.Add(new Vector2(across,along*0.16f));colors.Add(new Color(transition,0,0,1));
  }
 }
 // 반복 실행에서도 기존 하부를 UV로 찾아 보존한다.
 for(int row=0;row<oldVertices.Length;row+=FallColumns+1){if(oldUV[row].y<0.189f)continue;
  for(int col=0;col<=FallColumns;col++){vertices.Add(oldVertices[row+col]);uv.Add(oldUV[row+col]);colors.Add(Color.red);}
 }
 int rowCount=vertices.Count/(FallColumns+1);
 for(int row=0;row<rowCount-1;row++)for(int col=0;col<FallColumns;col++){int a=row*(FallColumns+1)+col,b=a+FallColumns+1;triangles.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
 if(vertices.Any(p=>float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)))throw new InvalidOperationException("잘못된 폭포 정점");
 Undo.RegisterCompleteObjectUndo(mesh,"폭포 낙구 연결");mesh.Clear();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
 messages.AppendLine("폭포 "+(fallIndex+1)+": 상류 "+(startZ-lipZ)+"m / "+rowCount+"행 / "+vertices.Count+"정점");
}
Undo.RecordObject(material,"폭포 수면 합성 동기화");
foreach(string property in new[]{"ShallowColor","DeepColor","FoamColor"})material.SetColor("_River"+property,riverMaterial.GetColor("_"+property));
material.SetVector("_RiverWaveVelocity",riverMaterial.GetVector("_WaveVelocity"));
var sourceProperties=new[]{"DepthDistance","Absorption","SurfaceOpacity","EdgeDistance","WaveScale","WaveStrength","ReflectionStrength","RefractionStrength","FoamWidth","FoamStrength","FoamScale","FoamSpeed","WeatherWindResponse"};
var targetProperties=new[]{"DepthDistance","Absorption","Opacity","EdgeDistance","WaveScale","WaveStrength","Reflection","Refraction","FoamWidth","FoamStrength","FoamScale","FoamSpeed","WindResponse"};
for(int i=0;i<sourceProperties.Length;i++)material.SetFloat("_River"+targetProperties[i],riverMaterial.GetFloat("_"+sourceProperties[i]));
material.SetTexture("_NormalMap",riverMaterial.GetTexture("_NormalMap"));
material.SetColor("_RiverUnderwaterColor",riverMaterial.GetColor("_UnderwaterColor"));
material.SetFloat("_RiverUnderwaterOpacity",riverMaterial.GetFloat("_UnderwaterOpacity"));
material.SetFloat("_RiverFrontFace",river.normals[0].y>0?1:0);
EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();SceneView.RepaintAll();
return messages.ToString();

