using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class CarSFXHandler : MonoBehaviour
{
    [Header("Mixers")]
    public AudioMixer audioMixer;

    [Header("Audio Sources")]
    public AudioSource tiresScreeachingAudioSource;
    public AudioSource engineAudioSource;
    public AudioSource carHitAudioSource;
    public AudioSource carJumpAudioSource;
    public AudioSource carJumpLandingAudioSource;

    // Local variable
    private float desiredEnginePitch = 0.5f;
    private float tireScreechPitch = 0.5f;

    // Components
    TopDownCarController topDownCarController;

    private void Awake()
    {
        topDownCarController = GetComponentInParent<TopDownCarController>();
    }
    // Start is called before the first frame update
    private void Start()
    {
        audioMixer.SetFloat("SFXVolume", 0.5f);
    }

    // Update is called once per frame
    private void Update()
    {
        UpdateEngineSFX();
        UpdateTiresScreechingSFX();
    }
    private void UpdateEngineSFX()
    {
        // Handle engine SFX
        float velocityMagnitute = topDownCarController.GetVelocityMagnitude();

        // Increase the engine volume as the car goes faster
        float desiredEngineVolume = velocityMagnitute * 0.05f;

        // But keep a minimum level so it plays even if the car is idle
        desiredEngineVolume = Mathf.Clamp(desiredEngineVolume, 0.2f, 1.0f);

        engineAudioSource.volume = Mathf.Lerp(engineAudioSource.volume, desiredEngineVolume, Time.deltaTime * 10);

        // To add more variation to the engine sound, we also change the pitch
        desiredEnginePitch = velocityMagnitute * 0.2f;
        desiredEnginePitch = Mathf.Clamp(desiredEnginePitch, 0.5f, 2f);
        engineAudioSource.pitch = Mathf.Lerp(engineAudioSource.pitch, desiredEnginePitch, Time.deltaTime * 1.5f);
    }

    private void UpdateTiresScreechingSFX()
    {
        // Handle tire screeching SFX
        if (topDownCarController.IsTireScreeching(out float lateralVelocity, out bool isBreaking))
        {
            if (isBreaking)
            {
                tiresScreeachingAudioSource.volume = Mathf.Lerp(tiresScreeachingAudioSource.volume, 1.0f, Time.deltaTime * 10);
                tireScreechPitch = Mathf.Lerp(tireScreechPitch, 0.5f, Time.deltaTime * 10);
            }
            else
            {
                // If we are not breaking we still want to play this screech sound if the player is drifting.
                tiresScreeachingAudioSource.volume = Mathf.Abs(lateralVelocity) * 0.05f;
                tireScreechPitch = Mathf.Abs(lateralVelocity) * 0.1f;
            }
        }
        // Fade out the tire screech SFX if we are not screeching
        else tiresScreeachingAudioSource.volume = Mathf.Lerp(tiresScreeachingAudioSource.volume, 0, Time.deltaTime * 10);
    }

    public void PlayJumpSFX()
    {
        carJumpAudioSource.Play();
    }

    public void PlayLandingSFX()
    {
        carJumpLandingAudioSource.Play();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Get the relative velocity of the collision
        float relativeVelocity = collision.relativeVelocity.magnitude;

        float volume = relativeVelocity * 0.1f;

        carHitAudioSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
        carHitAudioSource.volume = volume;

        if (!carHitAudioSource.isPlaying)
            carHitAudioSource.Play();
    }

}
