using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class UnscaledParticleSimulator : MonoBehaviour
{
    private ParticleSystem ps;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        ps.Play();
    }

    void Update()
    {
        // Si el juego está “pausado” (timeScale == 0), simulamos manualmente
        float dt = Time.timeScale > 0f
                 ? Time.deltaTime
                 : Time.unscaledDeltaTime;

        ps.Simulate(dt, true, false);
    }
}
