using UnityEngine;

public class CountdownTimer : Timer
{
    public CountdownTimer(float value) : base(value)
    {
        CurrentTime = value;
    }


    public override bool IsFinished => CurrentTime <= 0;

    public override void Tick(){}

    public override void Tick(float deltaTime)
    {
        if(!IsRunning || IsFinished) return;
        CurrentTime -= deltaTime;

        if(IsFinished) Stop();
    }
}
