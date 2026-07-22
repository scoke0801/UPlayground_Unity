#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Data.Crafting;
using UPlayGround.Data.Item;

namespace UPlayGround.Data.Editor.Authoring
{
    public sealed partial class RecipeDomainPanel
    {
        protected override VisualElement BuildDetail(RecipeData recipe)
        {
            var root = new VisualElement();
            root.style.paddingBottom = 12f;

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.borderTopWidth = 4f;
            header.style.borderTopColor = CategoryColor(recipe.category);
            header.style.paddingTop = 8f;

            var title = new Label(string.IsNullOrWhiteSpace(recipe.recipeName) ? "(이름 없음)" : recipe.recipeName);
            title.style.fontSize = 16f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.flexGrow = 1f;
            header.Add(title);

            var status = new Label();
            status.style.unityFontStyleAndWeight = FontStyle.Bold;
            status.style.color = new Color(1f, 0.55f, 0.35f);
            UpdateDuplicateStatus(status, recipe);
            header.Add(status);
            root.Add(header);

            VisualElement basic = MakeSection("기본 정보");
            var idField = new IntegerField("레시피 ID") { value = recipe.recipeID };
            idField.RegisterValueChangedCallback(evt =>
            {
                int oldId = recipe.recipeID;
                recipe.recipeID = evt.newValue;
                foreach (IngredientData ingredient in _ingredients.Where(row => row.recipeID == oldId))
                    ingredient.recipeID = evt.newValue;
                foreach (RecipeUnlockCondition condition in _unlockConditions.Where(row => row.recipeID == oldId))
                    condition.recipeID = evt.newValue;
                MarkDirty(recipe);
                UpdateDuplicateStatus(status, recipe);
            });
            basic.Add(idField);

            var nameField = new TextField("이름") { value = recipe.recipeName ?? string.Empty };
            nameField.RegisterValueChangedCallback(evt =>
            {
                recipe.recipeName = evt.newValue;
                title.text = string.IsNullOrWhiteSpace(evt.newValue) ? "(이름 없음)" : evt.newValue;
                MarkDirty(recipe);
            });
            basic.Add(nameField);

            var descriptionField = new TextField("설명")
            {
                value = recipe.description ?? string.Empty,
                multiline = true
            };
            descriptionField.style.minHeight = 54f;
            descriptionField.RegisterValueChangedCallback(evt =>
            {
                recipe.description = evt.newValue;
                MarkDirty(recipe);
            });
            basic.Add(descriptionField);

            var categoryField = new EnumField("카테고리", recipe.category);
            categoryField.RegisterValueChangedCallback(evt =>
            {
                recipe.category = (CraftingCategory)evt.newValue;
                header.style.borderTopColor = CategoryColor(recipe.category);
                MarkDirty(recipe);
            });
            basic.Add(categoryField);

            var debugUnlocked = new Toggle("디버그 즉시 해금") { value = recipe.isDebugUnlocked };
            debugUnlocked.RegisterValueChangedCallback(evt =>
            {
                recipe.isDebugUnlocked = evt.newValue;
                MarkDirty(recipe);
            });
            basic.Add(debugUnlocked);
            root.Add(basic);

            VisualElement result = MakeSection("결과물");
            result.Add(BuildItemIdRow("결과 아이템", recipe.resultItemID, id =>
            {
                recipe.resultItemID = id;
                MarkDirty(recipe);
            }));

            var resultQuantity = new IntegerField("결과 수량") { value = recipe.resultQuantity };
            resultQuantity.RegisterValueChangedCallback(evt =>
            {
                recipe.resultQuantity = Mathf.Max(1, evt.newValue);
                resultQuantity.SetValueWithoutNotify(recipe.resultQuantity);
                MarkDirty(recipe);
            });
            result.Add(resultQuantity);
            root.Add(result);

            VisualElement ingredients = MakeSection("필요 재료");
            RebuildIngredients(ingredients, recipe);
            root.Add(ingredients);

            VisualElement cost = MakeSection("비용 및 제작 시간");
            var amountField = new IntegerField("비용") { value = recipe.costAmount };
            var costType = new EnumField("비용 유형", recipe.costType);
            costType.RegisterValueChangedCallback(evt =>
            {
                recipe.costType = (CostType)evt.newValue;
                amountField.style.display = recipe.costType == CostType.Free ? DisplayStyle.None : DisplayStyle.Flex;
                MarkDirty(recipe);
            });
            cost.Add(costType);

            amountField.style.display = recipe.costType == CostType.Free ? DisplayStyle.None : DisplayStyle.Flex;
            amountField.RegisterValueChangedCallback(evt =>
            {
                recipe.costAmount = Mathf.Max(0, evt.newValue);
                amountField.SetValueWithoutNotify(recipe.costAmount);
                MarkDirty(recipe);
            });
            cost.Add(amountField);

            var castTime = new FloatField("제작 시간(초)") { value = recipe.castTimeSeconds };
            castTime.RegisterValueChangedCallback(evt =>
            {
                recipe.castTimeSeconds = Mathf.Max(0f, evt.newValue);
                castTime.SetValueWithoutNotify(recipe.castTimeSeconds);
                MarkDirty(recipe);
            });
            cost.Add(castTime);
            root.Add(cost);

            VisualElement unlock = MakeSection("언락 조건");
            RebuildUnlock(unlock, recipe);
            root.Add(unlock);

            return root;
        }

