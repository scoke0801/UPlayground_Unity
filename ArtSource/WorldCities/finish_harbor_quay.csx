var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByName("SaltwindHarbor");
var environment=scene.GetRootGameObjects().First(g=>g.name=="CityEnvironment");
var atlas=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/WorldCities/CityAtlas.mat");
var cache=new System.Collections.Generic.Dictionary<string,Mesh>();
int number=0;
foreach(var t in environment.transform.Cast<Transform>().Where(t=>t.name=="Quayside"||t.name.Contains("Parapet")||t.name=="WestBreakwater"||t.name=="BreakwaterEnd"||t.name=="PierEndRail")){
    var size=t.localScale;string key=size.ToString("F3");Mesh mesh;
    if(!cache.TryGetValue(key,out mesh)){
        var vertices=new System.Collections.Generic.List<Vector3>();var uv=new System.Collections.Generic.List<Vector2>();var triangles=new System.Collections.Generic.List<int>();
        System.Action<Vector3,Vector3,Vector3,Vector3> face=(a,b,c,d)=>{
            int nx=Mathf.Max(1,Mathf.CeilToInt(Vector3.Scale(b-a,size).magnitude/2.5f));int ny=Mathf.Max(1,Mathf.CeilToInt(Vector3.Scale(d-a,size).magnitude/2.5f));
            for(int y=0;y<ny;y++)for(int x=0;x<nx;x++){
                int first=vertices.Count;float u0=(float)x/nx,u1=(float)(x+1)/nx,v0=(float)y/ny,v1=(float)(y+1)/ny;
                vertices.Add(Vector3.Lerp(Vector3.Lerp(a,b,u0),Vector3.Lerp(d,c,u0),v0));vertices.Add(Vector3.Lerp(Vector3.Lerp(a,b,u1),Vector3.Lerp(d,c,u1),v0));vertices.Add(Vector3.Lerp(Vector3.Lerp(a,b,u1),Vector3.Lerp(d,c,u1),v1));vertices.Add(Vector3.Lerp(Vector3.Lerp(a,b,u0),Vector3.Lerp(d,c,u0),v1));
                uv.Add(new Vector2(.006f,.509f));uv.Add(new Vector2(.327f,.509f));uv.Add(new Vector2(.327f,.991f));uv.Add(new Vector2(.006f,.991f));
                triangles.Add(first);triangles.Add(first+1);triangles.Add(first+2);triangles.Add(first);triangles.Add(first+2);triangles.Add(first+3);
            }
        };
        var p=new[]{new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,-.5f,.5f),new Vector3(-.5f,-.5f,.5f),new Vector3(-.5f,.5f,-.5f),new Vector3(.5f,.5f,-.5f),new Vector3(.5f,.5f,.5f),new Vector3(-.5f,.5f,.5f)};
        face(p[0],p[1],p[5],p[4]);face(p[2],p[3],p[7],p[6]);face(p[3],p[0],p[4],p[7]);face(p[1],p[2],p[6],p[5]);face(p[4],p[5],p[6],p[7]);face(p[3],p[2],p[1],p[0]);
        // 위 순서의 앞면 노멀을 외부로 뒤집는다.
        for(int i=0;i<triangles.Count;i+=3){int temp=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=temp;}
        string path="Assets/10.Datas/World/WorldCities/SaltwindHarbor/Life/QuayStone_"+number+++".asset";
        mesh=UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null){mesh=new Mesh();UnityEditor.AssetDatabase.CreateAsset(mesh,path);}mesh.Clear();mesh.name="HarborQuayStone";mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();UnityEditor.EditorUtility.SetDirty(mesh);UnityEditor.AssetDatabase.SaveAssetIfDirty(mesh);cache.Add(key,mesh);
    }
    t.GetComponent<MeshFilter>().sharedMesh=mesh;t.GetComponent<Renderer>().sharedMaterial=atlas;
}
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return "방파제·난간의 실제 크기에 맞춘 석재 UV 적용: "+cache.Count;
