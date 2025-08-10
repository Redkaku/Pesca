using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public class HookController : MonoBehaviour
{
    [Header("Sonido de error")]
    public AudioClip errorSound;

    [Header("Movimiento del hook")]
    public float liftSpeed = 5f;
    public float topY = 5f;
    Vector3 initialPosition;

    bool dragging;
    Fish caughtFish;
    FishSpawner spawner;
    AudioSource audioSource;
    Collider2D hookCollider;

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        var rb = GetComponent<Rigidbody2D>();
        rb.isKinematic = true;

        hookCollider = GetComponent<Collider2D>();
        // Solo detecta peces: en el Inspector
        // Layer Mask de este Collider: marcar solo la capa "Fish"
    }

    void Start()
    {
        initialPosition = transform.position;
        spawner = FindObjectOfType<FishSpawner>();
    }

    void Update()
    {
        if (dragging && caughtFish == null)
        {
            var mp = Input.mousePosition;
            mp.z = -Camera.main.transform.position.z;
            var wp = Camera.main.ScreenToWorldPoint(mp);
            transform.position = new Vector3(wp.x, wp.y, transform.position.z);
        }
        else if (caughtFish != null)
        {
            // sube el pez
            transform.position += Vector3.up * liftSpeed * Time.deltaTime;
            caughtFish.transform.position = transform.position;

            if (transform.position.y >= topY)
            {
                // debug final
                Debug.Log($"[Hook] Delivering → {GetFishInfo(caughtFish)}");

                bool correct = spawner.EvaluateCatch(caughtFish);
                spawner.HandleCapture(caughtFish);
                if (!correct) audioSource.PlayOneShot(errorSound);

                // reset
                caughtFish = null;
                transform.position = initialPosition;
                hookCollider.enabled = true;  // volvemos a activar
            }
        }
        else if (!dragging)
        {
            transform.position = Vector3.Lerp(transform.position, initialPosition, Time.deltaTime * 2f);
        }
    }

    void OnMouseDown() => dragging = true;
    void OnMouseUp()   => dragging = false;

    // Cada frame que el hook esté solapando a un pez...
    void OnTriggerEnter2D(Collider2D other)

    {
        if (!dragging || caughtFish != null) return;
        var f = other.GetComponent<Fish>();
        if (f == null) return;

        caughtFish = f;
        Debug.Log($"[Hook] Hooked → {GetFishInfo(f)}");

        // para no atrapar otro
        hookCollider.enabled = false;
    }

    string GetFishInfo(Fish f)
    {
        if (f.number.HasValue)
            return $"Number={f.number}, Color={f.numberText.color}";
        else if (f.letter != '\0')
            return $"Letter={f.letter}, Color={f.letterText.color}";
        else
            return $"Species={f.species}, Color={f.colorName}";
    }
}
