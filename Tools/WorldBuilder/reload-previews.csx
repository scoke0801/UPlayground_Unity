// P01에서 생성한 두 미리보기만 재로드한다. 기존 월드는 저장하지 않는다.
var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(original.path.StartsWith("Assets/WorldBuilder/Worlds/")) {
    for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++) {
        var candidate=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
        if(!candidate.path.StartsWith("Assets/WorldBuilder/Worlds/")) { original=candidate; break; }
    }
}
var paths=System.IO.Directory.GetFiles("Assets/WorldBuilder/Worlds","WorldPreview.unity",System.IO.SearchOption.AllDirectories);
var results=new System.Collections.Generic.List<string>();
try {
    foreach(var pathOnDisk in paths) {
        string path=pathOnDisk.Replace('\\','/');
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
        if(scene.IsValid()&&scene.isLoaded)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
        results.Add(path);
    }
} finally { if(original.IsValid()&&original.isLoaded)UnityEngine.SceneManagement.SceneManager.SetActiveScene(original); }
return results;
