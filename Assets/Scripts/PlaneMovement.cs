using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class PlaneMovement : MonoBehaviour
{
    [Header("Speed Settings")]
    public float forwardSpeed = 6f; 
    private float currentSpeed;

    [Header("FOV Settings")]
    public CinemachineVirtualCamera vCam; // Drag your Virtual Camera here
    public float boostFOV = 60f;
    public float normalFOV = 40f;
    public float brakeFOV = 30f;
    public float fovLerpSpeed = 4f; // How fast the zoom happens
    private float targetFOV;

    [Header("Rail References")]
    public CinemachineDollyCart cart;

    [Header("VFX")]
    public ParticleSystem speedStreaks;
    public ParticleSystem engineTrail; 

    void Start()
    {
        currentSpeed = forwardSpeed;
        targetFOV = normalFOV;

        if (cart == null) cart = GetComponentInParent<CinemachineDollyCart>();
        
        UpdateVFX(-60f, 40, 1.0f);
    }

    void Update()
    {
        // --- THE HOLD LOGIC ---
        if (Input.GetKey(KeyCode.LeftShift)) 
        {
            // BOOST
            currentSpeed = forwardSpeed * 2f;
            targetFOV = boostFOV;
            UpdateVFX(-150f, 100, 2.0f);
        }
        else if (Input.GetKey(KeyCode.S)) 
        {
            // BRAKE
            currentSpeed = forwardSpeed / 2f;
            targetFOV = brakeFOV;
            UpdateVFX(-10f, 10, 0.2f);
        }
        else
        {
            // NORMAL
            currentSpeed = forwardSpeed;
            targetFOV = normalFOV;
            UpdateVFX(-60f, 40, 1.0f);
        }

        // 1. Apply Speed to Cart (with Lerp for smoothness)
        if (cart != null)
        {
            cart.m_Speed = Mathf.Lerp(cart.m_Speed, currentSpeed, Time.deltaTime * 4f);
        }

        // 2. Apply FOV to Camera (with Lerp for smoothness)
        if (vCam != null)
        {
            vCam.m_Lens.FieldOfView = Mathf.Lerp(vCam.m_Lens.FieldOfView, targetFOV, Time.deltaTime * fovLerpSpeed);
        }
    }

    void UpdateVFX(float streakSpeed, float streakEmission, float trailScale)
    {
        if (speedStreaks != null)
        {
            var main = speedStreaks.main;
            main.startSpeed = streakSpeed;
            var emission = speedStreaks.emission;
            emission.rateOverTime = streakEmission;
        }

        if (engineTrail != null)
        {
            engineTrail.transform.localScale = new Vector3(trailScale, trailScale, trailScale);
        }
    }
}