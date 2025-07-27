using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviour
{
    [Header("UI")]
    public GameObject pausePanel;          // Tu Canvas o panel de pausa
    public GameObject blockingObject;      // Si está activo, no se podrá pausar

    private bool isPaused = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Solo alterna si no hay un bloqueo activo
            if (blockingObject == null || !blockingObject.activeInHierarchy)
                TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        // Mostrar/ocultar panel
        pausePanel.SetActive(isPaused);

        // Pausar o reanudar el tiempo
        Time.timeScale = isPaused ? 0f : 1f;

        // Silenciar (o reactivar) todos los AudioSources de la escena
        // excepto los que en su AudioSource tengan ignoreListenerPause = true
        AudioListener.pause = isPaused;
    }

    // Estos métodos puedes enlazarlos a botones en el Canvas de pausa:

    public void Resume()
    {
        if (isPaused) TogglePause();
    }

    public void QuitToMenu()
    {
        // Asegurarnos de volver a normalizar el tiempo y el audio
        if (isPaused) TogglePause();
        SceneManager.LoadScene("MenuP");
    }
}
