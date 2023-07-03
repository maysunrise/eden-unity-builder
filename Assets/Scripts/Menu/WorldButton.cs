using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class WorldButton : MonoBehaviour
{

    public Text WorldName;

    public Button btn;

    public string NameFile;

    public WorldType worldType;

    public bool isNew;
    public bool isNewFormat;

    void Start()
    {

    }

    public void SelectNewWorld(int type)
    {
        worldType = (WorldType)type;
        World.Instance.Type = worldType;
        if (WorldType.Flat == worldType)
        {
            World.Instance.WorldSeed = 0;
        }
        else if (WorldType.Normal == worldType)
        {
            World.Instance.WorldSeed = RandomSeed();
        }
        StartCoroutine(WorldLoad());
    }

    public void LoadWorld()
    {
        if (!isNew)
        {
            StartCoroutine(WorldLoad());
        }
        else
        {
            CreateWorldPanel.Instance.WorldB = this;
            CreateWorldPanel.Instance.PanelRoot.SetActive(true);
        }

    }

    public int RandomSeed()
    {
        return Random.Range(1, int.MaxValue);
    }

    IEnumerator WorldLoad()
    {
        if (isNewFormat)
        {
            EdenWorldDecoder.Instance.LoadWorld(Application.persistentDataPath + "/" + NameFile + ".eden2");
        }
        else
        {
            EdenWorldDecoder.Instance.LoadWorld(Application.persistentDataPath + "/" + NameFile + ".eden");
        }
        GameController.Instance.StartGame();
        yield return null;
    }
}