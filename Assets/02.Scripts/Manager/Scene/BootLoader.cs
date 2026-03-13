using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Manager;
using UPlayGround.Enum;

/// <summary>
/// Boot 씬 전용 스크립트.
/// GameManager 초기화 완료 + 필수 DB 로딩 완료를 확인한 뒤 Title 씬으로 전환.
/// </summary>
public class BootLoader : MonoBehaviour
{
    [Header("로딩 UI (선택)")]
    [SerializeField] private Slider _progressBar;
    [SerializeField] private float _minimumLoadingTime = 1f; // 최소 로딩 연출 시간(초)

    private void Start()
    {
        StartCoroutine(CoWaitAndLoad());
    }

    private IEnumerator CoWaitAndLoad()
    {
        float elapsed = 0f;

        // GameManager가 씬에 없으면 Instance 접근으로 자동 생성됨
        // (BaseManager<T>의 싱글톤 보장)
        var gameManager = GameManager.Instance;

        while (true)
        {
            elapsed += Time.deltaTime;

            bool allReady = CheckAllManagersReady(out float progress);

            _progressBar?.SetValueWithoutNotify(progress);

            if (allReady && elapsed >= _minimumLoadingTime)
                break;

            yield return null;
        }

        _progressBar?.SetValueWithoutNotify(1f);

        SceneManager.Instance.LoadSceneDirect(SceneName.Title);
    }

    /// <summary>
    /// 비동기 로딩이 필요한 매니저들의 완료 여부를 확인.
    /// 새 DB가 추가되면 여기에만 추가하면 됨.
    /// </summary>
    private bool CheckAllManagersReady(out float progress)
    {
        // (완료여부, 가중치) — 가중치는 체감 진행률 조절용
        (bool ready, float weight)[] checks =
        {
            (AssetManager.Instance.IsLoaded,          1f),
            (ItemManager.Instance.IsItemDBLoaded,     1f),
            (UIManager.Instance.IsInitialized,        1f),
        };

        float totalWeight  = 0f;
        float doneWeight   = 0f;

        foreach (var (ready, weight) in checks)
        {
            totalWeight += weight;
            if (ready) doneWeight += weight;
        }

        progress = totalWeight > 0f ? doneWeight / totalWeight : 0f;

        return doneWeight >= totalWeight;
    }
}
