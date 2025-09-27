using UnityEngine;
using System.Collections;
using DG.Tweening;

/// <summary>
/// 负责处理主摄像机的震动效果。
/// 使用 AnimationCurve 来控制震动强度的动态变化，实现渐强渐弱的效果。
/// </summary>
public class CameraShaker : MonoBehaviour
{
    public static CameraShaker Instance { get; private set; }

    [Header("Curves")]
    [Tooltip("控制蓄力震动强度随时间变化的曲线。建议形状：缓入缓出，中间达到峰值。")]
    [SerializeField] private AnimationCurve buildupShakeCurve = new AnimationCurve(
        new Keyframe(0f, 0f), 
        new Keyframe(0.3f, 1f), 
        new Keyframe(1f, 0f));

    [Tooltip("控制冲击震动强度随时间变化的曲线。建议形状：立即达到峰值，然后快速衰减。")]
    [SerializeField] private AnimationCurve impactShakeCurve = new AnimationCurve(
        new Keyframe(0f, 1f), 
        new Keyframe(1f, 0f));

    [Tooltip("控制渐强震动强度随时间变化的曲线。建议形状：从0缓慢增长到1。")]
    [SerializeField] private AnimationCurve crescendoShakeCurve = new AnimationCurve(
        new Keyframe(0f, 0f), 
        new Keyframe(1f, 1f));

    private Transform cameraTransform;
    private Coroutine shakeCoroutine;

    private void Awake()
    {
        // 设置单例
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        cameraTransform = Camera.main.transform;
    }

    /// <summary>
    /// 触发一次带有动态强度曲线的摄像机震动。
    /// </summary>
    /// <param name="duration">总震动时长</param>
    /// <param name="maxStrength">震动强度的峰值</param>
    public void Shake(float duration, float maxStrength)
    {
        // 如果正在震动，先停止旧的
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        
        // 启动新的震动协程
        shakeCoroutine = StartCoroutine(ShakeCoroutine(duration, maxStrength, buildupShakeCurve));
    }

    /// <summary>
    /// 触发一次带有冲击效果的摄像机震动（瞬时最强，然后衰减）。
    /// </summary>
    /// <param name="duration">总震动时长</param>
    /// <param name="maxStrength">震动强度的峰值</param>
    public void ShakeImpact(float duration, float maxStrength)
    {
        // 如果正在震动，先停止旧的
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        
        // 启动新的震动协程，但这次使用冲击曲线
        shakeCoroutine = StartCoroutine(ShakeCoroutine(duration, maxStrength, impactShakeCurve));
    }

    /// <summary>
    /// 触发一次强度逐渐增强的摄像机震动。
    /// </summary>
    /// <param name="duration">总震动时长</param>
    /// <param name="maxStrength">震动在结束时的最大强度</param>
    public void ShakeCrescendo(float duration, float maxStrength)
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        shakeCoroutine = StartCoroutine(ShakeCoroutine(duration, maxStrength, crescendoShakeCurve));
    }

    private IEnumerator ShakeCoroutine(float duration, float maxStrength, AnimationCurve curve)
    {
        Vector3 originalPosition = cameraTransform.localPosition;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            // 使用 unscaledDeltaTime 来确保震动效果不受 Time.timeScale 的影响
            elapsedTime += Time.unscaledDeltaTime;
            
            // 计算当前时间点在总时长中的比例
            float progress = elapsedTime / duration;
            
            // 从指定的动画曲线获取当前的强度系数 (0 to 1)
            float curveMultiplier = curve.Evaluate(progress);
            
            // 计算当前帧的实际震动强度
            float currentStrength = maxStrength * curveMultiplier;
            
            // 生成一个随机方向的偏移量并应用
            cameraTransform.localPosition = originalPosition + Random.insideUnitSphere * currentStrength;

            yield return null;
        }

        // 震动结束后，确保摄像机回到原始位置
        cameraTransform.localPosition = originalPosition;
    }
}
