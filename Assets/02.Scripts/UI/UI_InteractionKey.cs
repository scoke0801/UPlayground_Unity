using System;
using UnityEngine;

public class UI_InteractionKey : MonoBehaviour
{
    [SerializeField]private Animator _animator;

    public void AnimationChange(string animKey)
    {
        _animator.SetTrigger(animKey);
    }

    public void Deactive() => Destroy(gameObject);
}
