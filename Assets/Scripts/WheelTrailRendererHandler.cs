using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelTrailRendererHandler : MonoBehaviour
{
    public bool isOverpassEmitter = false;
    // Components
    TopDownCarController topDownCarController;
    TrailRenderer trailRenderer;
    CarLayerHandler carLayerHandler;

    private void Awake()
    {
        topDownCarController = GetComponentInParent<TopDownCarController>();
        trailRenderer = GetComponent<TrailRenderer>();
        carLayerHandler = GetComponentInParent<CarLayerHandler>();

        // Set the trail renderer to not emit in the start
        trailRenderer.emitting = false;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        trailRenderer.emitting = false;

        // If the car tires are screeching then we'll emitt a trail.
        if (topDownCarController.IsTireScreeching(out float lateralVelocity, out bool isBreaking))
        {
            if(carLayerHandler.IsDrivingOnOverpass() && isOverpassEmitter)
            trailRenderer.emitting = true;

            if (!carLayerHandler.IsDrivingOnOverpass() && !isOverpassEmitter)
                trailRenderer.emitting = true;
        }
        else trailRenderer.emitting = false;
    }
}
