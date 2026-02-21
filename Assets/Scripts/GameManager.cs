using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement; // Needed for Restart/ReturnToMenu

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Menu, Playing, Paused, GameOver }
    public GameState currentState { get; private set; } = GameState.Menu;
    public static bool autoStartNextGame = false; // Used for seamless Restart

    [Header("Level & Spawning Rules")]
    [Tooltip("Visual boundaries for Camera, Player, and random Spawns")]
    public Bounds mapBounds = new Bounds(Vector3.zero, new Vector3(30f, 20f, 0f));
    [Tooltip("Drag your Cloud Prefab here to spawn it dynamically")]
    public GameObject playerPrefab;
    public Transform PlayerTransform { get; private set; }

    [Header("Input Settings")]
    public string pauseInputButton = "Cancel";

    [Header("Global Mood / Health")]
    public float maxMood = 100f;
    [Tooltip("Starting mood is not full as per design")]
    public float startingMood = 50f;
    public float minMood = 0f;
    [Tooltip("How much mood is lost naturally per second")]
    public float moodDecayRate = 1.5f;
    
    [SerializeField] private float currentMood;

    [Header("Statistics / Scoring")]
    public float survivalTime = 0f;
    public int totalNPCsSpawned = 0;
    public int satisfiedNPCs = 0;

    [Header("HUD Events (Runtime)")]
    public UnityEvent<float> OnMoodChanged; // Passes ratio 0-1 for UI sliders
    public UnityEvent<string> OnTimeUpdated; // MM:SS string
    public UnityEvent<string> OnNPCStatsUpdated; // "Happy / Total"
    public UnityEvent<string> OnHighscoreUpdated; // Best Time / NPCs
    
    [Tooltip("Push formatted sentences like 'You survived 2 mins...' to GameOver UI Panel")]
    public UnityEvent<string> OnGameOverSummary;

    [Header("State Machine UI Events (Panels)")]
    [Tooltip("Hook up your Main Menu Canvas Panel here via SetActive(true/false)")]
    public UnityEvent OnStateMenu;
    [Tooltip("Hook up your HUD Canvas Panel here via SetActive(true/false)")]
    public UnityEvent OnStatePlaying;
    [Tooltip("Hook up your Pause Canvas Panel here via SetActive(true/false)")]
    public UnityEvent OnStatePaused;
    [Tooltip("Hook up your GameOver Canvas Panel here via SetActive(true/false)")]
    public UnityEvent OnStateGameOver;

    [Header("Sub-Menu UI Events (Overlays)")]
    public UnityEvent OnSettingsOpened;
    public UnityEvent OnSettingsClosed;
    public UnityEvent OnGuideOpened;
    public UnityEvent OnGuideClosed;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateHighscoreUI();
        
        if (autoStartNextGame)
        {
            autoStartNextGame = false;
            StartGame();
        }
        else
        {
            ChangeState(GameState.Menu);
        }
    }

    private void Update()
    {
        if (currentState == GameState.Playing)
        {
            survivalTime += Time.deltaTime;
            ModifyMood(-moodDecayRate * Time.deltaTime);

            // Update Time UI seamlessly
            int minutes = Mathf.FloorToInt(survivalTime / 60);
            int seconds = Mathf.FloorToInt(survivalTime % 60);
            OnTimeUpdated?.Invoke(string.Format("{0:00}:{1:00}", minutes, seconds));

            // Dynamic Button checking
            if (Input.GetButtonDown(pauseInputButton))
            {
                PauseGame();
            }
        }
        else if (currentState == GameState.Paused)
        {
            if (Input.GetButtonDown(pauseInputButton))
            {
                ResumeGame();
            }
        }
    }

    // --- State Machine & Flow Control ---
    public void ChangeState(GameState newState)
    {
        currentState = newState;
        switch (newState)
        {
            case GameState.Menu:
                Time.timeScale = 0f; // Freeze everything
                OnStateMenu?.Invoke();
                break;
            case GameState.Playing:
                Time.timeScale = 1f; // Run normally
                OnStatePlaying?.Invoke();
                break;
            case GameState.Paused:
                Time.timeScale = 0f; // Pause physics/updates
                OnStatePaused?.Invoke();
                break;
            case GameState.GameOver:
                Time.timeScale = 0f; // Freeze game
                OnStateGameOver?.Invoke();
                
                // Formulate the summary string for the current run
                int m = Mathf.FloorToInt(survivalTime / 60);
                int s = Mathf.FloorToInt(survivalTime % 60);
                string summary = $"You survived {m:00}:{s:00} with {satisfiedNPCs} Happy NPCs!";
                OnGameOverSummary?.Invoke(summary);

                CheckAndSaveHighscore();
                break;
        }
    }

    // --- Public Button Interfaces ---
    public void StartGame()
    {
        // Handle Dynamic Player Spawning
        if (PlayerTransform != null)
        {
            Destroy(PlayerTransform.gameObject);
        }

        if (playerPrefab != null)
        {
            GameObject newPlayer = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            PlayerTransform = newPlayer.transform;
            Debug.Log("[GameManager] Player spawned successfully.");
        }
        else
        {
            Debug.LogWarning("[GameManager] No Player Prefab assigned! Make sure to assign the Cloud Prefab.");
        }

        // Reset all metrics for a fresh run
        currentMood = startingMood;
        survivalTime = 0f;
        totalNPCsSpawned = 0;
        satisfiedNPCs = 0;
        
        UpdateUI();
        UpdateNPCStatsUI();

        ChangeState(GameState.Playing);
    }

    public void PauseGame()
    {
        if (currentState == GameState.Playing)
            ChangeState(GameState.Paused);
    }

    public void ResumeGame()
    {
        if (currentState == GameState.Paused)
            ChangeState(GameState.Playing);
    }

    public void ReturnToMenu()
    {
        // Simplest way to clean up all dynamically spawned items/puddles/NPCs is to reload the active scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void RestartGame()
    {
        autoStartNextGame = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        Debug.Log("[GameManager] Quitting Game...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OpenSettings() { OnSettingsOpened?.Invoke(); }
    public void CloseSettings() { OnSettingsClosed?.Invoke(); }
    public void OpenGuide() { OnGuideOpened?.Invoke(); }
    public void CloseGuide() { OnGuideClosed?.Invoke(); }
    
    // --- Existing Mechanics ---
    public void ModifyMood(float amount)
    {
        if (currentState != GameState.Playing) return;

        currentMood = Mathf.Clamp(currentMood + amount, minMood, maxMood);
        UpdateUI();

        if (currentMood <= minMood)
        {
            ChangeState(GameState.GameOver);
        }
    }

    private void UpdateUI()
    {
        float ratio = (currentMood - minMood) / (maxMood - minMood);
        OnMoodChanged?.Invoke(ratio);
    }

    public void RegisterNPCSpawn()
    {
        totalNPCsSpawned++;
        UpdateNPCStatsUI();
    }

    public void RegisterNPCSatisfied()
    {
        satisfiedNPCs++;
        if (satisfiedNPCs > totalNPCsSpawned) satisfiedNPCs = totalNPCsSpawned;
        UpdateNPCStatsUI();
    }

    private void UpdateNPCStatsUI()
    {
        OnNPCStatsUpdated?.Invoke($"{satisfiedNPCs} / {totalNPCsSpawned}");
    }

    // --- Highscore Logic ---
    private void CheckAndSaveHighscore()
    {
        float bestTime = PlayerPrefs.GetFloat("Highscore_Time", 0f);
        int bestSatisfied = PlayerPrefs.GetInt("Highscore_NPCs", 0);

        bool newRecord = false;

        if (survivalTime > bestTime)
        {
            PlayerPrefs.SetFloat("Highscore_Time", survivalTime);
            bestTime = survivalTime;
            newRecord = true;
        }
        if (satisfiedNPCs > bestSatisfied)
        {
            PlayerPrefs.SetInt("Highscore_NPCs", satisfiedNPCs);
            bestSatisfied = satisfiedNPCs;
            newRecord = true;
        }

        if (newRecord)
        {
            PlayerPrefs.Save(); // Ensures it writes to disk immediately
        }

        UpdateHighscoreUI();
    }

    private void UpdateHighscoreUI()
    {
        float bestTime = PlayerPrefs.GetFloat("Highscore_Time", 0f);
        int bestSatisfied = PlayerPrefs.GetInt("Highscore_NPCs", 0);

        int m = Mathf.FloorToInt(bestTime / 60);
        int s = Mathf.FloorToInt(bestTime % 60);
        
        OnHighscoreUpdated?.Invoke($"BEST TIME: {m:00}:{s:00}\nHAPPY NPCs: {bestSatisfied}");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(mapBounds.center, mapBounds.size);
    }
}
