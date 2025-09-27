
using UnityEngine;
using System.Collections;
using DG.Tweening;

[RequireComponent(typeof(Collider))]
public class Barrier : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float disableDuration = 5f; // 屏障被关闭的持续时间
    [SerializeField] private float shaderAnimDuration = 0.5f; // shader动画时长

    [Header("Shader Properties")]
    [SerializeField] private string maskMultiplierProperty = "_MaskMultiplier";
    [SerializeField] private float disabledMultiplierValue = 0.05f;

    private Collider barrierCollider;
    private Renderer barrierRenderer;
    private float originalMultiplierValue;
    private bool isDisabled = false;

    void Awake()
    {
        barrierCollider = GetComponent<Collider>();
        barrierRenderer = GetComponent<Renderer>();

        // 启动时记录下shader属性的初始值
        if (barrierRenderer != null)
        {
            originalMultiplierValue = barrierRenderer.material.GetFloat(maskMultiplierProperty);
        }
    }

    /// <summary>
    /// 被外部调用以关闭屏障
    /// </summary>
    public void DisableBarrier()
    {
        if (!isDisabled)
        {
            StartCoroutine(DisableSequence());
        }
    }

    private IEnumerator DisableSequence()
    {
        isDisabled = true;

        // 1. 禁用碰撞
        barrierCollider.enabled = false;

        // 2. 播放Shader消失动画
        if (barrierRenderer != null)
        {
            barrierRenderer.material.DOFloat(disabledMultiplierValue, maskMultiplierProperty, shaderAnimDuration).SetEase(Ease.InOutQuad);
        }

        // 3. 等待指定时间
        yield return new WaitForSeconds(disableDuration);

        // 4. 播放Shader恢复动画
        if (barrierRenderer != null)
        {
            barrierRenderer.material.DOFloat(originalMultiplierValue, maskMultiplierProperty, shaderAnimDuration).SetEase(Ease.InOutQuad);
        }
        
        // 5. 恢复碰撞 (可以在动画开始时就恢复，或动画结束后再恢复，这里选择前者)
        barrierCollider.enabled = true;

        // 动画播放完后，重置状态
        yield return new WaitForSeconds(shaderAnimDuration);
        isDisabled = false;
    }
}



