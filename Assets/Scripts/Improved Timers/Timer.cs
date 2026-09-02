using System;
using UnityEngine;

namespace ImprovedTimers
{
    public abstract class Timer
    {
        
        protected float _initialTime;
        public float CurrentTime{get; protected set;}
        public bool IsRunning{get; protected set;}
        public float Progress => Mathf.Clamp01(CurrentTime/_initialTime);

        public event Action OnTimerStart;
        public event Action OnTimerEnd;

        protected Timer(float initial){
            _initialTime = initial;
        }

        public void Start(){
            CurrentTime = _initialTime;
            if(!IsRunning){
                IsRunning = true;
                OnTimerStart?.Invoke();
            }
        }
        public void Stop(){
            if(IsRunning){
                IsRunning = false;
                OnTimerEnd?.Invoke();
            }
        }
        public abstract void Tick(float deltaTime);
        public void Reset(float newValue){
            _initialTime = newValue;
            Reset();
        }
        public void Reset()=> CurrentTime = _initialTime;
        public void Resume()=> IsRunning = true; 
        public void Pause()=> IsRunning = false;
    }

    public class CountdownTimer{}
    
}
