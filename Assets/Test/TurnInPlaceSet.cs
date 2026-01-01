using System;
using UnityEngine;
using Animancer;

[Serializable]
public struct TurnInPlaceSet
{
    public ClipTransition Left45;
    public ClipTransition Left90;
    public ClipTransition Left135; // 목록에 있는 경우 사용 
    public ClipTransition Right45;
    public ClipTransition Right90;
    public ClipTransition Right135; // 목록에 있는 경우 사용 
    public ClipTransition Turn180;
}