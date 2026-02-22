using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntitySpawner : MonoBehaviour
{
    [Header("NPC Spawning")]
    public List<GameObject> npcPrefabs;
    public float baseNPCSpawnInterval = 5f;
    public int baseMaxNPCs = 3;
    public float timeToReachMaxDifficulty = 120f;
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
            // Debug.LogWarning("[EntitySpawner] No NPC prefabs assigned!");
        }

        if (powerUpPrefabs.Count > 0)
        {
            StartCoroutine(SpawnPowerUpRoutine());
        }
    }

    private IEnumerator SpawnNPCRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(baseNPCSpawnInterval);

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
        
        NPCBase npcScript = npc.GetComponent<NPCBase>();
        if (npcScript != null)
        {
            activeNPCs++;
            npc.AddComponent<NPCDeathMonitor>().OnDeath += () => activeNPCs--;
        }
    }

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

        if (Random.value > 0.5f)
        {
            x = (Random.value > 0.5f) ? bounds.min.x - 2f : bounds.max.x + 2f;
            y = Random.Range(bounds.min.y, bounds.max.y);
        }
        else
        {
            x = Random.Range(bounds.min.x, bounds.max.x);
            y = (Random.value > 0.5f) ? bounds.min.y - 2f : bounds.max.y + 2f;
        }

        return new Vector3(x, y, 0f);
    }

    private IEnumerator SpawnPowerUpRoutine()
    {
        while (true)
        {
            float survivalTime = GameManager.Instance != null ? GameManager.Instance.survivalTime : 0f;
            float ratio = Mathf.Clamp01(survivalTime / timeToReachMaxDifficulty);
            float waitTime = Mathf.Lerp(minPowerUpInterval, maxPowerUpInterval, ratio);
            waitTime = Mathf.Max(1f, Random.Range(waitTime - 1f, waitTime + 1f));

            yield return new WaitForSeconds(waitTime);

            SpawnRandomPowerUp();
        }
    }

    private void SpawnRandomPowerUp()
    {
        if (GameManager.Instance == null) return;
        Bounds bounds = GameManager.Instance.mapBounds;

        int dropsToSpawn = Random.Range(1, 4);

        for (int i = 0; i < dropsToSpawn; i++)
        {
            int prefabIndex = 0;
            if (powerUpPrefabs.Count > 1)
            {
                prefabIndex = Random.value <= 0.75f ? 0 : Random.Range(1, powerUpPrefabs.Count);
            }

            GameObject prefab = powerUpPrefabs[prefabIndex];
            
            float rx = Random.Range(bounds.min.x + 1f, bounds.max.x - 1f);
            float ry = Random.Range(bounds.min.y + 1f, bounds.max.y - 1f);
            Vector3 spawnPos = new Vector3(rx, ry, 0f);

            Instantiate(prefab, spawnPos, Quaternion.identity);
        }
    }
}
