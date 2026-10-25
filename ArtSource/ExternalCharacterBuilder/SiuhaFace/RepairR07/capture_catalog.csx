// stamp, hairIds, views는 실행자가 지정한다. 저장 에셋을 건드리지 않는 정적 스킨 캡처다.
var folder="output/SiuhaP09RepairR07/"+stamp;System.IO.Directory.CreateDirectory(folder);
var preset=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.BuildPreset>("Assets/10.Datas/Actor/ExternalModels/Siuha_P09_BuildPreset.asset");
var config=JsonUtility.FromJson<UPlayGround.Editor.P09Builder.CharacterBuildConfig>(JsonUtility.ToJson(preset.config));
foreach(UPlayGround.Editor.P09Builder.BuilderArmorSlot slot in Enum.GetValues(typeof(UPlayGround.Editor.P09Builder.BuilderArmorSlot)))config.ArmorSelections.Set(slot,null);
var helper=typeof(UPlayGround.Editor.P09Builder.PrefabBuildPipeline).Assembly.GetType("UPlayGround.Editor.P09Builder.P09PosePreview");
foreach(int hair in hairIds)foreach(float yaw in views){
var preview=new PreviewRenderUtility();
try{
var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics));preview.AddSingleGO(root);
config.HairStyleSo=AssetDatabase.LoadAssetAtPath<ScriptableObject>(UPlayGround.Editor.P09Builder.PathConfig.CatalogRoot+"/HairStyle/HairStyleEditPartData_"+Math.Max(1,hair).ToString("00")+".asset");
var context=new UPlayGround.Editor.P09Builder.BuildContext(config);context.RootInstance=root;new UPlayGround.Editor.P09Builder.ApplyAppearanceStep().Execute(context);
if(hair==0)foreach(var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))if(r.name.StartsWith("Hair_")||r.name.StartsWith("SkinHead_"))r.gameObject.SetActive(false);
root.transform.rotation=Quaternion.Euler(0,yaw,0);
foreach(var constraint in root.GetComponentsInChildren<UnityEngine.Animations.ParentConstraint>(true))helper.GetMethod("EvaluateConstraint",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{constraint});
var baked=new List<Mesh>();
foreach(var r in root.GetComponentsInChildren<SkinnedMeshRenderer>()){
if(!r.enabled)continue;var m=new Mesh();r.BakeMesh(m);baked.Add(m);var child=new GameObject("Capture");child.transform.SetParent(r.transform,false);child.AddComponent<MeshFilter>().sharedMesh=m;child.AddComponent<MeshRenderer>().sharedMaterials=r.sharedMaterials;r.enabled=false;
}
try{
var target=new Vector3(0,1.405f,0);var camera=preview.camera;camera.orthographic=true;camera.orthographicSize=.176f;camera.transform.position=target+new Vector3(0,-.025f,1.65f);camera.transform.LookAt(target);camera.nearClipPlane=.01f;camera.farClipPlane=10;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.30f,.36f,.43f);
preview.ambientColor=new Color(.55f,.55f,.55f);preview.lights[0].intensity=1.0f;preview.lights[0].transform.rotation=Quaternion.Euler(20,145,0);preview.lights[1].intensity=.4f;
preview.BeginStaticPreview(new Rect(0,0,640,640));preview.Render(true);var tex=preview.EndStaticPreview();System.IO.File.WriteAllBytes(folder+"/Hair"+hair.ToString("00")+"_"+yaw+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
}finally{foreach(var mesh in baked)UnityEngine.Object.DestroyImmediate(mesh);}
}finally{preview.Cleanup();}
}
return folder;
