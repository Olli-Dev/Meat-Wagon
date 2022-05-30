using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    public GameObject engineSfx;
    public GameObject screechSfx;
    public GameObject timesUpScreen;
    public float timeValue = 20;
    public TextMeshProUGUI timeText;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(CountdownTimer());
    }

    IEnumerator CountdownTimer()
    {
        while (timeValue > 0)
        {
            timeText.text = timeValue.ToString();

            yield return new WaitForSeconds(1f);

            timeValue--;
        }

        timeText.text = "TIME'S UP!!";
        Time.timeScale = 0;
        timesUpScreen.SetActive(true);
        engineSfx.GetComponent<AudioSource>().Stop();
        screechSfx.GetComponent<AudioSource>().Stop();
    }

}
