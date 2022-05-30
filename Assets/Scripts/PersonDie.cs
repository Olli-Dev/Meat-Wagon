using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersonDie : MonoBehaviour
{
    public GameObject death;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Instantiate(death, transform.position, Quaternion.identity);
        Score.instance.AddScore();
        Destroy(gameObject);
    }
}
