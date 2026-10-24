// Blender 출력의 재질·충돌을 Unity 자산으로 연결한다. 기존 자산은 덮어쓰지 않는다.
string directory = "Assets/10.Datas/World/WorldCities";
System.IO.Directory.CreateDirectory(directory);
UnityEditor.AssetDatabase.Refresh();
string texturePath = "Assets/04.Images/Environment/WorldCities/CityMaterialsAtlas.png";
var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(texturePath);
importer.maxTextureSize = 2048;
importer.mipmapEnabled = true;
importer.textureCompression = UnityEditor.TextureImporterCompression.CompressedHQ;
importer.anisoLevel = 4;
importer.SaveAndReimport();
var shader = Shader.Find("Universal Render Pipeline/Lit");
if (shader == null) throw new System.Exception("URP Lit 셰이더가 없습니다.");
var material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(directory + "/CityAtlas.mat");
if (material == null) {
    material = new Material(shader);
    material.SetTexture("_BaseMap", UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
    material.SetFloat("_Smoothness", .12f);
    material.SetFloat("_Cull", 0);
    UnityEditor.AssetDatabase.CreateAsset(material, directory + "/CityAtlas.mat");
}
string prefabDirectory = "Assets/03.Prefabs/Environment/WorldCities";
System.IO.Directory.CreateDirectory(prefabDirectory);
var report = new System.Collections.Generic.List<string>();
foreach (var path in System.IO.Directory.GetFiles("Assets/08.Models/WorldCities", "*.fbx")) {
    string normalized = path.Replace('\\','/');
    string name = System.IO.Path.GetFileNameWithoutExtension(path);
    string prefabPath = prefabDirectory + "/" + name + ".prefab";
    if (System.IO.File.Exists(prefabPath)) { report.Add(name+": 기존 프리팹 유지"); continue; }
    var model = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(normalized);
    if(model == null) throw new System.Exception("모델 임포트 실패: " + normalized);
    var instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(model);
    instance.name = name;
    foreach(var renderer in instance.GetComponentsInChildren<MeshRenderer>()) renderer.sharedMaterial=material;
    foreach(var filter in instance.GetComponentsInChildren<MeshFilter>()) {
        filter.gameObject.layer = LayerMask.NameToLayer("Ground");
        var collider = filter.gameObject.AddComponent<MeshCollider>();
        collider.sharedMesh = filter.sharedMesh;
        filter.gameObject.isStatic = true;
    }
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(instance,prefabPath);
    report.Add(name+": "+instance.GetComponentsInChildren<MeshFilter>()[0].sharedMesh.bounds);
    UnityEngine.Object.DestroyImmediate(instance);
}
UnityEditor.AssetDatabase.SaveAssets();
return string.Join("\n",report.ToArray());
