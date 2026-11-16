// 상류 끝을 지형에 닫힌 작은 소로 바꾸고 물판의 가로 절단면을 없앤다.
var terrain=Terrain.activeTerrain;var data=terrain.terrainData;
var origin=new Vector3(1144,0,1029);var forward=new Vector3(-.45f,0,-.8930286f).normalized;var right=Vector3.Cross(Vector3.up,forward);
var center=origin+forward*(-29.2f)+right*.5f;
int res=data.heightmapResolution;float dx=data.size.x/(res-1),dz=data.size.z/(res-1);
int ax=Mathf.FloorToInt((center.x-10)/dx),az=Mathf.FloorToInt((center.z-12-terrain.transform.position.z)/dz);int w=Mathf.CeilToInt(20/dx),h=Mathf.CeilToInt(24/dz);
var heights=data.GetHeights(ax,az,w,h);
for(int z=0;z<h;z++)for(int x=0;x<w;x++){
    var p=new Vector3((ax+x)*dx,0,terrain.transform.position.z+(az+z)*dz);var delta=p-center;
    float along=Vector3.Dot(delta,forward),across=Vector3.Dot(delta,right);
    float radius=Mathf.Sqrt(Mathf.Pow(along/5.1f,2)+Mathf.Pow(across/3.65f,2));
    float irregular=1+.075f*Mathf.Sin(along*.9f+across*.7f);
    radius/=irregular;if(radius>1.6f)continue;
    float old=heights[z,x]*data.size.y;float bed=100.15f+.9f*Mathf.Pow(Mathf.Min(radius,1),2);
    float bank=101.45f+.08f*Mathf.Sin(along);
    float target=radius<.8f?bed:Mathf.Lerp(bed,bank,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.8f,1.1f,radius)));
    float weight=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.1f,1.6f,radius));
    heights[z,x]=Mathf.Lerp(old,target,weight)/data.size.y;
}data.SetHeights(ax,az,heights);EditorUtility.SetDirty(data);
// 소의 낮은 출구를 기존 하상과 이어 물길 사이에 마른 둑이 남지 않게 한다.
var outlet=data.GetHeights(ax,az,w,h);
for(int z=0;z<h;z++)for(int x=0;x<w;x++){
    var p=new Vector3((ax+x)*dx,0,terrain.transform.position.z+(az+z)*dz);var delta=p-origin;
    float s=Vector3.Dot(delta,forward);if(s< -27||s> -18)continue;
    float bend=1.25f*Mathf.Sin((s+33)*.13f)*Mathf.Sin(Mathf.InverseLerp(-33,0,s)*Mathf.PI);
    float a=Mathf.Abs(Vector3.Dot(delta,right)-bend);float width=Mathf.Lerp(2.6f,4.2f,Mathf.InverseLerp(-27,-18,s));if(a>width+1.5f)continue;
    float water=s<=-24?101.1f:Mathf.Lerp(101.1f,99,Mathf.SmoothStep(0,1,Mathf.InverseLerp(-24,-20,s)));
    float bed=water-.8f+.7f*Mathf.Pow(Mathf.Min(a/width,1),2);
    float weight=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(width*.8f,width+1.5f,a));
    float old=outlet[z,x]*data.size.y;outlet[z,x]=Mathf.Lerp(old,Mathf.Min(old,bed),weight)/data.size.y;
}data.SetHeights(ax,az,outlet);
var go=GameObject.Find("LakeWaterfall_Northeast").transform.Find("R2 하상과 낙구/UpperStream");var mesh=go.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;
const int Columns=32;
for(int row=0;row<vertices.Length/(Columns+1);row++){
    float s=Vector3.Dot(go.TransformPoint(vertices[row+row*Columns])-origin,forward);
    if(s>=-20)continue;
    float water=s<=-24?101.1f:Mathf.Lerp(101.1f,99,Mathf.SmoothStep(0,1,Mathf.InverseLerp(-24,-20,s)));
    float bend=1.25f*Mathf.Sin((s+33)*.13f)*Mathf.Sin(Mathf.InverseLerp(-33,0,s)*Mathf.PI);
    var c=origin+forward*s+right*bend;
    float maxWidth=s< -25?Mathf.Max(.05f,3.6f*Mathf.Sqrt(Mathf.Max(0,1-Mathf.Pow((s+29.2f)/5.3f,2)))):3.3f;
    Func<float,float> reach=sign=>{float d=0;while(d<maxWidth&&terrain.SampleHeight(c+right*d*sign)<water+.012f)d+=.08f;return Mathf.Min(d,maxWidth);};
    float left=reach(-1),rr=reach(1);
    for(int col=0;col<=Columns;col++){int i=row*(Columns+1)+col;var p=c+right*Mathf.Lerp(-left,rr,col/(float)Columns);p.y=water;vertices[i]=go.InverseTransformPoint(p);}
}
mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);go.GetComponent<UPlayGround.Components.FootstepWaterSurface>().RefreshMesh();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.gameObject.scene);Physics.SyncTransforms();return "상류 소와 닫힌 물가 반영";
