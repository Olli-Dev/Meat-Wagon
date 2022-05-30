using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeOutAndDestroy : MonoBehaviour
{
    public float fadeDelay = 10f;
    public float alphaValue = 0;
    public bool destroyGameObject = false;

    public AudioSource zombieSplat;
    SpriteRenderer spriteRenderer;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        zombieSplat.Play();
        StartCoroutine(FadeTo(alphaValue, fadeDelay));
    }

    IEnumerator FadeTo(float aValue, float fadeTime)
    {
        float alpha = spriteRenderer.color.a;

        for (float t = 0.0f; t < 1.0f; t+= Time.deltaTime / fadeTime)
        {
            Color newColor = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, Mathf.Lerp(alpha, aValue, t));
            spriteRenderer.color = newColor;
            yield return null;
        }

        if (destroyGameObject)
            Destroy(gameObject);
    }
}
