using System.Collections.Generic;
using UnityEngine;

public class ApplePacer
{
    readonly DeployRegistry registry;
    readonly List<DeployerPair> conflictPairs;
    readonly int maxConcurrent;
    readonly float minSpawnInterval;

    float timeSinceLastSpawn;

    public ApplePacer(DeployRegistry registry, List<DeployerPair> conflictPairs, int maxConcurrent, float minSpawnInterval)
    {
        this.registry = registry;
        this.conflictPairs = conflictPairs ?? new List<DeployerPair>();
        this.maxConcurrent = maxConcurrent;
        this.minSpawnInterval = minSpawnInterval;
        timeSinceLastSpawn = minSpawnInterval; // lets the very first spawn happen immediately
    }

    public bool TryGetNextDeployer(float deltaTime, out AppleDeployer deployer)
    {
        deployer = null;
        timeSinceLastSpawn += deltaTime;

        if (timeSinceLastSpawn < minSpawnInterval) return false;
        if (registry.BusyCount >= maxConcurrent) return false;

        foreach (var candidate in registry.GetAllFree())
        {
            if (HasBusyConflict(candidate)) continue;

            deployer = candidate;
            timeSinceLastSpawn = 0f;
            return true;
        }

        return false;
    }

    bool HasBusyConflict(AppleDeployer candidate)
    {
        foreach (var pair in conflictPairs)
        {
            if (pair.a == candidate && pair.b.IsBusy) return true;
            if (pair.b == candidate && pair.a.IsBusy) return true;
        }
        return false;
    }
}
