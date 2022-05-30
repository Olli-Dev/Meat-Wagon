using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarLayerHandler : MonoBehaviour
{
    public SpriteRenderer carOutlineSpriteRenderer;
    List<SpriteRenderer> defaultLayerSpriteRenderer = new List<SpriteRenderer>();
    List<Collider2D> overpassColliderList = new List<Collider2D>();
    List<Collider2D> underpassColliderList = new List<Collider2D>();

    Collider2D carCollider;

    // State
    private bool isDrivingOnOverpass = false;
    private void Awake()
    {
        foreach (SpriteRenderer spriteRenderer in gameObject.GetComponentsInChildren<SpriteRenderer>())
        {
            if (spriteRenderer.sortingLayerName == "Default")
                defaultLayerSpriteRenderer.Add(spriteRenderer);
        }

        foreach (GameObject overpassColliderGameObject in GameObject.FindGameObjectsWithTag("OverpassCollider"))
        {
            overpassColliderList.Add(overpassColliderGameObject.GetComponent<Collider2D>());
        }
        
        foreach (GameObject underpassColliderGameObject in GameObject.FindGameObjectsWithTag("UnderpassCollider"))
        {
            underpassColliderList.Add(underpassColliderGameObject.GetComponent<Collider2D>());
        }

        carCollider = GetComponentInChildren<Collider2D>();

        // Default drive on underpass
        carCollider.gameObject.layer = LayerMask.NameToLayer("ObjectOnUnderpass");
    }

    // Start is called before the first frame update
    private void Start()
    {
        UpdateSortingAndCollisionLayers();
    }

    private void UpdateSortingAndCollisionLayers()
    {
        if (isDrivingOnOverpass)
        {
            SetSortingLayer("RaceTrackOverpass");

            carOutlineSpriteRenderer.enabled = false;
        }
        else
        {
            SetSortingLayer("Default");

            carOutlineSpriteRenderer.enabled = true;
        }

        SetCollisionWithOverpass();
    }

    private void SetCollisionWithOverpass()
    {
        foreach (Collider2D collider2D in overpassColliderList)
        {
            Physics2D.IgnoreCollision(carCollider, collider2D, !isDrivingOnOverpass);
        }

        foreach (Collider2D collider2D in underpassColliderList)
        {
            if (isDrivingOnOverpass)
                Physics2D.IgnoreCollision(carCollider, collider2D, true);
            else Physics2D.IgnoreCollision(carCollider, collider2D, false);
        }
    }

    private void SetSortingLayer(string layerName)
    {
        foreach (SpriteRenderer spriteRenderer in defaultLayerSpriteRenderer)
        {
            spriteRenderer.sortingLayerName = layerName;
        }
    }

    public bool IsDrivingOnOverpass()
    {
        return isDrivingOnOverpass;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("UnderpassTrigger"))
        {
            isDrivingOnOverpass = false;

            carCollider.gameObject.layer = LayerMask.NameToLayer("ObjectOnUnderpass");

            UpdateSortingAndCollisionLayers();
        }
        else if(collision.CompareTag("OverpassTrigger"))
        {
            isDrivingOnOverpass = true;

            carCollider.gameObject.layer = LayerMask.NameToLayer("ObjectOnOverpass");

            UpdateSortingAndCollisionLayers();
        }
    }

}
