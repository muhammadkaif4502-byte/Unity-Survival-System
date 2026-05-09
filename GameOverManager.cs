using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages Game Over screen and functionality
/// Attach to a Canvas or UI GameObject
/// </summary>
public class GameOverManager : MonoBehaviour
{
    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public Text gameOverText;
    public Button restartButton;
    public Button quitButton;
    
    [Header("Settings")]
    public string gameOverMessage = "GAME OVER";
    public float showDelay = 1f;
    public bool pauseGameOnGameOver = true;
    
    private static GameOverManager instance;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }
    }
    
    /// <summary>
    /// Call this to show Game Over screen
    /// </summary>
    public static void ShowGameOver()
    {
        if (instance != null)
        {
            instance.StartCoroutine(instance.ShowGameOverScreen());
        }
    }
    
    System.Collections.IEnumerator ShowGameOverScreen()
    {
        yield return new WaitForSeconds(showDelay);
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        if (gameOverText != null)
        {
            gameOverText.text = gameOverMessage;
        }
        
        if (pauseGameOnGameOver)
        {
            Time.timeScale = 0f;
        }
        
        Debug.Log(">>> GAME OVER SCREEN SHOWN <<<");
    }
    
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}