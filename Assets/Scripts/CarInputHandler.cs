using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarInputHandler : MonoBehaviour
{
    public int playerNumber = 1;
    
    public bool isUIInput = false;

    Vector2 inputVector = Vector2.zero;

    // Components
    TopDownCarController topDownCarController;

    // Awake is called when the script instance is being loaded
    private void Awake()
    {
        topDownCarController = GetComponent<TopDownCarController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (isUIInput)
        {

        }
        else
        {
            inputVector = Vector2.zero;
            switch (playerNumber)
            {
                case 1:
                    // Get input from Unity's input system
                    inputVector.x = Input.GetAxis("Horizontal");
                    inputVector.y = Input.GetAxis("Vertical");
                    break;
            }
        }

        // Send input to the car controller
        topDownCarController.SetInputVector(inputVector);

        //if (Input.GetButtonDown("Jump"))
        //topDownCarController.Jump(1.0f, 0.0f);
    }

    public void SetInput(Vector2 newInput)
    {
        inputVector = newInput;
    }
}
