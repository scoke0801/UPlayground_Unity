using Animancer;
using UnityEngine;

public class Movement2DMixer : MonoBehaviour
{
    [SerializeField] private AnimancerComponent _animancer;
    
    // 전진/정지/후진만 포함하는 1D 믹서
    [SerializeField] private LinearMixerTransition _moveMixer;

    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 100f;

    private void Start()
    {
        _animancer.Play(_moveMixer);
    }

    private void Update()
    {
        float horizontal = Input.GetAxis("Horizontal"); // 좌/우 입력
        float vertical = Input.GetAxis("Vertical");     // 전/후 입력

        // 1. 애니메이션 제어 (1D Mixer)
        // Y축 입력(Vertical)을 믹서의 파라미터로 전달 (-1: 후진, 0: 정지, 1: 전진)
        _moveMixer.State.Parameter = vertical;

        // 2. 캐릭터 회전 (좌우 입력으로 몸을 돌림)
        transform.Rotate(Vector3.up, horizontal * _rotationSpeed * Time.deltaTime);

        // 3. 실제 이동 (선택 사항: 루트 모션을 쓰지 않을 경우)
        Vector3 moveDir = transform.forward * vertical;
        transform.Translate(moveDir * _moveSpeed * Time.deltaTime, Space.World);
    }
}