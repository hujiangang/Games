using UnityEngine;

/// <summary>
/// 音频管理组件，负责游戏中的所有音效播放
/// </summary>
public class AudioComponent : MonoBehaviour
{
    [Header("音效设置")]
    [SerializeField] private AudioClip clickSound;      // 点击音效
    [SerializeField] private AudioClip snapSound;       // 吸附音效
    [SerializeField] private AudioClip completeSound;   // 完成音效
    [SerializeField] private AudioClip backgroundMusic; // 背景音乐

    [Header("音量设置")]
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 0.7f;    // 音效音量
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.3f;  // 音乐音量

    private AudioSource sfxSource;      // 音效播放源
    private AudioSource musicSource;    // 音乐播放源

    private bool isMuted = false;       // 是否静音

    private static AudioComponent instance;

    void Awake()
    {
        // 单例模式，确保只有一个音频组件
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        //DontDestroyOnLoad(gameObject);

        // 创建两个 AudioSource：一个用于音效，一个用于背景音乐
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.volume = sfxVolume;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.volume = musicVolume;

        // 自动加载音频资源
        LoadAudioClips();
    }

    void OnEnable()
    {
        // 注册游戏事件
        GameEvents.RegisterBasicEvent(GameBasicEvent.PieceDraggedStart, OnPieceClicked);
        GameEvents.RegisterBasicEvent(GameBasicEvent.UIClick, OnUIClick);
        GameEvents.RegisterBasicEvent(GameBasicEvent.PieceSnapped, OnPieceSnapped);
        GameEvents.RegisterBasicEvent(GameBasicEvent.CompleteLevel, OnLevelComplete);
        GameEvents.RegisterBasicEvent(GameBasicEvent.TurnAudio, ToggleMute);
    }

    void OnDisable()
    {
        // 注销游戏事件
        GameEvents.UnregisterBasicEvent(GameBasicEvent.PieceDraggedStart, OnPieceClicked);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.UIClick, OnUIClick);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.PieceSnapped, OnPieceSnapped);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.CompleteLevel, OnLevelComplete);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.TurnAudio, ToggleMute);
    }

    void Start()
    {
        // 开始播放背景音乐
        PlayBackgroundMusic();
    }

    /// <summary>
    /// 自动加载音频资源
    /// </summary>
    private void LoadAudioClips()
    {
        // 从 Resources 加载音频文件
        // 如果在编辑器中已经指定了音频文件，则不会被覆盖
        // 注意：音频文件需要复制到 Assets/Resources/Audio/ 文件夹
        if (clickSound == null)
            clickSound = Resources.Load<AudioClip>("Audio/click");
        
        if (snapSound == null)
            snapSound = Resources.Load<AudioClip>("Audio/snap");
        
        if (completeSound == null)
            completeSound = Resources.Load<AudioClip>("Audio/powerUp1");
        
        if (backgroundMusic == null)
            backgroundMusic = Resources.Load<AudioClip>("Audio/Retro Comedy");
    }

    /// <summary>
    /// 播放点击音效
    /// </summary>
    private void OnPieceClicked()
    {
        PlaySFX(clickSound);
    }

    /// <summary>
    /// 播放UI点击音效
    /// </summary>
    private void OnUIClick()
    {
        PlaySFX(clickSound);
    }

    /// <summary>
    /// 播放吸附音效
    /// </summary>
    private void OnPieceSnapped()
    {
        PlaySFX(snapSound);
    }

    /// <summary>
    /// 播放吸附音效（需要在碎片吸附时调用）
    /// </summary>
    public void PlaySnapSound()
    {
        PlaySFX(snapSound);
    }

    /// <summary>
    /// 静态方法：播放吸附音效
    /// </summary>
    public static void PlaySnap()
    {
        if (instance != null)
        {
            instance.PlaySnapSound();
        }
    }

    /// <summary>
    /// 播放完成音效
    /// </summary>
    private void OnLevelComplete()
    {
        PlaySFX(completeSound);
    }

    /// <summary>
    /// 播放背景音乐
    /// </summary>
    private void PlayBackgroundMusic()
    {
        if (backgroundMusic != null && !musicSource.isPlaying)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && !isMuted)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// 切换静音状态
    /// </summary>
    public void ToggleMute()
    {
        isMuted = !isMuted;
        
        if (isMuted)
        {
            sfxSource.volume = 0f;
            musicSource.volume = 0f;
        }
        else
        {
            sfxSource.volume = sfxVolume;
            musicSource.volume = musicVolume;
        }

        // 触发音频状态更新事件
        GameEvents.InvokeBasicEvent(GameBasicEvent.UpdateAudio);
    }

    /// <summary>
    /// 设置音效音量
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (!isMuted)
        {
            sfxSource.volume = sfxVolume;
        }
    }

    /// <summary>
    /// 设置音乐音量
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (!isMuted)
        {
            musicSource.volume = musicVolume;
        }
    }

    /// <summary>
    /// 获取静音状态
    /// </summary>
    public bool IsMuted()
    {
        return isMuted;
    }

    /// <summary>
    /// 停止背景音乐
    /// </summary>
    public void StopBackgroundMusic()
    {
        if (musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }

    /// <summary>
    /// 获取单例实例
    /// </summary>
    public static AudioComponent Instance
    {
        get { return instance; }
    }
}
