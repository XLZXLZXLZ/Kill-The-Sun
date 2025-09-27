using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CoverPanel : Singleton<CoverPanel>
{
    Image i;

    protected override void Awake() 
    {
        base.Awake();
        GenerateCover();
    }

    private void GenerateCover()
    {
        var c = this.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 100;

        var go = new GameObject();
        go.transform.SetParent(this.transform, false);

        i = go.AddComponent<Image>();
        i.color = new Color(0,0,0);
        i.transform.localScale = new Vector3(10000, 10000);
        DontDestroyOnLoad(gameObject);
    }

    public void ChangeScene(string sceneName)
    {
        ChangeScene(sceneName, 1f);
    }
    public void ChangeScene(string sceneName,float time)
    {
         ChangeScene(sceneName,time,0);
    }
    public void ChangeScene(string sceneName, Color color, float time = 2, float holdTime = 0)
    {
        if (isChanging)
            return;
        i.color = color;
        ChangeScene(sceneName, time, holdTime);
    }
    public void ChangeScene(string sceneName, float time,float holdTime)
    {
        if (isChanging)
            return;
        StartCoroutine(ChangingScene(sceneName, time / 2, holdTime));
    }
  

    private bool isChanging;

    private IEnumerator ChangingScene(string sceneName, float time, float holdTime)
    {
        isChanging = true;

        // 设置为纯黑但完全透明，准备开始淡出
        i.color = new Color(0f, 0f, 0f, 0f);

        // --- 淡出阶段 (从透明到纯黑) ---
        while (i.color.a < 1.0f)
        {
            // 基于时间增量alpha，确保动画时长准确
            float newAlpha = i.color.a + (Time.unscaledDeltaTime / time);
            i.color = new Color(0f, 0f, 0f, newAlpha);
            yield return null;
        }
        // 动画结束后，确保为纯黑
        i.color = Color.black;

        SceneManager.LoadScene(sceneName);

        if (holdTime > 0)
        {
            yield return new WaitForSecondsRealtime(holdTime);
        }

        // --- 淡入阶段 (从纯黑到透明) ---
        while (i.color.a > 0.0f)
        {
            float newAlpha = i.color.a - (Time.unscaledDeltaTime / time);
            i.color = new Color(0f, 0f, 0f, newAlpha);
            yield return null;
        }
        // 动画结束后，确保为完全透明
        i.color = new Color(0f, 0f, 0f, 0f);

        isChanging = false;
    }
}
