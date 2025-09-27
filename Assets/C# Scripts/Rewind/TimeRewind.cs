
using System.Collections.Generic;
using UnityEngine;

public class TimeRewind : MonoBehaviour
{
    [Header("Rewind Settings")]
    [SerializeField] private float rewindDuration = 3f; // 回溯的总时长
    [SerializeField] private float recordInterval = 0.05f; // 记录状态的时间间隔
    [SerializeField] private float cooldownDuration = 5f; // 回溯冷却时间

    [Header("Component References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MouseLook mouseLook;
    [SerializeField] private Transform cameraTransform;

    private bool isRewinding = false;
    private List<RewindPoint> rewindPoints;
    private int maxPoints;
    private float recordTimer = 0f;
    private float cooldownTimer = 0f;
    private bool isOnCooldown = false;

    // 公共属性，供UI使用
    public bool IsOnCooldown => isOnCooldown;
    public float CooldownProgress => isOnCooldown ? 1f - (cooldownTimer / cooldownDuration) : 1f;

    private struct RewindPoint
    {
        public Vector3 position;
        public Quaternion playerRotation;
        public Quaternion cameraRotation;
        public Vector3 moveDirection;
        public Vector3 impact;
    }

    void Start()
    {
        rewindPoints = new List<RewindPoint>();
        maxPoints = Mathf.CeilToInt(rewindDuration / recordInterval);
    }

    void Update()
    {
        // 更新冷却计时器
        if (isOnCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                isOnCooldown = false;
                cooldownTimer = 0f;
            }
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!isRewinding && !isOnCooldown && rewindPoints.Count > 0)
            {
                StartRewind();
            }
        }
    }

    void FixedUpdate()
    {
        // 使用FixedUpdate进行物理状态的倒带，会更稳定
        if (isRewinding)
        {
            PerformRewind();
        }
    }

    void LateUpdate()
    {
        // 在LateUpdate中记录，以捕获所有物理和输入更新后的最终状态
        if (!isRewinding)
        {
            Record();
        }
    }

    private void Record()
    {
        // 注意：这里的计时器仍然使用Update的时间，以响应TimeScale
        recordTimer += Time.deltaTime;
        if (recordTimer >= recordInterval)
        {
            recordTimer -= recordInterval;

            if (rewindPoints.Count >= maxPoints)
            {
                rewindPoints.RemoveAt(0);
            }

            rewindPoints.Add(new RewindPoint
            {
                position = transform.position,
                playerRotation = transform.rotation,
                cameraRotation = cameraTransform.localRotation,
                moveDirection = playerMovement.moveDirection,
                impact = playerMovement.impact
            });
        }
    }

    private void StartRewind()
    {
        isRewinding = true;
        playerMovement.enabled = false;
        mouseLook.enabled = false;
    }

    private void PerformRewind()
    {
        if (rewindPoints.Count > 0)
        {
            RewindPoint point = rewindPoints[rewindPoints.Count - 1];
            transform.position = point.position;
            transform.rotation = point.playerRotation;
            cameraTransform.localRotation = point.cameraRotation;
            playerMovement.moveDirection = point.moveDirection;
            playerMovement.impact = point.impact;
            
            rewindPoints.RemoveAt(rewindPoints.Count - 1);
        }
        else
        {
            StopRewind();
        }
    }

    private void StopRewind()
    {
        isRewinding = false;
        playerMovement.enabled = true;
        mouseLook.enabled = true;
        
        // 开始冷却
        isOnCooldown = true;
        cooldownTimer = cooldownDuration;
    }
}
