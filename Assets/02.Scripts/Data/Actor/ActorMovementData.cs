using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ActorMovementData", menuName = "UP/ActorData/ActorMovementData")]
public class ActorMovementData : ScriptableObject
{
    public float SprintSpeed = 12f;
    public float RunSpeed = 9f;
    public float WalkSpeed = 5f;
    
    public float OrientationSharpness = 10f;
    public float AccelerationSharpness = 7.5f; // 낮을수록 천천히 가속
    public float DecelerationSharpness = 15f; // 높을수록 빨리 멈춤
    
    public Vector3 Gravity = new Vector3(0, -30f, 0);
    public float Drag = 2f;
    public float AirDrag = 0.1f;
}