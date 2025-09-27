using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

[System.Serializable]
public struct LevelBGM
{
    public string levelSceneName;
    public string bgmName;
}

/// <summary>
/// 全局音频管理器，负责背景音乐和音效的播放。
/// 继承自 Singleton 基类，无需手动放入场景，会在首次被调用时自动创建。
/// </summary>
public class AudioManager : Singleton<AudioManager>
{
    // 重写基类属性，确保 AudioManager 在切换场景时不被销毁
    protected override bool IsDonDestroyOnLoad => true;

    private AudioSource bgmAudioSource;
    private AudioSource seAudioSource;
    private Dictionary<string, AudioClip> audioClips;

    [Header("Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float defaultBgmVolume = 0.5f;

    [Header("Level BGM Playlist")]
    [Tooltip("在此处配置每个关卡对应的背景音乐")]
    [SerializeField] private List<LevelBGM> levelBgms;

    private Dictionary<string, string> levelBgmMap;

    protected override void Awake()
    {
        // 必须调用基类的 Awake 来确保单例逻辑正确执行
        base.Awake();

        // 自动为自身添加 AudioSource 组件
        bgmAudioSource = gameObject.AddComponent<AudioSource>();
        bgmAudioSource.loop = true;
        bgmAudioSource.volume = defaultBgmVolume;

        seAudioSource = gameObject.AddComponent<AudioSource>();

        // 加载所有音频资源
        LoadAudioClips();
        
        // 将列表转换为字典以提高查找效率
        InitializeBgmMap();
    }

    /// <summary>
    /// 从 Resources/Audio 文件夹加载所有 AudioClip 并存入字典。
    /// </summary>
    private void LoadAudioClips()
    {
        audioClips = new Dictionary<string, AudioClip>();
        AudioClip[] clips = Resources.LoadAll<AudioClip>("Audio");
        
        foreach (var clip in clips)
        {
            if (!audioClips.ContainsKey(clip.name))
            {
                audioClips.Add(clip.name, clip);
            }
            else
            {
                Debug.LogWarning($"AudioManager: 音频文件 '{clip.name}' 重名，已忽略后一个。");
            }
        }
        
        Debug.Log($"AudioManager 加载了 {audioClips.Count} 个音频文件。");
    }
    
    private void InitializeBgmMap()
    {
        levelBgmMap = new Dictionary<string, string>();
        foreach (var item in levelBgms)
        {
            if (!levelBgmMap.ContainsKey(item.levelSceneName))
            {
                levelBgmMap.Add(item.levelSceneName, item.bgmName);
            }
        }
    }

    /// <summary>
    /// 淡入淡出地播放背景音乐。
    /// </summary>
    /// <param name="bgmName">BGM的文件名</param>
    /// <param name="fadeDuration">淡入淡出的时长</param>
    public void PlayBGM(string bgmName, float fadeDuration = 3.0f)
    {
        if (string.IsNullOrEmpty(bgmName) || !audioClips.TryGetValue(bgmName, out AudioClip clip))
        {
            Debug.LogError($"AudioManager: 找不到名为 '{bgmName}' 的BGM。");
            return;
        }

        // 如果当前已经在播放目标BGM，则不执行任何操作
        if (bgmAudioSource.clip == clip && bgmAudioSource.isPlaying)
        {
            return;
        }

        // 创建一个序列来实现平滑过渡
        DOTween.Sequence()
            .Append(bgmAudioSource.DOFade(0, fadeDuration / 2)) // 先用一半时间淡出
            .AppendCallback(() =>
            {
                bgmAudioSource.clip = clip;
                bgmAudioSource.Play();
            })
            .Append(bgmAudioSource.DOFade(defaultBgmVolume, fadeDuration / 2)); // 再用一半时间淡入
    }

    /// <summary>
    /// 根据场景名播放对应的BGM。
    /// </summary>
    /// <param name="sceneName">目标场景的名称</param>
    public void PlayBGMForLevel(string sceneName)
    {
        if (levelBgmMap.TryGetValue(sceneName, out string bgmName))
        {
            PlayBGM(bgmName);
        }
        else
        {
            Debug.LogWarning($"AudioManager: 在播放列表中未找到场景 '{sceneName}' 对应的BGM配置。");
        }
    }

    /// <summary>
    /// 播放一个一次性的音效。
    /// </summary>
    /// <param name="seName">音效的文件名</param>
    public void PlaySE(string seName)
    {
        if (!audioClips.TryGetValue(seName, out AudioClip clip))
        {
            Debug.Log($"AudioManager: 找不到名为 '{seName}' 的音效。");
            return;
        }
        
        seAudioSource.PlayOneShot(clip);
    }
}
