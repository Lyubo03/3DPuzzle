using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI")]
    public Text progressText;
    public GameObject winPanel;
    public ParticleSystem confettiEffect;

    [Header("Settings")]
    public int totalPieces = 6;

    private int placedCount = 0;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        if (winPanel != null)
            winPanel.SetActive(false);

        UpdateUI();
    }

    public void PiecePlaced()
    {
        placedCount++;
        UpdateUI();

        if (placedCount >= totalPieces)
        {
            Win();
        }
    }

    void UpdateUI()
    {
        if (progressText != null)
            progressText.text = $"Placed: {placedCount}/{totalPieces}";
    }

    void Win()
    {
        if (winPanel != null)
            winPanel.SetActive(true);

        if (confettiEffect != null)
            confettiEffect.Play();
    }

    public void ResetGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
