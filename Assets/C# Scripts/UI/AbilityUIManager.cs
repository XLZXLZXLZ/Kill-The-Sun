using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 管理游戏中玩家技能（弹匣和时间回溯）的UI显示。
/// 监听技能状态的变化并更新对应的UI元素填充进度。
/// </summary>
public class AbilityUIManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("用于显示弹匣充能状态的UI Image组件")]
    [SerializeField] private Image magazineIcon;

    [Tooltip("用于显示时间回溯技能冷却状态的UI Image组件")]
    [SerializeField] private Image timeRewindIcon;

    [Header("Animation Settings")]
    [Tooltip("弹匣UI填充动画的持续时间")]
    [SerializeField] private float magazineFillDuration = 0.3f;
    [Tooltip("时间回溯UI平滑更新的动画时间（建议设置一个较小的值，如0.1，使其反应灵敏）")]
    [SerializeField] private float rewindFillDuration = 0.1f;

    [Header("Component References")]
    [Tooltip("场景中包含TimeRewind脚本的玩家对象")]
    [SerializeField] private TimeRewind timeRewind;

    private Magazine magazine;

    /// <summary>
    /// 在脚本实例被加载时调用。
    /// 初始化引用，并订阅弹匣状态变化事件。
    /// </summary>
    void Start()
    {
        // 获取 Magazine 的单例
        magazine = Magazine.Instance;
        if (magazine != null)
        {
            // 订阅事件，当弹药数量变化时，自动调用 UpdateMagazineUI 方法
            magazine.OnChargesChanged += UpdateMagazineUI;
            // 在游戏开始时，立即根据当前弹药数量更新一次UI，确保初始状态正确显示（无动画）
            magazineIcon.fillAmount = (float)magazine.CurrentCharges / magazine.MaxCharges;
        }
        else
        {
            Debug.LogError("AbilityUIManager: 未能找到 Magazine 实例。请确保场景中存在一个激活的 Magazine 对象。");
        }

        if (timeRewind == null)
        {
            Debug.LogError("AbilityUIManager: TimeRewind 引用未设置。请在 Inspector 中拖入玩家对象。");
        }
        else
        {
            // 初始化时间回溯UI的初始状态
            timeRewindIcon.fillAmount = timeRewind.CooldownProgress;
        }
    }

    /// <summary>
    /// 每一帧调用一次。
    /// 用于更新需要连续变化状态的UI，如此处的技能冷却进度。
    /// </summary>
    void Update()
    {
        // 持续更新时间回溯技能的冷却UI
        if (timeRewind != null && timeRewindIcon != null)
        {
            // TimeRewind脚本中的CooldownProgress属性已经为我们计算好了0到1的进度值
            // 使用DOFillAmount可以平滑地追赶目标值，比直接赋值效果更柔和
            timeRewindIcon.DOFillAmount(timeRewind.CooldownProgress, rewindFillDuration);
        }
    }

    /// <summary>
    /// 当此脚本被销毁时调用。
    /// 在此取消订阅事件，以防止内存泄漏。
    /// </summary>
    void OnDestroy()
    {
        // 确保在对象销毁时取消事件订阅，这是一个好习惯
        if (magazine != null)
        {
            magazine.OnChargesChanged -= UpdateMagazineUI;
        }

        // 停止所有在此Image上活动的DOTWEEN动画，防止在场景切换或对象销毁时出错
        if (magazineIcon != null) magazineIcon.DOKill();
        if (timeRewindIcon != null) timeRewindIcon.DOKill();
    }

    /// <summary>
    /// 事件处理函数，在弹匣弹药数量变化时被调用。
    /// </summary>
    /// <param name="currentCharges">当前弹药数量</param>
    private void UpdateMagazineUI(int currentCharges)
    {
        if (magazineIcon != null)
        {
            // 根据当前弹药数和最大弹药数计算填充比例（0.0到1.0之间）
            float targetFillAmount = (float)currentCharges / magazine.MaxCharges;
            // 使用DOTWEEN的DOFillAmount方法创建平滑的填充动画
            magazineIcon.DOFillAmount(targetFillAmount, magazineFillDuration).SetEase(Ease.OutQuad);
        }
    }
}
