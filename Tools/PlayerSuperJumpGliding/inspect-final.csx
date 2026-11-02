if(EditorApplication.isCompiling||EditorApplication.isUpdating)return "컴파일 또는 임포트 대기";
System.IO.Directory.CreateDirectory("output/PlayerSuperJumpGliding");
string path="Assets/03.Prefabs/Traversal/PlayerGlider.prefab";
AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var roots=new System.Collections.Generic.List<GameObject>();
try{
    var model=UnityEngine.Object.Instantiate(asset);roots.Add(model);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(model,preview);
    var report=new System.Text.StringBuilder();
    foreach(var t in model.GetComponentsInChildren<Transform>(true))if(t.name=="GripLeft"||t.name=="GripRight"||t.name=="RiderAnchor"||t.name=="GliderRoot")report.AppendLine(t.name+"\tposition="+model.transform.InverseTransformPoint(t.position).ToString("F5")+"\trotation="+(Quaternion.Inverse(model.transform.rotation)*t.rotation).ToString("F5")+"\tscale="+t.lossyScale.ToString("F5"));
    var player=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Raon.prefab"));roots.Add(player);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(player,preview);
    player.SetActive(true);
    var equipment=player.GetComponent<UPlayGround.Components.PlayerEquipment>();if(equipment!=null)equipment.HideEquipmentForPresentationPreview();
    var animancer=player.GetComponent<Animancer.AnimancerComponent>();var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/07.Animations/Player/Traversal/SuperJumpGliding/Raon_GliderHang.anim");var state=animancer.Play(clip);state.Time=0.25f;animancer.Evaluate(0f);
    var binding=player.GetComponent<UPlayGround.Components.PlayerGliderBinding>();
    var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
    typeof(UPlayGround.Components.PlayerGliderBinding).GetMethod("Awake",flags).Invoke(binding,null);
    var view=model.GetComponent<UPlayGround.Components.PlayerGliderView>();
    binding.BeginTraversal();binding.Attach(view);
    typeof(UPlayGround.Components.PlayerGliderBinding).GetMethod("OnAnimatorIK",flags).Invoke(binding,new object[]{0});
    var lightObject=new GameObject("TraversalPreviewLight");roots.Add(lightObject);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightObject,preview);var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.7f;lightObject.transform.rotation=Quaternion.Euler(40,-30,0);
    var fillObject=new GameObject("TraversalPreviewFill");roots.Add(fillObject);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fillObject,preview);var fill=fillObject.AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=0.8f;fillObject.transform.rotation=Quaternion.Euler(20,130,0);
    var cameraObject=new GameObject("TraversalPreviewCamera");roots.Add(cameraObject);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,preview);var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.scene=preview;camera.orthographic=true;camera.orthographicSize=1.35f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(0.17f,0.18f,0.2f);camera.nearClipPlane=0.01f;camera.farClipPlane=30f;
    var utility=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UPlayGround.Tool.Editor.MinimapCaptureUtility")).First(t=>t!=null);
    var positions=new[]{new Vector3(2.4f,1.35f,-3.5f),new Vector3(2.7f,1.25f,2.8f)};
    for(int i=0;i<positions.Length;i++){
        camera.transform.position=positions[i];camera.transform.rotation=Quaternion.LookRotation(new Vector3(0,1.0f,0)-positions[i]);
        var texture=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(1000,1000),false,1f});
        try{System.IO.File.WriteAllBytes("output/PlayerSuperJumpGliding/unity-final-"+i+".png",texture.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(texture);}
    }
    System.IO.File.WriteAllText("output/PlayerSuperJumpGliding/final-model-import.txt",report.ToString());
    binding.EndTraversal();
    return report.ToString();
}finally{foreach(var root in roots)if(root!=null)UnityEngine.Object.DestroyImmediate(root);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}
