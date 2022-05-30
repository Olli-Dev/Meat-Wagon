using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelParticleHandler : MonoBehaviour
{
    // Local variables
    private float particleEmissionRate = 0f;

    // Components
    TopDownCarController topDownCarController;
    ParticleSystem particleSystemSmoke;
    ParticleSystem.EmissionModule particleSystemEmissionModule;

    private void Awake()
    {
        // Get the TopDownCarController
        topDownCarController = GetComponentInParent<TopDownCarController>();

        // Get the particle system
        particleSystemSmoke = GetComponent<ParticleSystem>();
        
        // Get the emission component
        particleSystemEmissionModule = particleSystemSmoke.emission;

        // Set it to zero emission
        particleSystemEmissionModule.rateOverTime = 0f;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Reduce the particle over time
        particleEmissionRate = Mathf.Lerp(particleEmissionRate, 0, Time.deltaTime * 5);
        particleSystemEmissionModule.rateOverTime = particleEmissionRate;

        if (topDownCarController.IsTireScreeching(out float lateralVelocity, out bool isBreaking))
        {
            // If the car tires are screeching then we'll emitt smoke. If the player is breaking then emitt a lot of smoke.
            if (isBreaking)
                particleEmissionRate = 30f;

            // If the player is drifting we'll emitt smoke based on how much the player is drifting.
            else particleEmissionRate = Mathf.Abs(lateralVelocity) * 2;
        }
    }
}
