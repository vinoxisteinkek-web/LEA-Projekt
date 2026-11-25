using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayButtonScript : MonoBehaviour
{
    public void PlayGame()
    {
        SceneLoader.Load("HouseScene");
    }
}