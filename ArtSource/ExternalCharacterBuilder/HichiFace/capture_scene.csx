string path="Assets/01.Scenes/Tests/Equipment/P09_Hichi_Review.unity";
var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
Camera captureCamera=null;RenderTexture target=null;Texture2D image=null;var active=RenderTexture.active;
try{
    var roots=scene.GetRootGameObjects();var camera=roots.SelectMany(r=>r.GetComponentsInChildren<Camera>()).Single();
    captureCamera=camera;if(roots.Length!=5)throw new Exception("검수 씬 루트 수 불일치");
    foreach(var r in roots.SelectMany(g=>g.GetComponentsInChildren<Renderer>()))r.gameObject.layer=30;
    camera.cullingMask=1<<30;target=new RenderTexture(1400,1000,24);camera.targetTexture=target;camera.Render();
    RenderTexture.active=target;image=new Texture2D(1400,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1400,1000),0,0);image.Apply();
    System.IO.File.WriteAllBytes("output/HichiP09/ReviewScene_LiveSkinned.png",image.EncodeToPNG());
    var metrics=roots.Where(r=>r.GetComponent<Animator>()!=null).Select(r=>new{r.name,renderers=r.GetComponentsInChildren<Renderer>().Count(x=>x.enabled),triangles=r.GetComponentsInChildren<SkinnedMeshRenderer>().Where(x=>x.enabled).Sum(x=>x.sharedMesh.triangles.Length/3),missing=r.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))}).ToArray();
    return new{scene=path,metrics};
}finally{if(captureCamera!=null)captureCamera.targetTexture=null;RenderTexture.active=active;if(image!=null)UnityEngine.Object.DestroyImmediate(image);if(target!=null)UnityEngine.Object.DestroyImmediate(target);UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
