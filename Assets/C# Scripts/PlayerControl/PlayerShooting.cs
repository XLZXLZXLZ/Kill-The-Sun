
using UnityEngine;
using DG.Tweening; // 确保你已经从 Asset Store 导入了 DOTween
using System.Collections; // 为了使用协程 (Coroutine)

public class PlayerShooting : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Transform muzzlePoint; // 激光的发射点
    public Transform bombMuzzlePoint; // 炸弹的发射点
    public GameObject laserPrefab; // 激光的预制体 (需要包含一个 LineRenderer 组件)
    public GameObject muzzleFlashPrefab; // 激光的枪口火焰特效
    public GameObject bombMuzzleFlashPrefab; // 炸弹的枪口火焰特效
    public GameObject impactEffectPrefab; // 命中点的特效
    public GameObject bombProjectilePrefab; // 发射的炸弹预制体
    public Animator crosshairAnimator; // 准星的动画控制器

    [Header("Shooting Parameters")]
    [SerializeField] private float raycastLength = 100f; // 射线的最大长度
    [SerializeField] private float fireRate = 0.5f; // 射击冷却时间（秒）
    [SerializeField] private float bombRecoilForce = 5f; // 发射炸弹时的后坐力

    [Header("Bomb Locking Settings")]
    [SerializeField] private GameObject lockOnUIPrefab; // 锁定UI的预制体 (UGUI)
    [SerializeField] private Canvas lockOnCanvas; // 锁定UI所在的画布
    [SerializeField] private float lockOnDistance = 50f; // 可锁定的最远物理距离
    [SerializeField] private float lockOnScreenRadius = 200f; // 屏幕中心可吸附的像素半径
    [SerializeField] private float lockOnUISmoothTime = 0.1f; // 锁定UI的平滑时间

    [Header("Bullet Time Settings")]
    [SerializeField] [Range(0.1f, 0.9f)] private float bulletTimeScale = 0.5f;
    [SerializeField] private float zoomAmount = 10f; // 视角放大的量 (减少的FOV值)
    [SerializeField] private float zoomDuration = 0.2f; // 缩放动画的时长

    [Header("Laser FX Settings")]
    [SerializeField] private float laserAnimDuration = 0.25f; // 激光动画总时长
    [SerializeField] private float laserMaxWidth = 0.5f; // 激光放大的最大宽度

    private float originalTimeScale;
    private float originalFixedDeltaTime;
    private float originalFOV;
    private bool isAiming = false;
    private bool isAimingBomb = false; // 是否正在用右键瞄准炸弹
    private float nextFireTime = 0f; // 用于计算下一次可以开火的时间
    private Bomb currentLockedBomb; // 当前锁定的炸弹
    private GameObject lockOnUIInstance; // 实例化的锁定UI
    private BombProjectile activeProjectile; // 已发射、等待引爆的炸弹
    private Vector3 uiLockOnVelocity = Vector3.zero; // 用于UI平滑移动

    void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        originalFOV = playerCamera.fieldOfView;
        originalTimeScale = Time.timeScale;
        originalFixedDeltaTime = Time.fixedDeltaTime;

        // 实例化锁定UI并隐藏
        if (lockOnUIPrefab != null && lockOnCanvas != null)
        {
            lockOnUIInstance = Instantiate(lockOnUIPrefab, lockOnCanvas.transform);
            lockOnUIInstance.SetActive(false);
        }
    }

    void Update()
    {
        HandleInput();

        if (isAiming)
        {
            HandleBombLocking();
        }
        else
        {
            ClearLock();
        }
    }
    
    void LateUpdate()
    {
        UpdateLockOnUI();
    }

    private void HandleInput()
    {
        // 增加冷却时间判断
        if (Input.GetMouseButtonDown(0) && Time.time >= nextFireTime)
        {
            StartAiming();
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (isAiming)
            {
                Shoot();
            }
        }

        // --- 右键输入逻辑 ---
        // 按下右键
        if (Input.GetMouseButtonDown(1))
        {
            // 如果已有激活的炸弹，则引爆它
            if (activeProjectile != null)
            {
                activeProjectile.Detonate();
                activeProjectile = null;
            }
            // 否则，如果弹匣有足够弹药，则开始瞄准
            else if (Magazine.Instance.CanShoot())
            {
                isAimingBomb = true;
                // 进入子弹时间
                Time.timeScale = bulletTimeScale;
                Time.fixedDeltaTime = originalFixedDeltaTime * Time.timeScale;
                // 放大FOV
                playerCamera.DOFieldOfView(originalFOV - zoomAmount, zoomDuration);
                // 播放准星动画
                if (crosshairAnimator != null) crosshairAnimator.Play("AimIn");
            }
        }

        // 松开右键 (仅在瞄准时触发发射)
        if (Input.GetMouseButtonUp(1))
        {
            if (isAimingBomb)
            {
                isAimingBomb = false;
                // 恢复正常时间
                Time.timeScale = originalTimeScale;
                Time.fixedDeltaTime = originalFixedDeltaTime;
                // 恢复原始视角
                playerCamera.DOFieldOfView(originalFOV, zoomDuration);
                // 播放准星动画
                if (crosshairAnimator != null) crosshairAnimator.Play("AimOut");
                
                ShootBomb();
            }
        }
    }

    private void StartAiming()
    {
        isAiming = true;
        
        // 进入子弹时间
        Time.timeScale = bulletTimeScale;
        Time.fixedDeltaTime = originalFixedDeltaTime * Time.timeScale; // 保证物理计算同步
        
        // 视角放大 (通过减小Field of View)
        playerCamera.DOFieldOfView(originalFOV - zoomAmount, zoomDuration);

        // 播放准星动画
        if (crosshairAnimator != null) crosshairAnimator.Play("AimIn");
    }

    private void Shoot()
    {
        nextFireTime = Time.time + fireRate;
        isAiming = false;
        
        // 恢复时间与视角
        Time.timeScale = originalTimeScale;
        Time.fixedDeltaTime = originalFixedDeltaTime;
        playerCamera.DOFieldOfView(originalFOV, zoomDuration);

        // 播放准星动画
        if (crosshairAnimator != null) crosshairAnimator.Play("AimOut");

        Vector3 targetPoint;
        Bomb targetBomb = null;

        // 优先处理锁定的目标
        if (currentLockedBomb != null)
        {
            targetBomb = currentLockedBomb;
            targetPoint = targetBomb.transform.position;
        }
        else
        {
            // 如果没有锁定目标，则执行射线检测
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, raycastLength))
            {
                targetPoint = hit.point;
                // 检查射线是否也击中了炸弹
                targetBomb = hit.collider.GetComponent<Bomb>();
            }
            else
            {
                targetPoint = ray.GetPoint(raycastLength);
            }
        }

        // 如果最终目标是炸弹，则引爆它
        if (targetBomb != null)
        {
            targetBomb.OnHitByLaser();
        }

        // 在命中点生成特效
        if (impactEffectPrefab != null)
        {
            // 计算从命中点指向枪口的旋转
            Quaternion impactRotation = Quaternion.LookRotation(muzzlePoint.position - targetPoint);
            Instantiate(impactEffectPrefab, targetPoint, impactRotation);
        }

        // --- 调用射击视觉效果 ---
        StartCoroutine(PlayLaserEffectCoroutine(targetPoint));
        
        // 播放激光发射音效
        AudioManager.Instance.PlaySE("Laser_Shoot");
        
        // 射击后清除锁定
        ClearLock();
    }

    private void ShootBomb()
    {
        if (bombProjectilePrefab == null)
        {
            Debug.LogError("Bomb Projectile Prefab 未设置!");
            return;
        }

        // 使用炸弹专用的发射点，如果没有设置则回退到激光发射点
        Transform bombMuzzle = bombMuzzlePoint != null ? bombMuzzlePoint : muzzlePoint;

        // 尝试消耗弹药
        if (Magazine.Instance.UseCharge())
        {
            // 播放炸弹发射音效
            AudioManager.Instance.PlaySE("Bomb_Launch");
            
            // 实例化炸弹
            GameObject bombGO = Instantiate(bombProjectilePrefab, bombMuzzle.position, bombMuzzle.rotation);
            activeProjectile = bombGO.GetComponent<BombProjectile>();
            
            // --- 后坐力与特效 ---
            PlayerMovement playerMovement = GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                // 1. 施加后坐力前清空当前速度，使其更具冲击感
                playerMovement.ResetVelocity();
                // 2. 施加向后的冲击力
                playerMovement.AddImpact(-bombMuzzle.forward * bombRecoilForce);
            }

            // 3. 在炸弹枪口生成开火特效
            if (bombMuzzleFlashPrefab != null)
            {
                Instantiate(bombMuzzleFlashPrefab, bombMuzzle.position, bombMuzzle.rotation);
            }
        }
    }

    private void HandleBombLocking()
    {
        float minScreenDist = lockOnScreenRadius;
        Bomb bestTarget = null;

        // 查找场景中所有的炸弹
        Bomb[] allBombs = FindObjectsOfType<Bomb>();

        foreach (var bomb in allBombs)
        {
            // 0. 检查炸弹是否已爆炸
            if (bomb.isExploded)
            {
                continue;
            }
            
            // 1. 检查物理距离
            float distance = Vector3.Distance(transform.position, bomb.transform.position);
            if (distance > lockOnDistance)
            {
                continue;
            }

            // 2. 检查屏幕距离
            Vector3 screenPoint = playerCamera.WorldToScreenPoint(bomb.transform.position);
            // 如果在摄像机后面，则忽略
            if (screenPoint.z < 0)
            {
                continue;
            }
            
            float screenDist = Vector2.Distance(new Vector2(screenPoint.x, screenPoint.y), new Vector2(Screen.width / 2f, Screen.height / 2f));

            if (screenDist < lockOnScreenRadius)
            {
                if (screenDist < minScreenDist)
                {
                    minScreenDist = screenDist;
                    bestTarget = bomb;
                }
            }
        }
        currentLockedBomb = bestTarget;
    }

    private void ClearLock()
    {
        currentLockedBomb = null;
    }

    private void UpdateLockOnUI()
    {
        if (lockOnUIInstance == null) return;

        if (currentLockedBomb != null && currentLockedBomb.gameObject.activeInHierarchy)
        {
            Vector3 targetScreenPos = playerCamera.WorldToScreenPoint(currentLockedBomb.transform.position);

            // 检查目标是否在摄像机前方
            if (targetScreenPos.z > 0)
            {
                // 如果UI正要从隐藏变为显示，先将它瞬移到目标初始位置，避免飞过屏幕
                if (!lockOnUIInstance.activeSelf)
                {
                    if (lockOnCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        Vector2 localPoint;
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(lockOnCanvas.GetComponent<RectTransform>(), targetScreenPos, null, out localPoint);
                        lockOnUIInstance.transform.localPosition = localPoint;
                    }
                    else
                    {
                        Vector3 worldPoint = playerCamera.ScreenToWorldPoint(new Vector3(targetScreenPos.x, targetScreenPos.y, lockOnCanvas.planeDistance));
                        lockOnUIInstance.transform.position = worldPoint;
                    }
                    uiLockOnVelocity = Vector3.zero; // 重置速度
                }
                
                lockOnUIInstance.SetActive(true);

                // 使用SmoothDamp平滑更新UI位置
                if (lockOnCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    Vector2 targetLocalPos;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(lockOnCanvas.GetComponent<RectTransform>(), targetScreenPos, null, out targetLocalPos);
                    lockOnUIInstance.transform.localPosition = Vector3.SmoothDamp(lockOnUIInstance.transform.localPosition, targetLocalPos, ref uiLockOnVelocity, lockOnUISmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
                }
                else // This covers Screen Space - Camera
                {
                    Vector3 targetWorldPos = playerCamera.ScreenToWorldPoint(new Vector3(targetScreenPos.x, targetScreenPos.y, lockOnCanvas.planeDistance));
                    lockOnUIInstance.transform.position = Vector3.SmoothDamp(lockOnUIInstance.transform.position, targetWorldPos, ref uiLockOnVelocity, lockOnUISmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
                    lockOnUIInstance.transform.rotation = lockOnCanvas.transform.rotation;
                }
            }
            else
            {
                lockOnUIInstance.SetActive(false);
            }
        }
        else
        {
            if (lockOnUIInstance.activeSelf)
            {
                lockOnUIInstance.SetActive(false);
            }
        }
    }

    private IEnumerator PlayLaserEffectCoroutine(Vector3 target)
    {
        GameObject muzzleFlash = null;
        // --- 生成枪口火焰 ---
        if (muzzleFlashPrefab != null)
        {
            // 确保特效生成时Z轴与枪口Z轴方向一致
            muzzleFlash = Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);
        }

        if (laserPrefab == null || muzzlePoint == null)
        {
            Debug.LogError("Laser Prefab 或 Muzzle Point 未设置！");
            yield break;
        }

        // --- 激光逻辑 ---
        GameObject laserInstance = Instantiate(laserPrefab, muzzlePoint.position, Quaternion.identity);
        LineRenderer lineRenderer = laserInstance.GetComponent<LineRenderer>();

        // 动画过程计时器
        float timer = 0f;

        // 使用 DOTween 创建宽度动画
        float expandTime = laserAnimDuration / 3f;
        float shrinkTime = laserAnimDuration * 2f / 3f;
        DOTween.To(() => 0f, width => {
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
        }, laserMaxWidth, expandTime)
        .SetEase(Ease.OutQuad)
        .OnComplete(() => {
            DOTween.To(() => lineRenderer.startWidth, width => {
                lineRenderer.startWidth = width;
                lineRenderer.endWidth = width;
            }, 0f, shrinkTime).SetEase(Ease.InQuad);
        });

        // 在动画持续时间内，持续更新激光起点位置
        while (timer < laserAnimDuration)
        {
            if (lineRenderer != null)
            {
                lineRenderer.SetPosition(0, muzzlePoint.position);
                lineRenderer.SetPosition(1, target);
            }

            if (muzzleFlash != null)
            {
                muzzleFlash.transform.position = muzzlePoint.position;
                muzzleFlash.transform.rotation = muzzlePoint.rotation;
            }

            timer += Time.deltaTime;
            yield return null; // 等待下一帧
        }

        // 动画结束后销毁激光
        if (laserInstance != null)
        {
            Destroy(laserInstance);
        }
    }
}
