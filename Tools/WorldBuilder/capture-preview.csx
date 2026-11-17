// Unity 카메라로 저장된 샘플만 촬영하고 임시 카메라·라벨은 제거한다.
System.IO.Directory.CreateDirectory("output/WorldBuilder");
var results = new System.Collections.Generic.List<object>();
var previousScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
try {
    for (int sceneIndex = 0; sceneIndex < UnityEngine.SceneManagement.SceneManager.sceneCount; sceneIndex++) {
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(sceneIndex);
        if (!scene.path.StartsWith("Assets/WorldBuilder/Worlds/")) continue;
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        var terrains = new System.Collections.Generic.List<Terrain>();
        foreach (var root in scene.GetRootGameObjects()) terrains.AddRange(root.GetComponentsInChildren<Terrain>());
        if (terrains.Count != 16) throw new System.Exception("4×4 Terrain이 아닙니다.");
        string sample = terrains[0].terrainData.size.y == 700 ? "C" : "B";
        var cameraObject = new GameObject("WorldBuilder 검수 카메라") { hideFlags = HideFlags.HideAndDontSave };
        var temporary = new System.Collections.Generic.List<GameObject>();
        var layers = new System.Collections.Generic.Dictionary<GameObject,int>();
        // 기존 씬은 건드리지 않고 이번 미리보기만 촬영용 레이어로 잠시 분리한다.
        int captureLayer=31;
        var used=new bool[32];
        foreach(var item in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None)) used[item.gameObject.layer]=true;
        while(captureLayer>=8 && used[captureLayer])captureLayer--;
        if(captureLayer<8)throw new System.Exception("촬영용 빈 레이어가 없습니다.");
        foreach(var terrain in terrains) { layers.Add(terrain.gameObject,terrain.gameObject.layer); terrain.gameObject.layer=captureLayer; }
        var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false; camera.scene = scene;
        camera.cullingMask=1<<captureLayer;
        camera.orthographic = true; camera.orthographicSize = 1160; camera.nearClipPlane = .1f; camera.farClipPlane = 4000;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.16f,.18f,.2f);
        camera.transform.position = new Vector3(1024, 2200, 1024); camera.transform.rotation = Quaternion.Euler(90,0,0);
        try {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var corners = new [] { new Vector3(80,1000,80),new Vector3(1540,1000,80),new Vector3(80,1000,1900),new Vector3(1540,1000,1900) };
            var names = new [] { "SW / +X EAST", "SE", "NW / +Z NORTH", "NE" };
            for (int i=0;i<4;i++) {
                var label = new GameObject(names[i]) { hideFlags = HideFlags.HideAndDontSave }; temporary.Add(label);
                label.layer=captureLayer;
                label.transform.position = corners[i]; label.transform.rotation = Quaternion.Euler(90,0,0);
                var text = label.AddComponent<TextMesh>(); text.text=names[i]; text.font=font; text.fontSize=64; text.characterSize=3; text.color=Color.black;
                label.GetComponent<MeshRenderer>().sharedMaterial=font.material;
            }
            var rt = RenderTexture.GetTemporary(1400,1400,24); var previous = RenderTexture.active;
            try {
                camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt;
                var texture=new Texture2D(1400,1400,TextureFormat.RGB24,false);
                try { texture.ReadPixels(new Rect(0,0,1400,1400),0,0); texture.Apply(); System.IO.File.WriteAllBytes("output/WorldBuilder/"+sample+"-overview.png",texture.EncodeToPNG()); }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            } finally { camera.targetTexture=null; RenderTexture.active=previous; RenderTexture.ReleaseTemporary(rt); }
            var cornersResult=new System.Collections.Generic.List<object>();
            foreach(var root in scene.GetRootGameObjects()) if(root.name.Contains(" SW")||root.name.Contains(" SE")||root.name.Contains(" NW")||root.name.Contains(" NE")) cornersResult.Add(new {root.name,position=root.transform.position});
            results.Add(new {sample,scene.path,terrainCount=terrains.Count,corners=cornersResult});
        } finally { foreach(var label in temporary) UnityEngine.Object.DestroyImmediate(label); UnityEngine.Object.DestroyImmediate(cameraObject); foreach(var item in layers)item.Key.layer=item.Value; }
    }
} finally { if(previousScene.IsValid() && previousScene.isLoaded) UnityEngine.SceneManagement.SceneManager.SetActiveScene(previousScene); }
return results;
