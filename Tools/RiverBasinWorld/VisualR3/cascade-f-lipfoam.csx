// R3e 낙구 거품 띠: 위 수면(LakeWater)과 낙수면(Polyart Waterfall)은 셰이더가 달라 만나는 선이 보였다.
// Polyart 원본 폭포도 같은 이음을 Upper Foam(Foam 셰이더)으로 덮는다. 낙구 호를 따라 위 수면 1.4m ~ 낙수면 0.9m 띠를 깐다.
const float Z0=-85f;
var lipLines=System.IO.File.ReadAllLines("output/RiverBasinWorld/VisualR3/cascade-lips.txt");var inv=System.Globalization.CultureInfo.InvariantCulture;
Func<int,float[]> lipOf=i=>lipLines[i].Split(' ')[1].Split(',').Select(q=>float.Parse(q,inv)).ToArray();
var lipS=new[]{24f,62f};var lipTop=new[]{68.6f,57.2f};var arc=new[]{1.4f,1.0f};var drop=new[]{9.6f,6.8f};
string dir="Assets/10.Datas/World/RiverBasinWorld/VisualR3";
var mp=dir+"/B1WaterfallLipFoam_R3.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(mp);
if(mat==null){AssetDatabase.CopyAsset("Assets/ExternalAssets/Environment/PolyartStudio/Farmlands/Materials/Water/Materials/Waterfall 01/MI_WaterfallUpperFoam_01.mat",mp);mat=AssetDatabase.LoadAssetAtPath<Material>(mp);}
mat.SetColor("_Color0",new Color(.86f,.89f,.89f,1));mat.SetColor("_Color1",new Color(.55f,.63f,.65f,0));EditorUtility.SetDirty(mat);
var root=GameObject.Find("GeneratedEnvironment/B1_ArtR1/B1 · 급류와 폭포").transform;
for(int f=0;f<2;f++){var lp=lipOf(f);float l=lp[0]*0.9f,r=lp[1]*0.9f,c0=lp[2];float lipZ=Z0-lipS[f];
 var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();const int C=24,R=6;float width=l+r;
 for(int i=0;i<=R;i++){float k=i/(float)R;float along=Mathf.Lerp(1.4f,-0.9f,k);
  for(int c=0;c<=C;c++){float a=c/(float)C;float bulge=arc[f]*(1-(2*a-1)*(2*a-1));float edgeZ=lipZ-bulge;
   // 수면 위(+)에서는 수면 2cm 위, 낙구를 넘으면 낙수면 곡률을 따라 내려간다
   float over=Mathf.Max(0,-along);float y=lipTop[f]+0.03f-drop[f]*0.02f*over*over-0.12f*over;
   float x=Mathf.Lerp(c0-l,c0+r,a);v.Add(new Vector3(x,y,edgeZ+along));uv.Add(new Vector2(a*width/3f,k));}}
 for(int i=0;i<R;i++)for(int c=0;c<C;c++){int a=i*(C+1)+c,b=a+C+1;t.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
 var meshPath=dir+"/B1WaterfallLipFoam_"+(f+1)+".asset";var m=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(m==null){m=new Mesh();AssetDatabase.CreateAsset(m,meshPath);}
 m.Clear();m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();EditorUtility.SetDirty(m);
 var name="낙구 거품 "+(f+1);var old=root.Find(name);var go=old!=null?old.gameObject:new GameObject(name);go.transform.SetParent(root,false);
 var mf=go.GetComponent<MeshFilter>();if(mf==null)mf=go.AddComponent<MeshFilter>();mf.sharedMesh=m;var mr=go.GetComponent<MeshRenderer>();if(mr==null)mr=go.AddComponent<MeshRenderer>();mr.sharedMaterial=mat;mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.gameObject.scene);AssetDatabase.SaveAssets();return "ok";
