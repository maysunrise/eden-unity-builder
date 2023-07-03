using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{

    public World World;

    public GameObject MainMenu;

    public GameObject Player;

    public static GameController Instance;

    public GameObject MenuAudio;

    public GameObject MainCamera;

    void Start()
    {
        Instance = this;
    }

    void Update()
    {

    }

    public void StartGame()
    {
        World.enabled = true;
        World.chunksPerFrame = 8;
    }

    public void CloseMainMenu()
    {
        World.chunksPerFrame = 1;
        World.LoadingText.text = "";
        MainMenu.SetActive(false);
    }

    public void TogglePlayerController(bool t)
    {
        Player.GetComponent<CharacterController>().enabled = t;
        Player.GetComponent<PlayerMovement>().enabled = t;
        Player.SetActive(t);
    }

    public void ToggleMainCamera(bool t)
    {
        MainCamera.SetActive(t);
    }

}