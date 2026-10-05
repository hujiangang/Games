using UnityEngine;

public class AudioComponent : MonoBehaviour
{
    [SerializeField] private AudioClip clickSound,snapSound,completeSound,backgroundMusic;
    [Range(0,1)] [SerializeField] private float sfxVolume=.7f,musicVolume=.3f;
    private AudioSource sfxSource,musicSource;
    private bool isMuted,paused,adPaused,hostPaused,focused=true;
    private bool unlocked;
    private static AudioComponent instance;
    public static AudioComponent Instance => instance;
    void Awake()
    {
        if(instance && instance!=this) { Destroy(this);return; }
        instance=this;isMuted=PlayerPrefs.GetInt("ColorBlocks.Muted",0)==1;
        sfxSource=gameObject.AddComponent<AudioSource>();sfxSource.playOnAwake=false;sfxSource.spatialBlend=0;
        musicSource=gameObject.AddComponent<AudioSource>();musicSource.playOnAwake=false;musicSource.loop=true;musicSource.spatialBlend=0;
        clickSound=Resources.Load<AudioClip>("Audio/click");
        snapSound=Resources.Load<AudioClip>("Audio/snap");
        completeSound=Resources.Load<AudioClip>("Audio/powerUp1");
        backgroundMusic=Resources.Load<AudioClip>("Audio/morning_mosaic");
        musicSource.clip=backgroundMusic;
        ApplyState();
    }
    void Start()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        Unlock();
#endif
        GameEvents.InvokeEvent(GameBasicEvent.UpdateAudio,isMuted);
    }
    void Update()
    {
        if(!unlocked && (Input.GetMouseButtonDown(0) || Input.touchCount>0)) Unlock();
    }
    void Unlock() { unlocked=true;ApplyState(); }
    void ApplyState()
    {
        if(!musicSource || !sfxSource) return;
        bool silent=isMuted || paused || adPaused || hostPaused || !focused;
        sfxSource.volume=silent?0:sfxVolume;
        musicSource.volume=musicVolume;
        if(silent || !unlocked) { musicSource.Pause();sfxSource.Stop(); }
        else if(!musicSource.isPlaying && backgroundMusic) { musicSource.UnPause();if(!musicSource.isPlaying) musicSource.Play(); }
    }
    void PlaySFX(AudioClip clip) { Unlock();if(clip && !isMuted && !paused && !adPaused && !hostPaused && focused) sfxSource.PlayOneShot(clip); }
    void OnClick() => PlaySFX(clickSound);
    void OnSnap() => PlaySFX(snapSound);
    void OnComplete() => PlaySFX(completeSound);
    public void PlaySnapSound() => OnSnap();
    public static void PlaySnap() { if(instance) instance.OnSnap(); }
    public bool IsMuted() => isMuted;
    public void ToggleMute()
    {
        isMuted=!isMuted;PlayerPrefs.SetInt("ColorBlocks.Muted",isMuted?1:0);PlayerPrefs.Save();
        ApplyState();GameEvents.InvokeEvent(GameBasicEvent.UpdateAudio,isMuted);
    }
    public void SetSFXVolume(float volume) { sfxVolume=Mathf.Clamp01(volume);ApplyState(); }
    public void SetMusicVolume(float volume) { musicVolume=Mathf.Clamp01(volume);ApplyState(); }
    public void StopBackgroundMusic() { musicSource.Stop(); }
    public void SetAdPaused(bool value) { adPaused=value;ApplyState(); }
    public void SetHostPaused(bool value) { hostPaused=value;ApplyState(); }
    void OnApplicationPause(bool value) { paused=value;ApplyState(); }
    void OnApplicationFocus(bool value) { focused=value;ApplyState(); }
    void OnEnable()
    {
        GameEvents.RegisterBasicEvent(GameBasicEvent.PieceDraggedStart,OnClick);
        GameEvents.RegisterBasicEvent(GameBasicEvent.UIClick,OnClick);
        GameEvents.RegisterBasicEvent(GameBasicEvent.PieceSnapped,OnSnap);
        GameEvents.RegisterBasicEvent(GameBasicEvent.CompleteLevel,OnComplete);
        GameEvents.RegisterBasicEvent(GameBasicEvent.TurnAudio,ToggleMute);
    }
    void OnDisable()
    {
        GameEvents.UnregisterBasicEvent(GameBasicEvent.PieceDraggedStart,OnClick);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.UIClick,OnClick);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.PieceSnapped,OnSnap);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.CompleteLevel,OnComplete);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.TurnAudio,ToggleMute);
    }
    void OnDestroy() { if(instance==this) instance=null; }
}
