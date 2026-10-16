using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Dialogue.Tests
{
    public sealed class DialogueExpressionSessionTests
    {
        private DialogueNodeSO _node;
        private DialoguePortraitSetSO _set;
        private SpeakerPortraitTableSO _table;
        private Texture2D _texture;
        private Sprite _neutral;
        private Sprite _smile;
        private DialogueExpressionSession _session;

        [SetUp]
        public void SetUp()
        {
            _node = ScriptableObject.CreateInstance<DialogueNodeSO>();
            _set = ScriptableObject.CreateInstance<DialoguePortraitSetSO>();
            _table = ScriptableObject.CreateInstance<SpeakerPortraitTableSO>();
            _texture = new Texture2D(4, 4);
            _neutral = Sprite.Create(_texture, new Rect(0, 0, 2, 2), Vector2.zero);
            _smile = Sprite.Create(_texture, new Rect(2, 2, 2, 2), Vector2.zero);
            _set.characterType = CharacterActorType.Hwarin;
            _set.speakerIds = new[] { "화린" };
            _set.defaultPortrait = _neutral;
            _set.expressions = new[] {
                new DialoguePortraitSetSO.ExpressionFrame { expression = DialogueExpression.Neutral, portrait = _neutral },
                new DialoguePortraitSetSO.ExpressionFrame { expression = DialogueExpression.Smile, portrait = _smile }
            };
            _table.presentationSets = new[] { _set };
            _node.speakerId = "화린";
            _session = new DialogueExpressionSession();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_node);
            UnityEngine.Object.DestroyImmediate(_set);
            UnityEngine.Object.DestroyImmediate(_table);
            UnityEngine.Object.DestroyImmediate(_neutral);
            UnityEngine.Object.DestroyImmediate(_smile);
            UnityEngine.Object.DestroyImmediate(_texture);
        }

        private Sprite Resolve(Sprite legacy = null) => _session.Resolve(_node,
            CharacterActorType.Raon, CharacterActorType.Hwarin, _table, legacy).Sprite;

        [Test]
        public void LegacyAndUnmigratedSpeakerPreserveOriginalImage()
        {
            Assert.AreSame(_smile, Resolve(_smile));
            _node.expressionMode = DialogueExpressionMode.Default;
            _node.speakerId = "미이관";
            Assert.AreSame(_smile, Resolve(_smile));
        }

        [Test]
        public void KeepDefaultHiddenAndNewSessionHaveSeparateMeaning()
        {
            _node.expressionMode = DialogueExpressionMode.Set;
            _node.expression = DialogueExpression.Smile;
            Assert.AreSame(_smile, Resolve());
            _node.expressionMode = DialogueExpressionMode.Hidden;
            Assert.IsNull(Resolve(_neutral));
            _node.expressionMode = DialogueExpressionMode.Keep;
            Assert.AreSame(_smile, Resolve());
            _session.Clear();
            Assert.AreSame(_neutral, Resolve());
            _node.expressionMode = DialogueExpressionMode.Set;
            Resolve();
            _node.expressionMode = DialogueExpressionMode.Default;
            Assert.AreSame(_neutral, Resolve());
        }

        [Test]
        public void ReservedSpeakersDoNotLeakOtherCharacterExpression()
        {
            _node.expressionMode = DialogueExpressionMode.Set;
            _node.expression = DialogueExpression.Smile;
            Resolve();
            _node.expressionMode = DialogueExpressionMode.Keep;
            _node.speakerId = "Player";
            Assert.IsNull(Resolve());
            _node.speakerId = "Protagonist";
            Assert.AreSame(_smile, Resolve());
        }

        [Test]
        public void MissingExpressionFallsBackToNeutralThenLegacyThenHidden()
        {
            _node.expressionMode = DialogueExpressionMode.Set;
            _node.expression = DialogueExpression.Worried;
            Assert.AreSame(_neutral, Resolve());
            _set.expressions = System.Array.Empty<DialoguePortraitSetSO.ExpressionFrame>();
            _set.defaultPortrait = null;
            Assert.AreSame(_smile, Resolve(_smile));
            Assert.IsNull(Resolve());
        }

        [Test]
        public void ExistingPortraitValidatorReportsCurrentAssets()
        {
            var type = System.Type.GetType("UPlayGround.Dialogue.Editor.DialoguePortraitValidator, UPlayGround.Data.Editor");
            Assert.NotNull(type);
            type.GetMethod("Validate").Invoke(null, null);
        }

        [Test]
        public void DialogueManagerRegistersBothFaceRestoreAndApplyTicks()
        {
            var manager = System.Type.GetType("UPlayGround.Dialogue.DialogueManager, Assembly-CSharp");
            Assert.NotNull(manager);
            var contracts = manager.GetInterfaces();
            Assert.That(contracts, Has.Some.Matches<System.Type>(type => type.Name == "IUpdatableManager"));
            Assert.That(contracts, Has.Some.Matches<System.Type>(type => type.Name == "ILateUpdatableManager"),
                "LateUpdate 등록이 없으면 실제 게임에서 머리·눈 시선과 깜빡임이 실행되지 않는다.");
        }

        [Test]
        public void ReviewGraphIsPlayableAndContainsNoProgressActions()
        {
            var graph = AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(
                "Assets/10.Datas/Dialogue/Presentation/Hwarin/DLG_HwarinPresentationReview.asset");
            Assert.NotNull(graph);
            Assert.IsTrue(graph.TryValidatePlayback(out string error), error);
            Assert.AreEqual(17, graph.nodes.Count);
            foreach (var node in graph.nodes)
                foreach (var action in node.eventActions)
                    Assert.AreEqual("ShowAnimatedDialogueIllustrationActionSO", action.GetType().Name);
        }
    }
}
