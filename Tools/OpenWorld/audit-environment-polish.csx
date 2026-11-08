// 현재 저장본만 검사하며 지형·내비게이션을 재생성하거나 저장하지 않는다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string folder="output/OpenWorld/EnvironmentPolish";
if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("월드 Edit Mode 필요");
var rows=new List<string>();var errors=new List<string>();
var identities=string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")).OrderBy(s=>s).ToArray());
if(identities!=System.IO.File.ReadAllText(folder+"/identities-before.txt"))errors.Add("진행 ID/좌표 변경");
var env=GameObject.Find("AuthoredEnvironment");var filters=env.GetComponentsInChildren<MeshFilter>(true);int triangles=0;
var surface=env.GetComponent<Unity.AI.Navigation.NavMeshSurface>();
if(surface==null||surface.navMeshData==null||AssetDatabase.GetAssetPath(surface.navMeshData)!="Assets/10.Datas/World/OpenWorld/EnvironmentPolish/Navigation.asset")errors.Add("환경 보정 NavMesh 연결 실패");
else{
    int tiles=new SerializedObject(surface.navMeshData).FindProperty("m_NavMeshTiles").arraySize;
    if(tiles==0)errors.Add("저장된 NavMesh 타일 없음");
    rows.Add("NavMesh tiles="+tiles+" bytes="+new System.IO.FileInfo(AssetDatabase.GetAssetPath(surface.navMeshData)).Length);
}
int microscopicTriangles=0;
var tunnels=filters.Where(f=>f.sharedMesh!=null&&AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith("Assets/10.Datas/World/OpenWorld/EnvironmentPolish/")).ToArray();
if(tunnels.Length!=2)errors.Add("파생 터널 메시 수="+tunnels.Length);
foreach(var filter in tunnels){
    var collider=filter.GetComponent<MeshCollider>();var mesh=filter.sharedMesh;
    if(collider==null||collider.sharedMesh==null||collider.sharedMesh==mesh){errors.Add("터널 원본 충돌 연결 실패: "+filter.name);continue;}
    var original=collider.sharedMesh;var source=original.vertices;var derived=mesh.vertices;var sourceIndices=original.triangles;
    if(sourceIndices.Length!=derived.Length)errors.Add("터널 삼각형 수 변경: "+filter.name);
    for(int i=0;i<sourceIndices.Length;i++)if(Vector3.Distance(source[sourceIndices[i]],derived[i])>.00001f){errors.Add("터널 형상 변경: "+filter.name);break;}
    if(mesh.uv.Length!=mesh.vertexCount||mesh.tangents.Length!=mesh.vertexCount)errors.Add("터널 UV/접선 누락");
    // 월드 길이에 비례하는 UV 넓이를 검사한다. 형상은 원본 콜라이더와 정확히 같아야 한다.
    var uv=mesh.uv;
    for(int i=0;i<derived.Length;i+=3){
        var a=filter.transform.TransformVector(derived[i]);var b=filter.transform.TransformVector(derived[i+1]);var c=filter.transform.TransformVector(derived[i+2]);
        float area=Vector3.Cross(b-a,c-a).magnitude;var u=uv[i+1]-uv[i];var v=uv[i+2]-uv[i];float uvArea=Mathf.Abs(u.x*v.y-u.y*v.x);
        // FBX의 극소 베벨 삼각형은 Vector3.normalized의 1e-5 한도 아래라 UV 밀도 판정에서 따로 센다.
        if(area<=1e-5f){microscopicTriangles++;continue;}
        if(Mathf.Abs(uvArea/area-1f/9f)>.0001f){errors.Add("터널 UV 밀도 오류: "+filter.name);break;}
        triangles++;
    }
    rows.Add(filter.name+" vertices="+mesh.vertexCount+" bounds="+mesh.bounds+" 충돌 원본="+AssetDatabase.GetAssetPath(original));
}
int missing=env.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));if(missing>0)errors.Add("환경 Missing Script="+missing);
foreach(var renderer in env.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.sharedMaterials.Any(m=>m==null)))errors.Add("재질 누락: "+renderer.name);
rows.AddRange(errors.Select(e=>"FAIL "+e));rows.Add("RESULT errors="+errors.Count+" UV triangles="+triangles+" microscopicTriangles="+microscopicTriangles+" identities="+identities.Split('\n').Length+" MissingScript="+missing);
System.IO.File.WriteAllLines(folder+"/audit.txt",rows);return string.Join("\n",rows.ToArray());
