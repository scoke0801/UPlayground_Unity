using System.Collections;
using System.Collections.Generic;
using Game.FSM;
using UnityEngine;
using UnityEngine.Events;
using KinematicCharacterController.Examples;

public class UPTeleporter : MonoBehaviour
{
    public UPTeleporter TeleportTo;

    public UnityAction<PlayerCharacterController> OnCharacterTeleport;

    public bool isBeingTeleportedTo { get; set; }

    private void OnTriggerEnter(Collider other)
    {
        if (!isBeingTeleportedTo)
        {
            PlayerCharacterController pc = other.GetComponent<PlayerCharacterController>();
            if (pc)
            {
                pc.Motor.SetPositionAndRotation(TeleportTo.transform.position, TeleportTo.transform.rotation);

                if (OnCharacterTeleport != null)
                {
                    OnCharacterTeleport(pc);
                }
                TeleportTo.isBeingTeleportedTo = true;
            }
        }

        isBeingTeleportedTo = false;
    }
}
