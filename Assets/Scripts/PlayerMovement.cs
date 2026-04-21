using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine; 
using UnityEngine.Rendering.PostProcessing; // Required for Post Processing

public class PlayerMovement : MonoBehaviour
{
    [Header("Visuals")]
    public Transform playerModel; 

    [Header("Settings")]
    public bool joystick = true;

    [Header("Parameters")]
    public float xySpeed = 18;
    public float lookSpeed = 340;
    public float aimDistance = 10; 

    [Header("Speed Logic (Hold to Boost)")]
    public CinemachineDollyCart cart; 
    public float forwardSpeed = 6f;
    private float currentSpeed;

    [Header("Post Processing Settings")]
    private PostProcessVolume postVolume;
    private LensDistortion lensDistortion;
    private ChromaticAberration chromaticAberration;
    public float lerpTime = 4f;

    [Header("Barrel Roll Settings")]
    public float rollDuration = 0.5f;
    public float rollDistance = 20f;
    private bool isRolling = false;

    [Header("Public References")]
    public Transform aimTarget;
    public Vector2 limits;

    [Header("Particles & Effects")]
    public TrailRenderer[] engineTrails; 
    public ParticleSystem circle; 
    public ParticleSystem barrel; 
    public ParticleSystem stars;  

    private float h, v;

    void Start()
    {
        if(playerModel == null && transform.childCount > 0) 
            playerModel = transform.GetChild(0);

        currentSpeed = forwardSpeed;
        if (cart == null) cart = GetComponentInParent<CinemachineDollyCart>();

        // Find and Cache Post Processing Settings from Main Camera
        postVolume = Camera.main.GetComponent<PostProcessVolume>();
        if (postVolume != null)
        {
            postVolume.profile.TryGetSettings(out lensDistortion);
            postVolume.profile.TryGetSettings(out chromaticAberration);
        }

        SetTrailsEmitting(true);
        if (stars != null) stars.Stop();
        if (circle != null) circle.Stop();
    }

    void Update()
    {
        h = joystick ? Input.GetAxis("Horizontal") : Input.GetAxis("Mouse X");
        v = joystick ? Input.GetAxis("Vertical") : Input.GetAxis("Mouse Y");
        
        if (!isRolling)
        {
            LocalMove(h, v, xySpeed);
            UpdateAimTarget(h, v);

            if (playerModel != null)
                HorizontalLean(playerModel, h, 80, .1f);
        }

        if (Input.GetKeyDown(KeyCode.R) && !isRolling)
        {
            StartCoroutine(ExecuteBarrelRoll(h, v));
        }

        HandleInputParticlesAndSpeed();
        ClampPosition();
    }

    IEnumerator ExecuteBarrelRoll(float xDir, float yDir)
    {
        isRolling = true;
        if (barrel != null) barrel.Play();

        float elapsed = 0;
        float rollDirX = (xDir != 0) ? xDir : 1; 
        float spinDirection = (rollDirX >= 0) ? -1 : 1;

        Quaternion initialModelRotation = playerModel.localRotation;

        while (elapsed < rollDuration)
        {
            elapsed += Time.deltaTime;
            Vector3 moveDir = new Vector3(xDir, yDir, 0).normalized;
            transform.localPosition += moveDir * rollDistance * Time.deltaTime;

            float degreesPerFrame = (360f / rollDuration) * spinDirection * Time.deltaTime;
            playerModel.Rotate(Vector3.right, degreesPerFrame, Space.Self);

            yield return null;
        }

        playerModel.localRotation = initialModelRotation;
        isRolling = false;
    }

    void LocalMove(float x, float y, float speed)
    {
        transform.localPosition += new Vector3(x, y, 0) * speed * Time.deltaTime;
        float clampedX = Mathf.Clamp(transform.localPosition.x, -limits.x, limits.x);
        float clampedY = Mathf.Clamp(transform.localPosition.y, -limits.y, limits.y);
        transform.localPosition = new Vector3(clampedX, clampedY, 0);
    }

