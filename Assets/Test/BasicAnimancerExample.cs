using Animancer;
using UnityEngine;

public class BasicAnimancerExample : MonoBehaviour
{
    // 1. AnimancerComponent를 인스펙터에서 할당하거나 GetComponent로 가져옵니다.
    [SerializeField] private AnimancerComponent _animancer;
    
    // 2. 재생할 애니메이션 클립을 할당합니다.
    [SerializeField] private AnimationClip _idleClip;

    private void Update()
    {
        // 스페이스바를 누르면 애니메이션 재생
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _animancer.Play(_idleClip);
        }
    }
}