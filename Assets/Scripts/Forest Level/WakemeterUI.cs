using UnityEngine;
using UnityEngine.UIElements;

public class WakemeterUI
{
    readonly ProgressBar progress;
    readonly int disturbAmount;
    readonly float lerpSpeed;

    float targetValue;
    IVisualElementScheduledItem scheduledItem;

    public WakemeterUI(ProgressBar progress, int disturb, int maxLimit, float lerpSpeed = 8f)
    {
        this.progress = progress;
        this.progress.lowValue = 0;
        this.progress.highValue = maxLimit;
        disturbAmount = disturb;
        this.lerpSpeed = lerpSpeed;
        targetValue = progress.value;
    }

    public void Disturb()
    {
        targetValue = Mathf.Min(targetValue + disturbAmount, progress.highValue);

        scheduledItem?.Pause(); // don't stack a second ticking animation on top
        scheduledItem = progress.schedule
            .Execute(AnimateStep)
            .Every(16)
            .Until(() => Mathf.Approximately(progress.value, targetValue));
    }

    void AnimateStep()
    {
        progress.value = Mathf.Lerp(progress.value, targetValue, lerpSpeed * 0.016f);
    }
}