// 기존 NPC 런타임 구성과 경량 역할별 외형을 연결하는 씬 저작 기록.
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByName("StonegateCitadel");
if(!scene.IsValid()||Application.isPlaying)throw new System.Exception("돌등성 Edit Mode가 필요합니다.");
if(scene.GetRootGameObjects().Any(r=>r.name=="CitadelResidents"))throw new System.Exception("이미 주민이 배치되었습니다.");
string directory="Assets/10.Datas/World/WorldCities/StonegateCitadel/Residents";
string prefabDirectory="Assets/03.Prefabs/Characters/WorldCities/StonegateCitadel";
System.IO.Directory.CreateDirectory(directory);System.IO.Directory.CreateDirectory(prefabDirectory);
UnityEditor.AssetDatabase.Refresh();
var root=new GameObject("CitadelResidents");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
UnityEditor.Undo.RegisterCreatedObjectUndo(root,"돌등성 주민 배치");
var source=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Characters/ScenarioResidents/Npc/NPC_Shop/NPC_Shop.prefab");
var database=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.ActorDatabase>("Assets/10.Datas/Actor/DataBase/ActorDatabase.asset");
UnityEditor.Undo.RecordObject(database,"돌등성 주민 등록");
var sourceActor=source.GetComponent<UPlayGround.NpcActor>();
var sourceDefinition=(UPlayGround.Data.Actor.ActorDefinitionSO)new UnityEditor.SerializedObject(sourceActor).FindProperty("_definition").objectReferenceValue;
var sourceCatalog=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Merchant.MerchantCatalogSO>("Assets/10.Datas/Merchant/Merchant_Penny.asset");
var catalogs=new System.Collections.Generic.Dictionary<string,UPlayGround.Data.Merchant.MerchantCatalogSO>();
foreach(string role in new[]{"supply","materials"}){
    var catalog=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Merchant.MerchantCatalogSO>(directory+"/Merchant_Citadel_"+role+".asset");
    if(catalog==null)catalog=UnityEngine.Object.Instantiate(sourceCatalog);catalog.name="Merchant_Citadel_"+role;
    var settings=new UnityEditor.SerializedObject(catalog);
    settings.FindProperty("_merchantId").stringValue="merchant_citadel_"+role;
    settings.FindProperty("_displayName").stringValue=role=="supply"?"하루의 여행용품":"담의 재료 매입";
    var offers=settings.FindProperty("_offers");
    for(int i=offers.arraySize-1;i>=0;i--){
        var offer=offers.GetArrayElementAtIndex(i);
        bool sellsSupplies=offer.FindPropertyRelative("_buyPrice").intValue>0;
        if(role=="materials"&&sellsSupplies)offers.DeleteArrayElementAtIndex(i);
    }
    settings.ApplyModifiedPropertiesWithoutUndo();
    string error;if(!catalog.TryValidate(out error))throw new System.Exception(error);
    if(!UnityEditor.AssetDatabase.Contains(catalog))UnityEditor.AssetDatabase.CreateAsset(catalog,directory+"/"+catalog.name+".asset");UnityEditor.EditorUtility.SetDirty(catalog);UnityEditor.AssetDatabase.SaveAssetIfDirty(catalog);catalogs.Add(role,catalog);
}
var culture=System.Globalization.CultureInfo.InvariantCulture;
int count=0;
foreach(string line in System.IO.File.ReadAllLines("ArtSource/WorldCities/citadel-residents.csv")){
    if(line.StartsWith("id,")||string.IsNullOrWhiteSpace(line))continue;
    var row=line.Split(',');string id="NPC_Citadel_"+row[0];
    if(database.All.Any(d=>d!=null&&d.actorId==id))throw new System.Exception("기존 ID 충돌: "+id);
    var model=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Characters/EquipmentReview/RoleVariants/r02_Performance/"+row[2]+".prefab");
    if(model==null)throw new System.Exception("외형 없음: "+row[2]);
    var obj=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(model,scene);
    UnityEditor.PrefabUtility.UnpackPrefabInstance(obj,UnityEditor.PrefabUnpackMode.Completely,UnityEditor.InteractionMode.AutomatedAction);
    obj.name=id;obj.transform.SetParent(root.transform);obj.transform.position=new Vector3(float.Parse(row[3],culture),10.07f,float.Parse(row[4],culture));obj.transform.rotation=Quaternion.Euler(0,float.Parse(row[5],culture),0);
    foreach(var t in obj.GetComponentsInChildren<Transform>(true))t.gameObject.layer=LayerMask.NameToLayer("Npc");
    foreach(var component in source.GetComponents<Component>()){
        if(component is Transform||component is Animator)continue;
        var copy=obj.GetComponent(component.GetType()); if(copy==null)copy=obj.AddComponent(component.GetType());
        UnityEditor.EditorUtility.CopySerialized(component,copy);
    }
    var animator=obj.GetComponent<Animator>();animator.applyRootMotion=true;animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;
    var capsule=obj.GetComponent<CapsuleCollider>();capsule.radius=.35f;capsule.height=1.8f;capsule.center=Vector3.up*.9f;
    var motor=obj.GetComponent<KinematicCharacterController.KinematicCharacterMotor>();
    var motorSettings=new UnityEditor.SerializedObject(motor);
    motorSettings.FindProperty("Capsule").objectReferenceValue=capsule;
    motorSettings.FindProperty("CapsuleRadius").floatValue=.35f;motorSettings.FindProperty("CapsuleHeight").floatValue=1.8f;motorSettings.FindProperty("CapsuleYOffset").floatValue=.9f;motorSettings.ApplyModifiedPropertiesWithoutUndo();
    var animancer=obj.GetComponent<Animancer.AnimancerComponent>();
    var animancerSettings=new UnityEditor.SerializedObject(animancer);animancerSettings.FindProperty("_Animator").objectReferenceValue=animator;animancerSettings.ApplyModifiedPropertiesWithoutUndo();
    var npcData=ScriptableObject.CreateInstance<UPlayGround.Data.Actor.NpcActorSO>();npcData.name=id;npcData.actorName=row[1];
    npcData.description=row[8]=="supply"?"여행에 필요한 물건을 사고 판다." : "";
    if(!string.IsNullOrEmpty(row[8]))npcData.merchantCatalog=catalogs[row[8]];
    UnityEditor.AssetDatabase.CreateAsset(npcData,directory+"/"+id+".asset");
    var definition=UnityEngine.Object.Instantiate(sourceDefinition);definition.name="ActorDef_"+id;definition.actorId=id;definition.displayName=row[1];definition.npcData=npcData;definition.prefab=null;
    UnityEditor.AssetDatabase.CreateAsset(definition,directory+"/"+definition.name+".asset");
    var actor=obj.GetComponent<UPlayGround.NpcActor>();var actorSettings=new UnityEditor.SerializedObject(actor);
    actorSettings.FindProperty("_definition").objectReferenceValue=definition;actorSettings.FindProperty("_actorId").stringValue=id;actorSettings.FindProperty("_data").objectReferenceValue=npcData;actorSettings.ApplyModifiedPropertiesWithoutUndo();
    var brain=obj.GetComponent<UPlayGround.Components.NpcBrain>();var brainSettings=new UnityEditor.SerializedObject(brain);
    float radius=float.Parse(row[6],culture);brainSettings.FindProperty("_enableWander").boolValue=radius>0;brainSettings.FindProperty("_patrolRadius").floatValue=radius;brainSettings.FindProperty("_patrolWaitTime").floatValue=6+count%4;
    brainSettings.FindProperty("_idleLookAngle").floatValue=20;
    if(!string.IsNullOrEmpty(row[7]))brainSettings.FindProperty("_workMotion").objectReferenceValue=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/LivingWorld/"+row[7]+".asset");
    brainSettings.ApplyModifiedPropertiesWithoutUndo();
    // 원본 모델의 외형만 재사용하고 런타임 식별자와 데이터는 주민마다 독립시킨다.
    var prefab=UnityEditor.PrefabUtility.SaveAsPrefabAssetAndConnect(obj,prefabDirectory+"/"+id+".prefab",UnityEditor.InteractionMode.AutomatedAction);
    definition.prefab=prefab;UnityEditor.EditorUtility.SetDirty(definition);UnityEditor.AssetDatabase.SaveAssetIfDirty(definition);database.AddDefinition(definition);
    count++;
}
var restData=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.InteractableActorSO>("Assets/10.Datas/Actor/Interaction/RestPoint.asset"));
restData.name="Citadel_Rest";restData.actorName="분수 옆 쉼터";restData.description="잠시 쉬며 일행의 기운을 되찾는다.";
UnityEditor.AssetDatabase.CreateAsset(restData,directory+"/Citadel_Rest.asset");
var rest=new GameObject("CitadelRestPoint");rest.transform.SetParent(root.transform);rest.transform.position=new Vector3(171,10.05f,206);
rest.layer=LayerMask.NameToLayer("InteractableObject");var restCollider=rest.AddComponent<BoxCollider>();restCollider.isTrigger=true;restCollider.center=Vector3.up;restCollider.size=new Vector3(2.8f,2,1.8f);
var restActor=rest.AddComponent<UPlayGround.RestPointActor>();var restSettings=new UnityEditor.SerializedObject(restActor);restSettings.FindProperty("_data").objectReferenceValue=restData;restSettings.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.AssetDatabase.SaveAssetIfDirty(database);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
return count+"명 주민과 상점 2개, 회복 쉼터 배치";
