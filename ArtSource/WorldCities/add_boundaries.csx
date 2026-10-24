foreach(string id in new[]{"SaltwindHarbor","StonegateCitadel"}){
    var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/01.Scenes/Scenario/"+id+".unity",UnityEditor.SceneManagement.OpenSceneMode.Single);
    var environment=scene.GetRootGameObjects().First(r=>r.name=="CityEnvironment");
    if(environment.transform.Find("MapBoundaries")!=null)continue;
    var root=new GameObject("MapBoundaries");root.transform.SetParent(environment.transform);
    System.Action<string,Vector3,Vector3> barrier=(name,pos,size)=>{
        var obj=new GameObject(name);obj.transform.SetParent(root.transform);obj.transform.position=pos;obj.layer=LayerMask.NameToLayer("Ground");var collider=obj.AddComponent<BoxCollider>();collider.size=size;
    };
    barrier("WestTerrainEdge",new Vector3(1,30,180),new Vector3(2,100,360));
    barrier("EastTerrainEdge",new Vector3(359,30,180),new Vector3(2,100,360));
    barrier("SouthTerrainEdge",new Vector3(180,30,1),new Vector3(360,100,2));
    barrier("NorthTerrainEdge",new Vector3(180,30,359),new Vector3(360,100,2));
    if(id=="SaltwindHarbor"){
        // 접근 가능한 부두·방파제는 난간으로, 도시 밖 해안은 바위 띠로 경계를 보인다.
        barrier("WestRockCoast",new Vector3(54,14,230),new Vector3(108,14,5));
        barrier("EastRockCoast",new Vector3(303,14,230),new Vector3(114,14,5));
        var rock=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalAssets/Environment/Idyllic Fantasy Nature/Prefabs/Rock_Big_01.prefab");
        for(int x=8;x<356;x+=12)if(x<110||x>244){var obj=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(rock,scene);obj.transform.SetParent(root.transform);obj.transform.position=new Vector3(x,7.5f,230);obj.transform.localScale=new Vector3(3,2,2.5f);}
    }
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
}
return "도시 외곽 낙하와 해안 이탈 경계 추가";
