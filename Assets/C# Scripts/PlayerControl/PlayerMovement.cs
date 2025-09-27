
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 7.5f;
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;
    public float maxFallSpeed = 50.0f; // 最大下落速度
    public float airControl = 2.0f; // 空中控制力

    [Header("Jump Grace Settings")]
    [Tooltip("土狼时间：允许玩家离开地面后仍能跳跃的时间窗口")]
    [SerializeField] private float coyoteTime = 0.15f;
    [Tooltip("跳跃缓冲：允许玩家在落地前提前按下跳跃键的时间窗口")]
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("Death Settings")]
    [SerializeField] private float deathYLevel = -50f; // 触发死亡的Y轴高度

    [Header("Force Settings")]
    public float impactDecay = 5.0f; // Speed at which impact force decays
    private CharacterController characterController;
    // 将这些变量设为public，以便TimeRewind脚本可以访问
    public Vector3 moveDirection = Vector3.zero;
    public Vector3 impact = Vector3.zero;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        // --- Ground Check & Coyote Time ---
        if (characterController.isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // --- Jump Input & Buffer ---
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // --- Horizontal Movement ---
        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0.0f, Input.GetAxis("Vertical"));
        input = transform.TransformDirection(input);

        if (characterController.isGrounded)
        {
            // 在地面上时，玩家输入直接决定移动方向
            moveDirection.x = input.x * speed;
            moveDirection.z = input.z * speed;
        }
        else
        {
            // 在空中时，玩家输入产生一个加速度
            moveDirection.x += input.x * airControl * Time.deltaTime;
            moveDirection.z += input.z * airControl * Time.deltaTime;

            // 限制空中水平速度不超过最大速度
            Vector3 horizontalVel = new Vector3(moveDirection.x, 0, moveDirection.z);
            if (horizontalVel.magnitude > speed)
            {
                Vector3 clampedVel = horizontalVel.normalized * speed;
                moveDirection.x = clampedVel.x;
                moveDirection.z = clampedVel.z;
            }
        }

        // --- Vertical Movement (Jump & Gravity) ---
        // 只有在地面上且没有向上速度时，才重置垂直速度，以正确处理斜坡
        if (characterController.isGrounded && moveDirection.y < 0)
        {
            moveDirection.y = -1f; // 施加一个微小的向下的力来更好地贴合斜坡
        }

        // 处理跳跃
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            moveDirection.y = jumpSpeed;
            // 消耗跳跃缓冲和土狼时间，防止连续跳跃
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
        }

        // 持续对垂直速度施加重力
        moveDirection.y -= gravity * Time.deltaTime;

        // 限制最大下落速度
        moveDirection.y = Mathf.Max(moveDirection.y, -maxFallSpeed);

        // --- Final Movement ---
        // 随时间衰减冲击力
        impact = Vector3.Lerp(impact, Vector3.zero, impactDecay * Time.deltaTime);

        // 合并玩家移动、重力和外部冲击力，得到最终的速度向量
        Vector3 finalVelocity = moveDirection + impact;

        // 用合并后的速度只调用一次Move方法
        CollisionFlags flags = characterController.Move(finalVelocity * Time.deltaTime);

        // 检查是否撞到头部
        if ((flags & CollisionFlags.CollidedAbove) != 0)
        {
            // 如果撞头，立即清除所有向上的速度，防止粘在天花板上
            if (moveDirection.y > 0)
            {
                moveDirection.y = -1f; // 取消跳跃速度
            }
            if (impact.y > 0)
            {
                impact.y = 0; // 取消冲击速度
            }
        }
        
        // --- 检查是否跌落死亡 ---
        CheckForFallDeath();
    }

    private void CheckForFallDeath()
    {
        if (transform.position.y < deathYLevel)
        {
            // 调用CheckpointManager的死亡函数
            CheckpointManager.Instance.PlayerDie();
        }
    }

    // Public method to add impact force
    public void AddImpact(Vector3 force)
    {
        impact += force;
    }

    /// <summary>
    /// 重置并清除所有当前速度（水平和垂直）
    /// </summary>
    public void ResetVelocity()
    {
        moveDirection = Vector3.zero;
        impact = Vector3.zero;
    }
}