        private void RebuildIngredients(VisualElement section, RecipeData recipe)
        {
            ClearSectionBody(section);
            var rows = _ingredients.Where(row => row.recipeID == recipe.recipeID).ToList();
            if (rows.Count == 0)
                section.Add(MutedLabel("등록된 재료가 없습니다."));

            for (int index = 0; index < rows.Count; index++)
            {
                IngredientData ingredient = rows[index];
                var box = MakeBox();
                var heading = new VisualElement();
                heading.style.flexDirection = FlexDirection.Row;
                heading.style.alignItems = Align.Center;
                var label = new Label($"재료 {index + 1}");
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.flexGrow = 1f;
                heading.Add(label);
                heading.Add(new Button(() =>
                {
                    _ingredients.Remove(ingredient);
                    MarkDirty(recipe);
                    RebuildIngredients(section, recipe);
                }) { text = "삭제" });
                box.Add(heading);

                box.Add(BuildItemIdRow("아이템", ingredient.ingredientItemID, id =>
                {
                    ingredient.ingredientItemID = id;
                    MarkDirty(recipe);
                }));

                var quantity = new IntegerField("필요 수량") { value = ingredient.requiredQuantity };
                quantity.RegisterValueChangedCallback(evt =>
                {
                    ingredient.requiredQuantity = Mathf.Max(1, evt.newValue);
                    quantity.SetValueWithoutNotify(ingredient.requiredQuantity);
                    MarkDirty(recipe);
                });
                box.Add(quantity);
                section.Add(box);
            }

            section.Add(new Button(() =>
            {
                _ingredients.Add(new IngredientData
                {
                    recipeID = recipe.recipeID,
                    requiredQuantity = 1
                });
                MarkDirty(recipe);
                RebuildIngredients(section, recipe);
            }) { text = "+ 재료 추가" });
        }

        private void RebuildUnlock(VisualElement section, RecipeData recipe)
        {
            ClearSectionBody(section);
            RecipeUnlockCondition condition = _unlockConditions.FirstOrDefault(row => row.recipeID == recipe.recipeID);
            var enabled = new Toggle("언락 조건 사용") { value = condition != null };
            enabled.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue && condition == null)
                {
                    _unlockConditions.Add(new RecipeUnlockCondition
                    {
                        recipeID = recipe.recipeID,
                        conditionType = UnlockConditionType.None
                    });
                }
                else if (!evt.newValue && condition != null)
                {
                    _unlockConditions.Remove(condition);
                }

