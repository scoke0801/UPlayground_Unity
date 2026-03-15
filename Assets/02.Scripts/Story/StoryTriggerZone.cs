using UnityEngine;
using UPlayGround.Story;

/// <summary>
/// Collider(Is Trigger)를 부착하고 StoryEntrySO를 연결하면,
/// 플레이어가 진입할 때 StoryManager에 트리거 요청을 보냅니다.
/// </summary>
[RequireComponent(typeof(CapsuleCollider))]
public class StoryTriggerZone : MonoBehaviour
{
    [SerializeField] private StoryEntrySO _storyEntry;
    [SerializeField] private string _playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(_playerTag)) return;
        StoryManager.Instance.TryTriggerStory(_storyEntry);
    }
}
