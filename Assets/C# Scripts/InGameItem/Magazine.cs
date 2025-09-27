
using System;
using UnityEngine;

// 继承自 Singleton 基类，使其成为一个全局单例
public class Magazine : Singleton<Magazine>
{
    [Header("Magazine Settings")]
    [SerializeField] private int maxCharges = 2; // 弹匣最大容量
    [SerializeField] private int chargesPerShot = 2; // 每次发射需要的弹药数

    public int CurrentCharges { get; private set; }
    public int MaxCharges => maxCharges;
    public int ChargesPerShot => chargesPerShot;

    // 当弹药数量变化时触发的事件，方便UI更新
    public event Action<int> OnChargesChanged;

    /// <summary>
    /// 为弹匣增加一发弹药
    /// </summary>
    public void AddCharge()
    {
        if (CurrentCharges < maxCharges)
        {
            CurrentCharges++;
            Debug.Log($"弹药增加! 当前数量: {CurrentCharges}/{maxCharges}");
            // 触发事件，通知UI等监听者
            OnChargesChanged?.Invoke(CurrentCharges);
        }
        else
        {
            Debug.Log($"弹药已满! 当前数量: {CurrentCharges}/{maxCharges}");
        }
    }

    /// <summary>
    /// 尝试使用弹药（需要消耗chargesPerShot发弹药）
    /// </summary>
    /// <returns>如果使用成功则返回true，否则返回false</returns>
    public bool UseCharge()
    {
        if (CurrentCharges >= chargesPerShot)
        {
            CurrentCharges -= chargesPerShot;
            Debug.Log($"消耗{chargesPerShot}发弹药! 剩余数量: {CurrentCharges}/{maxCharges}");
            OnChargesChanged?.Invoke(CurrentCharges);
            return true;
        }
        else
        {
            Debug.Log($"弹药不足! 需要{chargesPerShot}发，当前只有{CurrentCharges}发");
            return false;
        }
    }

    /// <summary>
    /// 检查是否有足够的弹药进行发射
    /// </summary>
    /// <returns>如果有足够弹药则返回true，否则返回false</returns>
    public bool CanShoot()
    {
        return CurrentCharges >= chargesPerShot;
    }
}
