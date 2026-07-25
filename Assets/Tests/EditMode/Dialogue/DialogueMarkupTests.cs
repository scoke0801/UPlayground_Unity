using NUnit.Framework;
using UnityEngine;
using UPlayGround.Dialogue;

namespace UPlayGround.Dialogue.Tests
{
    /// <summary>
    /// 인라인 색상 마크업 파싱 회귀 테스트.
    /// 가장 흔한 결함(태그 문자가 화면에 노출됨)은 타이핑을 maxVisibleCharacters로 처리하는 것과
    /// 여기서 검증하는 태그 짝 맞춤이 함께 성립해야 막을 수 있다.
    /// </summary>
    public class DialogueMarkupTests
    {
        private DialoguePaletteSO _palette;

        [SetUp]
        public void SetUp()
        {
            _palette = ScriptableObject.CreateInstance<DialoguePaletteSO>();

            // entries는 private 직렬화 필드이므로 SerializedObject 대신 JSON 덮어쓰기로 주입한다.
            JsonUtility.FromJsonOverwrite(
                "{\"defaultColor\":{\"r\":1,\"g\":1,\"b\":1,\"a\":1}," +
                "\"entries\":[{\"key\":\"emphasis\",\"color\":{\"r\":1,\"g\":0.5,\"b\":0,\"a\":1}}," +
                "{\"key\":\"danger\",\"color\":{\"r\":1,\"g\":0,\"b\":0,\"a\":1}}]}",
                _palette);
        }

        [TearDown]
        public void TearDown()
        {
            if (_palette != null)
                Object.DestroyImmediate(_palette);
        }

        [Test]
        public void 마크업이_없으면_원본을_그대로_반환한다()
        {
            const string source = "우리 용병단은 총 4명이다!";
            Assert.AreEqual(source, DialogueMarkup.ToRichText(source, _palette));
        }

        [Test]
        public void 등록된_키는_TMP_color_태그로_치환된다()
        {
            string result = DialogueMarkup.ToRichText("총 [c:emphasis]4명[/c]이다!", _palette);

            StringAssert.StartsWith("총 <color=#", result);
            StringAssert.EndsWith("</color>이다!", result);
            StringAssert.Contains("4명", result);
            Assert.AreEqual(1, CountOccurrences(result, "</color>"));
        }

        [Test]
        public void 미등록_키는_기본색으로_폴백하고_태그_짝은_유지된다()
        {
            // 미등록 키라도 태그를 열고 닫아야 이후 본문 색이 새지 않는다.
            string result = DialogueMarkup.ToRichText("[c:unknown]모름[/c] 뒤", _palette);

            Assert.AreEqual(1, CountOccurrences(result, "<color=#"));
            Assert.AreEqual(1, CountOccurrences(result, "</color>"));
            StringAssert.EndsWith("</color> 뒤", result);
        }

        [Test]
        public void 중첩_태그는_각각_닫힌다()
        {
            string result = DialogueMarkup.ToRichText("[c:emphasis]가[c:danger]나[/c]다[/c]", _palette);

            Assert.AreEqual(2, CountOccurrences(result, "<color=#"));
            Assert.AreEqual(2, CountOccurrences(result, "</color>"));
        }

        [Test]
        public void 미종료_태그는_문자열_끝에서_자동으로_닫힌다()
        {
            string result = DialogueMarkup.ToRichText("[c:emphasis]끝까지", _palette);

            Assert.AreEqual(1, CountOccurrences(result, "<color=#"));
            StringAssert.EndsWith("끝까지</color>", result);
        }

        [Test]
        public void 짝없는_닫기_태그는_버려진다()
        {
            // </color>가 과잉으로 남으면 TMP가 이후 텍스트를 깨뜨린다.
            string result = DialogueMarkup.ToRichText("본문[/c] 뒤", _palette);

            Assert.AreEqual(0, CountOccurrences(result, "</color>"));
            Assert.AreEqual("본문 뒤", result);
        }

        [Test]
        public void 닫는_괄호가_없는_깨진_태그는_일반_문자로_통과한다()
        {
            const string source = "[c:emphasis 괄호없음";
            Assert.AreEqual(source, DialogueMarkup.ToRichText(source, _palette));
        }

        [Test]
        public void TMP_원시_태그는_그대로_보존된다()
        {
            const string source = "총 <color=#FF8800>4명</color>이다!";
            Assert.AreEqual(source, DialogueMarkup.ToRichText(source, _palette));
        }

        [Test]
        public void 팔레트가_null이면_흰색으로_폴백하고_예외를_던지지_않는다()
        {
            string result = DialogueMarkup.ToRichText("[c:emphasis]가[/c]", null);

            Assert.AreEqual(1, CountOccurrences(result, "<color=#"));
            Assert.AreEqual(1, CountOccurrences(result, "</color>"));
        }

        [Test]
        public void ToPlainText는_커스텀_마크업과_TMP_태그를_모두_제거한다()
        {
            Assert.AreEqual("총 4명이다!",
                DialogueMarkup.ToPlainText("총 [c:emphasis]<b>4명</b>[/c]이다!"));
        }

        [Test]
        public void CountVisibleCharacters는_태그를_제외한_글자만_센다()
        {
            Assert.AreEqual(2, DialogueMarkup.CountVisibleCharacters("[c:emphasis]4명[/c]"));
        }

        private static int CountOccurrences(string source, string token)
        {
            int count = 0;
            int index = 0;

            while ((index = source.IndexOf(token, index, System.StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += token.Length;
            }

            return count;
        }
    }
}
