// false: 검수본 제작, true: 검수된 외형을 기존 정규 프리팹에 적용. MCP 메서드 본문.
bool applyToCanonical = false;
if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
    throw new System.InvalidOperationException("편집 모드와 컴파일 완료가 필요합니다.");
// 에디터에 Newtonsoft 복수 버전이 로드되어 있어 한 어셈블리의 파서를 명시적으로 사용한다.
System.Reflection.Assembly jsonAssembly = null;
foreach(var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
    if(assembly.GetName().Name == "Newtonsoft.Json") { jsonAssembly = assembly; break; }
object recipeRoot = jsonAssembly.GetType("Newtonsoft.Json.Linq.JObject").GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{System.IO.File.ReadAllText("ArtSource/P09NpcOutfits/recipes.json")});
System.Func<object,string,object> field = (value,key) => value.GetType().GetProperty("Item",new[]{typeof(string)}).GetValue(value,new object[]{key});
System.Collections.IEnumerable variants = (System.Collections.IEnumerable)field(recipeRoot,"variants");
string folder = "Assets/08.Models/Equipment/P09NpcOutfits";
string staging = "Assets/03.Prefabs/Characters/EquipmentReview/NpcVariants";
string backupFolder = "output/P09NpcOutfits/Backup";
System.IO.Directory.CreateDirectory(staging); System.IO.Directory.CreateDirectory(backupFolder);
AssetDatabase.Refresh();
var originals = new System.Collections.Generic.Dictionary<string, byte[]>();
var report = new System.Collections.Generic.List<object>();
foreach (object recipe in variants)
{
    string path = field(recipe,"prefab").ToString();
    originals.Add(path, System.IO.File.ReadAllBytes(path));
    string backup = backupFolder + "/" + field(recipe,"actorId") + ".prefab";
    if (!System.IO.File.Exists(backup)) System.IO.File.WriteAllBytes(backup, originals[path]);
}
try
{
    foreach (object recipe in variants)
    {
        string name = field(recipe,"name").ToString();
        string actorId = field(recipe,"actorId").ToString();
        string path = field(recipe,"prefab").ToString();
        string modelPath = field(recipe,"fbx").ToString();
        string texturePath = field(recipe,"texture").ToString();
        AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.sRGBTexture = true; importer.mipmapEnabled = true; importer.wrapMode = TextureWrapMode.Repeat; importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        var template = AssetDatabase.LoadAssetAtPath<Material>("Assets/08.Models/Equipment/P09RangerVest/RangerVest_Leather.mat");
        var materials = new Material[3];
        string[] parts = { "Fabric", "Edge", "Brass" };
        string[] colors = { "fabricTint", "edgeTint", "metalTint" };
        for (int i = 0; i < 3; i++)
        {
            string materialPath = folder + "/" + name + "_" + parts[i] + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (mat == null) { mat = new Material(template); AssetDatabase.CreateAsset(mat, materialPath); }
            var rgb = new System.Collections.Generic.List<float>();
            foreach(var channel in (System.Collections.IEnumerable)field(recipe,colors[i]))rgb.Add(float.Parse(channel.ToString(),System.Globalization.CultureInfo.InvariantCulture));
            var color = new Color(rgb[0],rgb[1],rgb[2],1);
            foreach (string property in mat.GetTexturePropertyNames()) mat.SetTexture(property, null);
            foreach (string property in new[] { "_MainTex", "_BaseMap", "_BaseColorMap", "_OutlineTex" })
                if (mat.HasProperty(property)) mat.SetTexture(property, i == 0 ? texture : null);
            foreach (string property in new[] { "_Color", "_BaseColor" }) if (mat.HasProperty(property)) mat.SetColor(property, color);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", i == 2 ? 0.55f : 0);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", i == 2 ? 0.4f : 0.12f);
            mat.name = name + "_" + parts[i]; EditorUtility.SetDirty(mat); AssetDatabase.SaveAssetIfDirty(mat); materials[i] = mat;
        }
        string guid = AssetDatabase.AssetPathToGUID(path);
        var root = PrefabUtility.LoadPrefabContents(path);
        UnityEditor.EditorWindow tool = null;
        try
        {
            var protectedComponents = new System.Collections.Generic.Dictionary<MonoBehaviour,string>();
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                if (component != null) protectedComponents.Add(component,EditorJsonUtility.ToJson(component));
            var oldOutfit = root.transform.Find("AuthoredNpcOutfit");
            if (oldOutfit != null) UnityEngine.Object.DestroyImmediate(oldOutfit.gameObject);
            var outfit = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(outfit,root.scene);
            outfit.name = "AuthoredNpcOutfit"; outfit.transform.SetParent(root.transform,false);
            var skin = outfit.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (skin == null) throw new System.InvalidOperationException("의상 SMR 누락");
            skin.sharedMaterials = materials;
            var beforeMesh = new Mesh(); skin.BakeMesh(beforeMesh);
            var before = beforeMesh.vertices;
            for(int i=0;i<before.Length;i++) before[i]=skin.transform.TransformPoint(before[i]);
            UnityEngine.Object.DestroyImmediate(beforeMesh);
            var targetRoot = root.transform.Find("Skeleton_P09_Body/root");
            var type = typeof(UPlayGround.Editor.AvatarArmatureBakeTool);
            tool = (UnityEditor.EditorWindow)ScriptableObject.CreateInstance(type);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var fields = new System.Collections.Generic.Dictionary<string,object> {
                {"_avatarObject",root},{"_sourceObject",outfit},{"_sourceRoot",skin.rootBone},{"_targetRoot",targetRoot},
                {"_keepSourceRootAsChild",false},{"_autoDetectPrefixSuffix",false},{"_prefix",""},{"_suffix",""},
                {"_retargetSkinnedMeshes",true},{"_deleteRetargetedDuplicateBones",true},{"_saveBakedMeshesAsAssets",false}
            };
            foreach(var entry in fields) type.GetField(entry.Key,flags).SetValue(tool,entry.Value);
            type.GetMethod("Bake",flags).Invoke(tool,null);
            if(skin.rootBone != targetRoot) throw new System.InvalidOperationException("본 병합 실패");
            foreach(var bone in skin.bones) if(bone==null || bone!=targetRoot && !bone.IsChildOf(targetRoot)) throw new System.InvalidOperationException("외부 본 참조");
            var afterMesh = new Mesh(); skin.BakeMesh(afterMesh); var after = afterMesh.vertices;
            float maxError=0; for(int i=0;i<after.Length;i++) maxError=Mathf.Max(maxError,Vector3.Distance(before[i],skin.transform.TransformPoint(after[i])));
            UnityEngine.Object.DestroyImmediate(afterMesh);
            if(maxError>0.0001f) throw new System.InvalidOperationException("병합 중 형상 변형: "+maxError);
            var generated = skin.sharedMesh;
            string meshPath = folder+"/"+name+"_BakedMesh.asset";
            var savedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(savedMesh==null) AssetDatabase.CreateAsset(generated,meshPath);
            else
            {
                // 네이티브 메시 직렬화 복사는 GPU 스키닝 버퍼를 갱신하지 않으므로 Mesh API로 재구성한다.
                if(generated.blendShapeCount!=0)throw new System.InvalidOperationException("의상 BlendShape 복사 경로가 필요합니다.");
                savedMesh.Clear(false);
                savedMesh.indexFormat=generated.indexFormat;
                savedMesh.vertices=generated.vertices;savedMesh.normals=generated.normals;
                savedMesh.tangents=generated.tangents;savedMesh.colors32=generated.colors32;
                for(int channel=0;channel<8;channel++)
                {
                    var uv=new System.Collections.Generic.List<Vector4>();
                    generated.GetUVs(channel,uv);
                    if(uv.Count>0)savedMesh.SetUVs(channel,uv);
                }
                savedMesh.bindposes=generated.bindposes;savedMesh.boneWeights=generated.boneWeights;
                savedMesh.subMeshCount=generated.subMeshCount;
                for(int sub=0;sub<generated.subMeshCount;sub++)savedMesh.SetIndices(generated.GetIndices(sub),generated.GetTopology(sub),sub,false);
                savedMesh.bounds=generated.bounds;savedMesh.UploadMeshData(false);
                UnityEngine.Object.DestroyImmediate(generated);skin.sharedMesh=savedMesh;
                EditorUtility.SetDirty(savedMesh);AssetDatabase.SaveAssetIfDirty(savedMesh);
            }
            skin.localBounds=new Bounds(new Vector3(0,0.95f,0.03f),new Vector3(0.9f,1.15f,0.8f));
            var allowedVisible = new System.Collections.Generic.HashSet<Renderer>();
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
                if(renderer.gameObject.activeInHierarchy)allowedVisible.Add(renderer);
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                foreach(object hidden in (System.Collections.IEnumerable)field(recipe,"hide"))
                    if(renderer.name==hidden.ToString() || renderer.name.StartsWith(hidden+"_",System.StringComparison.Ordinal)) renderer.gameObject.SetActive(false);
                if(renderer.name==field(recipe,"skin").ToString())
                {
                    allowedVisible.Add(renderer);
                    for(var t=renderer.transform;t!=root.transform;t=t.parent)t.gameObject.SetActive(true);
                }
                var shown=field(recipe,"show") as System.Collections.IEnumerable;
                if(shown!=null)foreach(object visible in shown)
                    if(renderer.name==visible.ToString())
                    {
                        allowedVisible.Add(renderer);
                        for(var t=renderer.transform;t!=root.transform;t=t.parent)t.gameObject.SetActive(true);
                    }
            }
            // 부품 그룹을 켜면서 선택하지 않은 형제 몸체까지 노출되는 것을 막는다.
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
                if(renderer.gameObject.activeInHierarchy && !allowedVisible.Contains(renderer))renderer.gameObject.SetActive(false);
            foreach(var pair in protectedComponents)
                if(pair.Key==null || EditorJsonUtility.ToJson(pair.Key)!=pair.Value)throw new System.InvalidOperationException("행동·상호작용 컴포넌트 변경");
            foreach(var t in root.GetComponentsInChildren<Transform>(true))
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new System.InvalidOperationException("Missing Script");
            foreach(var weight in skin.sharedMesh.boneWeights)
                if(Mathf.Abs(weight.weight0+weight.weight1+weight.weight2+weight.weight3-1)>0.001f)throw new System.InvalidOperationException("웨이트 정규화 실패");
            string destination=applyToCanonical?path:staging+"/"+actorId+".prefab";
            bool success; PrefabUtility.SaveAsPrefabAsset(root,destination,out success);
            if(!success || AssetDatabase.AssetPathToGUID(path)!=guid)throw new System.InvalidOperationException("프리팹 저장 또는 GUID 보존 실패");
            // 모델 전용 검수본은 게임 행동을 실행하지 않고 기존 골격과 얼굴만 공유한다.
            // 정규 변형 관계를 저장한 뒤 검수본을 만들 때만 연결을 해제한다.
            if(PrefabUtility.IsPartOfPrefabInstance(root))
                PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            var model = new GameObject(actorId+"_OutfitModel");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(model,root.scene);
            model.AddComponent<Animator>().avatar=root.GetComponent<Animator>().avatar;
            var children = new System.Collections.Generic.List<Transform>();
            foreach(Transform child in root.transform)children.Add(child);
            foreach(var child in children)
            {
                child.SetParent(model.transform,false);
                if(child.parent!=model.transform)throw new System.InvalidOperationException("검수용 모델 자식 분리 실패");
            }
            foreach(var behaviour in model.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(behaviour);
            PrefabUtility.SaveAsPrefabAsset(model,staging+"/"+actorId+"_Model.prefab",out success);
            if(!success)throw new System.InvalidOperationException("검수 모델 저장 실패");
            report.Add(new {actorId,destination,guid,maxBindError=maxError,vertices=skin.sharedMesh.vertexCount,triangles=skin.sharedMesh.triangles.Length/3,protectedComponents=protectedComponents.Count});
        }
        finally {if(tool!=null)UnityEngine.Object.DestroyImmediate(tool);PrefabUtility.UnloadPrefabContents(root);}
    }
}
catch
{
    if(applyToCanonical)foreach(var original in originals){System.IO.File.WriteAllBytes(original.Key,original.Value);AssetDatabase.ImportAsset(original.Key,ImportAssetOptions.ForceSynchronousImport);}
    throw;
}
string json = (string)jsonAssembly.GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{report});
System.IO.File.WriteAllText("output/P09NpcOutfits/"+(applyToCanonical?"canonical":"staging")+"-validation.json",json);
return report;
