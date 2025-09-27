
using UnityEngine;
using System.Collections;
using DG.Tweening; // 确保你已经从 Asset Store 导入了 DOTween

public class Bomb : MonoBehaviour
{
    [Header("Explosion Settings")]
    [SerializeField] private float explosionRadius = 15f;
    [SerializeField] private float peakForce = 70f; // 爆炸在最近点能施加的最大力
    [SerializeField] private AnimationCurve forceFalloffCurve; // 力随距离衰减的曲线
    [SerializeField] private float upwardForceBias = 0.2f; // 爆炸力向上倾斜的程度
    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private Transform subParticle;

    [Header("Respawn Settings")]
    [SerializeField] private float respawnTime = 5.0f;
    [SerializeField] private float respawnScaleDuration = 0.5f;

    // Component references
    private PlayerMovement playerMovement;
    private Collider bombCollider;
    private MeshRenderer meshRenderer;

    private Vector3 originalScale;

    [SerializeField]
    public bool isExploded = false;

    void Start()
    {
        // 缓存组件以提高性能
        bombCollider = GetComponent<Collider>();
        meshRenderer = GetComponent<MeshRenderer>();
        originalScale = transform.localScale;

        // 在开始时只查找一次玩家
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
        }
        else
        {
            Debug.LogError("炸弹无法找到标签为 'Player' 的游戏对象。请确保你的玩家对象已设置此标签。", this);
        }
    }

    // 这个公共方法将由射击脚本调用
    public void OnHitByLaser()
    {
        if (isExploded)
        {
            return; // 如果已经爆炸，则不执行任何操作
        }

        Explode();
    }

    private void Explode()
    {
        isExploded = true;

        // 播放爆炸音效
        AudioManager.Instance.PlaySE("Bomb_Explosion");

        // 为弹匣增加一发弹药
        Magazine.Instance.AddCharge();

        // 1. 播放爆炸视觉效果
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        // 2. 如果在范围内，则对玩家施加力
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

        // 3. 隐藏炸弹并开始重生计时
        StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine()
    {
        subParticle.DOScale(Vector3.zero, respawnScaleDuration / 3);
        
        // 等待重生时间
        yield return new WaitForSeconds(respawnTime);

        transform.localScale = Vector3.zero;

        // 播放重生动画
        subParticle.DOScale(Vector3.one, respawnScaleDuration).SetEase(Ease.OutBack);

        transform.localScale = originalScale;

        isExploded = false; // 重置状态，可以再次被引爆
    }

    // 在Scene视图中绘制爆炸半径的Gizmos
    void OnDrawGizmos()
    {
        // 设置Gizmos颜色为红色，半透明
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        
        // 绘制爆炸半径球体
        Gizmos.DrawSphere(transform.position, explosionRadius);
        
        // 绘制爆炸半径的边框线
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
