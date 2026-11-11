using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Pet;
using UPlayGround.Manager;

namespace UPlayGround.UI.Tests
{
    public sealed class PetAccompanyActionTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void 함께하기는_외형_적용_성공_때만_동행을_켠다(bool canApply)
        {
            var holder = new GameObject("PetApplyTest"); holder.SetActive(false);
            try
            {
                var catalog = AssetDatabase.LoadAssetAtPath<PetCatalogSO>("Assets/10.Datas/Pet/PetCatalog.asset");
                var service = new PetServiceStub(catalog, canApply);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/UI/Scene/Pet/UI_Scene_PetMenu.prefab");
                var instance = UnityEngine.Object.Instantiate(prefab, holder.transform);
                var menu = instance.GetComponent<UI_Scene_PetMenu>();
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(UI_Scene_PetMenu).GetField("_pets", flags).SetValue(menu, service);
                typeof(UI_Scene_PetMenu).GetField("_previewAppearanceId", flags).SetValue(menu, service.GetSnapshot().owned[0].appearanceId);
                menu.Initialize();
                instance.transform.Find("V2ModelPage/Apply").GetComponent<Button>().onClick.Invoke();
                Assert.That(service.GetSnapshot().isAccompanying, Is.EqualTo(canApply));
                Assert.That(service.AccompanyRequests, Is.EqualTo(canApply ? 1 : 0));
            }
            finally { UnityEngine.Object.DestroyImmediate(holder); }
        }

        private sealed class PetServiceStub : IPetService
        {
            private readonly PetSaveData _save;
            private readonly bool _canApply;
            public int AccompanyRequests { get; private set; }
            public PetServiceStub(PetCatalogSO catalog, bool canApply)
            {
                Catalog = catalog; _canApply = canApply;
                var definition = catalog.definitions[0];
                _save = PetSaveRules.Normalize(null);
                _save.selectedPetId = definition.petId; _save.modelId = definition.modelId;
                _save.isAccompanying = false;
                var owned = new PetOwnedEntry { petId = definition.petId, appearanceId = definition.appearances[0].appearanceId };
                foreach (var appearance in definition.appearances) owned.unlockedAppearances.Add(appearance.appearanceId);
                _save.owned.Add(owned);
            }
            public event Action Changed { add { } remove { } }
            public PetCatalogSO Catalog { get; }
            public PetRuntimeState State => PetRuntimeState.Unavailable;
            public string FailureReason => null;
            public bool HasUnsavedChanges => false;
            public float PickupInterval => 0;
            public float PickupRadius => 0;
            public float LootLeash => 0;
            public bool IsAutoPickupEnabled => true;
            public PetSaveData GetSnapshot() => PetSaveRules.Normalize(_save);
            public bool CanEdit(out string reason) { reason = null; return true; }
            public bool TrySetModel(string modelId) => _canApply;
            public bool TrySetAppearance(string appearanceId) => _canApply;
            public void SetAccompanying(bool accompanying) { AccompanyRequests++; _save.isAccompanying = accompanying; }
            public int GetSkillLevel(string id) => 1;
            public bool TrySelect(string id) => false;
            public bool TrySetSupport(string id) => false;
            public bool TrySetAutoPickup(bool enabled) => false;
            public PetUpgradeResult TryUpgrade(PetUpgradeRequest request) => PetUpgradeResult.Rejected;
            public bool RetrySave() => false;
            public IDisposable Suspend(object owner) => null;
        }
    }
}
