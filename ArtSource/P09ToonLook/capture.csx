// 현재 저장된 결과와 원본 재질을 같은 카메라·조명으로 비교한다. 씬과 에셋은 저장하지 않는다.
System.IO.Directory.CreateDirectory("output/P09ToonLook");
var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Characters/ScenarioResidents/Npc/Npc_Daon/Npc_Daon.prefab");
var profile = AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09ToonLookProfileSO>("Assets/10.Datas/Actor/ScenarioResidents/ToonLook/P09_Daon_ToonLook.asset");
var capture = typeof(UPlayGround.Editor.P09Builder.ScenarioResidentAuthoring).GetMethod("Capture", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
foreach (bool before in new[] { true, false })
foreach (bool face in new[] { true, false })
foreach (bool side in new[] { false, true })
{
    var root = UnityEngine.Object.Instantiate(prefab);
    try
    {
        root.SetActive(true);
        if (before)
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                    foreach (var binding in profile.bindings)
                        if (materials[i] == binding.replacement) { materials[i] = binding.source; break; }
                renderer.sharedMaterials = materials;
            }
        capture.Invoke(null, new object[] { root, "output/P09ToonLook/" + (before ? "Before" : "After") + "_" + (face ? "Face" : "Full") + "_" + (side ? "Side" : "Soft") + ".png", face, 0f, side, true });
    }
    finally { if (root != null) UnityEngine.Object.DestroyImmediate(root); }
}
return "전후·얼굴/전신·부드러운 조명/측면 조명 8장 저장";
