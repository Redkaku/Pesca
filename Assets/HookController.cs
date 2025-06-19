using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public class HookController : MonoBehaviour
{
    public AudioClip errorSound;
    public float liftSpeed = 5f;
    public float topY = 5f;           // altura en world units donde cae el pez capturado
    public Vector3 initialPosition;   // asigna en Inspector o guarda en Start

    private Camera mainCam;
    private bool dragging = false;
    private Fish caughtFish = null;
    private FishSpawner spawner;
    private AudioSource audioSource;

    void Awake()
    {
        mainCam = Camera.main;
        audioSource = gameObject.AddComponent<AudioSource>();
        // Rigidbody2D kinematic
        var rb = GetComponent<Rigidbody2D>();
        rb.isKinematic = true;
        // Collider2D debe tener isTrigger = true en Inspector
    }

    void Start()
    {
        initialPosition = transform.position;
        // Referencia al spawner:
        spawner = FindObjectOfType<FishSpawner>();
        if (spawner == null)
            Debug.LogWarning("HookController: no encontró FishSpawner en la escena");
    }

    void Update()
    {
        if (dragging && caughtFish == null)
        {
            // Mover con ratón/touch
            Vector3 mp = Input.mousePosition;
            mp.z = -mainCam.transform.position.z;
            Vector3 wp = mainCam.ScreenToWorldPoint(mp);
            transform.position = new Vector3(wp.x, wp.y, transform.position.z);
        }
        else if (caughtFish != null)
        {
            // Subir anzuelo y pez
            Vector3 pos = transform.position;
            pos.y += liftSpeed * Time.deltaTime;
            transform.position = pos;
            caughtFish.transform.position = new Vector3(pos.x, pos.y, caughtFish.transform.position.z);
            if (pos.y >= topY)
{
    Fish f = caughtFish;
    caughtFish = null;

    bool wasCorrect = EvaluateFish(f);
    if (!wasCorrect && errorSound != null)
        audioSource.PlayOneShot(errorSound);

    if (spawner != null)
        spawner.HandleHookCatch(f);

    transform.position = initialPosition;
}
        }
        else
        {
            // Si quieres, regresar anzuelo a initialPosition cuando no dragging ni pez
            if (!dragging && transform.position != initialPosition && caughtFish == null)
            {
                // Lerp o instant reset:
                transform.position = initialPosition;
            }
        }
    }

    void OnMouseDown()
    {
        // Inicia arrastre
        dragging = true;
    }

    void OnMouseUp()
    {
        dragging = false;
    }

    void OnTriggerEnter2D(Collider2D other)
{
    if (caughtFish != null) return;
    Fish f = other.GetComponent<Fish>();
    if (f != null)
    {
        caughtFish = f;
        // No hagas: spawner.activeFish.Remove(f);
        // Solo desactiva collider para no re-enganchar:
        var col = f.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        // Si en Click mode estaba suscrito, quita OnClicked:
        f.OnClicked -= spawner.OnFishClicked;
    }
}

    
    bool EvaluateFish(Fish f)
    {
        // Reutiliza la misma lógica de criterio en FishSpawner
        // Podemos delegar a spawner, o repetir aquí:
        if (spawner == null) return false;
        // Llamamos al mismo método interno de spawner:
        // Por conveniencia, podrías exponer un método en spawner:
        // return spawner.EvaluateCriterion(f);
        // Pero aquí repetimos:
        switch (spawner.currentCriterion)
        {
            case FishSpawner.Criterion.Color:
                return f.colorName == spawner.targetColor;
            case FishSpawner.Criterion.Species:
                return f.species == spawner.targetSpecies;
            case FishSpawner.Criterion.ColorAndSpecies:
                return f.species == spawner.targetSpecies && f.colorName == spawner.targetColor;
            case FishSpawner.Criterion.Letter:
                return f.letter == spawner.targetLetter;
            case FishSpawner.Criterion.Number:
                return f.number == spawner.targetNumber;
            case FishSpawner.Criterion.Shape:
                return f.shapeRenderer.sprite == spawner.targetShape;
            case FishSpawner.Criterion.ShapeAndColor:
                return f.shapeRenderer.sprite == spawner.targetShape && f.colorName == spawner.targetColor;
        }
        return false;
    }
}
