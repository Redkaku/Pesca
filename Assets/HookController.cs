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

    [Header("Parámetros de cámara")]
    public Camera mainCam;             // asignar o dejar null para Camera.main
    public float depthLimit = 5f;      // cuánto puede bajar la cámara desde su y inicial al arrastrar
    public float camFollowSpeed = 5f;  // velocidad de Lerp al seguir camera
    public float camReturnSpeed = 2f;  // velocidad de Lerp al regresar cámara

    [Header("Detección de agua")]
    public Collider2D waterSurfaceTrigger; // Collider2D (IsTrigger) colocado en la línea de superficie
    // Alternativamente, podrías usar tag: 
    // public string waterSurfaceTag = "WaterSurface";

    private bool dragging = false;
    private Fish caughtFish = null;
    private FishSpawner spawner;
    private AudioSource audioSource;
    private Vector3 camInitialPos;
    private float surfaceY;            // y de superficie (posición de cámara inicial)
    private float camMinY;             // y mínima de cámara (surfaceY - depthLimit)
    private bool inWater = false;      // true si hook está dentro del agua (bajo superficie)

    void Awake()
    {
        if (mainCam == null)
            mainCam = Camera.main;
        audioSource = gameObject.AddComponent<AudioSource>();
        var rb = GetComponent<Rigidbody2D>();
        rb.isKinematic = true;
        // Asegurar que el Collider2D del hook esté marcado isTrigger = true en Inspector
    }

    void Start()
    {
        initialPosition = transform.position;
        spawner = FindObjectOfType<FishSpawner>();
        if (mainCam != null)
        {
            camInitialPos = mainCam.transform.position;
            surfaceY = camInitialPos.y;
            camMinY = surfaceY - depthLimit;
        }
        else
        {
            Debug.LogWarning("HookController: mainCam no asignada y no se encontró Camera.main.");
            surfaceY = transform.position.y;
            camInitialPos = new Vector3(0, surfaceY, -10f);
            camMinY = surfaceY - depthLimit;
        }
    }

    void Update()
    {
        // 1) Arrastre del hook (solo si no tiene pez enganchado)
        if (dragging && caughtFish == null)
        {
            Vector3 mp = Input.mousePosition;
            mp.z = -mainCam.transform.position.z;
            Vector3 wp = mainCam.ScreenToWorldPoint(mp);
            transform.position = new Vector3(wp.x, wp.y, transform.position.z);

            // Ajustar cámara si baja por debajo de initialPosition.y
            float hookY = transform.position.y;
            if (hookY < initialPosition.y)
            {
                // Solo seguir entre surfaceY y camMinY
                float desiredCamY = Mathf.Clamp(hookY, camMinY, surfaceY);
                Vector3 cp = mainCam.transform.position;
                Vector3 targetCamPos = new Vector3(cp.x, desiredCamY, cp.z);
                mainCam.transform.position = Vector3.Lerp(cp, targetCamPos, Time.deltaTime * camFollowSpeed);
            }
            else
            {
                // Si aún sobre superficie, regresar cámara hacia surfaceY
                Vector3 cp = mainCam.transform.position;
                Vector3 targetCamPos = new Vector3(cp.x, surfaceY, cp.z);
                mainCam.transform.position = Vector3.Lerp(cp, targetCamPos, Time.deltaTime * camReturnSpeed);
            }
        }
        // 2) Si hay pez enganchado, subir hook+pez y que la cámara suba también hasta surface
        else if (caughtFish != null)
        {
            // Subir hook y pez
            Vector3 pos = transform.position;
            pos.y += liftSpeed * Time.deltaTime;
            transform.position = pos;
            caughtFish.transform.position = new Vector3(pos.x, pos.y, caughtFish.transform.position.z);

            // Cámara sigue hacia arriba hasta surfaceY
            if (mainCam != null)
            {
                Vector3 cp = mainCam.transform.position;
                // Deseado: subir cámara hasta surfaceY, pero no más arriba
                float desiredCamY = Mathf.Clamp(pos.y, cp.y, surfaceY);
                Vector3 targetCamPos = new Vector3(cp.x, desiredCamY, cp.z);
                mainCam.transform.position = Vector3.Lerp(cp, targetCamPos, Time.deltaTime * camFollowSpeed);
            }

            // Verificar tope para procesar captura
            if (pos.y >= topY)
            {
                Fish f = caughtFish;
                caughtFish = null;
                bool wasCorrect = EvaluateFish(f);
                if (!wasCorrect && errorSound != null)
                    audioSource.PlayOneShot(errorSound);
                if (spawner != null)
                    spawner.HandleHookCatch(f);
                // Resetear hook a posición inicial
                transform.position = initialPosition;
                // inWater se restablecerá cuando baje de nuevo y cruce trigger
                // La cámara regresará en el bloque “else” siguiente
            }
        }
        // 3) No dragging y no pez enganchado: regresar hook y cámara a posición inicial
        else
        {
            if (!dragging)
            {
                // Hook
                if (transform.position != initialPosition)
                    transform.position = Vector3.Lerp(transform.position, initialPosition, Time.deltaTime * camReturnSpeed);

                // Cámara: regresar a camInitialPos.y
                if (mainCam != null)
                {
                    Vector3 cp = mainCam.transform.position;
                    Vector3 targetCamPos = new Vector3(cp.x, surfaceY, cp.z);
                    mainCam.transform.position = Vector3.Lerp(cp, targetCamPos, Time.deltaTime * camReturnSpeed);
                }
            }
        }
    }

    void OnMouseDown()
    {
        dragging = true;
    }

    void OnMouseUp()
    {
        dragging = false;
    }

    // Detección de entrada/salida en agua (trigger en Surface)
    void OnTriggerEnter2D(Collider2D other)
    {
        // Detectar trigger de línea de superficie
        if (other == waterSurfaceTrigger)
        {
            inWater = true;
            return;
        }
        // Capturar pez solo si dragging y dentro del agua y no ya enganchado
        if (!dragging || !inWater || caughtFish != null) return;
        Fish f = other.GetComponent<Fish>();
        if (f != null)
        {
            caughtFish = f;
            // Desactivar collider del pez para no re-enganchar
            var col = f.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            // En Click Mode, ya se quitó OnClicked; en Hook Mode no se suscribió
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other == waterSurfaceTrigger)
        {
            inWater = false;
        }
    }

    bool EvaluateFish(Fish f)
    {
        if (spawner == null) return false;
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
                return f.number.HasValue && f.number.Value == spawner.targetNumber;
            case FishSpawner.Criterion.Shape:
                return f.shapeRenderer.sprite == spawner.targetShape;
            case FishSpawner.Criterion.ShapeAndColor:
                return f.shapeRenderer.sprite == spawner.targetShape && f.colorName == spawner.targetColor;
        }
        return false;
    }
}
