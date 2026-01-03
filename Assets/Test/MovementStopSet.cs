using System;
using UnityEngine;
using Animancer;

[Serializable]
public struct MovementStopSet
{
    public ClipTransition WalkStop;   // Walk_F_To_Idle 
    public ClipTransition RunStop;    // Run_F_To_Idle [cite: 35]
    public ClipTransition SprintStop; // Sprint_F_To_Idle [cite: 42]
    public ClipTransition CrouchStop; // Crouch_F_To_Idle 
}