using BetterSingletons;
using UnityEngine;
using UnityEngine.UIElements;

public class WakeManager :Singleton<WakeManager> 
{
    [SerializeField] UIDocument document;
    [SerializeField] int WakeUpLimit;
    [SerializeField] int disturbAmount;
    [SerializeField] float lerpSpeed = 8 ;

    WakemeterUI wakemeter;

    protected override void Awake()
    {
        var progress = document.rootVisualElement.Q<ProgressBar>("wake-progressbar");
        wakemeter = new (progress,disturbAmount,WakeUpLimit,lerpSpeed);
    }

    public void InterruptSleep(){
        wakemeter.Disturb();
    }

    // Add animating for the progress bar
}
