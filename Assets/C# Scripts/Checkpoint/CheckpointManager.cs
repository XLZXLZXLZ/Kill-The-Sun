
using UnityEngine;
using System.Collections;
using DG.Tweening;

public class CheckpointManager : Singleton<CheckpointManager>
{
    [Header("References")]
    [SerializeField] private CanvasGroup fadeScreen; // 用于转场的黑屏UI
    [SerializeField] private Transform playerTransform;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.5f;

    private Vector3 latestCheckpointPosition;
    private bool isPlayerDead = false;

    void Start()
    {
        // 游戏开始时，将初始位置设为第一个存档点
        if (playerTransform != null)
        {
            latestCheckpointPosition = playerTransform.position;
        }
    }

    /// <summary>
    /// 更新最新的存档点位置
    /// </summary>
    public void UpdateCheckpoint(Vector3 newPosition)
    {
        latestCheckpointPosition = newPosition;
        Debug.Log("存档点已更新至: " + newPosition);
    }

    /// <summary>
    /// 触发玩家死亡与重生流程
    /// </summary>
    public void PlayerDie()
    {
        if (!isPlayerDead)
        {
            StartCoroutine(DeathSequence());
        }
    }

    private IEnumerator DeathSequence()
    {
        isPlayerDead = true;

        // 1. 禁用控制
        if (playerMovement) playerMovement.enabled = false;
        
        // 2. 淡出 (暗屏)
        fadeScreen.gameObject.SetActive(true);
        yield return fadeScreen.DOFade(1, fadeDuration).SetUpdate(true).WaitForCompletion();

        // 3. 传送并重置状态
        if (playerTransform) playerTransform.position = latestCheckpointPosition;
        if (playerMovement) playerMovement.ResetVelocity();
        
        // 4. 淡入 (亮屏)
        yield return fadeScreen.DOFade(0, fadeDuration).SetUpdate(true).WaitForCompletion();
        fadeScreen.gameObject.SetActive(false);

        // 5. 恢复控制
        if (playerMovement) playerMovement.enabled = true;

        isPlayerDead = false;
    }
}



