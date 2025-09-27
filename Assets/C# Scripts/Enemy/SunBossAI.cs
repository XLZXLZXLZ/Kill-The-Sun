using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// 控制太阳Boss在战斗阶段的核心AI行为。
/// </summary>
[RequireComponent(typeof(Collider))]
public class SunBossAI : MonoBehaviour
{
    [Header("Core Stats")]
    [Tooltip("太阳Boss的总生命值")]
    [SerializeField] private int maxHealth = 10;

    [Header("Controller Reference")]
    [Tooltip("对总战斗流程控制器的引用，用于在首次受伤时触发开场")]
    [SerializeField] private SunBossFightController fightController;
    
    [Header("Regular Attack")]
    [Tooltip("用于常规攻击的炸弹预制体")]
    [SerializeField] private GameObject bombPrefab;
    [Tooltip("自动攻击的间隔时间（秒）")]
    [SerializeField] private float attackInterval = 6f;
    // (这里可以从SunAttack脚本中复制所有关于炸弹生成、随机化和发射的参数)
    [SerializeField] private float launchSpeed = 10f;
    [SerializeField] private int numberOfWaves = 3;
    [SerializeField] private float timeBetweenWaves = 0.5f;
    [SerializeField] private int angleStep = 10;
    [SerializeField] private float bombLifetime = 5.0f;
    [Range(0f, 1f)][SerializeField] private float spawnChance = 0.9f;
    [SerializeField] private float maxAngleOffset = 5f;
    [SerializeField] private float maxYOffset = 1.0f;
    [SerializeField] private float sunRadius = 10f;


    [Header("Damage Feedback Effects")]
    [Tooltip("被玩家炸弹击中时生成的粒子效果")]
    [SerializeField] private GameObject hitParticlePrefab;
    [Tooltip("被击中时的冲击震屏时长")]
    [SerializeField] private float hitShakeDuration = 0.4f;
    [Tooltip("被击中时的冲击震屏强度峰值")]
    [SerializeField] private float hitShakeStrength = 0.8f;

