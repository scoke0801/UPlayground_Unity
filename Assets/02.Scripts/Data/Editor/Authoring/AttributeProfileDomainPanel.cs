#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Stat;

namespace UPlayGround.Data.Editor.Authoring
{
    [InitializeOnLoad]
    internal static class AttributeProfileDomainRegistration
    {
        static AttributeProfileDomainRegistration()
        {
            DataAuthoringDomainRegistry.Register(
                AttributeProfileDomainPanel.DomainKey,
                "Attribute Profile",
                () => new AttributeProfileDomainPanel(),
                420);
        }
    }

    /// <summary>안정 Attribute ID 기반 기본값 Profile을 관리한다.</summary>
    public sealed class AttributeProfileDomainPanel
        : DataDomainPanel<AttributeProfileSO>
    {
        public const string DomainKey = "attribute-profiles";
        private const string DefaultPath =
            "Assets/10.Datas/Ability/Attributes/Migrated";

        public override string DomainId => DomainKey;
        public override string DisplayName => "Attribute Profile";
        public override Texture2D Icon =>
            EditorGUIUtility.IconContent("d_Profiler.CPU").image as Texture2D;
        protected override string CreateButtonLabel => "+ 새 Profile";
        protected override bool CanCreate => true;
        protected override bool CanDuplicate(AttributeProfileSO asset) =>
            asset != null;
        protected override bool CanDelete(AttributeProfileSO asset) =>
            asset != null;

        protected override IEnumerable<AttributeProfileSO> LoadAssets() =>
            AssetDatabase.FindAssets("t:AttributeProfileSO")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<AttributeProfileSO>)
                .Where(asset => asset != null)
                .OrderBy(asset => asset.name, StringComparer.CurrentCulture);

        protected override string KeyOf(AttributeProfileSO asset) =>
            asset != null ? asset.name : string.Empty;

        protected override string LabelOf(AttributeProfileSO asset) =>
            asset != null
                ? $"{asset.name}  ·  {asset.Entries.Count}개"
                : string.Empty;

        protected override void CreateNew()
        {
            AttributeProfileSO created =
                AssetCrudService.CreateAsset<AttributeProfileSO>(
                    DefaultPath,
                    "AttributeProfile_New",
                    profile =>
                    {
                        var entries = new List<AttributeProfileEntry>();
                        foreach (AttributeId id in
                                 UPlayGroundAttributeDefaults.ProfileAttributes)
                        {
                            entries.Add(new AttributeProfileEntry(
                                id,
                                UPlayGroundAttributeDefaults.Get(id)));
                        }
                        profile.EditorReplace(entries);
                    },
                    "Attribute Profile 생성");
            EditorGUIUtility.PingObject(created);
            RefreshAssets(created);
        }

        protected override AttributeProfileSO Duplicate(
            AttributeProfileSO asset)
        {
            AttributeProfileSO copy = AssetCrudService.DuplicateAsset(
                asset,
                undoName: "Attribute Profile 복제");
            EditorGUIUtility.PingObject(copy);
            return copy;
        }

        protected override bool Delete(AttributeProfileSO asset)
        {
            if (!EditorUtility.DisplayDialog(
                    "Attribute Profile 삭제",
                    $"'{asset.name}' 자산을 삭제할까요?\nActorDefinition 또는 성장 데이터의 참조를 먼저 확인하세요.",
                    "삭제",
                    "취소"))
                return false;

            return AssetCrudService.DeleteAsset(
                asset,
                "Attribute Profile 삭제");
        }

        protected override VisualElement BuildDetail(
            AttributeProfileSO asset)
        {
            var detail = new VisualElement();

            foreach (AttributeProfileEntry entry in asset.Entries)
            {
                if (entry == null)
                    continue;
                var row = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        marginBottom = 2f,
                    },
                };
                row.Add(new Label(entry.AttributeId.Value)
                {
                    style = { flexGrow = 1f },
                });
                row.Add(new Label(entry.BaseValue.ToString("0.###"))
                {
                    style = { width = 90f },
                });
                detail.Add(row);
            }

            detail.Add(new Button(() =>
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            })
            {
                text = "Inspector에서 편집",
            });
            return detail;
        }
    }
}
#endif
