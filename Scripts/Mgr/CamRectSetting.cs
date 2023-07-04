using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamRectSetting : MonoBehaviour
{
    private void Start()
    {
        SetRect();
    }

    public void SetRect(int width = 1600, int height = 900)
    {
        int deviceWidth = Screen.width;
        int deviceHeight = Screen.height;
        Rect rect = new Rect();
        Screen.SetResolution(width, (int)(((float)deviceHeight / deviceWidth) * width), Screen.fullScreen);

        if ((float)width / height < (float)deviceWidth / deviceHeight)
        {
            float newWidth = ((float)width / (float)height) / ((float)deviceWidth / deviceHeight);
            rect.Set((1f - newWidth) / 2f, 0f, newWidth, 1f);
            GetComponent<Camera>().rect = rect;
        }
        else
        {
            float newHeight = ((float)deviceWidth / (float)deviceHeight) / ((float)width / (float)height);
            rect.Set(0f, (1f - newHeight) / 2f, 1f, newHeight);
            GetComponent<Camera>().rect = rect;

        }
    }
}
