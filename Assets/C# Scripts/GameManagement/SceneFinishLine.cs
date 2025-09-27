using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Text.RegularExpressions;

/// <summary>
/// 通用的关卡终点。当玩家进入时，会自动计算并加载下一关。
/// 关卡命名需遵循 "LevelX" 的格式，例如 "Level1", "Level2"。
/// </summary>
[RequireComponent(typeof(Collider))]
public class SceneFinishLine : MonoBehaviour
{
    [Header("Transition Settings")]
    [Tooltip("玩家到达终点后，在屏幕变黑前等待的时间")]
    [SerializeField] private float waitDuration = 1.5f;
    [Tooltip("屏幕淡出（变黑）的动画时长")]
    [SerializeField] private float fadeOutDuration = 1.0f;
    [Tooltip("屏幕淡入（变亮）的动画时长")]
    [SerializeField] private float fadeInDuration = 1.0f;
    [Tooltip("无法确定下一关时，要加载的回退场景（例如主菜单）")]
    [SerializeField] private string fallbackSceneName = "MainMenu";

    private bool isTriggered = false;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isTriggered)
        {
            isTriggered = true;
            Debug.Log("玩家已到达关卡终点！准备加载下一关...");
            StartCoroutine(FinishSequence());
        }
    }

    private IEnumerator FinishSequence()
    {
        yield return new WaitForSeconds(waitDuration);

        LoadNextLevel();
    }

    private void LoadNextLevel()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        
        // 使用正则表达式从 "LevelX" 中提取数字 X
        Match match = Regex.Match(currentSceneName, @"\d+$");

        if (match.Success && int.TryParse(match.Value, out int currentLevelNumber))
        {
            int nextLevelNumber = currentLevelNumber + 1;
            string nextSceneName = $"Level{nextLevelNumber}";

            // 在加载场景前，命令AudioManager播放新关卡的BGM
            AudioManager.Instance.PlayBGMForLevel(nextSceneName);

            // 检查下一关是否存在于Build Settings中
            // 注意: SceneUtility.GetBuildIndexByScenePath 在编辑器外不可用，
            // 因此我们直接尝试加载，并通过fallback处理失败。
            // 一个更健壮的方法是有一个关卡列表或最大关卡数。
            Debug.Log($"尝试加载下一关: {nextSceneName}");
            CoverPanel.Instance.ChangeScene(nextSceneName, fadeOutDuration + fadeInDuration);
        }
        else
        {
            Debug.LogWarning($"无法从当前场景 '{currentSceneName}' 推断出下一关。将加载回退场景: {fallbackSceneName}");
            // 回退时也可以指定一个BGM
            AudioManager.Instance.PlayBGMForLevel(fallbackSceneName);
            CoverPanel.Instance.ChangeScene(fallbackSceneName, fadeOutDuration + fadeInDuration);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 0.8f, 1f, 0.5f); // 半透明蓝色
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
    }
}
