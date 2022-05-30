using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseScreen : MonoBehaviour
{
    public GameObject pauseScreen;
    public GameObject engineSfx;
    public GameObject screechSfx;
    public static bool GameIsPaused = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameIsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    void Resume()
    {
        pauseScreen.SetActive(false);
        Time.timeScale = 1;
        GameIsPaused = false;
        engineSfx.GetComponent<AudioSource>().Play();
        screechSfx.GetComponent<AudioSource>().Play();
    }

    void Pause()
    {
        pauseScreen.SetActive(true);
        Time.timeScale = 0;
        GameIsPaused = true;
        engineSfx.GetComponent<AudioSource>().Stop();
        screechSfx.GetComponent<AudioSource>().Stop();
    }
}
