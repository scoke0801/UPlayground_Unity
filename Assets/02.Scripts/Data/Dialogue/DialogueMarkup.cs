using System;
using System.Text;
using UnityEngine;

namespace UPlayGround.Dialogue
{
    /// <summary>
    /// 대사 본문의 커스텀 색상 마크업 <c>[c:key]...[/c]</c> 를 TMP 리치 텍스트로 변환합니다.
    ///
    /// 색을 데이터로 중앙 관리하기 위한 권장 경로이며, TMP 원시 태그(<c>&lt;color=#RRGGBB&gt;</c>)를
    /// 직접 쓴 기존 대사도 그대로 통과합니다(하위 호환).
    /// 타이핑은 문자열 누적이 아니라 maxVisibleCharacters로 처리하므로 태그가 화면에 노출되지 않습니다.
    /// </summary>
    public static class DialogueMarkup
    {
        private const string OpenPrefix = "[c:";
        private const string CloseTag = "[/c]";
        private const string TmpCloseColor = "</color>";

        /// <summary>
        /// 커스텀 마크업을 TMP 리치 텍스트로 치환합니다.
        /// 미종료 태그는 문자열 끝에서 자동으로 닫고, 짝 없는 <c>[/c]</c>는 조용히 버립니다.
        /// 미등록 키는 팔레트 기본색으로 폴백합니다(에디터에서만 경고).
        /// </summary>
        public static string ToRichText(string source, DialoguePaletteSO palette)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;

            // 마크업이 없으면 원본을 그대로 돌려준다(대부분의 기존 대사 경로).
            if (source.IndexOf(OpenPrefix, StringComparison.Ordinal) < 0 &&
                source.IndexOf(CloseTag, StringComparison.Ordinal) < 0)
                return source;

            var builder = new StringBuilder(source.Length + 32);
            int openDepth = 0;
            int index = 0;

            while (index < source.Length)
            {
                if (Matches(source, index, CloseTag))
                {
                    // 짝이 없는 닫기 태그는 버려서 </color> 과잉을 막는다.
                    if (openDepth > 0)
                    {
                        builder.Append(TmpCloseColor);
                        openDepth--;
                    }

                    index += CloseTag.Length;
                    continue;
                }

                if (Matches(source, index, OpenPrefix))
                {
                    int keyStart = index + OpenPrefix.Length;
                    int close = source.IndexOf(']', keyStart);
                    if (close > 0)
                    {
                        string key = source.Substring(keyStart, close - keyStart).Trim();
                        builder.Append("<color=#")
                               .Append(ColorUtility.ToHtmlStringRGBA(ResolveColor(palette, key)))
                               .Append('>');
                        openDepth++;

                        index = close + 1;
                        continue;
                    }

                    // ']'가 없는 깨진 태그는 일반 문자로 흘려보낸다.
                }

                builder.Append(source[index]);
                index++;
            }

            while (openDepth-- > 0)
                builder.Append(TmpCloseColor);

            return builder.ToString();
        }

        /// <summary>
        /// 커스텀 마크업과 TMP 태그를 모두 제거한 순수 본문을 반환합니다.
        /// TMP 없이 보이는 글자 수를 세거나 로그를 평문으로 다룰 때 사용합니다.
        /// </summary>
        public static string ToPlainText(string source)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;

            var builder = new StringBuilder(source.Length);
            int index = 0;

            while (index < source.Length)
            {
                if (Matches(source, index, CloseTag))
                {
                    index += CloseTag.Length;
                    continue;
                }

                if (Matches(source, index, OpenPrefix))
                {
                    int close = source.IndexOf(']', index + OpenPrefix.Length);
                    if (close > 0)
                    {
                        index = close + 1;
                        continue;
                    }
                }

                if (source[index] == '<')
                {
                    int close = source.IndexOf('>', index + 1);
                    if (close > 0)
                    {
                        index = close + 1;
                        continue;
                    }
                }

                builder.Append(source[index]);
                index++;
            }

            return builder.ToString();
        }

        /// <summary>
        /// 태그를 제외한 '보이는' 글자 수. TMP textInfo를 쓸 수 없는 곳(테스트·로직)의 보조 계산용입니다.
        /// </summary>
        public static int CountVisibleCharacters(string source) => ToPlainText(source).Length;

        private static Color ResolveColor(DialoguePaletteSO palette, string key)
        {
            if (palette == null)
                return Color.white;

            if (palette.TryGet(key, out Color color))
                return color;

#if UNITY_EDITOR
            // 런타임 로그 스팸을 피하기 위해 에디터에서만 경고한다.
            Debug.LogWarning($"[DialogueMarkup] 팔레트에 등록되지 않은 색상 키: '{key}' — 기본색으로 폴백합니다.");
#endif
            return color;
        }

        private static bool Matches(string source, int index, string token)
        {
            if (index + token.Length > source.Length)
                return false;

            for (int i = 0; i < token.Length; i++)
            {
                if (source[index + i] != token[i])
                    return false;
            }

            return true;
        }
    }
}
