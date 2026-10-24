// 검수본에서 확인한 뒤 applyToCanonical을 켜서 같은 GUID의 정규 프리팹에 반영한다.
bool applyToCanonical=false;
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new System.InvalidOperationException("편집 모드가 필요합니다.");
string path="Assets/03.Prefabs/Characters/ScenarioResidents/Npc/NPC_Jun/NPC_Jun.prefab";
string folder="Assets/08.Models/Equipment/P09JunWorkwear";
string staging="Assets/03.Prefabs/Characters/EquipmentReview/JunWorkwear";
System.IO.Directory.CreateDirectory(staging);
System.IO.Directory.CreateDirectory("output/P09JunWorkwear");
var original=System.IO.File.ReadAllBytes(path);
if(!System.IO.File.Exists("output/P09JunWorkwear/NPC_Jun_Before.prefab"))System.IO.File.WriteAllBytes("output/P09JunWorkwear/NPC_Jun_Before.prefab",original);
string guid=AssetDatabase.AssetPathToGUID(path);
AssetDatabase.Refresh();
string[] names={"Canvas","Leather","Edge","Brass","Wood","Hair","HairBase"};
string[] textures={"Assets/04.Images/Equipment/P09JunWorkwear/TaupeCanvas_BaseColor.png","Assets/04.Images/Equipment/P09RangerVest/ChestnutLeather_BaseColor.png",null,null,null,"Assets/04.Images/Equipment/P09JunWorkwear/AshBrownHair_BaseColor.png",null};
Color[] colors={Color.white,new Color(.64f,.60f,.54f),new Color(.12f,.09f,.065f),new Color(.50f,.36f,.18f),new Color(.24f,.14f,.075f),Color.white,new Color(.18f,.125f,.085f)};
var materials=new System.Collections.Generic.Dictionary<string,Material>();
for(int i=0;i<names.Length;i++)
{
    string materialPath=folder+"/Jun_"+names[i]+".mat";
    var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
    if(material==null){material=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/08.Models/Equipment/P09RangerVest/RangerVest_Leather.mat"));AssetDatabase.CreateAsset(material,materialPath);}
    material.CopyPropertiesFromMaterial(AssetDatabase.LoadAssetAtPath<Material>("Assets/08.Models/Equipment/P09RangerVest/RangerVest_Leather.mat"));
    if(names[i].StartsWith("Hair"))
    {
        var hairTemplate=AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r=>r.name=="Hair_05").sharedMaterial;
        foreach(string property in new[]{"_ShadowBorder","_ShadowBlur","_ShadowReceive","_Shadow2ndBorder","_Shadow2ndReceive","_ShadowEnvStrength"})
            if(material.HasProperty(property)&&hairTemplate.HasProperty(property))material.SetFloat(property,hairTemplate.GetFloat(property));
    }
    Texture2D texture=null;
    if(textures[i]!=null)
    {
        AssetDatabase.ImportAsset(textures[i],ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(textures[i]);
        importer.sRGBTexture=true;importer.wrapMode=TextureWrapMode.Repeat;importer.mipmapEnabled=true;importer.maxTextureSize=2048;importer.SaveAndReimport();
        texture=AssetDatabase.LoadAssetAtPath<Texture2D>(textures[i]);
    }
    foreach(string property in material.GetTexturePropertyNames())material.SetTexture(property,null);
    foreach(string property in new[]{"_MainTex","_BaseMap","_OutlineTex"})if(material.HasProperty(property))material.SetTexture(property,texture);
    foreach(string property in new[]{"_Color","_BaseColor"})if(material.HasProperty(property))material.SetColor(property,colors[i]);
    if(material.HasProperty("_Metallic"))material.SetFloat("_Metallic",names[i]=="Brass"?.55f:0);
    if(material.HasProperty("_Smoothness"))material.SetFloat("_Smoothness",names[i]=="Brass"?.4f:.12f);
    material.name="Jun_"+names[i];EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);materials.Add(material.name,material);
}
var root=PrefabUtility.LoadPrefabContents(path);
var report=new System.Collections.Generic.List<object>();
UnityEditor.EditorWindow tool=null;
try
{
    var protectedComponents=new System.Collections.Generic.Dictionary<MonoBehaviour,string>();
    foreach(var component in root.GetComponentsInChildren<MonoBehaviour>(true))if(component!=null)protectedComponents.Add(component,EditorJsonUtility.ToJson(component));
    var previous=root.transform.Find("AuthoredJunWorkwear");
    if(previous!=null)
    {
        foreach(var renderer in previous.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if(renderer.rootBone!=null&&renderer.rootBone.name=="JunHairRoot")UnityEngine.Object.DestroyImmediate(renderer.rootBone.gameObject);
        UnityEngine.Object.DestroyImmediate(previous.gameObject);
    }
    var container=new GameObject("AuthoredJunWorkwear");
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(container,root.scene);container.transform.SetParent(root.transform,false);
    var targetRoot=root.transform.Find("Skeleton_P09_Body/root");
    var head=targetRoot.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Head");
    var type=typeof(UPlayGround.Editor.AvatarArmatureBakeTool);
    var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
    tool=(UnityEditor.EditorWindow)ScriptableObject.CreateInstance(type);
    for(int part=0;part<2;part++)
    {
        bool isHair=part==1;
        string modelPath=folder+(isHair?"/Jun_SweptShortHair.fbx":"/Jun_Workwear.fbx");
        AssetDatabase.ImportAsset(modelPath,ImportAssetOptions.ForceSynchronousImport);
        var obj=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(obj,root.scene);obj.transform.SetParent(container.transform,false);
        var skins=obj.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if(skins.Length!=(isHair?1:3))throw new System.InvalidOperationException("제작 메시 수가 일치하지 않습니다.");
        var before=new System.Collections.Generic.Dictionary<SkinnedMeshRenderer,Vector3[]>();
        foreach(var skin in skins)
        {
            var assigned=new Material[skin.sharedMaterials.Length];
            for(int i=0;i<assigned.Length;i++)
            {
                string key=skin.sharedMaterials[i].name;
                if(!materials.ContainsKey(key))throw new System.InvalidOperationException("재질 매핑 누락: "+key);
                assigned[i]=materials[key];
            }
            skin.sharedMaterials=assigned;
            var mesh=new Mesh();skin.BakeMesh(mesh);var points=mesh.vertices;
            for(int i=0;i<points.Length;i++)points[i]=skin.transform.TransformPoint(points[i]);
            before.Add(skin,points);UnityEngine.Object.DestroyImmediate(mesh);
        }
        var fields=new System.Collections.Generic.Dictionary<string,object>{
            {"_avatarObject",root},{"_sourceObject",obj},{"_sourceRoot",skins[0].rootBone},{"_targetRoot",isHair?head:targetRoot},
            {"_keepSourceRootAsChild",isHair},{"_autoDetectPrefixSuffix",false},{"_prefix",""},{"_suffix",""},
            {"_retargetSkinnedMeshes",!isHair},{"_deleteRetargetedDuplicateBones",!isHair},{"_saveBakedMeshesAsAssets",false}
        };
        foreach(var field in fields)type.GetField(field.Key,flags).SetValue(tool,field.Value);
        type.GetMethod("Bake",flags).Invoke(tool,null);
        foreach(var skin in skins)
        {
            if(isHair&&skin.rootBone.parent!=head)throw new System.InvalidOperationException("머리 본 연결 실패");
            if(!isHair&&skin.rootBone!=targetRoot)throw new System.InvalidOperationException("의상 본 병합 실패");
            foreach(var bone in skin.bones)if(bone==null||!bone.IsChildOf(root.transform))throw new System.InvalidOperationException("외부 본 참조");
            var baked=new Mesh();skin.BakeMesh(baked);var points=baked.vertices;float error=0;
            Bounds bounds=new Bounds(skin.rootBone.InverseTransformPoint(skin.transform.TransformPoint(points[0])),Vector3.zero);
            for(int i=0;i<points.Length;i++)
            {
                error=Mathf.Max(error,Vector3.Distance(before[skin][i],skin.transform.TransformPoint(points[i])));
                bounds.Encapsulate(skin.rootBone.InverseTransformPoint(skin.transform.TransformPoint(points[i])));
            }
            UnityEngine.Object.DestroyImmediate(baked);
            if(error>.0001f)throw new System.InvalidOperationException("연결 중 형상 변형: "+skin.name+" "+error);
            if(!isHair)
            {
                var generated=skin.sharedMesh;
                string meshPath=folder+"/"+skin.name+"_BakedMesh.asset";
                var saved=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if(saved==null)AssetDatabase.CreateAsset(generated,meshPath);
                else
                {
                    saved.Clear(false);saved.indexFormat=generated.indexFormat;saved.vertices=generated.vertices;
                    saved.normals=generated.normals;saved.tangents=generated.tangents;saved.colors32=generated.colors32;
                    for(int channel=0;channel<8;channel++){var uv=new System.Collections.Generic.List<Vector4>();generated.GetUVs(channel,uv);if(uv.Count>0)saved.SetUVs(channel,uv);}
                    saved.bindposes=generated.bindposes;saved.boneWeights=generated.boneWeights;saved.subMeshCount=generated.subMeshCount;
                    for(int sub=0;sub<generated.subMeshCount;sub++)saved.SetIndices(generated.GetIndices(sub),generated.GetTopology(sub),sub,false);
                    saved.bounds=generated.bounds;saved.UploadMeshData(false);UnityEngine.Object.DestroyImmediate(generated);
                    skin.sharedMesh=saved;EditorUtility.SetDirty(saved);AssetDatabase.SaveAssetIfDirty(saved);
                }
            }
            bounds.Expand(.04f);
            skin.localBounds=isHair?bounds:new Bounds(new Vector3(0,.65f,0),new Vector3(1.4f,1.7f,1.9f));
            foreach(var w in skin.sharedMesh.boneWeights)if(Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)>.001f)throw new System.InvalidOperationException("웨이트 정규화 실패");
            report.Add(new{name=skin.name,vertices=skin.sharedMesh.vertexCount,triangles=skin.sharedMesh.triangles.Length/3,maxBindError=error,root=skin.rootBone.name});
        }
    }
    // 노출 없는 하체에는 숨겨진 피부를 추가하지 않아 부츠와의 중복 표면을 피한다.
    foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
        if(renderer.name=="Male_Armor_003_Leg"||renderer.name=="Male_Armor_002_Waist"||renderer.name=="Hair_05")renderer.gameObject.SetActive(false);
    foreach(var pair in protectedComponents)if(pair.Key==null||EditorJsonUtility.ToJson(pair.Key)!=pair.Value)throw new System.InvalidOperationException("기존 행동 데이터 변경");
    foreach(var t in root.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)!=0)throw new System.InvalidOperationException("Missing Script");
    bool savedPrefab;
    PrefabUtility.SaveAsPrefabAsset(root,applyToCanonical?path:staging+"/NPC_Jun_Workwear.prefab",out savedPrefab);
    if(!savedPrefab||AssetDatabase.AssetPathToGUID(path)!=guid)throw new System.InvalidOperationException("정규 GUID 또는 저장 실패");
    if(PrefabUtility.IsPartOfPrefabInstance(root))PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
    var model=new GameObject("NPC_Jun_Workwear_Model");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(model,root.scene);
    model.AddComponent<Animator>().avatar=root.GetComponent<Animator>().avatar;
    var children=new System.Collections.Generic.List<Transform>();foreach(Transform child in root.transform)children.Add(child);
    foreach(var child in children){child.SetParent(model.transform,false);if(child.parent!=model.transform)throw new System.InvalidOperationException("검수본 분리 실패");}
    foreach(var behaviour in model.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(behaviour);
    PrefabUtility.SaveAsPrefabAsset(model,staging+"/NPC_Jun_Workwear_Model.prefab",out savedPrefab);
    if(!savedPrefab)throw new System.InvalidOperationException("검수 모델 저장 실패");
    report.Add(new{guid,protectedComponents=protectedComponents.Count,canonical=applyToCanonical});
}
catch
{
    if(applyToCanonical){System.IO.File.WriteAllBytes(path,original);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);}
    throw;
}
finally{if(tool!=null)UnityEngine.Object.DestroyImmediate(tool);PrefabUtility.UnloadPrefabContents(root);}
var jsonAssembly=System.AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json");
string json=(string)jsonAssembly.GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{report});
System.IO.File.WriteAllText("output/P09JunWorkwear/"+(applyToCanonical?"canonical":"staging")+"-validation.json",json);
return report;
