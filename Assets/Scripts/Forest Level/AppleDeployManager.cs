using UnityEngine;
using BetterPooling;
using BetterSingletons;
/// <summary>
/// Deploys apples in different locations to fall and manages all active apples via pooling
/// </summary>

public class AppleDeployManager : Singleton<AppleDeployManager>
{
    [SerializeField] Apple ApplePrefab;
    [SerializeField] int defaultCapacity = 10;
    [SerializeField] int maxSize = 10;

    ISingleObjectPool<Apple> applePool;

    protected override void Awake()
    {
        base.Awake();
        applePool = new SingleObjectPool<Apple>(prefab: ApplePrefab,defaultCapacity: defaultCapacity,maxSize: maxSize);
    }

    public void ReturnToPool(Apple apple) => applePool.Release(apple);
    public void DeployAt(Vector3 position){
        var apple = applePool.Get();
        apple.transform.position = position;
        apple.StartFalling();
    }
    

}
