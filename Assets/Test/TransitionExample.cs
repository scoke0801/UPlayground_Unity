using Animancer;
using UnityEngine;

public class TransitionExample : MonoBehaviour
{
    [SerializeField] private AnimancerComponent _animancer;
    
    // ClipTransition은 애니메이션 클립과 믹싱 시간(Fade Duration)을 포함합니다.
    [SerializeField] private ClipTransition _idle;
    [SerializeField] private ClipTransition _run;

    [SerializeField] private float _duration = 0.1f;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // 0.25초(기본값) 동안 부드럽게 Idle로 전환
            _animancer.Play(_idle,_duration);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            _animancer.Play(_run,_duration);
        }
    }
}