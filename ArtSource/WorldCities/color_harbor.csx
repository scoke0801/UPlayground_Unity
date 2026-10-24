// 공용 FBX와 아틀라스는 보존하고 항구 인스턴스용 메시·재질만 만든다.
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByName("SaltwindHarbor");
var environment=scene.GetRootGameObjects().First(g=>g.name=="CityEnvironment");
string directory="Assets/10.Datas/World/WorldCities/SaltwindHarbor/Life/";
var source=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Environment/WorldCities/HarborHouse.prefab").GetComponentInChildren<MeshFilter>().sharedMesh;
var atlas=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/WorldCities/CityAtlas.mat");
var wallColors=new[]{new Color(.7f,.87f,.88f),new Color(1,.92f,.77f),new Color(.91f,.76f,.64f)};
var meshes=new Mesh[3];var materials=new Material[3];
var read=UnityEditor.MeshUtility.AcquireReadOnlyMeshData(source);
try{
    var vertices=new Unity.Collections.NativeArray<Vector3>(source.vertexCount,Unity.Collections.Allocator.Temp);
    var normals=new Unity.Collections.NativeArray<Vector3>(source.vertexCount,Unity.Collections.Allocator.Temp);
    var uv=new Unity.Collections.NativeArray<Vector2>(source.vertexCount,Unity.Collections.Allocator.Temp);
    var indices=new Unity.Collections.NativeArray<int>((int)source.GetIndexCount(0),Unity.Collections.Allocator.Temp);
    try{
        read[0].GetVertices(vertices);read[0].GetNormals(normals);read[0].GetUVs(0,uv);read[0].GetIndices(indices,0);
        for(int variant=0;variant<3;variant++){
            string path=directory+"HousePalette_"+variant+".asset";
            var mesh=UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null){mesh=new Mesh();UnityEditor.AssetDatabase.CreateAsset(mesh,path);}
            mesh.name="HarborHousePalette_"+variant;mesh.vertices=vertices.ToArray();mesh.normals=normals.ToArray();var coords=uv.ToArray();
            var walls=new System.Collections.Generic.List<int>();var other=new System.Collections.Generic.List<int>();
            for(int i=0;i<indices.Length;i+=3){var point=coords[indices[i]];bool plaster=point.x>2f/3&&point.y>.5f;var list=plaster?walls:other;list.Add(indices[i]);list.Add(indices[i+1]);list.Add(indices[i+2]);}
            if(variant==1)for(int i=0;i<coords.Length;i++)if(coords[i].x<1f/3&&coords[i].y<.5f)coords[i].x+=1f/3;
            mesh.uv=coords;mesh.subMeshCount=2;mesh.SetTriangles(other,0);mesh.SetTriangles(walls,1);mesh.RecalculateBounds();mesh.RecalculateTangents();
            if(mesh.vertexCount!=source.vertexCount||other.Count+walls.Count!=indices.Length)throw new System.Exception("건물 메시 보존 검사 실패");
            UnityEditor.EditorUtility.SetDirty(mesh);UnityEditor.AssetDatabase.SaveAssetIfDirty(mesh);meshes[variant]=mesh;
            string materialPath=directory+"HousePlaster_"+variant+".mat";var material=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){material=new Material(atlas);UnityEditor.AssetDatabase.CreateAsset(material,materialPath);}material.color=wallColors[variant];UnityEditor.EditorUtility.SetDirty(material);UnityEditor.AssetDatabase.SaveAssetIfDirty(material);materials[variant]=material;
        }
    }finally{vertices.Dispose();normals.Dispose();uv.Dispose();indices.Dispose();}
}finally{read.Dispose();}
int index=0;
foreach(var t in environment.transform.Cast<Transform>().Where(t=>t.name=="HarborHouse")){
    int variant=index++%3;var filter=t.GetComponentInChildren<MeshFilter>();var renderer=filter.GetComponent<MeshRenderer>();filter.sharedMesh=meshes[variant];renderer.sharedMaterials=new[]{atlas,materials[variant]};
    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(filter);UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
}
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return "항구 벽·지붕 3종 팔레트, 원본 정점·인덱스 보존 검사 통과";
