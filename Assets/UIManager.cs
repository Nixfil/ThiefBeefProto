using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    [Header("Interrupt UI")]
    public GameObject interruptX;
    public void Start()
    {
        Instance = this;
    }
    public void UpdateInterruptUI(Vector3 screenPosition)
    {
        if (interruptX != null)
        {
            interruptX.SetActive(true);
            interruptX.transform.position = screenPosition;
        }
    }

    public void HideInterruptUI()
    {
        if (interruptX != null)
            interruptX.SetActive(false);
    }
   
    public void HideCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined; // Keeps it in window
    }

    public void ShowCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
