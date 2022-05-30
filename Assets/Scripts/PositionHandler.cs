using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PositionHandler : MonoBehaviour
{
    public List<CarLapCounter> carLapCounters = new List<CarLapCounter>();
    // Start is called before the first frame update
    void Start()
    {
        // Get all car lap counters in the scene
        CarLapCounter[] carLapCounterArray = FindObjectsOfType<CarLapCounter>();

        // After all the car lap counters are found, store the lap counters in a list
        carLapCounters = carLapCounterArray.ToList<CarLapCounter>();

        // Hook up the passed checkpoint event
        foreach (CarLapCounter lapCounter in carLapCounters)
            lapCounter.OnPassCheckPoint += OnPassCheckpoint;
    }

    private void OnPassCheckpoint(CarLapCounter carLapCounter)
    {
        // Sort the cars position first based on how many checkpoints they have passed, more is always better. Then sort on time where shorter time is better. - P.S "s" is just anything. It can also be "bob.." if you want.
        carLapCounters = carLapCounters.OrderByDescending(s => s.GetTheNumberOfCheckpointsPassed()).ThenBy(s => s.GetTimeAtLastCheckPoint()).ToList();

        // Get the car position(it ends with +1, because list usually start with pos 0, and we want it to start with pos 1)
        int carPosition = carLapCounters.IndexOf(carLapCounter) + 1;

        // Tell the lap counter which position the car has
        carLapCounter.SetCarPosition(carPosition);
    }

}
