
using UnityEngine;
using System.Collections;
using DG.Tweening; // 确保你已经从 Asset Store 导入了 DOTween
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class TutorialTrigger : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private CanvasGroup fadeScreen;
    [SerializeField] private CanvasGroup tutorialPanel;

    [Header("Player References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MouseLook mouseLook;

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float fadeToAlpha = 0.5f; // 暗场的目标透明度

    [Header("Events")]
    [Tooltip("当教程被阅读并成功关闭后触发此事件")]
    public UnityEvent OnTutorialEnd;

    private bool isPlayerInRange = false;
    private bool isTutorialActive = false;
    private bool hasBeenTriggered = false; // 新增一个标志，确保事件只触发一次

    void Start()
    {
        // 确保初始状态是隐藏的
        interactionPrompt.SetActive(false);
        fadeScreen.alpha = 0;
        tutorialPanel.alpha = 0;
        fadeScreen.gameObject.SetActive(false);
        tutorialPanel.gameObject.SetActive(false);
    }

    void Update()
    {
        // 玩家在范围内按下Q，且教程未激活时，显示教程
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.Q) && !isTutorialActive)
        {
            StartCoroutine(ShowTutorialCoroutine());
        }

        // 教程激活时，按下ESC，隐藏教程
        if (isTutorialActive && Input.GetKeyDown(KeyCode.Escape))
        {
            StartCoroutine(HideTutorialCoroutine());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            interactionPrompt.SetActive(true);
            
            // 自动查找玩家组件，如果未在Inspector中指定
            if (playerMovement == null) playerMovement = other.GetComponent<PlayerMovement>();
            if (mouseLook == null) mouseLook = other.GetComponentInChildren<MouseLook>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            interactionPrompt.SetActive(false);
        }
    }

    private IEnumerator ShowTutorialCoroutine()
    {
        isTutorialActive = true;
        interactionPrompt.SetActive(false); // 教程开始时隐藏交互提示

        // 禁用玩家控制
        if (playerMovement) playerMovement.enabled = false;
        if (mouseLook) mouseLook.enabled = false;

        // 激活UI对象并开始动画
        fadeScreen.gameObject.SetActive(true);
        tutorialPanel.gameObject.SetActive(true);

        // 半透明暗场
        fadeScreen.DOFade(fadeToAlpha, fadeDuration).SetUpdate(true);
        // 显示教程面板 (可以延迟一点开始)
        yield return tutorialPanel.DOFade(1, fadeDuration).SetUpdate(true).SetDelay(fadeDuration * 0.5f).WaitForCompletion();
    }

    private IEnumerator HideTutorialCoroutine()
    {
        // 先隐藏教程面板
        tutorialPanel.DOFade(0, fadeDuration).SetUpdate(true);
        // 然后淡出暗场
        yield return fadeScreen.DOFade(0, fadeDuration).SetUpdate(true).SetDelay(fadeDuration * 0.5f).WaitForCompletion();

        // 动画结束后禁用UI对象
        fadeScreen.gameObject.SetActive(false);
        tutorialPanel.gameObject.SetActive(false);
        
        // 恢复玩家控制
        if (playerMovement) playerMovement.enabled = true;
        if (mouseLook) mouseLook.enabled = true;

        // 如果玩家仍在范围内，则重新显示交互提示
        if (isPlayerInRange)
        {
            interactionPrompt.SetActive(true);
        }

        isTutorialActive = false;

        // 触发教程结束事件，并且只触发一次
        if (!hasBeenTriggered)
        {
            hasBeenTriggered = true;
            OnTutorialEnd?.Invoke();
        }
    }
}



