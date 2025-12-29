using UnityEngine;

public class TestAnimController : MonoBehaviour
{
    private Animator _animator;

    // 트리거 이름을 배열로 관리 (인스펙터에서 수정 가능)
    // 0번 인덱스는 비워두거나 무시하고 1~9번을 사용합니다.
    [SerializeField]
    private string[] triggerNames = { "", "Trigger1", "Trigger2", "Trigger3", "Trigger4", "Trigger5", "Trigger6", "Trigger7", "Trigger8", "Trigger9" };

    void Awake()
    {
        // 동일한 오브젝트의 Animator 컴포넌트 참조
        _animator = GetComponent<Animator>();
    }

    void Update()
    {
        // 1부터 9까지의 키 입력을 체크
        for (int i = 1; i <= 9; i++)
        {
            // Alpha1은 숫자키 1을 의미합니다. 
            // KeyCode.Alpha1 + (i - 1) 방식을 통해 1~9까지 순회 가능합니다.
            if (Input.GetKeyDown(KeyCode.Alpha1 + (i - 1)))
            {
                ExecuteTrigger(i);
            }
        }
    }

    private void ExecuteTrigger(int index)
    {
        if (index < triggerNames.Length && !string.IsNullOrEmpty(triggerNames[index]))
        {
            _animator.SetTrigger(triggerNames[index]);
            Debug.Log($"[AnimTest] 키보드 {index}번 클릭 -> 트리거 '{triggerNames[index]}' 실행");
        }
    }
}
