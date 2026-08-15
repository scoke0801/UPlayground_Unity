#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UPlayGround.UI.Editor
{
    /// <summary>
    /// 기존 파티 HUD 슬롯의 HP 바/글로우 스프라이트를 재사용해 협주 게이지를 구성한다.
    /// 외부 UI 팩 원본은 수정하지 않으며, 재실행해도 같은 오브젝트를 중복 생성하지 않는다.
    /// </summary>
    public static class UIHudPartyConcertoSetupTool
    {
        private const string PrefabPath =
            "Assets/03.Prefabs/UI/HUD/Party/UIHudPartyEntry.prefab";

        [UPlayGround.EditorTools.UPlaygroundTool(
            "UPlayGround/UI/HUD/파티 협주 게이지 적용")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog(
                    "파티 협주 게이지 적용",
                    "기존 파티 HUD 슬롯 프리팹에 협주 게이지와 준비 강조를 추가합니다. 계속할까요?",
                    "적용",
                    "취소"))
                return;

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null)
                throw new System.InvalidOperationException($"프리팹을 열지 못했습니다: {PrefabPath}");
            try
            {
                UIHudPartyEntry entry = root.GetComponent<UIHudPartyEntry>();
                if (entry == null)
                    throw new System.InvalidOperationException(
                        $"UIHudPartyEntry 컴포넌트를 찾지 못했습니다: {PrefabPath}");

                var serializedEntry = new SerializedObject(entry);
                serializedEntry.Update();
                var hpFill = serializedEntry.FindProperty("_hpFill")?.objectReferenceValue as Image;
                var ultimateGlow = serializedEntry.FindProperty("_glowObject")?.objectReferenceValue as GameObject;
                var spawnedObject = serializedEntry.FindProperty("_spawnedObject")?.objectReferenceValue as GameObject;
                hpFill ??= FindImageByName(root, "HpFIllBar");
                Image readySource = ultimateGlow != null
                    ? FindFirstImageWithSprite(ultimateGlow)
                    : null;
                readySource ??= spawnedObject != null
                    ? FindFirstImageWithSprite(spawnedObject)
                    : null;
                readySource ??= FindImageByName(root, "Circle_fx_1");
                readySource ??= FindFirstImageWithSprite(root);
                Image fillSource = hpFill ?? readySource;
                Sprite fillSprite = fillSource?.sprite ?? readySource?.sprite;
                if (fillSource == null || fillSprite == null || readySource?.sprite == null)
                {
                    throw new System.InvalidOperationException(
                        "기존 HP Fill 또는 준비 강조 스프라이트가 없어 같은 UI 팩으로 협주 HUD를 구성할 수 없습니다.");
                }

                RectTransform gaugeRoot = FindOrCreateImage(
                    root.transform,
                    "ConcertoGauge",
                    fillSprite,
                    new Color(0.04f, 0.08f, 0.13f, 0.9f));
                SetRect(gaugeRoot, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(44.5f, -100f), new Vector2(110f, 6f));

                RectTransform fillRect = FindOrCreateImage(
                    gaugeRoot,
                    "Fill",
                    fillSprite,
                    new Color(0.2f, 0.86f, 1f, 1f));
                SetRect(fillRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                Image concertoFill = fillRect.GetComponent<Image>();
                concertoFill.type = Image.Type.Filled;
                concertoFill.fillMethod = Image.FillMethod.Horizontal;
                concertoFill.fillOrigin = 0;
                concertoFill.fillAmount = 0f;

                RectTransform readyRect = FindOrCreateImage(
                    root.transform,
                    "ConcertoReadyGlow",
                    readySource.sprite,
                    new Color(0.15f, 0.9f, 1f, 0.95f));
                SetRect(readyRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(1.5f, 8.1f), new Vector2(118f, 118f));
                Image readyImage = readyRect.GetComponent<Image>();
                readyImage.material = readySource.material;
                readyImage.type = readySource.type;
                readyImage.preserveAspect = readySource.preserveAspect;
                readyRect.gameObject.SetActive(false);

                serializedEntry.Update();
                serializedEntry.FindProperty("_concertoFill").objectReferenceValue = concertoFill;
                serializedEntry.FindProperty("_concertoReadyObject").objectReferenceValue = readyRect.gameObject;
                serializedEntry.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[UIHudPartyConcertoSetup] 협주 HUD 적용 완료: {PrefabPath}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Image FindImageByName(GameObject root, string objectName)
        {
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.gameObject.name == objectName)
                    return image;
            }
            return null;
        }

        private static Image FindFirstImageWithSprite(GameObject root)
        {
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite != null)
                    return image;
            }
            return null;
        }

        private static RectTransform FindOrCreateImage(
            Transform parent,
            string objectName,
            Sprite sprite,
            Color color)
        {
            Transform existing = parent.Find(objectName);
            GameObject target = existing != null
                ? existing.gameObject
                : new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
            if (existing == null)
                target.transform.SetParent(parent, false);

            Image image = target.GetComponent<Image>() ?? target.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return target.GetComponent<RectTransform>();
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            rect.localScale = Vector3.one;
        }
    }
}
#endif
