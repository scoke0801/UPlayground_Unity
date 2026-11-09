// 수중 배치 전용 재질을 만든다. 외부 패키지와 지상 식생의 원본 재질은 보존한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("정규 월드 Edit Mode 필요");
var root=scene.GetRootGameObjects().Single(g=>g.name=="LakeUnderwaterEcology");
string folder="Assets/10.Datas/World/OpenWorld/LakeUnderwaterEcology";
var materials=new Dictionary<Material,Material>();
foreach(var renderer in root.GetComponentsInChildren<Renderer>(true)) {
    var slots=renderer.sharedMaterials;
    for(int i=0;i<slots.Length;i++) {
        var original=slots[i];
        if(AssetDatabase.GetAssetPath(original).StartsWith(folder))continue;
        Material material;
        if(!materials.TryGetValue(original,out material)) {
            string path=folder+"/Lake_"+original.name+".mat";
            material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null) {
                material=new Material(original);
                if(original.shader.name=="Standard") {
                    material.shader=Shader.Find("Universal Render Pipeline/Lit");
                    if(material.shader==null)throw new Exception("URP Lit 셰이더 누락");
                    material.SetTexture("_BaseMap",original.GetTexture("_MainTex"));
                    material.SetColor("_BaseColor",new Color(.8f,.9f,.85f,1));
                    material.SetFloat("_Smoothness",.35f);
                }
                if(material.HasProperty("_Top_Color")) {
                    material.SetColor("_Top_Color",new Color(.18f,.37f,.22f,0));
                    material.SetColor("_Bottom_Color",new Color(.07f,.18f,.13f,0));
                }
                if(material.HasProperty("_Coverage_Color")) {
                    material.SetColor("_Coverage_Color",new Color(.17f,.27f,.18f,1));
                    material.SetColor("_Color",new Color(.48f,.55f,.48f,1));
                }
                material.enableInstancing=true;
                AssetDatabase.CreateAsset(material,path);
            }
            materials.Add(original,material);
        }
        slots[i]=material;
    }
    renderer.sharedMaterials=slots;
}
PrefabUtility.ApplyPrefabInstance(root,InteractionMode.AutomatedAction);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return new {materials=materials.Count,root=root.name};
