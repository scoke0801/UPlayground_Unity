// R3b 폭포 B단계: 굴착된 지형에 맞춘 수면(폭은 실제 강둑과 만나는 지점까지)과 포물선 낙수면을 만든다.
// 낙수면 UV는 Polyart Waterfall 셰이더 규약(v=0 낙구, v=1 바닥)을 따르고, 원본 메시(폭23m·높이61m)와
// 무늬 비율이 같도록 u 밀도를 맞춘다. 수면은 B1RiverWater(LakeWater)를 재사용해 다리 아래 수면과 이어진다.
const float CX=307.2f,Z0=-85f;
var prof=new float[,]{{0,72,2.4f},{12,71.6f,1.6f},{24,68.6f,0.6f},{24.01f,59.6f,2.6f},{46,59.6f,2.6f},{62,57.2f,0.6f},{62.01f,51f,2.4f},{82,51,2.2f},{99.3f,50,1.6f}};
Func<float,int,float> P=(s,c)=>{int n=prof.GetLength(0);if(s<=prof[0,0])return prof[0,c];for(int i=1;i<n;i++)if(s<=prof[i,0]){float t=(s-prof[i-1,0])/(prof[i,0]-prof[i-1,0]);return Mathf.Lerp(prof[i-1,c],prof[i,c],t);}return prof[n-1,c];};
Func<float,float,float> H=(x,z)=>{var t=Terrain.activeTerrains.First(tt=>x>=tt.transform.position.x&&x<tt.transform.position.x+512&&z>=tt.transform.position.z&&z<tt.transform.position.z+512);return t.SampleHeight(new Vector3(x,0,z))+t.transform.position.y;};
Func<float,float,float,float> reach=(z,water,sign)=>{float d=0.5f;while(d<16f&&H(CX+sign*d,z)<water+0.2f)d+=0.25f;return Mathf.Min(d+0.6f,16.6f);};
string dir="Assets/10.Datas/World/RiverBasinWorld/VisualR3";
Func<string,Mesh> meshAsset=name=>{var p=dir+"/"+name+".asset";var m=AssetDatabase.LoadAssetAtPath<Mesh>(p);if(m==null){m=new Mesh();AssetDatabase.CreateAsset(m,p);}m.Clear();return m;};
var parent=GameObject.Find("GeneratedEnvironment/B1_ArtR1").transform;var old=parent.Find("B1 · 급류와 폭포");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
var root=new GameObject("B1 · 급류와 폭포");root.transform.SetParent(parent,false);Undo.RegisterCreatedObjectUndo(root,"R3b cascade");
Action<string,Mesh,Material> place=(n,m,mat)=>{var g=new GameObject(n);g.transform.SetParent(root.transform,false);g.AddComponent<MeshFilter>().sharedMesh=m;var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;};
// 1) 수면: 낙차로 끊긴 세 구간
var surfV=new List<Vector3>();var surfT=new List<int>();var surfUV=new List<Vector2>();const int Cols=16;
var spans=new[]{new[]{0f,24f},new[]{24.2f,62f},new[]{62.2f,99.3f}};var lipHalf=new Dictionary<float,float[]>();
foreach(var sp in spans){int baseRow=-1;for(float s=sp[0];;s+=1f){float ss=Mathf.Min(s,sp[1]);float z=Z0-ss;float wy=P(ss,1);float l=reach(z,wy,-1),r=reach(z,wy,1);
  if(Mathf.Abs(ss-24f)<0.01f)lipHalf[24f]=new[]{l,r};if(Mathf.Abs(ss-62f)<0.01f)lipHalf[62f]=new[]{l,r};
  int row=surfV.Count;for(int c=0;c<=Cols;c++){float x=Mathf.Lerp(CX-l,CX+r,c/(float)Cols);surfV.Add(new Vector3(x,wy,z));surfUV.Add(new Vector2(x/6f,z/6f));}
  if(baseRow>=0)for(int c=0;c<Cols;c++){int a=baseRow+c,b=row+c;surfT.AddRange(new[]{a,b,a+1,a+1,b,b+1});}baseRow=row;if(ss>=sp[1])break;}}
var surf=meshAsset("B1CascadeSurface");surf.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;surf.SetVertices(surfV);surf.SetUVs(0,surfUV);surf.SetTriangles(surfT,0);surf.RecalculateNormals();surf.RecalculateBounds();EditorUtility.SetDirty(surf);
place("급류 수면",surf,AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/RiverBasinWorld/VisualR1/B1RiverWater.mat"));
// 2) 낙수면 재질 (전용 복제본)
Func<string,string,Material> cloneMat=(src,name)=>{var p=dir+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(m==null){AssetDatabase.CopyAsset(src,p);m=AssetDatabase.LoadAssetAtPath<Material>(p);}return m;};
var fallMat=cloneMat("Assets/ExternalAssets/Environment/PolyartStudio/Farmlands/Materials/Water/Materials/Waterfall 01/MI_Waterfall_01.mat","B1Waterfall_R3");
// 3) 낙수면: 낙구에서 포물선으로 떨어져 아래 소 수면 밑 0.6m까지
var falls=new[]{new[]{24f,68.6f,59.6f,1.7f},new[]{62f,57.2f,51f,1.3f}};int fi=0;
foreach(var f in falls){fi++;float s0=f[0],top=f[1],bottom=f[2]-0.6f,thr=f[3];var lr=lipHalf[s0];float l=lr[0]*0.9f,r=lr[1]*0.9f;float lipZ=Z0-s0;
 var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();const int Rows=18,C=14;float uSpan=(l+r)*(61f/23f)/(top-bottom);
 for(int i=0;i<=Rows;i++){float k=i/(float)Rows;
  // 낙구는 둥글게 넘어가고(첫 구간 수평 이동 위주), 이후 포물선 낙하
  float y=top-(top-bottom)*k*k;float z=lipZ-thr*k-0.25f*Mathf.Sin(k*Mathf.PI*0.5f);
  for(int c=0;c<=C;c++){float a=c/(float)C;float wob=(Mathf.PerlinNoise(a*3.1f+fi*7f,k*2.3f)-0.5f)*0.5f;float x=Mathf.Lerp(CX-l,CX+r,a)+(c==0||c==C?wob*(0.6f+k):0)-(c==0?-k*0.35f:c==C?k*0.35f:0);
   v.Add(new Vector3(x,y,z+wob*0.3f*k));uv.Add(new Vector2(a*uSpan,k));}}
 for(int i=0;i<Rows;i++)for(int c=0;c<C;c++){int a=i*(C+1)+c,b=a+C+1;t.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
 // 위·아래에서 모두 보이도록 뒷면도 만든다(Polyart 셰이더가 단면 컬링).
 int n=v.Count;var back=new List<int>();for(int k=0;k<t.Count;k+=3)back.AddRange(new[]{t[k]+n,t[k+2]+n,t[k+1]+n});v.AddRange(v.ToArray());uv.AddRange(uv.ToArray());t.AddRange(back);
 var m=meshAsset("B1Waterfall_"+fi);m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();EditorUtility.SetDirty(m);
 place("낙수면 "+fi,m,fallMat);}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);AssetDatabase.SaveAssets();
return "surface verts "+surfV.Count+" lip1 "+string.Join(",",lipHalf[24f])+" lip2 "+string.Join(",",lipHalf[62f]);
