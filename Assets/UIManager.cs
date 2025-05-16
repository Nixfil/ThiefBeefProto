using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public void Start()
    {
        Instance = this;
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
