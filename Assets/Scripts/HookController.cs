using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public class HookController : MonoBehaviour
{
    [Header("Sonido de error")]
    public AudioClip errorSound;
    [Header("Movimiento del hook")]
    public float liftSpeed = 5f;
    public float topY = 5f;            // altura world donde procesar captura y reset
    private Vector3 initialPosition;   // posición inicial del hook (sobre superficie)

    [Header("Detección de agua")]
    public Collider2D waterSurfaceTrigger; // Trigger de la superficie

    private bool dragging = false;
    private Fish caughtFish = null;
    private FishSpawner spawner;
    private AudioSource audioSource;
    private bool inWater = false;

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        var rb = GetComponent<Rigidbody2D>();
        rb.isKinematic = true;
    }

    void Start()
    {
        initialPosition = transform.position;
        spawner = FindObjectOfType<FishSpawner>();
        if (spawner == null)
            Debug.LogError("No se encontró FishSpawner en la escena.");
    }

    void Update()
    {
        // 1) Arrastre del hook
        if (dragging && caughtFish == null)
        {
            Vector3 mp = Input.mousePosition;
            mp.z = -Camera.main.transform.position.z;
            Vector3 wp = Camera.main.ScreenToWorldPoint(mp);
            transform.position = new Vector3(wp.x, wp.y, transform.position.z);
        }
        // 2) Si hay pez enganchado, sube hook y pez
        else if (caughtFish != null)
        {
            transform.position += Vector3.up * liftSpeed * Time.deltaTime;
            caughtFish.transform.position = transform.position;

            // 3) Cuando alcance topY, procesar captura
            if (transform.position.y >= topY)
            {
                // HookController.cs, en lugar de usar un EvaluateCatch inexistente
bool wasCorrect = spawner.EvaluateCatch(caughtFish);
spawner.HandleCapture(caughtFish);
if (!wasCorrect)
    audioSource.PlayOneShot(errorSound);


                // Resetear gancho
                caughtFish = null;
                transform.position = initialPosition;
            }
        }
        // 4) Si no hay arrastre ni pez, regresa gancho
        else if (!dragging)
        {
            transform.position = Vector3.Lerp(transform.position, initialPosition, Time.deltaTime * 2f);
        }
    }

    void OnMouseDown()  { dragging = true;  }
    void OnMouseUp()    { dragging = false; }

    void OnTriggerEnter2D(Collider2D other)
{
    // 1) Si entro al trigger de superficie, marco inWater.
    if (other == waterSurfaceTrigger)
    {
        inWater = true;
        return;
    }

    // 2) Detecto peces sin mirar inWater. Solo necesito dragging y que aún no haya pescado.
    if (dragging && caughtFish == null)
    {
        var f = other.GetComponent<Fish>();
        if (f != null)
        {
            caughtFish = f;
            var col = f.GetComponent<Collider2D>();
            if (col) col.enabled = false;
        }
    }
}

void OnTriggerExit2D(Collider2D other)
{
    // Solo limpio el flag de agua
    if (other == waterSurfaceTrigger)
        inWater = false;
}

}
