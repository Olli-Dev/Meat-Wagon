using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

public class CarLapCounter : MonoBehaviour
{
    // Local variables
    private float timeAtLastPassedCheckPoint = 0;
    private float hideUIDelayTime;
    private int passedCheckPointNumber = 0;
    private int numberOfPassedCheckpoints = 0;
    private int lapsCompleted = 0;
    private int carPosition = 0;
    private bool isRaceCompleted = false;
    private bool isHideRoutineRunning = false;
    const int lapsToComplete = 2;

    // Public variables
    public Text carPositionText;

    // Events
    public event Action<CarLapCounter> OnPassCheckPoint;

    public void SetCarPosition(int position)
    {
        carPosition = position;
    }

    public int GetTheNumberOfCheckpointsPassed()
    {
        return numberOfPassedCheckpoints;
    }

    public float GetTimeAtLastCheckPoint()
    {
        return timeAtLastPassedCheckPoint;
    }

    IEnumerator ShowPositionCO(float delayUntilHidePosition)
    {
        hideUIDelayTime += delayUntilHidePosition;

        carPositionText.text = carPosition.ToString();

        carPositionText.gameObject.SetActive(true);
        if (!isHideRoutineRunning)
        {
            isHideRoutineRunning = true;
            yield return new WaitForSeconds(hideUIDelayTime);
            carPositionText.gameObject.SetActive(false);
            isHideRoutineRunning = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collider2D)
    {
        if (collider2D.CompareTag("CheckPoint"))
        {

            // Once a car has completed the race we don't need to check any checkpoints or laps
            if (isRaceCompleted)
                return;

            CheckPoint checkPoint = collider2D.GetComponent<CheckPoint>();
            
            // Make sure that the car is passing the checkpoints in the correct order. The correct checkpoint must have exactly 1 higher value than the passed checkpoint
            if(passedCheckPointNumber +1 == checkPoint.checkPointNumber)
            {
                passedCheckPointNumber = checkPoint.checkPointNumber;

                numberOfPassedCheckpoints++;

                // Store the time at the checkpoint
                timeAtLastPassedCheckPoint = Time.time;

                if (checkPoint.isFinishLine)
                {
                    passedCheckPointNumber = 0;
                    lapsCompleted++;

                    if (lapsCompleted >= lapsToComplete)
                        isRaceCompleted = true;

                }

                // Invoke the passed checkpoint event
                OnPassCheckPoint?.Invoke(this);

                // Now show the cars position as it has been calculated
                if (isRaceCompleted)
                    StartCoroutine(ShowPositionCO(100f));
                else StartCoroutine(ShowPositionCO(1.5f));
            }
        }
    }
}
