using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// 控制主菜单的所有UI动画和交互逻辑。
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Main Menu View")]
    [SerializeField] private CanvasGroup mainMenuGroup;
    [SerializeField] private List<RectTransform> mainMenuElements;

    [Header("Chapter Select View")]
    [SerializeField] private CanvasGroup chapterSelectGroup;
    [SerializeField] private List<Button> chapterButtons;
    [SerializeField] private List<Image> chapterBorders; // 章节按钮的边框

    [Header("Animation Settings")]
    [SerializeField] private float flyInDuration = 0.5f;
    [SerializeField] private float flyInStaggerDelay = 0.1f;
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float hoverDuration = 0.2f;
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private float clickScale = 0.95f;

    private List<Vector2> mainMenuOriginalPositions;
    private List<Vector2> chapterOriginalPositions;
    private List<Color> originalBorderColors;

    private void Start()
    {
        // 每次加载主菜单时，都确保鼠标是可见且未锁定的
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetupInitialPositions();
        AnimateMainMenuIn();
        // 播放主菜单的BGM
        AudioManager.Instance.PlayBGM("SpaceBGM");
    }

    private void SetupInitialPositions()
    {
        // --- Main Menu Setup ---
        mainMenuOriginalPositions = new List<Vector2>();
        foreach (var element in mainMenuElements)
        {
            mainMenuOriginalPositions.Add(element.anchoredPosition);
            // Move off-screen to the left
            element.anchoredPosition -= new Vector2(Screen.width, 0);
        }
        mainMenuGroup.alpha = 1;
        mainMenuGroup.interactable = true;

        // --- Chapter Select Setup ---
        chapterOriginalPositions = new List<Vector2>();
        foreach (var button in chapterButtons)
        {
            RectTransform rt = button.GetComponent<RectTransform>();
            chapterOriginalPositions.Add(rt.anchoredPosition);
            // Move off-screen to the bottom
            rt.anchoredPosition -= new Vector2(0, Screen.height);
        }
        originalBorderColors = new List<Color>();
        foreach(var border in chapterBorders)
        {
            originalBorderColors.Add(border.color);
        }
        chapterSelectGroup.alpha = 0;
        chapterSelectGroup.interactable = false;
    }

    #region Main Menu Logic

    private void AnimateMainMenuIn()
    {
        mainMenuGroup.gameObject.SetActive(true);
        mainMenuGroup.interactable = true;
        
        Sequence s = DOTween.Sequence();
        // 使用 Insert 在交错的时间点插入动画，而不是等待上一个完成
        for (int i = 0; i < mainMenuElements.Count; i++)
        {
            s.Insert(i * flyInStaggerDelay, mainMenuElements[i].DOAnchorPos(mainMenuOriginalPositions[i], flyInDuration).SetEase(Ease.OutCubic));
        }
    }

    public void OnStartGamePressed()
    {
        AudioManager.Instance.PlaySE("UI_Button_Click");
        mainMenuGroup.interactable = false;
        
        Sequence s = DOTween.Sequence();
        // 反向交错飞出
        for (int i = 0; i < mainMenuElements.Count; i++)
        {
            // (mainMenuElements.Count - 1 - i) a simple trick to reverse the order
            int reverseIndex = mainMenuElements.Count - 1 - i;
            s.Insert(i * flyInStaggerDelay, mainMenuElements[reverseIndex].DOAnchorPos(mainMenuOriginalPositions[reverseIndex] - new Vector2(Screen.width, 0), flyInDuration).SetEase(Ease.InCubic));
        }
        s.OnComplete(AnimateChapterSelectIn);
    }

    public void OnExitGamePressed()
    {
        AudioManager.Instance.PlaySE("UI_Button_Click");
        Debug.Log("Exiting Game...");
        Application.Quit();
    }

    #endregion

    #region Chapter Select Logic

    private void AnimateChapterSelectIn()
    {
        mainMenuGroup.gameObject.SetActive(false);
        chapterSelectGroup.alpha = 1;
        chapterSelectGroup.interactable = true;
        
        Sequence chapterSequence = DOTween.Sequence();
        for (int i = 0; i < chapterButtons.Count; i++)
        {
            RectTransform rt = chapterButtons[i].GetComponent<RectTransform>();
            chapterSequence.Insert(i * flyInStaggerDelay, rt.DOAnchorPos(chapterOriginalPositions[i], flyInDuration).SetEase(Ease.OutCubic));
        }
    }

    public void OnChapterSelected(int chapterIndex)
    {
        if (chapterIndex < 0 || chapterIndex >= chapterButtons.Count) return;

        AudioManager.Instance.PlaySE("UI_Button_Click");
        
        chapterSelectGroup.interactable = false;
        RectTransform rt = chapterButtons[chapterIndex].GetComponent<RectTransform>();
        
        DOTween.Sequence()
            .Append(rt.DOScale(clickScale, 0.1f))
            .Append(rt.DOScale(1f, 0.1f))
            .OnComplete(() =>
            {
                string sceneToLoad = $"Level{chapterIndex + 1}";
                // 在加载场景前，播放对应关卡的BGM
                AudioManager.Instance.PlayBGMForLevel(sceneToLoad);
                // 使用新的 CoverPanel 进行转场
                CoverPanel.Instance.ChangeScene(sceneToLoad, 1.0f);
            });
    }

    #endregion

    #region Hover Effects

    public void OnPointerEnter(Button button)
    {
        AudioManager.Instance.PlaySE("UI_Button_Hover");
        button.transform.DOScale(hoverScale, hoverDuration);
        // Assuming button text color change
        Text buttonText = button.GetComponentInChildren<Text>();
        if (buttonText != null) buttonText.DOColor(hoverColor, hoverDuration);
    }

    public void OnPointerExit(Button button)
    {
        button.transform.DOScale(1f, hoverDuration);
        Text buttonText = button.GetComponentInChildren<Text>();
        if (buttonText != null) buttonText.DOColor(Color.white, hoverDuration); // Change to original color
    }
    
    public void OnChapterPointerEnter(int index)
    {
        AudioManager.Instance.PlaySE("UI_Button_Hover");
        chapterButtons[index].transform.DOScale(hoverScale, hoverDuration);
        chapterBorders[index].DOColor(hoverColor, hoverDuration);
    }

    public void OnChapterPointerExit(int index)
    {
        chapterButtons[index].transform.DOScale(1f, hoverDuration);
        chapterBorders[index].DOColor(originalBorderColors[index], hoverDuration);
    }

    #endregion
}
