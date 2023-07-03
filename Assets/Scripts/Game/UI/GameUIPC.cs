using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameUIPC : MonoBehaviour
{

    public GameObject[] AndroidUI;
    public GameObject[] WindowsUI;

    public CameraMovement CamMove;

    public bool IsPC;

    public static GameUIPC I;

    void Start()
    {
        I = this;
        InitUI();
    }

    public void InitUI()
    {
        if (Application.platform != RuntimePlatform.Android && Application.platform != RuntimePlatform.WindowsEditor)
        {
            for (int i = 0; i < AndroidUI.Length; i++)
            {
                AndroidUI[i].SetActive(false);
            }
            for (int i = 0; i < WindowsUI.Length; i++)
            {
                WindowsUI[i].SetActive(true);
            }
            IsPC = true;
        }
    }

    private void Update()
    {
        if (!IsPC) return;

        if (BuildController.Instance.InventoryBlocks.activeInHierarchy || BuildController.Instance.InventoryColors.activeInHierarchy)
        {
            Cursor.lockState = CursorLockMode.None;
            CamMove.enabled = false;
        }
        else
        {
            CamMove.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
        }
        // CamMove.LookInput.TouchDist.x = Input.GetAxis("Mouse X") * CamMove.Sensitivity;
        // CamMove.LookInput.TouchDist.y = Input.GetAxis("Mouse Y") * CamMove.Sensitivity;
    }

}