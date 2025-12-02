using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MoveSceneTest : MonoBehaviour
{
    // public AudioSource audioSource;
    // public void PlayMusic()
    // {
    //     audioSource.Play();
    // }
    public void OnClickTest()
    {
        Debug.Log("Button Clicked");
        SceneManager.LoadScene("jungle_scene");
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
