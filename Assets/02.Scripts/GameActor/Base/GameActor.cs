using System;
using UnityEngine;
using UPlayGround.GameActor.MovementController;

namespace UPlayGround.GameActor.Base
{
    public abstract class GameActor<T> : MonoBehaviour where T : ActorMovementController
    {
        protected T movementController;

        protected virtual void Awake()
        {
            movementController = GetComponent<T>();
        }
    }
}