    void UpdateAimTarget(float hInput, float vInput)
    {
        if (joystick)
        {
            Vector3 targetPos = new Vector3(hInput, vInput, aimDistance);
            aimTarget.localPosition = Vector3.Lerp(aimTarget.localPosition, targetPos, Time.deltaTime * 5f);
        }
        else
        {
            aimTarget.localPosition += new Vector3(hInput, vInput, 0) * (lookSpeed * 0.05f) * Time.deltaTime;
            aimTarget.localPosition = new Vector3(Mathf.Clamp(aimTarget.localPosition.x, -limits.x, limits.x), Mathf.Clamp(aimTarget.localPosition.y, -limits.y, limits.y), aimDistance);
        }
    }

    void HorizontalLean(Transform target, float axis, float leanLimit, float lerpTime)
    {
        Vector3 rot = target.localEulerAngles;
        float targetZ = -axis * leanLimit;
        float z = Mathf.LerpAngle(rot.z, targetZ, lerpTime);
        target.localEulerAngles = new Vector3(rot.x, rot.y, z);
    }

    void HandleInputParticlesAndSpeed()
    {
        float targetDistortion = 0f;
        float targetChromatic = 0f;

        // --- BOOST (HOLD SHIFT) ---
        if (Input.GetKeyDown(KeyCode.LeftShift)) 
        {
            circle?.Play(); 
        }

        if (Input.GetKey(KeyCode.LeftShift)) 
        {
            currentSpeed = forwardSpeed * 2f;
            targetDistortion = -30f; 
            targetChromatic = 1f;   
            if (stars != null && !stars.isPlaying) stars.Play();
        }
        else if (Input.GetKey(KeyCode.S)) 
        {
            currentSpeed = forwardSpeed / 2f;
            targetDistortion = 0f;
            targetChromatic = 0f;
            if (stars != null) stars.Stop();
        }
        else 
        {
            currentSpeed = forwardSpeed;
            targetDistortion = 0f;
            targetChromatic = 0f;
            if (stars != null) stars.Stop();
        }

        // Apply Speed to Cart
        if (cart != null)
        {
            cart.m_Speed = Mathf.Lerp(cart.m_Speed, currentSpeed, Time.deltaTime * lerpTime);
        }

        // Apply Post Processing Lerp
        if (lensDistortion != null)
            lensDistortion.intensity.value = Mathf.Lerp(lensDistortion.intensity.value, targetDistortion, Time.deltaTime * lerpTime);
        
        if (chromaticAberration != null)
            chromaticAberration.intensity.value = Mathf.Lerp(chromaticAberration.intensity.value, targetChromatic, Time.deltaTime * lerpTime);

        // --- BRAKE VFX ---
        if (Input.GetKeyDown(KeyCode.S)) 
        {
            barrel?.Play(); 
        }
    }

    void SetTrailsEmitting(bool state)
    {
        if (engineTrails == null) return;
        foreach (TrailRenderer tr in engineTrails)
        {
            if (tr != null) tr.emitting = state;
        }
    }

    void LateUpdate()
    {
        Vector3 localPos = transform.localPosition;
        transform.localPosition = new Vector3(Mathf.Clamp(localPos.x, -limits.x, limits.x), Mathf.Clamp(localPos.y, -limits.y, limits.y), localPos.z);
    }

    void ClampPosition()
    {
        if (Camera.main == null) return;
        Vector3 pos = Camera.main.WorldToViewportPoint(transform.position);
        pos.x = Mathf.Clamp01(pos.x);
        pos.y = Mathf.Clamp01(pos.y);
        transform.position = Camera.main.ViewportToWorldPoint(pos);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Vector3 linePos = transform.position;
        Gizmos.DrawLine(new Vector3(-limits.x, -limits.y, linePos.z), new Vector3(limits.x, -limits.y, linePos.z));
        Gizmos.DrawLine(new Vector3(-limits.x, limits.y, linePos.z), new Vector3(limits.x, limits.y, linePos.z));
        Gizmos.DrawLine(new Vector3(-limits.x, -limits.y, linePos.z), new Vector3(-limits.x, limits.y, linePos.z));
        Gizmos.DrawLine(new Vector3(limits.x, -limits.y, linePos.z), new Vector3(limits.x, limits.y, linePos.z));
        if (aimTarget != null) Gizmos.DrawWireSphere(aimTarget.position, 0.3f);
    }
}