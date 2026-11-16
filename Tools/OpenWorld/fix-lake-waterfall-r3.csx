// 낙구 유입 접선과 이어지는 중력 궤적·두께를 저작한다.
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Unity 편집 대기 필요");
const string Dir="Assets/10.Datas/World/OpenWorld/LakeWaterfall/R3/";
var root=GameObject.Find("LakeWaterfall_Northeast");var go=root.transform.Find("R2 하상과 낙구/WideFall");
var origin=new Vector3(1144,0,1029);var f=new Vector3(-.45f,0,-.8930286f).normalized;var r=Vector3.Cross(Vector3.up,f);
var v=new List<Vector3>();var uv=new List<Vector2>();var color=new List<Color>();var triangles=new List<int>();
const int Rows=90,Cols=48;int skin=(Rows+1)*(Cols+1);
for(int side=0;side<2;side++)for(int row=0;row<=Rows;row++){
    float a=row/(float)Rows,s,y,flow,transition,thickness;Vector3 tangent;
    if(a<.22f){float t=a/.22f;s=Mathf.Lerp(-4.5f,0,t);y=93.5f-s*.233333f;flow=t*.16f;transition=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.68f,t));thickness=0;tangent=(f-Vector3.up*.233333f).normalized;}
    else{float k=(a-.22f)/.78f;s=6.2f*k;y=93.5f-1.45f*k-15.55f*k*k;flow=Mathf.Lerp(.16f,1,k);transition=1;thickness=.13f*Mathf.Sin(k*Mathf.PI);tangent=(f*6.2f-Vector3.up*(1.45f+31.1f*k)).normalized;}
    var normal=Vector3.Cross(tangent,r).normalized;float half=9.3f*(1-.065f*Mathf.InverseLerp(.22f,1,a));
    for(int col=0;col<=Cols;col++){
        float u=col/(float)Cols;float k=Mathf.InverseLerp(.22f,1,a);
        float ripple=(Mathf.PerlinNoise(u*6.3f+3,k*4.1f)-.5f)*.18f*k;
        var p=origin+f*(s+ripple)+r*((u*2-1)*half);p.y=y+(Mathf.PerlinNoise(u*5.4f,4)-.5f)*.24f*Mathf.Sin(a*Mathf.PI);
        p+=normal*thickness*(side==0?.5f:-.5f);v.Add(go.InverseTransformPoint(p));uv.Add(new Vector2(Mathf.Lerp(.015f,.985f,u),flow));color.Add(new Color(transition,0,0,1));
    }
}
for(int side=0;side<2;side++)for(int row=0;row<Rows;row++)for(int col=0;col<Cols;col++){
    int a=side*skin+row*(Cols+1)+col,b=a+Cols+1;
    if(side==0)triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});else triangles.AddRange(new[]{a,a+1,b,a+1,b+1,b});
}
foreach(int col in new[]{0,Cols})for(int row=0;row<Rows;row++){int a=row*(Cols+1)+col,b=a+Cols+1;triangles.AddRange(new[]{a,a+skin,b,b,a+skin,b+skin});}
var mesh=new Mesh{name="LakeFallGravityVolume"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetColors(color);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
AssetDatabase.CreateAsset(mesh,Dir+"Fall.asset");go.GetComponent<MeshFilter>().sharedMesh=mesh;
var material=new Material(go.GetComponent<MeshRenderer>().sharedMaterial);material.name="LakeFallGravityVolume";material.SetFloat("_FoamAmount",1.35f);material.SetFloat("_FoamStart",.08f);material.SetFloat("_AmbientFloor",.52f);material.SetFloat("_Opacity",.85f);material.SetVector("_NoiseTiling",new Vector4(4.6f,1.6f,0,0));
AssetDatabase.CreateAsset(material,Dir+"Fall.mat");go.GetComponent<MeshRenderer>().sharedMaterial=material;
var impact=origin+f*6.2f+Vector3.up*77.2f;
foreach(var particle in root.GetComponentsInChildren<ParticleSystem>().Where(p=>p.name.StartsWith("입수 물보라"))){int i=int.Parse(particle.name.Substring(particle.name.Length-1))-2;particle.transform.position=impact+r*i*5.8f;}
root.transform.Find("입수 미스트").position=impact+Vector3.up*.9f;
root.transform.Find("호수 입수 지점").position=impact;
var source=root.transform.Find("입수 미스트");
foreach(float k in new[]{.48f,.75f}){var mist=UnityEngine.Object.Instantiate(source.gameObject,root.transform);mist.name="낙수 난류 미스트";mist.transform.position=origin+f*(6.2f*k)+Vector3.up*(93.5f-1.45f*k-15.55f*k*k);mist.transform.localScale=new Vector3(1.2f,.35f,.35f);}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.gameObject.scene);AssetDatabase.SaveAssets();return new{vertices=mesh.vertexCount,triangles=triangles.Count/3,drop=17f,throwLength=6.2f};
