var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (scene.isDirty || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.EditorApplication.isCompiling) return "BUSY";
var log = new System.Text.StringBuilder();
var clock = System.Diagnostics.Stopwatch.StartNew(); long last = 0;
System.Action<string> lap = name => { long now = clock.ElapsedMilliseconds; log.AppendLine(name + "=" + (now-last)); last=now; };
var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.ReactiveEnvironment.VegetationReactionCatalogSO>("Assets/10.Datas/Environment/Vegetation/Catalog.asset");
var candidates = UPlayGround.EditorTools.VegetationReactionAuthoring.Collect(scene,catalog);
var plants = new System.Collections.Generic.List<UPlayGround.EditorTools.VegetationReactionAuthoring.Plant>();
foreach (var plant in candidates) if (plant.root.GetComponent<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject>() == null) { plants.Add(plant); break; }
UPlayGround.Gameplay.World.LivingWorld.EnvironmentController weather = null;
foreach (var root in scene.GetRootGameObjects()) { var candidate=root.GetComponentInChildren<UPlayGround.Gameplay.World.LivingWorld.EnvironmentController>(true); if(candidate!=null)weather=candidate; }
const float MinimumInputThickness = 0.001f;
System.Action<UnityEditor.SerializedProperty,UnityEngine.Object[]> SetObjects = (array,objects) => {array.arraySize=objects.Length; for(int i=0;i<objects.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=objects[i];};
var boundsMethod=typeof(UPlayGround.EditorTools.VegetationReactionAuthoring).GetMethod("LocalBounds",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
System.Func<UPlayGround.EditorTools.VegetationReactionAuthoring.Plant,UnityEngine.Bounds> LocalBounds = p => (UnityEngine.Bounds)boundsMethod.Invoke(null,new object[]{p});
lap("collect");
UnityEditor.Undo.IncrementCurrentGroup(); int outer=UnityEditor.Undo.GetCurrentGroup();
UnityEditor.EditorApplication.LockReloadAssemblies(); UnityEditor.AssetDatabase.DisallowAutoRefresh();
var target=plants[0].root; var profile=plants[0].definition;
try {
            if (target == null || profile == null || !profile.IsValid()) throw new ArgumentException("대상/프로필 오류입니다.");
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            string source = stage != null ? stage.assetPath : AssetDatabase.GetAssetPath(target);
            if (EditorUtility.IsPersistent(target) || source.StartsWith("Assets/ExternalAssets/", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("프로젝트 wrapper의 Prefab Mode 또는 씬 인스턴스를 선택하세요.");
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("반응 환경 연결");
            try
            {
                var id = target.GetComponent<UPlayGround.Components.SceneEntityId>();
                if (id == null) id = Undo.AddComponent<UPlayGround.Components.SceneEntityId>(target);
                lap("SceneEntityId add");
                var reaction = target.GetComponent<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject>();
                if (reaction == null) reaction = Undo.AddComponent<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject>(target);
                lap("reaction add");
                Undo.RecordObject(reaction, "반응 프로필 연결");
                var serialized = new SerializedObject(reaction);
                serialized.FindProperty("_definition").objectReferenceValue = profile;
                serialized.FindProperty("_weather").objectReferenceValue = weather;
                lap("definition");
                if (serialized.FindProperty("_rainSamples").arraySize == 0)
                {
                    var samples = new GameObject("ReactionSamples");
                    Undo.RegisterCreatedObjectUndo(samples, "반응 샘플 생성");
                    Undo.SetTransformParent(samples.transform, target.transform, "반응 샘플 연결");
                    samples.transform.localPosition = Vector3.zero;
                    foreach (string name in new[] { "_rainSamples", "_heatSamples" })
                    {
                        var array = serialized.FindProperty(name); array.arraySize = 1;
                        array.GetArrayElementAtIndex(0).objectReferenceValue = samples.transform;
                    }
                }
                lap("samples");
                if (serialized.FindProperty("_hitColliders").arraySize == 0)
                {
                    var colliders = target.GetComponentsInChildren<Collider>(true);
                    var array = serialized.FindProperty("_hitColliders"); array.arraySize = colliders.Length;
                    for (int i = 0; i < colliders.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = colliders[i];
                }
                lap("hit colliders");
                serialized.ApplyModifiedProperties();
                lap("serialize reaction");
                if (stage == null && target.GetComponent<UPlayGround.Components.WorldPlacementMetadata>() == null)
                {
                    string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target);
                    if (!string.IsNullOrEmpty(prefabPath))
                    {
                        var metadata = Undo.AddComponent<UPlayGround.Components.WorldPlacementMetadata>(target);
                        metadata.EditorSetPlacementInfo(UPlayGround.Components.WorldPlacementMetadata.PlacementSourceKind.DirectPrefab,
                            AssetDatabase.AssetPathToGUID(prefabPath), UPlayGround.Components.WorldPlacementMetadata.PlacementBakeMode.RuntimeData,
                            "", 0, target.activeSelf);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(metadata);
                    }
                }
                lap("metadata add");
                if (profile.reaction.canBreak && target.GetComponent<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentDestruction>() == null)
                    Undo.AddComponent<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentDestruction>(target);
                if (stage != null) { Undo.RecordObject(id, "프리팹 배치 ID 비우기"); id.EditorSetGuid(""); }
                lap("after metadata");
                PrefabUtility.RecordPrefabInstancePropertyModifications(reaction);
                lap("prefab record");
                Undo.CollapseUndoOperations(group);
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
return log.ToString();
} finally {
UnityEditor.Undo.RevertAllDownToGroup(outer);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
UnityEditor.AssetDatabase.AllowAutoRefresh(); UnityEditor.EditorApplication.UnlockReloadAssemblies();
System.IO.File.WriteAllText("output/ReactiveEnvironment/VegetationConversion-20261114/authoring-base-cost.txt",log.ToString());
}

