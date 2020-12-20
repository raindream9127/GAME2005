﻿using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlayerBehaviour : MonoBehaviour
{
    public Transform bulletSpawn;
    public GameObject bullet;
    public int fireRate;


    public BulletManager bulletManager;

    [Header("Movement")]
    public float controlledAcceleration;
    public float playerSpeedLimit;
    public float playerSpeedDamping;
    public float jumpSpeed;
    public bool isGrounded;
    private Vector3 controlledMovingSpeed;

    public RigidBody3D body;
    public CubeBehaviour cube;
    public Camera playerCam;

    private enum MovingDirection:int
    {
        LEFT = 0,
        RIGHT = 1,
        FORWARD = 2,
        BACK = 3
    }
    private bool[] buttonPressed = new bool[4] { false, false, false, false };

    void Start()
    {
        controlledMovingSpeed = Vector3.zero;
    }

    // Update is called once per frame
    public void PlayerUpdate()
    {
        _Fire();
        _Move();
    }

    private void _Move()
    {
        if (cube.isGrounded)
        {
            Vector3 accelerationDirection;
            // Calculate player controlled moving speed apart from rigidbody simulation movement
            if (Input.GetAxisRaw("Horizontal") > 0.0f)
            {
                // move right
                accelerationDirection = playerCam.transform.right;
                accelerationDirection.y = 0;
                controlledMovingSpeed += accelerationDirection.normalized * controlledAcceleration * Time.deltaTime;
            }

            if (Input.GetAxisRaw("Horizontal") < 0.0f)
            {
                // move left
                accelerationDirection = -playerCam.transform.right;
                accelerationDirection.y = 0;
                controlledMovingSpeed += accelerationDirection.normalized * controlledAcceleration * Time.deltaTime;
            }

            if (Input.GetAxisRaw("Vertical") > 0.0f)
            {
                // move forward
                accelerationDirection = playerCam.transform.forward;
                accelerationDirection.y = 0;
                controlledMovingSpeed += accelerationDirection.normalized * controlledAcceleration * Time.deltaTime;
            }

            if (Input.GetAxisRaw("Vertical") < 0.0f) 
            {
                // move Back
                accelerationDirection = -playerCam.transform.forward;
                accelerationDirection.y = 0;
                controlledMovingSpeed += accelerationDirection.normalized * controlledAcceleration * Time.deltaTime;
            }

            // clamp speed
            if (controlledMovingSpeed.sqrMagnitude > playerSpeedLimit * playerSpeedLimit)  
            {
                controlledMovingSpeed = controlledMovingSpeed.normalized * playerSpeedLimit; 
            }

            // damp speed
            Vector3 dampingDeltaSpeed = controlledMovingSpeed.normalized * (-playerSpeedDamping) * Time.deltaTime;
            if (controlledMovingSpeed.sqrMagnitude < dampingDeltaSpeed.sqrMagnitude)
            {
                controlledMovingSpeed = Vector3.zero;
            }
            else
            {
                controlledMovingSpeed += dampingDeltaSpeed;
            }            

            if (Input.GetAxisRaw("Jump") > 0.0f)
            {
                body.velocity += new Vector3(0.0f, jumpSpeed, 0.0f);
                cube.isGrounded = false;
                body.isFalling = true;
            }
        }

        // update the position with controlled speed and no damping in the air
        transform.position += controlledMovingSpeed * Time.deltaTime;
    }

    private void _Fire()
    {
        if (Input.GetAxisRaw("Fire1") > 0.0f)
        {
            // delays firing
            if (Time.frameCount % fireRate == 0)
            {

                var tempBullet = bulletManager.GetBullet(bulletSpawn.position, bulletSpawn.forward);
                tempBullet.transform.SetParent(bulletManager.gameObject.transform);
            }
        }
    }
}
