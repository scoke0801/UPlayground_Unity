#if UNITY_EDITOR
using UnityEngine;

namespace UPlayGround.Animation
{
    /// <summary>
    /// 애니메이션 에디터(MotionSetEditorWindow) 프리뷰 잠금이 켜져 있는 동안 게임뷰 상단에
    /// 항상 보이는 배너를 그린다. 잠금은 에디터 창이 숨겨져 있어도 유지되므로, 창을 잊은 채
    /// "플레이어가 왜 안 움직이지?" 하는 혼란을 막기 위한 가시화 장치다.
    ///
    /// 에디터 툴이 <see cref="Show"/>로 on/off 한다. 인게임 로직에서는 절대 켜지지 않는다.
    /// 런타임 어셈블리에 두는 이유: OnGUI는 플레이 중 게임뷰에 그려져야 하고, 에디터 전용
    /// 어셈블리는 게임뷰에 직접 그릴 수 없기 때문(MotionSetEventDebugOverlay와 동일한 이유).
    /// </summary>
    public class MotionPreviewLockBanner : MonoBehaviour
    {
        private static MotionPreviewLockBanner _instance;
        private static bool _active;
        private static bool _unlockRequested;
        private static string _message = "모션 프리뷰 — 플레이어 입력 잠금 중";

        private GUIStyle _labelStyle;
        private Texture2D _bgTex;

        /// <summary>
        /// 배너의 "해제" 버튼이 눌렸는지 1회성으로 가져온다. 에디터 툴이 매 프레임 폴링해
        /// 잠금 토글을 끄는 데 쓴다. 인게임 로직과 무관(에디터 전용).
        /// </summary>
        public static bool ConsumeUnlockRequest()
        {
            if (!_unlockRequested)
                return false;
            _unlockRequested = false;
            return true;
        }

        /// <summary>
        /// 잠금 배너 표시 on/off. 매 프레임 호출돼도 안전(멱등).
        /// </summary>
        public static void Show(bool active, string message = null)
        {
            _active = active;
            if (!string.IsNullOrEmpty(message))
                _message = message;

            if (active)
                EnsureInstance();
            else if (_instance != null)
            {
                // 잠금 해제 시 즉시 정리. 플레이모드 종료로 이미 파괴됐으면 Unity-null 가드로 건너뜀.
                Destroy(_instance.gameObject);
                _instance = null;
            }
        }

        private static void EnsureInstance()
        {
            if (_instance != null) // Unity-null 체크: 플레이모드 종료로 파괴되면 재생성
                return;

            var go = new GameObject("[MotionPreviewLockBanner]")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _instance = go.AddComponent<MotionPreviewLockBanner>();
        }

        private void OnGUI()
        {
            if (!_active)
                return;

            if (_labelStyle == null)
            {
                _bgTex = new Texture2D(1, 1);
                _bgTex.SetPixel(0, 0, new Color(0.78f, 0.16f, 0.16f, 0.92f));
                _bgTex.Apply();

                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
            }

            var content = new GUIContent(_message);
            Vector2 textSize = _labelStyle.CalcSize(content);
            const float buttonW = 56f;
            const float pad = 14f;
            const float gap = 10f;
            float w = textSize.x + buttonW + gap + pad * 2f;
            float h = Mathf.Max(textSize.y, 20f) + 12f;
            var rect = new Rect((Screen.width - w) * 0.5f, 10f, w, h);

            GUI.DrawTexture(rect, _bgTex);

            GUILayout.BeginArea(rect);
            GUILayout.BeginHorizontal();
            GUILayout.Space(pad);
            GUILayout.Label(content, _labelStyle, GUILayout.Height(h - 12f));
            GUILayout.Space(gap);
            if (GUILayout.Button("해제", GUILayout.Width(buttonW), GUILayout.Height(h - 14f)))
                _unlockRequested = true;
            GUILayout.Space(pad);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            if (_bgTex != null)
                Destroy(_bgTex);
            if (_instance == this)
                _instance = null;
        }
    }
}
#endif
