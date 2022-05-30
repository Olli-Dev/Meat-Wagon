using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementRandomizer : MonoBehaviour
{
    Vector2 newPosition;
    Quaternion newRotation;
    [SerializeField] private Vector2 min;
    [SerializeField] private Vector2 max;
    [SerializeField] private Vector2 yRotationRange;
    [SerializeField] [Range(0.01f, 0.1f)] private float lerpSpeed = 0.05f;

    private void Awake() 
    {
        newPosition = transform.position;
        newRotation = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * lerpSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * lerpSpeed);

        if(Vector3.Distance(transform.position, newPosition) < 1f)
        {
            GetNewPosition();
        }
    }

    private void GetNewPosition()
    {
        var xPos = UnityEngine.Random.Range(min.x, max.x);
        var yPos = UnityEngine.Random.Range(min.y, max.y);
        newRotation = Quaternion.Euler(0, UnityEngine.Random.Range(yRotationRange.x, yRotationRange.y), 0);
        newPosition = new Vector3(xPos, 0, yPos);
    }
}
