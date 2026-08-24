using NUnit.Framework;
using UnityEngine;

namespace UPlayGround.Dialogue.Tests
{
    /// <summary>다이얼로그 노드 복제본의 식별자와 직렬화 값이 원본에서 분리되는지 검증한다.</summary>
    public sealed class DialogueNodeDuplicationTests
    {
        private DialogueNodeSO _source;
        private DialogueNodeSO _duplicate;

        [TearDown]
        public void TearDown()
        {
            if (_source != null)
                UnityEngine.Object.DestroyImmediate(_source);
            if (_duplicate != null)
                UnityEngine.Object.DestroyImmediate(_duplicate);
        }

        [Test]
        public void 복제한_노드는_새_ID와_독립된_직렬화값을_가진다()
        {
            _source = ScriptableObject.CreateInstance<DialogueNodeSO>();
            _source.AssignNewId();
            _source.dialogueText = "원본 대사";
            _source.editorPosition = new Vector2(10f, 20f);
            _source.choices.Add(new ChoiceData { choiceText = "원본 선택지" });

            _duplicate = UnityEngine.Object.Instantiate(_source);
            _duplicate.AssignNewId();

            Assert.AreNotSame(_source, _duplicate);
            Assert.AreNotEqual(_source.nodeId, _duplicate.nodeId);

            _duplicate.dialogueText = "복제본 대사";
            _duplicate.editorPosition = new Vector2(40f, 50f);
            _duplicate.choices[0].choiceText = "복제본 선택지";

            Assert.AreEqual("원본 대사", _source.dialogueText);
            Assert.AreEqual(new Vector2(10f, 20f), _source.editorPosition);
            Assert.AreEqual("원본 선택지", _source.choices[0].choiceText);
        }
    }
}
