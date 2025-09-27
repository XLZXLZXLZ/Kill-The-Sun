using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement; // Added for SceneManager

/// <summary>
/// 导演和管理与太阳Boss的整场战斗流程。
/// </summary>
public class SunBossFightController : MonoBehaviour
{
    [Header("Player References")]
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Scene References")]
    [Tooltip("所有参与战斗的平台，它们的Rigidbody将被统一管理")]
    [SerializeField] private List<Rigidbody> platformRigidbodies;

    [Header("Opening Explosion Settings")]
    [Tooltip("预设的爆炸中心点。如果未设置，则默认为玩家的位置。")]
    [SerializeField] private Transform explosionPoint;
    [Tooltip("开场爆炸的特效预制体")]
    [SerializeField] private GameObject explosionEffectPrefab;
    [Tooltip("开场爆炸的半径")]
    [SerializeField] private float explosionRadius = 20f;
    [Tooltip("施加给平台的爆炸力")]
    [SerializeField] private float explosionForce = 500f;
    [Tooltip("施加给玩家的垂直方向上的冲击力")]
    [SerializeField] private float playerUpwardForce = 20f;

    private bool sequenceHasRun = false; // 确保开场序列只运行一次

    private void Awake()
    {
        // 游戏开始时，将所有平台设置为运动学状态，让它们表现为静态物体
        SetPlatformsKinematic(true);
    }

    /// <summary>
    /// 公共方法，由教程结束事件或其他外部事件调用，用于正式开启战斗序列。
    /// </summary>
    public void StartFightSequence()
    {
        // 防止被重复调用
        if (sequenceHasRun) return;
        sequenceHasRun = true;

        Debug.Log("太阳Boss战开场序列启动！");

        // 启动开场爆炸的协程
        StartCoroutine(OpeningExplosionCoroutine());
    }

    private IEnumerator OpeningExplosionCoroutine()
    {
        // 等待一帧，确保所有初始化完成
        yield return null;
        
        // 确定爆炸中心点，如果未预设，则使用玩家位置
        Vector3 explosionPosition = explosionPoint != null ? explosionPoint.position : playerMovement.transform.position;
        
        // 1. 在爆炸点实例化爆炸特效
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, explosionPosition, Quaternion.identity);
        }

        // 2. 将所有平台切换为动态刚体
        SetPlatformsKinematic(false);

        // 3. 计算爆炸效果
        // 对玩家施加一个直接向上的力，确保他被炸飞
        playerMovement.AddImpact(Vector3.up * playerUpwardForce);

        // 对所有平台施加爆炸力
        foreach (Rigidbody rb in platformRigidbodies)
        {
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, explosionPosition, explosionRadius);
            }
        }
    }

    /// <summary>
    /// 辅助方法，用于统一设置所有平台的Rigidbody的isKinematic状态。
    /// </summary>
    /// <param name="isKinematic">是否设置为运动学状态</param>
    private void SetPlatformsKinematic(bool isKinematic)
    {
        foreach (Rigidbody rb in platformRigidbodies)
        {
            if (rb != null)
            {
                rb.isKinematic = isKinematic;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 仅在编辑器中且选中该对象时绘制Gizmos
        if (explosionRadius > 0)
        {
            // 确定Gizmos的绘制中心
            Vector3 center = explosionPoint != null ? explosionPoint.position : (playerMovement != null ? playerMovement.transform.position : transform.position);
            
            Gizmos.color = new Color(1, 0, 0, 0.3f); // 半透明红色
            Gizmos.DrawSphere(center, explosionRadius);
        }
    }
}
