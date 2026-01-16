using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIContentUpdate : MonoBehaviour
{
    public Slider progressSlider;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI lookText;

    public Button play;
    public Image levelLock;
    public Image audioIcon;
    public Button prevLevel;
    public Button nextLevel;

    public Button audioButton;
    public Button lookButton;

    public GameObject finishPanel;

    public Sprite[] audioSprites;
    public Sprite[] lockSprites;


    void Awake()
    {
        Init();
    }

    // Start is called before the first frame update
    void Init()
    {
        play?.onClick.AddListener(() =>
        {
            GameEvents.InvokeBasicEvent(GameBasicEvent.Play);
        });

        lookButton?.onClick.AddListener(() =>
        {
            GameEvents.InvokeBasicEvent(GameBasicEvent.Look);
        });

        prevLevel?.onClick.AddListener(() =>
        {
            GameEvents.InvokeBasicEvent(GameBasicEvent.PrevLevel);
        });
        nextLevel?.onClick.AddListener(() =>
        {
            GameEvents.InvokeBasicEvent(GameBasicEvent.NextLevel);
        });

        audioButton?.onClick.AddListener(() =>
        {
            GameEvents.InvokeBasicEvent(GameBasicEvent.TurnAudio);
        });

        finishPanel?.SetActive(false);
        finishPanel?.GetComponent<Button>()?.onClick.AddListener(() =>
        {
            GameEvents.InvokeBasicEvent(GameBasicEvent.NextLevel);
            finishPanel?.SetActive(false);
        });
    }
    
    public void OnEnable()
    {
        GameEvents.RegisterEvent<int, int, LevelUnlockStatus>(GameBasicEvent.UpdateLevel, UpdateLevel);
        GameEvents.RegisterBasicEvent(GameBasicEvent.CompleteLevel, CompleteLevel);
        GameEvents.RegisterEvent<bool>(GameBasicEvent.UpdateAudio, UpdateAudio);
        GameEvents.RegisterBasicEvent(GameBasicEvent.ResetUI, ResetUI);
        GameEvents.RegisterEvent<int>(GameBasicEvent.UpdateLookCount, UpdateLookText);
    }

    public void OnDisable()
    {
        GameEvents.UnregisterEvent<int, int, LevelUnlockStatus>(GameBasicEvent.UpdateLevel, UpdateLevel);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.CompleteLevel, CompleteLevel);
        GameEvents.UnregisterEvent<bool>(GameBasicEvent.UpdateAudio, UpdateAudio);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.ResetUI, ResetUI);
        GameEvents.UnregisterEvent<int>(GameBasicEvent.UpdateLookCount, UpdateLookText);
    }

    private void ResetUI()
    {
        finishPanel.SetActive(false);
    }

    /// <summary>
    /// 更新看题次数文本.
    /// </summary>
    /// <param name="lookCount"></param>
    private void UpdateLookText(int lookCount)
    {
        lookText.text = lookCount > 0 ? $"+{lookCount}" : "";
    }



    private void UpdateLevel(int level, int sumLevel, LevelUnlockStatus unlockStatus)
    {
        Debug.Log($"UpdateLevel: {level}, {sumLevel}, {unlockStatus}");
        progressSlider.value =  (float)level / sumLevel;
        progressText.text = $"{level}/{sumLevel}";

        Sprite sprite;
        if (unlockStatus == LevelUnlockStatus.Current)
        {
            sprite = lockSprites[1];
        }
        else if (unlockStatus == LevelUnlockStatus.Locked)
        {
            sprite = lockSprites[2];
        }
        else
        {
            sprite = lockSprites[0];
        }

        levelLock.sprite = sprite;

    }

    private void CompleteLevel()
    {
        finishPanel?.SetActive(true);
    }

    /// <summary>
    /// 更新音频开关.
    /// </summary>
    private void UpdateAudio(bool isMute)
    {
        audioIcon.sprite = isMute ? audioSprites[0] : audioSprites[1];
    }
}
