
using UnityEngine;
using System.Collections;
using DG.Tweening; // 确保你已经从 Asset Store 导入了 DOTween

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class BombProjectile : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private Transform modelTransform; // 炸弹的模型/视觉部分
    [SerializeField] private float spawnScaleDuration = 0.3f; // 出生时放大的动画时长

    [Header("Movement Settings")]
    [SerializeField] private float speed = 40f; // 匀速飞行的速度
    [SerializeField] private float lifetime = 5f; // 飞行状态下的生命周期

    [Header("Explosion Settings")]
    [SerializeField] private float explosionRadius = 10f;
    [SerializeField] private float peakForce = 50f; // 爆炸在最近点能施加的最大力
    [SerializeField] private AnimationCurve forceFalloffCurve; // 力随距离衰减的曲线
    [SerializeField] private float upwardForceBias = 0.3f; // 爆炸力向上倾斜的程度 (可与固定炸弹不同)
    [SerializeField] private GameObject explosionEffectPrefab;

    private Rigidbody rb;
    private PlayerMovement playerMovement;
    private bool isStuck = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
        }
        else
        {
            Debug.LogError("BombProjectile无法找到玩家。请确保玩家对象拥有 'Player' 标签。", this);
        }

        // 匀速飞行
        rb.velocity = transform.forward * speed;

        // 开始生命周期和出生动画
        StartCoroutine(LifetimeCoroutine());
        AnimateSpawn();
    }

    private void AnimateSpawn()
    {
        if (modelTransform != null)
        {
            Vector3 originalScale = modelTransform.localScale;
            modelTransform.localScale = Vector3.zero;
            modelTransform.DOScale(originalScale, spawnScaleDuration).SetEase(Ease.OutBack);
        }
    }

    private IEnumerator LifetimeCoroutine()
    {
        yield return new WaitForSeconds(lifetime);

        // 如果生命周期结束时, 炸弹还未粘在任何地方, 则自动引爆
        if (!isStuck)
        {
            Detonate();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isStuck || collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        isStuck = true;
        rb.isKinematic = true; 
        transform.SetParent(collision.transform);
    }

    public void Detonate()
    {
        if (isStuck)
        {
            transform.SetParent(null);
        }

        // 播放爆炸音效
        AudioManager.Instance.PlaySE("BombProjectile_Explosion");
        
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        // --- 核心改动：检测并关闭范围内的屏障 ---
        Collider[] collidersInRange = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var col in collidersInRange)
        {
            Barrier barrier = col.GetComponent<Barrier>();
            if (barrier != null)
            {
                barrier.DisableBarrier();
            }
        }

        if (playerMovement != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerMovement.transform.position);

            if (distanceToPlayer <= explosionRadius)
            {
                // 1. 计算距离比例 (0=最近, 1=最远)
                float distanceRatio = distanceToPlayer / explosionRadius;
                // 2. 从曲线上查询对应的力比例
                float forceMultiplier = forceFalloffCurve.Evaluate(distanceRatio);
                // 3. 计算最终的力大小
                float forceMagnitude = peakForce * forceMultiplier;
                
                // 计算从炸弹指向玩家的方向，并混入向上的力
                Vector3 rawDirection = (playerMovement.transform.position - transform.position).normalized;
                Vector3 forceDirection = (rawDirection + Vector3.up * upwardForceBias).normalized;
                
                playerMovement.AddImpact(forceDirection * forceMagnitude);
            }
        }
        
        Destroy(gameObject);
    }
}
