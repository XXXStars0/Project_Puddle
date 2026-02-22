using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement; 

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Menu, Playing, Paused, GameOver }
    public GameState currentState { get; private set; } = GameState.Menu;
    public static bool autoStartNextGame = false;

    [Header("Level & Spawning")]
    [Tooltip("Visual boundaries")]
    public Bounds mapBounds = new Bounds(Vector3.zero, new Vector3(30f, 20f, 0f));
    [Tooltip("Cloud Prefab")]
    public GameObject playerPrefab;
    public Transform PlayerTransform { get; private set; }

    [Header("Input (Gamepad/Keyboard)")]
    [Tooltip("Pause/resume input")]
    public string pauseInputButton = "Pause";
    [Tooltip("Menu close input")]
    public string cancelInputButton = "Cancel";

    [Header("UI Navigation")]
    [Tooltip("Main Menu selection")]
    public GameObject firstSelectedMenuButton;
    [Tooltip("Pause Menu selection")]
    public GameObject firstSelectedPauseButton;
    [Tooltip("Game Over selection")]
    public GameObject firstSelectedGameOverButton;

    private GameObject lastSelectedUI;

    [Header("Health/Mood")]
    public float maxMood = 100f;
    public float startingMood = 50f;
    public float minMood = 0f;
    [Tooltip("Mood lost per second")]
    public float moodDecayRate = 1.5f;
    [Tooltip("Decay scaling over time")]
    [Range(0f, 0.1f)]
    public float moodDecayScalePerSecond = 0.01f;
    
    [SerializeField] private float currentMood;

    [Header("Statistics")]
    public float survivalTime = 0f;
    public int totalNPCsSpawned = 0;
    public int satisfiedNPCs = 0;

    [Header("HUD Events")]
    public UnityEvent<float> OnMoodChanged;
    public UnityEvent<string> OnTimeUpdated;
    public UnityEvent<string> OnNPCStatsUpdated;
    public UnityEvent<string> OnHighscoreUpdated;
    
    [Tooltip("Summary for GameOver UI")]
    public UnityEvent<string> OnGameOverSummary;

    [Header("Panels")]
    public UnityEvent OnStateMenu;
    public UnityEvent OnStatePlaying;
    public UnityEvent OnStatePaused;
    public UnityEvent OnStateGameOver;

    [Header("Overlays")]
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
        if (currentState != GameState.Playing && UnityEngine.EventSystems.EventSystem.current != null)
        {
            GameObject currentSel = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
            if (currentSel != null && currentSel.activeInHierarchy)
            {
                lastSelectedUI = currentSel;
            }
            else if (currentSel == null && lastSelectedUI != null && lastSelectedUI.activeInHierarchy)
            {
                if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f)
                {
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(lastSelectedUI);
                }
            }
        }

        if (currentState == GameState.Playing)
        {
            survivalTime += Time.deltaTime;
            float effectiveDecayRate = moodDecayRate * (1f + survivalTime * moodDecayScalePerSecond);
            ModifyMood(-effectiveDecayRate * Time.deltaTime);

            int minutes = Mathf.FloorToInt(survivalTime / 60);
            int seconds = Mathf.FloorToInt(survivalTime % 60);
            OnTimeUpdated?.Invoke(string.Format("{0:00}:{1:00}", minutes, seconds));

            if (Input.GetButtonDown(pauseInputButton))
            {
                PauseGame();
            }
        }
        else if (currentState == GameState.Paused)
        {
            if (Input.GetButtonDown(pauseInputButton) || Input.GetButtonDown(cancelInputButton))
            {
                ResumeGame();
            }
        }
    }

    public void ChangeState(GameState newState)
    {
        currentState = newState;

        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }

        switch (newState)
        {
            case GameState.Menu:
                if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic(AudioManager.Instance.titleBGM);
                Time.timeScale = 0f;
                OnStateMenu?.Invoke();
                SetSelectedUIObject(firstSelectedMenuButton);
                break;
            case GameState.Playing:
                if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic(AudioManager.Instance.gameBGM);
                Time.timeScale = 1f;
                OnStatePlaying?.Invoke();
                break;
            case GameState.Paused:
                if (AudioManager.Instance != null) AudioManager.Instance.PlayPause();
                Time.timeScale = 0f;
                OnStatePaused?.Invoke();
                SetSelectedUIObject(firstSelectedPauseButton);
                break;
            case GameState.GameOver:
                if (AudioManager.Instance != null) AudioManager.Instance.PlayGameOverSequence();
                Time.timeScale = 0f;
                OnStateGameOver?.Invoke();
                
                int m = Mathf.FloorToInt(survivalTime / 60);
                int s = Mathf.FloorToInt(survivalTime % 60);
                string summary = $"You kept the fun going for {m:00}:{s:00},\nand made {satisfiedNPCs} children happy!";
                OnGameOverSummary?.Invoke(summary);

                CheckAndSaveHighscore();
                break;
        }
    }

    private void SetSelectedUIObject(GameObject obj)
    {
        if (obj == null) return;
        StartCoroutine(SelectUIObjectNextFrame(obj));
    }

    private System.Collections.IEnumerator SelectUIObjectNextFrame(GameObject obj)
    {
        yield return null; 
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(obj);
            lastSelectedUI = obj;
        }
    }

    public void StartGame()
    {
        if (PlayerTransform != null)
        {
            Destroy(PlayerTransform.gameObject);
        }

        if (playerPrefab != null)
        {
            GameObject newPlayer = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            PlayerTransform = newPlayer.transform;
            // Debug.Log("[GameManager] Player spawned successfully.");
        }
        else
        {
            // Debug.LogWarning("[GameManager] No Player Prefab assigned!");
        }

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
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void RestartGame()
    {
        autoStartNextGame = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        // Debug.Log("[GameManager] Quitting Game...");
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
        OnNPCStatsUpdated?.Invoke($"Happy: {satisfiedNPCs}\nTotal: {totalNPCsSpawned}");
    }

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
            PlayerPrefs.Save();
        }

        UpdateHighscoreUI();
    }

    private void UpdateHighscoreUI()
    {
        float bestTime = PlayerPrefs.GetFloat("Highscore_Time", 0f);
        int bestSatisfied = PlayerPrefs.GetInt("Highscore_NPCs", 0);

        int m = Mathf.FloorToInt(bestTime / 60);
        int s = Mathf.FloorToInt(bestTime % 60);
        
        OnHighscoreUpdated?.Invoke($"BEST TIME: {m:00}:{s:00}\nHAPPY Children: {bestSatisfied}");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(mapBounds.center, mapBounds.size);
    }
}
