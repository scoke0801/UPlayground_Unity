using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Tool.Editor
{
    /// <summary>인스펙터 필드 검색기의 그리기 부분.</summary>
    public partial class InspectorFieldSearchWindow
    {
        // 지금 Hierarchy에서 선택한 오브젝트의 그룹을 한눈에 찾도록 헤더 박스를 물들인다.
        private static readonly Color SelectedGroupTint = new(0.45f, 0.78f, 1f);

        private const float TagLayerLabelWidth = 40f;
        private const float InlineToggleWidth = 108f;

        // Behaviour·Renderer·Collider가 아닌 컴포넌트의 enabled 프로퍼티는 타입별로 한 번만 찾아 캐시한다.
        private static readonly Dictionary<System.Type, PropertyInfo> EnabledPropertyByType = new();

        // 참조 필드를 펼쳐 그대로 편집하는 인라인 에디터. 대상 인스턴스 단위로 캐시한다.
        private readonly Dictionary<int, UnityEditor.Editor> _inlineEditors = new();
        private readonly HashSet<int> _inlineUsedIds = new();
        private readonly Dictionary<int, bool> _inlineExpandedByKey = new();

        // 머티리얼 프로퍼티 조회용 1칸 배열. 인라인 경로에서 매 프레임 배열을 새로 만들지 않는다.
        private readonly UnityEngine.Object[] _materialContextScratch = new UnityEngine.Object[1];

        private void DrawGroups()
        {
            float viewWidth = position.width - VerticalScrollBarWidth;
            float contentWidth = Mathf.Max(viewWidth, MinContentWidth);

            using var scrollScope = new EditorGUILayout.ScrollViewScope(_scroll);
            _scroll = scrollScope.scrollPosition;

            // labelWidth/wideMode는 전역 상태라 다른 에디터 GUI에 새지 않도록 반드시 되돌린다.
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            bool previousWideMode = EditorGUIUtility.wideMode;
            EditorGUIUtility.labelWidth =
                Mathf.Clamp(contentWidth * LabelWidthRatio, MinLabelWidth, MaxLabelWidth);
            EditorGUIUtility.wideMode = contentWidth >= WideModeMinWidth;

            try
            {
                // 내용에 하한 폭을 주면 창이 더 좁을 때 잘리는 대신 가로 스크롤바가 생긴다.
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(contentWidth)))
                {
                    for (int i = 0; i < _groups.Count; i++)
                        DrawGroup(_groups[i]);
                }
            }
            finally
            {
                EditorGUIUtility.labelWidth = previousLabelWidth;
                EditorGUIUtility.wideMode = previousWideMode;
            }
        }

        private void DrawGroup(TargetGroup group)
        {
            DrawGroupHeader(group);

            if (!group.IsExpanded)
                return;

            EditorGUI.indentLevel++;
            for (int i = 0; i < group.Entries.Count; i++)
            {
                InspectedEntry entry = group.Entries[i];

                // 플레이 모드 전환·파괴로 대상이 사라지면 다음 프레임에 다시 수집한다.
                if (entry.Target == null)
                {
                    _isDirty = true;
                    continue;
                }

                // 화면 밖 항목은 그리지 않고 자리만 남긴다. 커스텀 인스펙터 수십 개가 매 프레임 도는 것을 막는다.
                if (entry.IsCulled)
                {
                    GUILayout.Space(entry.CachedHeight);
                    continue;
                }

                DrawEntry(entry);
            }
            EditorGUI.indentLevel--;
        }

        private void DrawGroupHeader(TargetGroup group)
        {
            bool isSelected = group.Owner != null && Selection.activeObject == group.Owner;

            Color previousBackground = GUI.backgroundColor;
            if (isSelected)
                GUI.backgroundColor = SelectedGroupTint;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // 배경만 물들이고 내부 위젯은 원래 색으로 되돌린다.
                GUI.backgroundColor = previousBackground;

                switch (group.Kind)
                {
                    case GroupKind.GameObject:
                        DrawGameObjectHeader(group);
                        break;
                    case GroupKind.MultiSelection:
                        DrawMultiSelectionHeader(group);
                        break;
                    default:
                        DrawAssetHeader(group);
                        break;
                }
            }

            GUI.backgroundColor = previousBackground;
        }

        /// <summary>GameObject 단위 헤더. 활성 상태·Tag·Layer를 여기서 바로 고칠 수 있다.</summary>
        private void DrawGameObjectHeader(TargetGroup group)
        {
            var owner = group.Owner as GameObject;
            if (owner == null)
            {
                _isDirty = true;
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                bool isActive = EditorGUILayout.Toggle(owner.activeSelf, GUILayout.Width(16f));
                if (EditorGUI.EndChangeCheck())
                    ApplyChange(owner, isActive ? "오브젝트 활성화" : "오브젝트 비활성화", () => owner.SetActive(isActive));

                string label = string.IsNullOrEmpty(group.RelativePath) ? owner.name : group.RelativePath;
                DrawGroupFoldout(group, label, EditorGUIUtility.ObjectContent(owner, typeof(GameObject)).image);

                GUILayout.FlexibleSpace();
                DrawSelectButton(owner);
            }

            DrawTagAndLayer(SingleGameObject(owner));
        }

        private void DrawMultiSelectionHeader(TargetGroup group)
        {
            GameObject[] owners = group.OwnerGameObjects;
            if (owners == null || owners.Length == 0)
            {
                _isDirty = true;
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                bool isActive = owners[0].activeSelf;
                bool isActiveMixed = false;
                for (int i = 1; i < owners.Length; i++)
                {
                    if (owners[i].activeSelf == isActive)
                        continue;

                    isActiveMixed = true;
                    break;
                }

                EditorGUI.showMixedValue = isActiveMixed;
                EditorGUI.BeginChangeCheck();
                bool newActive = EditorGUILayout.Toggle(isActive, GUILayout.Width(16f));
                if (EditorGUI.EndChangeCheck())
                {
                    ApplyChange(owners, newActive ? "오브젝트 활성화" : "오브젝트 비활성화", () =>
                    {
                        foreach (GameObject owner in owners)
                            owner.SetActive(newActive);
                    });
                }
                EditorGUI.showMixedValue = false;

                DrawGroupFoldout(group, group.Title, null);
                GUILayout.FlexibleSpace();
            }

            DrawTagAndLayer(owners);
        }

        private void DrawAssetHeader(TargetGroup group)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var icon = group.Owner != null
                    ? EditorGUIUtility.ObjectContent(group.Owner, group.Owner.GetType()).image
                    : null;

                DrawGroupFoldout(group, group.Title, icon);

                GUILayout.FlexibleSpace();
                if (group.Owner != null)
                    DrawSelectButton(group.Owner);
            }

            if (group.Owner == null)
                return;

            string assetPath = AssetDatabase.GetAssetPath(group.Owner);
            if (!string.IsNullOrEmpty(assetPath))
                EditorGUILayout.LabelField(assetPath, EditorStyles.miniLabel);
        }

        private void DrawGroupFoldout(TargetGroup group, string label, Texture icon)
        {
            bool isExpanded = EditorGUILayout.Foldout(group.IsExpanded, new GUIContent(label, icon), true,
                EditorStyles.foldoutHeader);
            if (isExpanded == group.IsExpanded)
                return;

            group.IsExpanded = isExpanded;
            _groupExpandedById[group.Key] = isExpanded;
        }

        private static void DrawSelectButton(UnityEngine.Object target)
        {
            if (!GUILayout.Button(new GUIContent("선택", "이 대상을 선택하고 Hierarchy·Project에서 강조한다."),
                    EditorStyles.miniButton, GUILayout.Width(40f)))
            {
                return;
            }

            Selection.activeObject = target;
            EditorGUIUtility.PingObject(target);
        }

        private GameObject[] SingleGameObject(GameObject owner)
        {
            _singleOwnerScratch[0] = owner;
            return _singleOwnerScratch;
        }

        private readonly GameObject[] _singleOwnerScratch = new GameObject[1];

        /// <summary>Tag·Layer는 여러 오브젝트에 한 번에 적용한다. 값이 갈리면 혼합 표시로 알린다.</summary>
        private void DrawTagAndLayer(GameObject[] owners)
        {
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = TagLayerLabelWidth;

            try
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    string tag = owners[0].tag;
                    int layer = owners[0].layer;
                    bool isTagMixed = false;
                    bool isLayerMixed = false;

                    for (int i = 1; i < owners.Length; i++)
                    {
                        isTagMixed |= owners[i].tag != tag;
                        isLayerMixed |= owners[i].layer != layer;
                    }

                    EditorGUI.showMixedValue = isTagMixed;
                    EditorGUI.BeginChangeCheck();
                    string newTag = EditorGUILayout.TagField("Tag", tag);
                    if (EditorGUI.EndChangeCheck())
                    {
                        GameObject[] applyTargets = CloneOwners(owners);
                        ApplyChange(applyTargets, "태그 변경", () =>
                        {
                            foreach (GameObject owner in applyTargets)
                                owner.tag = newTag;
                        });
                    }

                    EditorGUI.showMixedValue = isLayerMixed;
                    EditorGUI.BeginChangeCheck();
                    int newLayer = EditorGUILayout.LayerField("Layer", layer);
                    if (EditorGUI.EndChangeCheck())
                    {
                        GameObject[] applyTargets = CloneOwners(owners);
                        ApplyChange(applyTargets, "레이어 변경", () =>
                        {
                            foreach (GameObject owner in applyTargets)
                                owner.layer = newLayer;
                        });
                    }

                    EditorGUI.showMixedValue = false;
                }
            }
            finally
            {
                EditorGUIUtility.labelWidth = previousLabelWidth;
            }
        }

        /// <summary>스크래치 배열을 그대로 클로저에 넘기면 다음 항목을 그릴 때 내용이 바뀌므로 복사해 둔다.</summary>
        private static GameObject[] CloneOwners(GameObject[] owners)
        {
            var copy = new GameObject[owners.Length];
            System.Array.Copy(owners, copy, owners.Length);
            return copy;
        }

        private void DrawEntry(InspectedEntry entry)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawEntryHeader(entry);

                if (entry.IsExpanded)
                {
                    EditorGUI.indentLevel++;
                    if (entry.Kind == EntryKind.Material)
                        DrawMaterialBody(entry);
                    else if (IsSearching)
                        DrawMatchedFields(entry);
                    else
                        DrawFullInspector(entry);
                    EditorGUI.indentLevel--;
                }
            }

            // 컬링 판정에 쓸 실측 높이는 Repaint에서만 얻을 수 있다.
            // helpBox 바깥 여백까지 더해야 자리만 남길 때 스크롤 길이가 튀지 않는다.
            if (Event.current.type == EventType.Repaint)
            {
                entry.CachedHeight =
                    GUILayoutUtility.GetLastRect().height + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        private void DrawEntryHeader(InspectedEntry entry)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var icon = EditorGUIUtility.ObjectContent(entry.Target, entry.Target.GetType()).image;

                bool isExpanded = EditorGUILayout.Foldout(entry.IsExpanded,
                    new GUIContent(BuildEntryTitle(entry), icon), true, EditorStyles.foldoutHeader);
                if (isExpanded != entry.IsExpanded)
                {
                    entry.IsExpanded = isExpanded;
                    _expandedStateByKey[entry.Key] = isExpanded;
                }

                GUILayout.FlexibleSpace();

                DrawComponentEnabledToggle(entry);

                if (GUILayout.Button(new GUIContent("핑", "이 항목을 Project·Hierarchy에서 강조한다."),
                        EditorStyles.miniButton, GUILayout.Width(28f)))
                {
                    EditorGUIUtility.PingObject(entry.Target);
                }
            }
        }

        private static string BuildEntryTitle(InspectedEntry entry)
        {
            if (entry.IsMultiTarget)
                return $"{entry.Target.GetType().Name}   × {entry.Targets.Length}";

            switch (entry.Kind)
            {
                case EntryKind.Material:
                    return entry.MaterialSlot >= 0
                        ? $"{entry.Target.name}   (머티리얼 {entry.MaterialSlot})"
                        : entry.Target.name;
                case EntryKind.Asset:
                    return $"{entry.Target.name}   ({entry.Target.GetType().Name})";
                default:
                    return entry.Target.GetType().Name;
            }
        }

        /// <summary>Unity 기본 인스펙터 헤더처럼 컴포넌트의 활성 상태를 편집한다. 다중 선택이면 한 번에 적용한다.</summary>
        private void DrawComponentEnabledToggle(InspectedEntry entry)
        {
            if (entry.Target is not Component first || !TryGetComponentEnabled(first, out bool current))
            {
                // enabled가 없는 컴포넌트(Transform 등)도 토글 자리를 비워 헤더 정렬을 맞춘다.
                GUILayout.Space(18f);
                return;
            }

            bool isMixed = false;
            for (int i = 1; i < entry.Targets.Length; i++)
            {
                if (entry.Targets[i] is not Component other ||
                    !TryGetComponentEnabled(other, out bool otherEnabled) ||
                    otherEnabled == current)
                {
                    continue;
                }

                isMixed = true;
                break;
            }

            using (new EditorGUI.DisabledScope((first.hideFlags & HideFlags.NotEditable) != 0))
            {
                EditorGUI.showMixedValue = isMixed;
                EditorGUI.BeginChangeCheck();
                bool isEnabled = EditorGUILayout.Toggle(current, GUILayout.Width(18f));
                EditorGUI.showMixedValue = false;

                if (!EditorGUI.EndChangeCheck())
                    return;

                UnityEngine.Object[] targets = entry.Targets;
                ApplyChange(targets, isEnabled ? "컴포넌트 활성화" : "컴포넌트 비활성화", () =>
                {
                    foreach (UnityEngine.Object target in targets)
                    {
                        if (target is Component component)
                            SetComponentEnabled(component, isEnabled);
                    }
                });
            }
        }

        private static bool TryGetComponentEnabled(Component component, out bool isEnabled)
        {
            switch (component)
            {
                case Behaviour behaviour:
                    isEnabled = behaviour.enabled;
                    return true;
                case Renderer renderer:
                    isEnabled = renderer.enabled;
                    return true;
                case Collider collider:
                    isEnabled = collider.enabled;
                    return true;
            }

            PropertyInfo property = ResolveEnabledProperty(component.GetType());
            if (property == null)
            {
                isEnabled = false;
                return false;
            }

            isEnabled = (bool)property.GetValue(component);
            return true;
        }

        private static void SetComponentEnabled(Component component, bool isEnabled)
        {
            switch (component)
            {
                case Behaviour behaviour:
                    behaviour.enabled = isEnabled;
                    return;
                case Renderer renderer:
                    renderer.enabled = isEnabled;
                    return;
                case Collider collider:
                    collider.enabled = isEnabled;
                    return;
            }

            ResolveEnabledProperty(component.GetType())?.SetValue(component, isEnabled);
        }

        private static PropertyInfo ResolveEnabledProperty(System.Type type)
        {
            if (EnabledPropertyByType.TryGetValue(type, out PropertyInfo cached))
                return cached;

            PropertyInfo property = type.GetProperty("enabled", BindingFlags.Public | BindingFlags.Instance);
            if (property != null && (property.PropertyType != typeof(bool) || !property.CanRead || !property.CanWrite))
                property = null;

            EnabledPropertyByType[type] = property;
            return property;
        }

        /// <summary>검색어가 없을 때는 실제 Editor를 그려 커스텀 인스펙터를 그대로 재현한다.</summary>
        private void DrawFullInspector(InspectedEntry entry)
        {
            if (entry.IsRenderFailed)
            {
                EditorGUILayout.HelpBox("이 항목의 인스펙터를 그리는 중 예외가 발생해 표시를 중단했습니다.",
                    MessageType.Warning);
                return;
            }

            if (entry.Editor == null || entry.Editor.target != entry.Target)
            {
                if (entry.Editor != null)
                    UnityEngine.Object.DestroyImmediate(entry.Editor);

                entry.Editor = UnityEditor.Editor.CreateEditor(entry.Targets);
            }

            if (entry.Editor == null)
            {
                EditorGUILayout.HelpBox("Editor를 생성하지 못했습니다.", MessageType.Warning);
                return;
            }

            try
            {
                entry.Editor.OnInspectorGUI();
            }
            catch (ExitGUIException)
            {
                // 오브젝트 피커 등이 정상적으로 GUI를 빠져나가는 경로이므로 삼키면 안 된다.
                throw;
            }
            catch (System.Exception exception)
            {
                // 서드파티 인스펙터 하나가 창 전체를 망가뜨리지 않도록 해당 항목만 차단한다.
                entry.IsRenderFailed = true;
                Debug.LogException(exception);
            }
        }

        private void DrawMatchedFields(InspectedEntry entry)
        {
            if (entry.SerializedObject == null || entry.SerializedObject.targetObject == null)
            {
                _isDirty = true;
                return;
            }

            entry.SerializedObject.Update();

            for (int i = 0; i < entry.Matches.Count; i++)
            {
                string path = entry.Matches[i].Path;
                SerializedProperty property = entry.SerializedObject.FindProperty(path);
                if (property == null)
                {
                    // 배열 크기 변경 등으로 경로가 깨졌다. 캐시된 스캔 결과를 버리고 다시 훑게 한다.
                    entry.ScanSignature = null;
                    _isDirty = true;
                    continue;
                }

                DrawParentBreadcrumb(path);
                EditorGUILayout.PropertyField(property, true);
                DrawInlineReference(property);
            }

            entry.SerializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 참조 필드를 그 자리에서 펼쳐 편집한다.
        /// SO를 타고 들어갈 때마다 창을 옮겨 다니지 않아도 되도록, 검색으로 걸린 참조에만 붙인다.
        /// </summary>
        private void DrawInlineReference(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference ||
                property.hasMultipleDifferentValues)
            {
                return;
            }

            UnityEngine.Object reference = property.objectReferenceValue;
            if (reference == null || !IsInlineEditable(reference))
                return;

            int referenceId = reference.GetInstanceID();
            int key;
            unchecked
            {
                key = referenceId * 397 ^ property.propertyPath.GetHashCode();
            }

            bool isOpen = _inlineExpandedByKey.TryGetValue(key, out bool stored) && stored;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUI.indentLevel * 15f);
                bool toggled = GUILayout.Toggle(isOpen,
                    new GUIContent(isOpen ? "▼ 인라인 편집" : "▶ 인라인 편집", "참조 대상을 이 자리에서 펼쳐 편집한다."),
                    EditorStyles.miniButton, GUILayout.Width(InlineToggleWidth));

                if (toggled != isOpen)
                {
                    _inlineExpandedByKey[key] = toggled;
                    isOpen = toggled;
                }

                GUILayout.FlexibleSpace();
            }

            if (!isOpen)
                return;

            _inlineUsedIds.Add(referenceId);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (reference is Material material)
                {
                    DrawMaterialProperties(material, ResolveInlineMaterialEditor(referenceId, material), null);
                    return;
                }

                UnityEditor.Editor editor = ResolveInlineEditor(referenceId, reference);
                if (editor == null)
                {
                    EditorGUILayout.HelpBox("Editor를 생성하지 못했습니다.", MessageType.Warning);
                    return;
                }

                try
                {
                    editor.OnInspectorGUI();
                }
                catch (ExitGUIException)
                {
                    throw;
                }
                catch (System.Exception exception)
                {
                    _inlineExpandedByKey[key] = false;
                    Debug.LogException(exception);
                }
            }
        }

        /// <summary>
        /// 데이터를 담는 참조만 인라인으로 연다.
        /// GameObject는 인스펙터에 이름·Tag·Layer만 나와 얻는 게 없고, 텍스처 등은 미리보기 비용만 크다.
        /// </summary>
        private static bool IsInlineEditable(UnityEngine.Object reference)
        {
            return reference is ScriptableObject || reference is Material || reference is Component;
        }

        private UnityEditor.Editor ResolveInlineEditor(int referenceId, UnityEngine.Object reference)
        {
            if (_inlineEditors.TryGetValue(referenceId, out UnityEditor.Editor cached) &&
                cached != null && cached.target == reference)
            {
                return cached;
            }

            if (cached != null)
                UnityEngine.Object.DestroyImmediate(cached);

            UnityEditor.Editor editor = UnityEditor.Editor.CreateEditor(reference);
            _inlineEditors[referenceId] = editor;
            return editor;
        }

        private MaterialEditor ResolveInlineMaterialEditor(int referenceId, Material material)
        {
            if (_inlineEditors.TryGetValue(referenceId, out UnityEditor.Editor cached) &&
                cached is MaterialEditor materialEditor && cached.target == material)
            {
                return materialEditor;
            }

            if (cached != null)
                UnityEngine.Object.DestroyImmediate(cached);

            var created = UnityEditor.Editor.CreateEditor(material, typeof(MaterialEditor)) as MaterialEditor;
            _inlineEditors[referenceId] = created;
            return created;
        }

        /// <summary>닫힌 지 오래된 인라인 에디터를 정리한다. 재수집 시점에만 돌아 그리기 비용에는 얹히지 않는다.</summary>
        private void PruneInlineEditors()
        {
            if (_inlineEditors.Count == 0)
                return;

            var removed = new List<int>();
            foreach (KeyValuePair<int, UnityEditor.Editor> pair in _inlineEditors)
            {
                if (_inlineUsedIds.Contains(pair.Key))
                    continue;

                if (pair.Value != null)
                    UnityEngine.Object.DestroyImmediate(pair.Value);
                removed.Add(pair.Key);
            }

            for (int i = 0; i < removed.Count; i++)
                _inlineEditors.Remove(removed[i]);

            _inlineUsedIds.Clear();
        }

        private void DisposeInlineEditors()
        {
            foreach (KeyValuePair<int, UnityEditor.Editor> pair in _inlineEditors)
            {
                if (pair.Value != null)
                    UnityEngine.Object.DestroyImmediate(pair.Value);
            }

            _inlineEditors.Clear();
            _inlineUsedIds.Clear();
        }

        private void DrawMaterialBody(InspectedEntry entry)
        {
            var material = (Material)entry.Target;
            if (material.shader == null)
            {
                EditorGUILayout.HelpBox("셰이더가 없어 프로퍼티를 표시할 수 없습니다.", MessageType.Warning);
                return;
            }

            if (entry.MaterialEditor == null || entry.MaterialEditor.target != material)
            {
                if (entry.MaterialEditor != null)
                    UnityEngine.Object.DestroyImmediate(entry.MaterialEditor);

                entry.MaterialEditor =
                    UnityEditor.Editor.CreateEditor(material, typeof(MaterialEditor)) as MaterialEditor;
                entry.MaterialContext = new UnityEngine.Object[] { material };
            }

            if (entry.MaterialEditor == null)
            {
                EditorGUILayout.HelpBox("MaterialEditor를 생성하지 못했습니다.", MessageType.Warning);
                return;
            }

            DrawMaterialProperties(material, entry.MaterialEditor, IsSearching ? entry.Matches : null,
                entry.MaterialContext);
        }

        /// <summary>
        /// 머티리얼은 셰이더 프로퍼티를 평평하게 나열한다.
        /// 커스텀 ShaderGUI(lilToon 등)를 그대로 호출하면 펼치는 순간 수백 개 위젯이 매 프레임 돌아 창이 멈춘다.
        /// </summary>
        private void DrawMaterialProperties(Material material, MaterialEditor editor, List<MatchedField> filter,
            UnityEngine.Object[] context = null)
        {
            if (editor == null || material.shader == null)
                return;

            if (context == null)
            {
                _materialContextScratch[0] = material;
                context = _materialContextScratch;
            }

            EditorGUILayout.LabelField("Shader", material.shader.name, EditorStyles.miniLabel);

            MaterialProperty[] properties = MaterialEditor.GetMaterialProperties(context);

            EditorGUI.BeginChangeCheck();

            if (filter != null)
            {
                for (int i = 0; i < filter.Count; i++)
                {
                    MaterialProperty property = FindMaterialProperty(properties, filter[i].Path);
                    if (property == null)
                    {
                        // 셰이더가 바뀌어 프로퍼티가 사라졌다. 다음 프레임에 다시 훑는다.
                        _isDirty = true;
                        continue;
                    }

                    editor.ShaderProperty(property, property.displayName);
                }
            }
            else
            {
                for (int i = 0; i < properties.Length; i++)
                {
                    if ((properties[i].propertyFlags & UnityEngine.Rendering.ShaderPropertyFlags.HideInInspector) != 0)
                        continue;

                    editor.ShaderProperty(properties[i], properties[i].displayName);
                }
            }

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(material);

            editor.RenderQueueField();
        }

        private static MaterialProperty FindMaterialProperty(MaterialProperty[] properties, string name)
        {
            for (int i = 0; i < properties.Length; i++)
            {
                if (properties[i].name == name)
                    return properties[i];
            }

            return null;
        }

        /// <summary>중첩 필드는 어느 부모 아래에 있는지 보여야 값을 오해하지 않는다.</summary>
        private static void DrawParentBreadcrumb(string path)
        {
            int lastDot = path.LastIndexOf('.');
            if (lastDot < 0)
                return;

            string parent = path.Substring(0, lastDot).Replace(".Array.data", "");
            EditorGUILayout.LabelField(parent, EditorStyles.miniLabel);
        }

        /// <summary>Undo·프리팹 오버라이드 기록까지 묶어 인스펙터와 같은 편집 경험을 보장한다.</summary>
        private void ApplyChange(UnityEngine.Object target, string undoName, System.Action apply)
        {
            Undo.RecordObject(target, undoName);
            apply();

            if (PrefabUtility.IsPartOfPrefabInstance(target))
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);

            EditorUtility.SetDirty(target);
            _isDirty = true;
        }

        private void ApplyChange(UnityEngine.Object[] targets, string undoName, System.Action apply)
        {
            Undo.RecordObjects(targets, undoName);
            apply();

            foreach (UnityEngine.Object target in targets)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(target))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(target);

                EditorUtility.SetDirty(target);
            }

            _isDirty = true;
        }
    }
}
