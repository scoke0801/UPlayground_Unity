using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.Item;
using UPlayGround.Data.Path;
using UPlayGround.Data.EnumType;
using UPlayGround.Gameplay.World.LivingWorld;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class WildlifeAuthoring
    {
        [Serializable] private sealed class HuntingCatalog { public HuntingSpecies[] species; }
        [Serializable] private sealed class HuntingSpecies
        {
            public string profile, secondary, death;
            public string[] prefabs;
            public float health, secondaryRate;
            public int minimum, maximum;
        }
        /// <summary>배치된 생물의 참조를 보존하며 사냥 보상과 요리 소비처를 연결한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/환경 생물/사냥 연결")]
        public static void ConnectHunting()
        {
            if (Application.isPlaying) throw new InvalidOperationException("편집 모드에서 실행해야 합니다.");
            var database = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/10.Datas/Path/ItemDatabase.asset");
            var recipes = AssetDatabase.LoadAssetAtPath<RecipeDatabase>("Assets/10.Datas/Craft/RecipeDatabase.asset");
            if (database == null || recipes == null) throw new InvalidOperationException("아이템·제작 DB가 필요합니다.");
            var icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ExternalAssets/UI/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Icon_ItemIcons/NoShadow/64/icon_food_meat.png");
            if (icon == null) throw new InvalidOperationException("고기 아이콘이 없습니다.");
            var meat = LoadHuntingAsset<ItemSO>("Assets/10.Datas/Item/Material/AnimalMeat.asset");
            meat.itemId = 100013; meat.itemName = "동물고기";
            meat.itemDescription = "사냥으로 얻은 신선한 고기. 장작으로 구우면 든든한 식사가 된다.";
            meat.itemType = ItemType.MATERIAL; meat.itemRarity = ItemRarity.COMMON; meat.weight = 0.5f; meat.icon = icon;
            EditorUtility.SetDirty(meat);
            var food = LoadHuntingAsset<ConsumableSO>("Assets/10.Datas/Item/Consume/GrilledMeat.asset");
            food.itemId = 200007; food.itemName = "구운 고기";
            food.itemDescription = "불에 노릇하게 구운 고기. 먹으면 최대 체력의 25%를 회복한다.";
            food.itemType = ItemType.CONSUMABLE; food.itemRarity = ItemRarity.COMMON; food.weight = 0.4f; food.icon = icon;
            food.effectType = ConsumableEffectType.HealPercent; food.amount = 0.25f; food.cooldownDuration = 10;
            food.requireEffectiveUse = true;
            EditorUtility.SetDirty(food);
            RegisterHuntingItems(database, meat, food);
            ConnectCookingRecipe(recipes, meat.itemId, food.itemId);
            var leather = AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/10.Datas/Item/Leather.asset");
            var feather = AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/10.Datas/Item/Feather.asset");
            var catalog = JsonUtility.FromJson<HuntingCatalog>(File.ReadAllText("Tools/Wildlife/Hunting.catalog.json"));
            foreach (var species in catalog.species)
            {
                var secondary = string.IsNullOrEmpty(species.secondary) ? null
                    : AssetDatabase.LoadAssetAtPath<ItemSO>(species.secondary);
                ConnectHuntingSpecies(species.profile, species.prefabs, species.health, meat,
                    species.minimum, species.maximum, secondary, species.secondaryRate, species.death);
            }
            AssetDatabase.SaveAssets();
        }

        private static T LoadHuntingAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void RegisterHuntingItems(ItemDatabase database, params ItemSO[] items)
        {
            var serialized = new SerializedObject(database);
            var entries = serialized.FindProperty("allItems");
            foreach (var item in items)
            {
                if (database.AllItems.Any(i => i != null && i != item && i.itemId == item.itemId))
                    throw new InvalidOperationException("아이템 ID 중복: " + item.itemId);
                if (database.AllItems.Contains(item)) continue;
                int index = entries.arraySize++;
                entries.GetArrayElementAtIndex(index).objectReferenceValue = item;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConnectCookingRecipe(RecipeDatabase database, int meatId, int foodId)
        {
            const int recipeId = 40;
            if (database.AllRecipes.Any(r => r.recipeID == recipeId && r.resultItemID != foodId))
                throw new InvalidOperationException("요리 레시피 ID 중복");
            if (database.AllRecipes.Any(r => r.recipeID == recipeId)) return;
            var serialized = new SerializedObject(database);
            var list = serialized.FindProperty("recipes");
            var entry = list.GetArrayElementAtIndex(list.arraySize++);
            entry.FindPropertyRelative("recipeID").intValue = recipeId;
            entry.FindPropertyRelative("recipeName").stringValue = "구운 고기";
            entry.FindPropertyRelative("description").stringValue = "고기를 장작불에 구워 체력을 회복할 식사를 만든다.";
            entry.FindPropertyRelative("resultItemID").intValue = foodId;
            entry.FindPropertyRelative("resultQuantity").intValue = 1;
            entry.FindPropertyRelative("costType").intValue = 0;
            entry.FindPropertyRelative("costAmount").intValue = 0;
            entry.FindPropertyRelative("castTimeSeconds").floatValue = 2;
            entry.FindPropertyRelative("category").intValue = 0;
            entry.FindPropertyRelative("isDebugUnlocked").boolValue = false;
            list = serialized.FindProperty("ingredients");
            int firewoodId = AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/10.Datas/Item/Firewood.asset").itemId;
            foreach (int ingredientId in new[] { meatId, firewoodId })
            {
                entry = list.GetArrayElementAtIndex(list.arraySize++);
                entry.FindPropertyRelative("recipeID").intValue = recipeId;
                entry.FindPropertyRelative("ingredientItemID").intValue = ingredientId;
                entry.FindPropertyRelative("requiredQuantity").intValue = 1;
            }
            list = serialized.FindProperty("unlockConditions");
            entry = list.GetArrayElementAtIndex(list.arraySize++);
            entry.FindPropertyRelative("recipeID").intValue = recipeId;
            entry.FindPropertyRelative("conditionType").intValue = 0;
            entry.FindPropertyRelative("conditionValue").intValue = 0;
            entry.FindPropertyRelative("conditionValue2").intValue = 0;
            entry.FindPropertyRelative("conditionStringValue").stringValue = "";
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConnectHuntingSpecies(string species, string[] prefabs, float health,
            ItemSO meat, int minimum, int maximum, ItemSO secondary, float secondaryRate, string deathSource)
        {
            var profile = LoadOrCreateProfile(species);
            var table = LoadHuntingAsset<EnemyDropTableSO>(DataRoot + "/Hunting_" + species + ".asset");
            table.dropItems = new System.Collections.Generic.List<ItemDropList>
            {
                new() { itemData = meat, rate = 100, minimumDropCount = minimum, maximumDropCount = maximum }
            };
            if (secondary != null)
                table.dropItems.Add(new ItemDropList { itemData = secondary, rate = secondaryRate,
                    minimumDropCount = 1, maximumDropCount = 2 });
            EditorUtility.SetDirty(table);
            profile.MaxHealth = health;
            profile.DropTable = table;
            profile.DeathRemainsSeconds = 3;
            profile.Death = null;
            if (!string.IsNullOrEmpty(deathSource))
            {
                profile.Death = CopyClip(deathSource, null, species + "_Death");
                // 기존 GUID는 유지하면서 모델의 원본 리그에 맞는 클립으로 갱신한다.
                var source = AssetDatabase.LoadAllAssetsAtPath(deathSource).OfType<AnimationClip>()
                    .First(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal));
                EditorUtility.CopySerialized(source, profile.Death);
                profile.Death.name = species + "_Death";
                var settings = AnimationUtility.GetAnimationClipSettings(profile.Death);
                settings.loopTime = false;
                settings.keepOriginalPositionXZ = true;
                settings.keepOriginalPositionY = true;
                settings.keepOriginalOrientation = true;
                AnimationUtility.SetAnimationClipSettings(profile.Death, settings);
                AnimationUtility.SetAnimationEvents(profile.Death, Array.Empty<AnimationEvent>());
                EditorUtility.SetDirty(profile.Death);
            }
            EditorUtility.SetDirty(profile);
            foreach (string name in prefabs)
            {
                string path = PrefabRoot + "/" + name + ".prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    ConfigureHuntingColliders(root, profile);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
        }

        /// <summary>몸 충돌과 공격 피격 영역을 분리하고 실제 몸 크기로 카메라 초점을 유지한다.</summary>
        private static void ConfigureHuntingColliders(GameObject root, UPlayGround.Data.World.WildlifeProfileSO profile)
        {
            profile.Idle.SampleAnimation(root, 0);
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new InvalidOperationException(root.name + ": 모델 Renderer 누락");
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            Vector3 center = root.transform.InverseTransformPoint(bounds.center);
            Vector3 scale = root.transform.lossyScale;
            scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            Vector3 bodySize = new(bounds.size.x / scale.x, bounds.size.y / scale.y, bounds.size.z / scale.z);
            var hurtbox = root.GetComponent<BoxCollider>();
            if (hurtbox == null) hurtbox = root.AddComponent<BoxCollider>();
            Vector3 hurtSize = Vector3.Max(bounds.size, profile.MinimumHurtboxSize);
            hurtbox.size = new Vector3(hurtSize.x / scale.x, hurtSize.y / scale.y, hurtSize.z / scale.z);
            // 확장분은 위쪽으로 더해 피격 영역이 지면 아래에 묻히지 않게 한다.
            hurtbox.center = center + Vector3.up * ((hurtbox.size.y - bodySize.y) * 0.5f);
            hurtbox.isTrigger = true;
            root.layer = LayerMask.NameToLayer("Enemy");

            Collider body = null;
            if (!profile.IsFlying)
            {
                var child = root.transform.Find("BodyCollider");
                if (child == null)
                {
                    child = new GameObject("BodyCollider").transform;
                    child.SetParent(root.transform, false);
                }
                // 공격 레이어에서 제외해 몸이 HurtBox보다 커도 피격 범위를 넓히지 않는다.
                child.gameObject.layer = LayerMask.NameToLayer("Default");
                var box = child.GetComponent<BoxCollider>();
                if (box == null) box = child.gameObject.AddComponent<BoxCollider>();
                box.center = center;
                box.size = bodySize;
                box.isTrigger = false;
                body = box;
            }
            var serialized = new SerializedObject(root.GetComponent<WildlifeCreature>());
            serialized.FindProperty("_bodyCollider").objectReferenceValue = body;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>기존 참조를 유지하면서 환경 생물의 몸 충돌체와 피격 영역만 동기화한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/환경 생물/충돌체 피격 영역 동기화")]
        public static void SynchronizeHuntingColliders()
        {
            if (Application.isPlaying) throw new InvalidOperationException("편집 모드에서 실행해야 합니다.");
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var creature = prefab.GetComponent<WildlifeCreature>();
                if (creature == null || creature.Profile == null || creature.Profile.IsFlying || creature.Profile.MaxHealth <= 0) continue;
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    ConfigureHuntingColliders(root, creature.Profile);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }
    }
}
