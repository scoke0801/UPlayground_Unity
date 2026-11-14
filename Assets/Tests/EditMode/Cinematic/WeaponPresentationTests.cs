using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Cinematic.Tests
{
    public sealed class WeaponPresentationTests
    {
        private WeaponPresentationProfileSO _profile;
        private UltimateSequenceAsset _asset;

        [SetUp]
        public void SetUp()
        {
            _profile = ScriptableObject.CreateInstance<WeaponPresentationProfileSO>();
            _profile.weaponType = WeaponType.Sword;
            for (int i = 0; i < 5; i++)
                _profile.beats.Add(new WeaponPresentationBeat { beat = (PresentationBeat)i, normalizedTime = i * 0.2f });
            _profile.beats[0].eventTemplates.Add(new UltimateFaceStripEvent());
            _asset = ScriptableObject.CreateInstance<UltimateSequenceAsset>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_asset);
            UnityEngine.Object.DestroyImmediate(_profile);
        }

        private static MethodInfo Generator(string name)
        {
            Type type = Type.GetType("UPlayGround.Data.Editor.WeaponPresentationGenerator, Assembly-CSharp-Editor");
            Assert.That(type, Is.Not.Null, "기존 궁극기 에디터 어셈블리를 로드해야 합니다.");
            return type.GetMethod(name, BindingFlags.Public | BindingFlags.Static);
        }

        private void Apply() => Generator("Apply").Invoke(null, new object[] { _asset, _profile, 4f });

        [Test]
        public void 다음_Undo_범위의_복구가_이전_저작을_되돌리지_않는다()
        {
            Apply();
            var unrelated = ScriptableObject.CreateInstance<UltimateSequenceAsset>();
            int nextGroup = Undo.GetCurrentGroup();
            try
            {
                Undo.RecordObject(unrelated, "다음 작업");
                unrelated.timelineUseUnscaledTime = true;
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(nextGroup);
                Assert.That(_asset.weaponProfile, Is.SameAs(_profile));
                Assert.That(_asset.events.Count, Is.EqualTo(1));
            }
            finally { UnityEngine.Object.DestroyImmediate(unrelated); }
        }

        [Test]
        public void 재적용은_수작업과_전투이벤트를_그대로_보존한다()
        {
            var damage = new UltimateDamageWindowEvent { startTime = 1.2f, duration = 0.3f };
            var slow = new UltimateTimeScaleEvent { startTime = 1f, duration = 0.5f, timeScale = 0.2f };
            var callback = new UltimateCustomCallbackEvent { callbackName = "기존콜백" };
            _asset.events.Add(damage); _asset.events.Add(slow); _asset.events.Add(callback);
            Apply(); Apply();
            Assert.That(_asset.events.Count, Is.EqualTo(4));
            Assert.That(_asset.events[0], Is.SameAs(damage));
            Assert.That(_asset.events[1], Is.SameAs(slow));
            Assert.That(_asset.events[2], Is.SameAs(callback));
            Assert.That(damage.startTime, Is.EqualTo(1.2f));
            Assert.That(slow.timeScale, Is.EqualTo(0.2f));
            Assert.That(_asset.timelineUseUnscaledTime, Is.False);
        }

        [Test]
        public void 실패한_생성은_목록과_프로필과_ID를_바꾸지_않는다()
        {
            Apply();
            List<UltimateTimelineEvent> previous = _asset.events;
            string generation = _asset.generatedPresentationId;
            _profile.beats[1].eventTemplates.Add(new UltimateDamageWindowEvent());
            Assert.Throws<TargetInvocationException>(Apply);
            Assert.That(_asset.events, Is.SameAs(previous));
            Assert.That(_asset.generatedPresentationId, Is.EqualTo(generation));
            Assert.That(_asset.weaponProfile, Is.SameAs(_profile));
        }

        [Test]
        public void 비트누락과_중복과_비정상시간을_거절한다()
        {
            _profile.beats[4].beat = PresentationBeat.Finish;
            Assert.That(_profile.IsValid(out _), Is.False);
            _profile.beats[4].beat = PresentationBeat.Recovery;
            _profile.beats[4].normalizedTime = float.NaN;
            Assert.That(_profile.IsValid(out _), Is.False);
            _profile.beats.RemoveAt(4);
            Assert.That(_profile.IsValid(out _), Is.False);
        }

        [Test]
        public void 생성은_원본과_에셋참조를_분리해_수정할_수_있다()
        {
            var template = (UltimateFaceStripEvent)_profile.beats[0].eventTemplates[0];
            Apply();
            var generated = (UltimateFaceStripEvent)_asset.events[0];
            Assert.That(generated, Is.Not.SameAs(template));
            generated.hold = 0.5f;
            Assert.That(template.hold, Is.EqualTo(1.4f));
            Assert.That(generated.useAccentOverride, Is.True);
        }

        [Test]
        public void 수작업_보존으로_해제한_생성이벤트는_재적용에서도_유지된다()
        {
            Apply();
            UltimateTimelineEvent retained = _asset.events[0];
            retained.presentationGenerationId = null;
            Apply();
            Assert.That(_asset.events.Count, Is.EqualTo(2));
            Assert.That(_asset.events[0], Is.SameAs(retained));
        }

        [TestCase(WeaponType.Sword)]
        [TestCase(WeaponType.GreatSword)]
        public void 수직슬라이스는_실제모션을_바꾸지_않고_생성된다(WeaponType weapon)
        {
            var profile = AssetDatabase.LoadAssetAtPath<WeaponPresentationProfileSO>(
                $"Assets/10.Datas/Combat/WeaponPresentation/WeaponPresentation_{weapon}.asset");
            Assert.That(profile, Is.Not.Null);
            var result = (List<UltimateTimelineEvent>)Generator("Build").Invoke(null, new object[] { profile, 4f });
            Assert.That(result.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(result.TrueForAll(WeaponPresentationProfileSO.IsPresentationEvent), Is.True);
        }
    }

    public sealed class WeaponPresentationCatalogTests
    {
        [Test]
        public void 십종_무기는_중복없이_유효한_프로필과_생성결과를_갖는다()
        {
            string[] guids = AssetDatabase.FindAssets("t:WeaponPresentationProfileSO",
                new[] { "Assets/10.Datas/Combat/WeaponPresentation" });
            Assert.That(guids.Length, Is.EqualTo(10));
            var weapons = new HashSet<WeaponType>();
            Type generator = Type.GetType("UPlayGround.Data.Editor.WeaponPresentationGenerator, Assembly-CSharp-Editor");
            foreach (string guid in guids)
            {
                var profile = AssetDatabase.LoadAssetAtPath<WeaponPresentationProfileSO>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.That(weapons.Add(profile.weaponType), Is.True);
                Assert.That(profile.IsValid(out string error), Is.True, error);
                Assert.That(UnityEditor.SerializationUtility.HasManagedReferencesWithMissingTypes(profile), Is.False);
                var events = (List<UltimateTimelineEvent>)generator.GetMethod("Build").Invoke(null, new object[] { profile, 4f });
                Assert.That(events.Count, Is.GreaterThanOrEqualTo(2));
            }
        }
    }

    public sealed class WeaponPresentationBindingsTests
    {
        [TestCase("Raon")]
        [TestCase("Hwarin")]
        public void 보유_궁극기는_정의의_얼굴과_같은_무기_프로필에_연결된다(string name)
        {
            var definition = AssetDatabase.LoadAssetAtPath<global::UPlayGround.Data.Party.PlayerCharacterDefinitionSO>(
                $"Assets/10.Datas/Party/PlayerCharacters/PlayerCharacterDefinition_{name}.asset");
            var sequence = AssetDatabase.LoadAssetAtPath<UltimateSequenceAsset>(
                $"Assets/10.Datas/Actor/Ultimate/UltimateSequence_{name}.asset");
            Assert.That(definition.ultimateFaceSprite, Is.Not.Null);
            Assert.That(sequence.weaponProfile, Is.Not.Null);
            Assert.That(sequence.weaponProfile.weaponType, Is.EqualTo(definition.defaultWeaponType));
            Assert.That(UnityEditor.SerializationUtility.HasManagedReferencesWithMissingTypes(sequence), Is.False);
            Assert.That(sequence.events.Exists(item => item is UltimateFaceStripEvent), Is.True);
            string spriteGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(definition.ultimateFaceSprite));
            Assert.That(System.IO.File.ReadAllText(AssetDatabase.GetAssetPath(definition)), Does.Contain(spriteGuid));
            Assert.That(System.IO.File.ReadAllText(AssetDatabase.GetAssetPath(sequence)),
                Does.Contain(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sequence.weaponProfile))));
        }

        [Test]
        public void 저장된_UI_DB가_실제_얼굴_프리팹을_참조한다()
        {
            var database = AssetDatabase.LoadAssetAtPath<global::UPlayGround.Data.Path.UIPrefabDatabase>(
                "Assets/10.Datas/Path/UIPrefabDatabase.asset");
            var serialized = new SerializedObject(database);
            SerializedProperty entries = serialized.FindProperty("prefabs");
            int found = 0;
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty item = entries.GetArrayElementAtIndex(i);
                if (item.FindPropertyRelative("key").stringValue != "FaceStrip") continue;
                found++;
                var prefab = item.FindPropertyRelative("prefab").objectReferenceValue as GameObject;
                Assert.That(prefab, Is.Not.Null);
                Assert.That(AssetDatabase.GetAssetPath(prefab), Is.EqualTo("Assets/03.Prefabs/UI/Common/UIFaceStrip.prefab"));
            }
            Assert.That(found, Is.EqualTo(1));
            Assert.That(System.IO.File.ReadAllText(AssetDatabase.GetAssetPath(database)), Does.Contain("FaceStrip"));
        }
    }
}
