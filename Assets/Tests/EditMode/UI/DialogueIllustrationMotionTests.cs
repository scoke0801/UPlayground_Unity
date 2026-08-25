using System;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.UI;

namespace UPlayGround.UI.Tests
{
    /// <summary>삽화 연출 프리셋이 어떤 저작 경로에서도 정지 화면을 만들지 않는지 검증한다.</summary>
    public sealed class DialogueIllustrationMotionTests
    {
        [Test]
        public void 모든_프리셋은_재생_길이를_가진다()
        {
            foreach (DialogueIllustrationMotion motion in
                     (DialogueIllustrationMotion[])Enum.GetValues(typeof(DialogueIllustrationMotion)))
            {
                DialogueIllustrationMotionValues values =
                    DialogueIllustrationMotionLibrary.Resolve(motion);
                Assert.That(values.HasMotion, Is.True, $"{motion} 프리셋에 연출이 없습니다.");
            }
        }

        [Test]
        public void 프리셋마다_이동이나_확대_중_하나는_변한다()
        {
            foreach (DialogueIllustrationMotion motion in
                     (DialogueIllustrationMotion[])Enum.GetValues(typeof(DialogueIllustrationMotion)))
            {
                DialogueIllustrationMotionValues values =
                    DialogueIllustrationMotionLibrary.Resolve(motion);
                bool moves = values.StartOffset != values.EndOffset;
                bool scales = !Mathf.Approximately(values.StartScale, values.EndScale);
                Assert.That(moves || scales, Is.True, $"{motion} 프리셋이 화면에서 움직이지 않습니다.");
            }
        }

        [Test]
        public void 재생_길이가_비어_있는_직접_입력은_기본_프리셋으로_대체된다()
        {
            DialogueIllustrationMotionValues fallback =
                DialogueIllustrationMotionLibrary.ResolveCustom(
                    Vector2.zero,
                    Vector2.zero,
                    1f,
                    1f,
                    0f,
                    DialogueIllustrationEase.Linear);
            DialogueIllustrationMotionValues expected = DialogueIllustrationMotionLibrary.Resolve(
                DialogueIllustrationMotionLibrary.DefaultMotion);

            Assert.That(fallback.Duration, Is.EqualTo(expected.Duration));
            Assert.That(fallback.EndScale, Is.EqualTo(expected.EndScale));
        }

        [Test]
        public void 직접_입력한_수치는_그대로_전달된다()
        {
            DialogueIllustrationPresentation presentation = DialogueIllustrationMotionLibrary
                .Resolve(
                    DialogueIllustrationMotion.Custom,
                    new Vector2(10f, 0f),
                    new Vector2(-10f, 0f),
                    1.02f,
                    1.08f,
                    9.6f,
                    DialogueIllustrationEase.Linear)
                .ToPresentation(revealImmediately: true);

            Assert.That(presentation.Duration, Is.EqualTo(9.6f));
            Assert.That(presentation.MotionEase, Is.EqualTo(DialogueIllustrationEase.Linear));
            Assert.That(presentation.RevealImmediately, Is.True);
        }
    }
}
