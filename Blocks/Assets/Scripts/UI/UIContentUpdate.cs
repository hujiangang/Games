using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIContentUpdate : MonoBehaviour
{
    public Slider progressSlider;
    public TextMeshProUGUI progressText;

    public Button play;
    public Image levelLock;
    public Button prevLevel;
    public Button nextLevel;

    public Button audioButton;
    public Button lookButton;
    
    
    // Start is called before the first frame update
    void Start()
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
        
    }
}
