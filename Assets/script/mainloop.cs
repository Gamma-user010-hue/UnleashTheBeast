using UnityEngine;
using TMPro;

public class mainloop : MonoBehaviour
{
    public Animator anim; 
    public TMP_Text timer;
    private float timeLeft;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        anim.Play("chest_beat 1");
        timeLeft = 30;
    }   

    // Update is called once per frame
    void Update()
    {
        if (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;
            timer.text = "00:" + Mathf.Ceil(timeLeft).ToString();
        }
        else
        {
            timer.text = "GO!";
        }
    }
}
