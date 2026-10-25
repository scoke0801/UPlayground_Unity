const string path="Assets/01.Scenes/Tests/Equipment/Siuha_P09_Review.unity";
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(path);var preview=new PreviewRenderUtility();
try{
var roots=scene.GetRootGameObjects();
foreach(var root in roots.Where(g=>g.name.StartsWith("Siuha_P09"))){var instance=UnityEngine.Object.Instantiate(root);instance.name=root.name;preview.AddSingleGO(instance);}
var sourceCamera=roots.Select(g=>g.GetComponent<Camera>()).First(c=>c!=null);
// CopyFrom은 검수 전용 scene culling mask까지 덮으므로 카메라 표시 설정만 복사한다.
preview.camera.orthographic=sourceCamera.orthographic;preview.camera.orthographicSize=sourceCamera.orthographicSize;preview.camera.fieldOfView=sourceCamera.fieldOfView;
preview.camera.nearClipPlane=sourceCamera.nearClipPlane;preview.camera.farClipPlane=sourceCamera.farClipPlane;preview.camera.clearFlags=sourceCamera.clearFlags;preview.camera.backgroundColor=sourceCamera.backgroundColor;
preview.camera.transform.SetPositionAndRotation(sourceCamera.transform.position,sourceCamera.transform.rotation);
var lights=roots.Select(g=>g.GetComponent<Light>()).Where(l=>l!=null).ToArray();
for(int i=0;i<preview.lights.Length;i++){preview.lights[i].intensity=i<lights.Length?lights[i].intensity:0;if(i<lights.Length){preview.lights[i].color=lights[i].color;preview.lights[i].transform.rotation=lights[i].transform.rotation;}}
preview.ambientColor=new Color(.55f,.55f,.55f);
preview.BeginStaticPreview(new Rect(0,0,1600,900));preview.Render(true);var image=preview.EndStaticPreview();System.IO.Directory.CreateDirectory("output/SiuhaP09RepairR03");System.IO.File.WriteAllBytes("output/SiuhaP09RepairR03/ReviewScene_R03.png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
var rows=new List<string>{path};foreach(var root in roots.Where(g=>g.name.StartsWith("Siuha_P09")))rows.Add(root.name+": "+string.Join(",",root.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled).Select(r=>r.name)));
System.IO.File.WriteAllLines("output/SiuhaP09RepairR03/review_capture_identity.txt",rows);return rows;
}finally{preview.Cleanup();UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