                MarkDirty(recipe);
                RebuildUnlock(section, recipe);
            });
            section.Add(enabled);

            if (condition == null)
            {
                section.Add(MutedLabel("조건이 없으면 즉시 해금됩니다."));
                return;
            }

            var typeField = new EnumField("조건 유형", condition.conditionType);
            typeField.RegisterValueChangedCallback(evt =>
            {
                condition.conditionType = (UnlockConditionType)evt.newValue;
                MarkDirty(recipe);
                RebuildUnlock(section, recipe);
            });
            section.Add(typeField);

            if (condition.conditionType == UnlockConditionType.ItemCollect
                || condition.conditionType == UnlockConditionType.ItemHave)
            {
                section.Add(BuildItemIdRow("조건 아이템", condition.conditionValue, id =>
                {
                    condition.conditionValue = id;
                    MarkDirty(recipe);
                }));
            }
            else
            {
                var value = new IntegerField(ConditionValueLabel(condition.conditionType)) { value = condition.conditionValue };
                value.RegisterValueChangedCallback(evt =>
                {
                    condition.conditionValue = evt.newValue;
                    MarkDirty(recipe);
                });
                section.Add(value);
            }

            var count = new IntegerField("수량 / 횟수") { value = condition.conditionValue2 };
            count.RegisterValueChangedCallback(evt =>
            {
                condition.conditionValue2 = Mathf.Max(0, evt.newValue);
                count.SetValueWithoutNotify(condition.conditionValue2);
                MarkDirty(recipe);
            });
            section.Add(count);

            if (condition.conditionType == UnlockConditionType.MonsterKill)
            {
                var actorId = new TextField("몬스터 ActorId") { value = condition.conditionStringValue ?? string.Empty };
                actorId.RegisterValueChangedCallback(evt =>
                {
                    condition.conditionStringValue = evt.newValue;
                    MarkDirty(recipe);
                });
                section.Add(actorId);
                section.Add(MutedLabel("ActorId가 지정되면 레거시 숫자 몬스터 ID보다 우선합니다."));
            }
        }

        private VisualElement BuildItemIdRow(string label, int currentId, System.Action<int> onChanged)
        {
            var container = new VisualElement();
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;

            var idField = new IntegerField(label) { value = currentId };
            idField.style.flexGrow = 1f;
            row.Add(idField);

            var pickerButton = new Button { text = "선택" };
            row.Add(pickerButton);
            container.Add(row);

            var hint = MutedLabel(string.Empty);
            container.Add(hint);

            void Apply(int id)
            {
                idField.SetValueWithoutNotify(id);
                UpdateItemHint(hint, id);
                onChanged(id);
            }

            idField.RegisterValueChangedCallback(evt =>
            {
                UpdateItemHint(hint, evt.newValue);
                onChanged(evt.newValue);
            });
            pickerButton.clicked += () =>
            {
                _itemCache.TryGetValue(idField.value, out ItemSO current);
                SharedItemPicker.Show(pickerButton, current, item => Apply(item != null ? item.itemId : 0));
            };
            UpdateItemHint(hint, currentId);
            return container;
        }

        private void UpdateItemHint(Label hint, int itemId)
        {
            if (itemId == 0)
            {
                hint.text = "아이템 미지정";
                hint.style.color = StyleKeyword.Null;
            }
            else if (_itemCache.TryGetValue(itemId, out ItemSO item))
            {
                hint.text = $"→ {item.itemName}  [{item.itemType}]";
                hint.style.color = new Color(0.45f, 0.85f, 0.45f);
            }
            else
            {
                hint.text = $"⚠ ID {itemId}: 등록된 아이템 없음";
                hint.style.color = new Color(1f, 0.45f, 0.4f);
            }
        }

        private void UpdateDuplicateStatus(Label status, RecipeData recipe)
        {
            status.text = HasDuplicateKey(recipe) ? "⚠ 중복 ID" : string.Empty;
        }

        private static VisualElement MakeSection(string title)
        {
            var section = new VisualElement();
            section.style.marginTop = 12f;
            section.style.paddingLeft = 8f;
            section.style.paddingRight = 8f;
            section.style.paddingTop = 7f;
            section.style.paddingBottom = 8f;
            section.style.borderLeftWidth = 1f;
            section.style.borderRightWidth = 1f;
            section.style.borderTopWidth = 1f;
            section.style.borderBottomWidth = 1f;
            Color border = EditorGUIUtility.isProSkin ? new Color(0.18f, 0.18f, 0.18f) : new Color(0.7f, 0.7f, 0.7f);
            section.style.borderLeftColor = border;
            section.style.borderRightColor = border;
            section.style.borderTopColor = border;
            section.style.borderBottomColor = border;

            var heading = new Label(title) { name = "section-title" };
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.marginBottom = 5f;
            section.Add(heading);
            return section;
        }

        private static VisualElement MakeBox()
        {
            var box = new VisualElement();
            box.style.marginBottom = 5f;
            box.style.paddingLeft = 6f;
            box.style.paddingRight = 6f;
            box.style.paddingTop = 5f;
            box.style.paddingBottom = 5f;
            box.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.1f);
            return box;
        }

        private static void ClearSectionBody(VisualElement section)
        {
            while (section.childCount > 1)
                section.RemoveAt(section.childCount - 1);
        }

        private static Label MutedLabel(string text)
        {
            var label = new Label(text);
            label.style.fontSize = 10f;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.color = EditorGUIUtility.isProSkin
                ? new Color(0.68f, 0.68f, 0.68f)
                : new Color(0.35f, 0.35f, 0.35f);
            return label;
        }

        private static string ConditionValueLabel(UnlockConditionType type) => type switch
        {
            UnlockConditionType.MonsterKill => "레거시 몬스터 ID",
            UnlockConditionType.RecipeCraft => "선행 레시피 ID",
            _ => "조건 값"
        };

        private static Color CategoryColor(CraftingCategory category) => category switch
        {
            CraftingCategory.Consumable => new Color(0.3f, 0.85f, 0.3f),
            CraftingCategory.Equipment => new Color(0.3f, 0.55f, 1f),
            CraftingCategory.Material => new Color(0.95f, 0.75f, 0.2f),
            CraftingCategory.Special => new Color(0.85f, 0.3f, 0.85f),
            _ => Color.gray
        };
    }
}
#endif
