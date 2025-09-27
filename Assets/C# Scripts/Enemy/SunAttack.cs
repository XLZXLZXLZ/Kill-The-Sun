using System.Collections;
using UnityEngine;

/// <summary>
/// 控制太阳的攻击行为，主要是释放环形炸弹阵列。
/// </summary>
public class SunAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [Tooltip("要生成的炸弹预制体")]
    [SerializeField] private GameObject bombPrefab;

    [Tooltip("太阳的半径，炸弹将在此半径的边缘生成")]
    [SerializeField] private float sunRadius = 10f;

    [Tooltip("炸弹发射的初速度")]
    [SerializeField] private float launchSpeed = 10f;

    [Tooltip("总共释放几波炸弹")]
    [SerializeField] private int numberOfWaves = 3;

    [Tooltip("每一波炸弹之间的间隔时间（秒）")]
    [SerializeField] private float timeBetweenWaves = 0.5f;

    [Tooltip("每一波中，炸弹之间的角度间隔")]
    [SerializeField] private int angleStep = 5;

    [Header("Bomb Lifetime")]
    [Tooltip("炸弹在被自动销毁前可以存在的时间（秒）")]
    [SerializeField] private float bombLifetime = 5.0f;

    [Header("Randomization")]
    [Tooltip("每个炸弹成功生成的概率（0到1之间）")]
    [Range(0f, 1f)]
    [SerializeField] private float spawnChance = 0.9f;

    [Tooltip("炸弹飞行角度在水平方向上的最大随机偏移度数")]
    [SerializeField] private float maxAngleOffset = 2.5f;

    [Tooltip("炸弹生成位置在垂直（Y轴）方向上的最大随机偏移量")]
    [SerializeField] private float maxYOffset = 1.0f;

    [Header("Effects")]
    [Tooltip("每次攻击时在太阳中心生成的粒子效果预制体")]
    [SerializeField] private GameObject attackParticlePrefab;
    [Tooltip("技能爆发前，震屏效果提前开始的时间")]
    [SerializeField] private float preShakeDuration = 1.0f;
    [Tooltip("释放技能时的震屏强度峰值")]
    [SerializeField] private float shakeStrength = 0.3f;

    [Header("Automation")]
    [Tooltip("自动攻击的间隔时间（秒）")]
    [SerializeField] private float attackInterval = 8f;

    private bool isAttacking = false;

    private void Start()
    {
        // 游戏开始后，启动自动攻击循环
        StartCoroutine(AutoAttackLoop());
    }

    /// <summary>
    /// 自动攻击的循环协程。
    /// </summary>
    private IEnumerator AutoAttackLoop()
    {
        // 无限循环，让太阳持续攻击
        while (true)
        {
            // 等待指定的攻击间隔
            yield return new WaitForSeconds(attackInterval);

            // 如果上一轮攻击还未结束，则额外等待一下，避免攻击重叠
            yield return new WaitUntil(() => !isAttacking);

            // 开始新一轮攻击
            StartCoroutine(AttackCoroutine());
        }
    }

    /// <summary>
    /// 执行完整攻击流程的协程。
    /// </summary>
    private IEnumerator AttackCoroutine()
    {
        isAttacking = true;

        // 1. 触发震屏特效
        // 计算攻击本身所需的时间
        float attackActionDuration = numberOfWaves * timeBetweenWaves;
        // 总震动时间 = 提前震动时间 + 攻击动作时间
        float totalShakeDuration = preShakeDuration + attackActionDuration;
        CameraShaker.Instance?.Shake(totalShakeDuration, shakeStrength);

        // 2. 等待"前摇"时间，此时只有震屏效果在逐渐增强
        yield return new WaitForSeconds(preShakeDuration);

        // 3. 在震屏达到顶峰时，瞬间爆发粒子效果和第一波炸弹
        if (attackParticlePrefab != null)
        {
            Instantiate(attackParticlePrefab, transform.position, Quaternion.identity);
        }

        // 播放技能爆炸音效
        AudioManager.Instance.PlaySE("Sun_Skill_Explosion");

        // 4. 循环释放指定波数的炸弹
        for (int i = 0; i < numberOfWaves; i++)
        {
            // 环形释放一波炸弹
            SpawnBombWave();

            // 等待指定间隔时间
            yield return new WaitForSeconds(timeBetweenWaves);
        }

        isAttacking = false;
    }

    /// <summary>
    /// 生成并以环形模式发射一波炸弹。
    /// </summary>
    private void SpawnBombWave()
    {
        // 从0度到360度，按指定步长创建炸弹
        for (int angle = 0; angle < 360; angle += angleStep)
        {
            // 调整1：有一定概率取消本次生成
            if (Random.value > spawnChance)
            {
                continue;
            }

            // 调整2：对飞行角度进行随机偏移
            float randomAngleOffset = Random.Range(-maxAngleOffset, maxAngleOffset);
            float finalAngle = angle + randomAngleOffset;
            
            // 将最终角度转换为弧度
            float radian = finalAngle * Mathf.Deg2Rad;
            
            // 计算x和z方向的单位向量（在一个水平面上）
            float x = Mathf.Cos(radian);
            float z = Mathf.Sin(radian);
            
            Vector3 direction = new Vector3(x, 0, z).normalized;
            
            // 调整3：对初始坐标进行随机偏移，并从太阳边缘生成
            float randomYOffset = Random.Range(-maxYOffset, maxYOffset);
            Vector3 spawnPosition = transform.position + direction * sunRadius + new Vector3(0, randomYOffset, 0);

            // 在计算好的随机位置实例化炸弹
            GameObject bombInstance = Instantiate(bombPrefab, spawnPosition, Quaternion.identity);

            // 新增：在指定时间后销毁炸弹，避免卡顿
            Destroy(bombInstance, bombLifetime);

            // 为炸弹添加刚体（如果它没有的话），并赋予初速度
            if (bombInstance.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.velocity = direction * launchSpeed;
            }
            else
            {
                // 如果预制体上没有Rigidbody，添加一个并进行基本设置
                Rigidbody newRb = bombInstance.AddComponent<Rigidbody>();
                newRb.useGravity = false; // 假设炸弹在太空中飞行，不受重力影响
                newRb.velocity = direction * launchSpeed;
            }
        }
    }
}
