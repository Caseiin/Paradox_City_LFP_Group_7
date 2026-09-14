using BetterEventBus;
using UnityEngine;

public class AppleCollectedEvent : IGameplayEvent
{
    public Apple Apple{get;}
    public AppleCollectedEvent(Apple apple) => Apple = apple;
}
