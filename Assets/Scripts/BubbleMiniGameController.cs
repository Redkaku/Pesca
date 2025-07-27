// BubbleMiniGameController.cs
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class BubbleMiniGameController : MonoBehaviour
{
    [Header("Configuración de burbujas")]
    public GameObject      bubblePrefab;    // Debe llevar BubbleMovement + Bubble
    public float           spawnInterval = 0.5f;
    public int             maxBubbles     = 30;
    [Header("Botones Finales")]
public Button continueButton;   // para siguiente nivel
public Button menuButton;       // para volver al menú principal


    [Header("Sprites de peces (opcional)")]
    public Sprite[]        fishSprites;

    [Header("Temporizador")]
    public float           gameDuration = 30f;

    [Header("UI (TextMeshProUGUI)")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText;

    [Header("Sonidos")]
    public AudioClip       bgMusic;         // Loop de fondo
    public AudioClip       popSound;        // Al reventar

    [Header("Panel de fin")]
    public GameObject      endPanel;        // Panel con botones Next/Menu
    public float           endDelay = 2f;   // Segundos antes de mostrar el panel
    public UnityEvent<bool> OnMiniGameFinished;
    [Header("UI de burbujas")]
public GameObject bubbleCanvas;

    // Estado interno
    float                 remainingTime;
    bool                  running;
    int                   score;
    AudioSource           audioSource;
    Coroutine             spawnRoutine, timerRoutine;
    List<GameObject>      activeBubbles = new List<GameObject>();
    float                 prefabZ;
    Camera                cam;
    const float           margin = 0.05f; // 5% de margen en los bordes

    void Awake()
    {
        cam = Camera.main;
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        prefabZ = bubblePrefab.transform.position.z;

        if (OnMiniGameFinished == null)
            OnMiniGameFinished = new UnityEvent<bool>();

        if (endPanel != null)
            endPanel.SetActive(false);
        continueButton.onClick.AddListener(() =>
    {
        // incrementa nivel
        GameSettings.LevelIndex++;
        // carga la escena de pesca que guardamos
        SceneManager.LoadScene(GameSettings.NextFishScene);
    });
    menuButton.onClick.AddListener(() =>
        SceneManager.LoadScene("MenuP")
    );
    }

    void Start()
    {
        // Arranca automáticamente al cargar la escena
        StartMiniGame();
    }
    IEnumerator ShowEndPanelAfterDelay()
{
    yield return new WaitForSeconds(endDelay);

    // 1) Ocultamos la UI de juego
    if (bubbleCanvas != null)
        bubbleCanvas.SetActive(false);

    // 2) Ajustamos botones según nivel
    bool isLastLevel = GameSettings.LevelIndex >= 2;
    continueButton.gameObject.SetActive(!isLastLevel);
    menuButton.gameObject.SetActive(true);

    // 3) Mostramos el panel final
    if (endPanel != null)
        endPanel.SetActive(true);
}


    public void StartMiniGame()
    {
        // Reiniciar
        running = true;
        remainingTime = gameDuration;
        score = 0;
        ClearBubbles();
        if (endPanel != null) endPanel.SetActive(false);

        // Música de fondo
        if (bgMusic != null)
        {
            audioSource.clip = bgMusic;
            audioSource.Play();
        }

        UpdateTimerUI();
        UpdateScoreUI();

        spawnRoutine = StartCoroutine(SpawnLoop());
        timerRoutine = StartCoroutine(CountdownLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (running)
        {
            if (activeBubbles.Count < maxBubbles)
                SpawnOne();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnOne()
    {
        // Elige una posición aleatoria en viewport con margen
        Vector3 vp = new Vector3(
            Random.Range(margin, 1f - margin),
            Random.Range(margin, 1f - margin),
            cam.nearClipPlane + Mathf.Abs(prefabZ - cam.transform.position.z)
        );
        Vector3 worldPos = cam.ViewportToWorldPoint(vp);
        worldPos.z = prefabZ;

        var go = Instantiate(bubblePrefab, worldPos, Quaternion.identity, transform);

        // Asignar sprite de pez aleatorio
        if (fishSprites != null && fishSprites.Length > 0)
        {
            SpriteRenderer fishRenderer = null;
            var child = go.transform.Find("FishSprite");
            if (child != null)
                fishRenderer = child.GetComponent<SpriteRenderer>();
            if (fishRenderer == null)
            {
                var renderers = go.GetComponentsInChildren<SpriteRenderer>();
                if (renderers.Length > 1)
                    fishRenderer = renderers[renderers.Length - 1];
            }
            if (fishRenderer != null)
                fishRenderer.sprite = fishSprites[Random.Range(0, fishSprites.Length)];
        }

        // Suscribir al pop
        var bubble = go.GetComponent<Bubble>();
if (bubble != null)
{
    bubble.OnPopped.AddListener(b =>
    {
        if (!running) return;
        if (popSound != null && GameSettings.SpecialEffectsEnabled)
            audioSource.PlayOneShot(popSound);

        activeBubbles.Remove(b.gameObject);
        score++;
        UpdateScoreUI();
        StartCoroutine(DelayedSpawn());
    });
}

        activeBubbles.Add(go);
    }

    IEnumerator DelayedSpawn()
    {
        yield return new WaitForSeconds(spawnInterval);
        if (running && activeBubbles.Count < maxBubbles)
            SpawnOne();
    }

    IEnumerator CountdownLoop()
    {
        while (running && remainingTime > 0f)
        {
            remainingTime -= Time.deltaTime;
            UpdateTimerUI();
            yield return null;
        }
        FinishMiniGame();
    }

    void FinishMiniGame()
    {
        if (!running) return;
        running = false;
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        if (timerRoutine != null) StopCoroutine(timerRoutine);
        audioSource.Stop();
        ClearBubbles();
        OnMiniGameFinished.Invoke(true);

        // Mostrar panel final tras un delay
        StartCoroutine(ShowEndPanelAfterDelay());
    }



    void ClearBubbles()
    {
        foreach (var b in activeBubbles) if (b != null) Destroy(b);
        activeBubbles.Clear();
    }

    void UpdateTimerUI()
    {
        if (timerText != null)
            timerText.text = Mathf.CeilToInt(remainingTime).ToString();
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = score.ToString();
    }
}
