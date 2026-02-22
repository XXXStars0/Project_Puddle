using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the dynamic spawning of random NPCs at screen edges and Power-ups within the map.
/// Over time, more NPCs are spawned based on the survival duration stored in GameManager.
/// </summary>
public class EntitySpawner : MonoBehaviour
{
    [Header("NPC Spawning")]
    public List<GameObject> npcPrefabs;
    public float baseNPCSpawnInterval = 5f;
    public int baseMaxNPCs = 3;
    public float timeToReachMaxDifficulty = 120f; // Seconds to reach peak difficulty
    public int peakMaxNPCs = 15;
    
    [Header("Power-up Spawning")]
    public List<GameObject> powerUpPrefabs;
    public float minPowerUpInterval = 4f;
    public float maxPowerUpInterval = 8f;

    private int activeNPCs = 0;

    private void Start()
    {
        if (npcPrefabs.Count > 0)
        {
            StartCoroutine(SpawnNPCRoutine());
        }
        else
        {
            Debug.LogWarning("[EntitySpawner] No NPC prefabs assigned!");
        }

        if (powerUpPrefabs.Count > 0)
        {
            StartCoroutine(SpawnPowerUpRoutine());
        }
    }

    // --- NPC Spawner ---
    private IEnumerator SpawnNPCRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(baseNPCSpawnInterval);

            // Calculate difficulty scaling based on survival time
            float survivalTime = GameManager.Instance != null ? GameManager.Instance.survivalTime : 0f;
            float difficultyRatio = Mathf.Clamp01(survivalTime / timeToReachMaxDifficulty);
            int currentMaxNPCs = Mathf.RoundToInt(Mathf.Lerp(baseMaxNPCs, peakMaxNPCs, difficultyRatio));

            if (activeNPCs < currentMaxNPCs)
            {
                SpawnRandomNPC();
            }
        }
    }

    private void SpawnRandomNPC()
    {
        GameObject prefab = npcPrefabs[Random.Range(0, npcPrefabs.Count)];
        Vector3 spawnPos = GetRandomEdgePosition();

        GameObject npc = Instantiate(prefab, spawnPos, Quaternion.identity);
        
        // Track NPC lifecycle
        NPCBase npcScript = npc.GetComponent<NPCBase>();
        if (npcScript != null)
        {
            activeNPCs++;
            // Attach an internal monitor script to decrement counter when the NPC runs off screen and destroys itself
            npc.AddComponent<NPCDeathMonitor>().OnDeath += () => activeNPCs--;
        }
    }

    // Helper script to track NPC destruction safely
    private class NPCDeathMonitor : MonoBehaviour
    {
        public System.Action OnDeath;
        private void OnDestroy() { OnDeath?.Invoke(); }
    }

    private Vector3 GetRandomEdgePosition()
    {
        float x = 0f;
        float y = 0f;

        if (GameManager.Instance == null) return Vector3.zero;
        Bounds bounds = GameManager.Instance.mapBounds;

        // Choose randomly between Left/Right edges or Top/Bottom edges
        if (Random.value > 0.5f)
        {
            // Left or Right
            x = (Random.value > 0.5f) ? bounds.min.x - 2f : bounds.max.x + 2f;
            y = Random.Range(bounds.min.y, bounds.max.y);
        }
        else
        {
            // Top or Bottom
            x = Random.Range(bounds.min.x, bounds.max.x);
            y = (Random.value > 0.5f) ? bounds.min.y - 2f : bounds.max.y + 2f;
        }

        return new Vector3(x, y, 0f);
    }

    // --- Power-up Spawner ---
    private IEnumerator SpawnPowerUpRoutine()
    {
        while (true)
        {
            // Interval increases linearly from minPowerUpInterval (4s) to maxPowerUpInterval (8s) over time
            float survivalTime = GameManager.Instance != null ? GameManager.Instance.survivalTime : 0f;
            float ratio = Mathf.Clamp01(survivalTime / timeToReachMaxDifficulty);
            float waitTime = Mathf.Lerp(minPowerUpInterval, maxPowerUpInterval, ratio);
            waitTime = Mathf.Max(1f, Random.Range(waitTime - 1f, waitTime + 1f)); // ±1s random

            yield return new WaitForSeconds(waitTime);

            SpawnRandomPowerUp();
        }
    }

    private void SpawnRandomPowerUp()
    {
        if (GameManager.Instance == null) return;
        Bounds bounds = GameManager.Instance.mapBounds;

        // Spawn 1 to 3 powerups simultaneously (majority will be Water based on the 75% logic below)
        int dropsToSpawn = Random.Range(1, 4);

        for (int i = 0; i < dropsToSpawn; i++)
        {
            // Assumes index 0 is Water, others are different powerups. 
            // 75% chance to drop Water, 25% chance to drop others like Speed.
            int prefabIndex = 0;
            if (powerUpPrefabs.Count > 1)
            {
                prefabIndex = Random.value <= 0.75f ? 0 : Random.Range(1, powerUpPrefabs.Count);
            }

            GameObject prefab = powerUpPrefabs[prefabIndex];
            
            // Spawn strictly inside the map
            float rx = Random.Range(bounds.min.x + 1f, bounds.max.x - 1f);
            float ry = Random.Range(bounds.min.y + 1f, bounds.max.y - 1f);
            Vector3 spawnPos = new Vector3(rx, ry, 0f);

            Instantiate(prefab, spawnPos, Quaternion.identity);
        }
    }
}