    [Header("Death Sequence")]
    [Tooltip("太阳死亡时播放的粒子效果，会周期性生成")]
    [SerializeField] private GameObject deathParticlePrefab;
    [Tooltip("死亡粒子生成的间隔时间")]
    [SerializeField] private float deathParticleInterval = 1.0f;
    [Tooltip("死亡时，渐强震屏的总时长")]
    [SerializeField] private float deathShakeDuration = 5.0f;
    [Tooltip("死亡震屏在结束时的最大强度")]
    [SerializeField] private float deathShakeMaxStrength = 2.0f;
    [Tooltip("死亡序列期间，游戏的时间缩放")]
    [Range(0.01f, 1f)]
    [SerializeField] private float deathTimeScale = 0.2f;
    [Tooltip("瞬间黑屏后，在加载主菜单前等待的真实秒数")]
    [SerializeField] private float blackScreenDuration = 2.0f;
    [Tooltip("要加载的主菜单场景名称")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";


    private int currentHealth;
    private bool isAttacking = false;
    private bool fightHasStarted = false; // 新增状态，追踪开场事件是否已被触发

    private void Awake()
    {
        // 确保碰撞体是触发器，以便检测炸弹射弹
        GetComponent<Collider>().isTrigger = true;
    }

    private void Start()
    {
        currentHealth = maxHealth;
        // Boss战开始后（可以由Controller激活此组件来触发），启动攻击循环
        StartCoroutine(AutoAttackLoop());
    }

    private IEnumerator AutoAttackLoop()
    {
        while (currentHealth > 0)
        {
            yield return new WaitForSeconds(attackInterval);
            yield return new WaitUntil(() => !isAttacking);
            StartCoroutine(AttackCoroutine());
        }
    }

    private IEnumerator AttackCoroutine()
    {
        isAttacking = true;
        for (int i = 0; i < numberOfWaves; i++)
        {
            SpawnBombWave();
            yield return new WaitForSeconds(timeBetweenWaves);
        }
        isAttacking = false;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // 检查是否是被玩家的炸弹射弹击中
        if (other.TryGetComponent<BombProjectile>(out BombProjectile bombProjectile))
        {
            // 立即引爆射弹
            bombProjectile.Detonate();
            
            // 处理受伤反馈
            TakeDamage();
        }
    }

    private void TakeDamage()
    {
        // 首次受伤时，触发开场事件
        if (!fightHasStarted)
        {
            fightHasStarted = true;
            if (fightController != null)
            {
                fightController.StartFightSequence();
            }
            else
            {
                Debug.LogError("SunBossAI: 战斗控制器未设置，无法触发开场爆炸！");
            }
        }

        currentHealth--;
        Debug.Log($"太阳受到攻击！剩余生命值: {currentHealth}/{maxHealth}");

        // 1. 播放粒子效果
        if (hitParticlePrefab != null)
        {
            Instantiate(hitParticlePrefab, transform.position, Quaternion.identity);
        }

        // 播放受击音效
        AudioManager.Instance.PlaySE("Sun_Skill_Explosion");

        // 2. 触发冲击震屏 (将调用我们稍后添加的新方法)
        CameraShaker.Instance?.ShakeImpact(hitShakeDuration, hitShakeStrength);

        if (currentHealth == 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("太阳被击败了！启动死亡序列...");
        // 停止所有常规协程，特别是攻击循环
        StopAllCoroutines();
        // 启动专门的死亡序列协程
        StartCoroutine(DeathSequenceCoroutine());
    }

    private IEnumerator DeathSequenceCoroutine()
    {
        // 1. 慢放时间
        Time.timeScale = deathTimeScale;

        // 2. 开始渐强的长时间震屏
        CameraShaker.Instance?.ShakeCrescendo(deathShakeDuration, deathShakeMaxStrength);

        // 3. 周期性地生成死亡粒子
        float particleTimer = 0f;
        float sequenceTimer = 0f;
        while (sequenceTimer < deathShakeDuration)
        {
            // 使用 unscaledDeltaTime 来确保计时器不受慢放影响
            sequenceTimer += Time.unscaledDeltaTime;
            particleTimer += Time.unscaledDeltaTime;

            if (particleTimer >= deathParticleInterval)
            {
                particleTimer -= deathParticleInterval;
                if (deathParticlePrefab != null)
                {
                    // 播放技能爆炸音效
                    AudioManager.Instance.PlaySE("Sun_Skill_Explosion");
                    Instantiate(deathParticlePrefab, transform.position, Quaternion.identity);
                }
            }
            yield return null;
        }


        // 5. 在黑屏状态下等待一段时间，让玩家沉淀一下
        // 使用WaitForSecondsRealtime来确保等待时间不受慢放影响
        yield return new WaitForSecondsRealtime(blackScreenDuration);

        // 6. 在加载新场景前，务必恢复正常的时间流速
        Time.timeScale = 1.0f;
        
        // 7. 使用 CoverPanel 加载主菜单
        CoverPanel.Instance.ChangeScene(mainMenuSceneName, 2f);
    }
    
    // (这个方法是从SunAttack.cs中迁移过来的，并去除了特效相关的代码)
    private void SpawnBombWave()
    {
        for (int angle = 0; angle < 360; angle += angleStep)
        {
            if (Random.value > spawnChance) continue;
            float randomAngleOffset = Random.Range(-maxAngleOffset, maxAngleOffset);
            float finalAngle = angle + randomAngleOffset;
            float radian = finalAngle * Mathf.Deg2Rad;
            float x = Mathf.Cos(radian);
            float z = Mathf.Sin(radian);
            Vector3 direction = new Vector3(x, 0, z).normalized;
            float randomYOffset = Random.Range(-maxYOffset, maxYOffset);
            Vector3 spawnPosition = transform.position + direction * sunRadius + new Vector3(0, randomYOffset, 0);
            GameObject bombInstance = Instantiate(bombPrefab, spawnPosition, Quaternion.identity);
            Destroy(bombInstance, bombLifetime);
            if (bombInstance.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.velocity = direction * launchSpeed;
            }
            else
            {
                Rigidbody newRb = bombInstance.AddComponent<Rigidbody>();
                newRb.useGravity = false;
                newRb.velocity = direction * launchSpeed;
            }
        }
    }
}
