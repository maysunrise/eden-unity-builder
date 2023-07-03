using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateWorldPanel : MonoBehaviour
{

    public WorldButton WorldB;

    public GameObject PanelRoot;

    public static CreateWorldPanel Instance;

    private void Start()
    {
        Instance = this;
    }

    public void CreateWorld(int worldtype)
    {
        WorldB.worldType = (WorldType)worldtype;
        PanelRoot.SetActive(false);
        Debug.Log("Creating world, type " + WorldB.worldType.ToString());
        if (WorldType.Flat == (WorldType)worldtype)
        {
            World.Instance.WorldSeed = 0;
        }
        else if (WorldType.Normal == (WorldType)worldtype)
        {
            World.Instance.WorldSeed = RandomSeed();
        }
        Debug.Log("World seed " + World.Instance.WorldSeed);
        World.Instance.Name = WorldB.WorldName.text;
        GameController.Instance.StartGame();
    }

    public int RandomSeed()
    {
        return Random.Range(1, int.MaxValue);
    }

}