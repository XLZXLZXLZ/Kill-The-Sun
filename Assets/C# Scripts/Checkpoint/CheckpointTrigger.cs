
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CheckpointTrigger : MonoBehaviour
{
    [SerializeField] private bool disableOnTrigger = true; // 触发后是否禁用，防止重复触发
    private Collider triggerCollider;

    void Awake()
    {
        // 确保碰撞体是触发器
        triggerCollider = GetComponent<Collider>();
        if (!triggerCollider.isTrigger)
        {
            Debug.LogWarning("CheckpointTrigger的碰撞体未设置为IsTrigger，已自动修复。", this);
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 更新存档点
            CheckpointManager.Instance.UpdateCheckpoint(transform.position);

            // 禁用自身以防重复触发
            if (disableOnTrigger)
            {
                triggerCollider.enabled = false;
                
                // (可选) 在这里可以添加触发后的视觉/音效反馈，比如播放粒子、改变颜色等
                // GetComponent<Renderer>().material.color = Color.green;
            }
        }
    }
}

