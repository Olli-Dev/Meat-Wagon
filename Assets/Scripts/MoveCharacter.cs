using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCharacter : MonoBehaviour
{
    // Start is called before the first frame update
    // Player Movement Variables/....
    public static float movespeed = 0.2f;
    public Vector3 userDirection = Vector3.right;

    public void Update()
    {
        transform.Translate(userDirection * movespeed * Time.deltaTime);
    }
}
