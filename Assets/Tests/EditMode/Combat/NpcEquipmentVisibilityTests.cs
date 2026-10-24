using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Combat.Tests
{
    /// <summary>플레이어 외형을 복제한 NPC의 장비 노출 회귀를 검사한다.</summary>
    public sealed class NpcEquipmentVisibilityTests
    {
        [TestCase(CharacterActorType.Raon, ActorType.NPC, false)]
        [TestCase(CharacterActorType.Reine, ActorType.NPC, false)]
        [TestCase(CharacterActorType.None, ActorType.NPC, true)]
        [TestCase(CharacterActorType.Raon, ActorType.NPC | ActorType.Combat, true)]
        public void PlayerModelEquipmentIsHiddenOnlyForNonCombatCharacterNpcs(
            CharacterActorType characterType, ActorType actorType, bool equipmentVisible)
        {
            var root = new GameObject("NpcEquipmentTest");
            root.SetActive(false);
            try
            {
                var actor = root.AddComponent<NpcActor>();
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(GameActor).GetField("_characterActorType", flags).SetValue(actor, characterType);
                typeof(GameActor).GetField("_actorType", flags).SetValue(actor, actorType);

                var weapon = CreateChild(root.transform, "Weapon");
                var backWeapon = CreateChild(root.transform, "Weapon_back");
                var hand = CreateChild(root.transform, "Hand_R");
                var tools = CreateChild(hand.transform, "InteractionObject");
                var body = CreateChild(root.transform, "Body");
                var routineProp = CreateChild(hand.transform, "RoutineProp");
                var inactiveModel = CreateChild(root.transform, "InactiveModel");
                inactiveModel.SetActive(false);
                var inactiveWeapon = CreateChild(inactiveModel.transform, "Weapon");

                var hideEquipment = typeof(NpcActor).GetMethod("HidePlayerModelEquipment", flags);
                Assert.NotNull(hideEquipment);
                hideEquipment.Invoke(actor, null);
                hideEquipment.Invoke(actor, null);

                Assert.AreEqual(equipmentVisible, weapon.activeSelf);
                Assert.AreEqual(equipmentVisible, backWeapon.activeSelf);
                Assert.AreEqual(equipmentVisible, tools.activeSelf);
                Assert.AreEqual(equipmentVisible, inactiveWeapon.activeSelf);
                Assert.IsTrue(body.activeSelf, "몸체는 표시 상태를 유지해야 한다.");
                Assert.IsTrue(hand.activeSelf, "손 본은 비활성화하면 안 된다.");
                Assert.IsTrue(routineProp.activeSelf, "NPC 일과 소품은 장비 정리 대상이 아니다.");
                Assert.IsFalse(inactiveModel.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }
    }
}
