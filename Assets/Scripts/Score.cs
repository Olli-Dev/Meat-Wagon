using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Score : MonoBehaviour
{
    public static Score instance;

    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highscoreText;

    int score = 0;
    int highscore = 0;

    private void Awake()
    {
        instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        highscore = PlayerPrefs.GetInt("highscore");
        scoreText.text = score.ToString();
        highscoreText.text = highscore.ToString();
    }

    public void AddScore()
    {
        score += 1;
        scoreText.text = score.ToString();
        if(highscore < score)
            PlayerPrefs.SetInt("highscore", score);
    }
}
