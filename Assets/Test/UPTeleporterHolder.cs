using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using KinematicCharacterController.Examples;
using System;
using Game.FSM;


public class UPTeleporterHolder : MonoBehaviour, IMoverController
{
    public PhysicsMover PlanetMover;
    public SphereCollider GravityField;
    public float GravityStrength = 10;
    public Vector3 OrbitAxis = Vector3.forward;
    public float OrbitSpeed = 10;

    public UPTeleporter OnPlaygroundTeleportingZone;
    public UPTeleporter OnPlanetTeleportingZone;

    private List<PlayerCharacterController> _characterControllersOnPlanet = new List<PlayerCharacterController>();
    private Vector3 _savedGravity;
    private Quaternion _lastRotation;

    private void Start()
    {
        OnPlaygroundTeleportingZone.OnCharacterTeleport -= ControlGravity;
        OnPlaygroundTeleportingZone.OnCharacterTeleport += ControlGravity;

        OnPlanetTeleportingZone.OnCharacterTeleport -= UnControlGravity;
        OnPlanetTeleportingZone.OnCharacterTeleport += UnControlGravity;

        _lastRotation = PlanetMover.transform.rotation;

        PlanetMover.MoverController = this;
    }

    public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime)
    {
        goalPosition = PlanetMover.Rigidbody.position;

        // Rotate
        Quaternion targetRotation = Quaternion.Euler(OrbitAxis * OrbitSpeed * deltaTime) * _lastRotation;
        goalRotation = targetRotation;
        _lastRotation = targetRotation;

        // Apply gravity to characters
        foreach (PlayerCharacterController cc in _characterControllersOnPlanet)
        {
            cc.Brain.MovementData.Gravity = (PlanetMover.transform.position - cc.transform.position).normalized * GravityStrength;
        }
    }

    void ControlGravity(PlayerCharacterController cc)
    {
        _savedGravity = cc.Brain.MovementData.Gravity;
        _characterControllersOnPlanet.Add(cc);
    }

    void UnControlGravity(PlayerCharacterController cc)
    {
        cc.Brain.MovementData.Gravity = _savedGravity;
        _characterControllersOnPlanet.Remove(cc);
    }
}
