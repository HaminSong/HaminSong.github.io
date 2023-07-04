using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;

public class SettingPanel : MonoBehaviour
{
    public AudioMixer audioMixer;
    public Slider[] sliders_Sound;
    public Slider slider_Mouse;

    private string[] typeNames = { "BGM", "SFX" };
    private CameraFollow CameraFollow;

    public void AudioControl(int typeNumber)
    {
        float volume = sliders_Sound[typeNumber].value * 2 - 40;
        if (volume <= -40f)
            volume = -80f;
        GeneralMgr.volume[typeNumber] = volume;

        audioMixer.SetFloat(typeNames[typeNumber].ToString(), volume);
    }

    public void MouseControl() //마우스 민감도 설정
    {
        float sensitivity = slider_Mouse.value * 0.1f; // 민감도가 0~2지만 슬라이더 값은 20이라 10으로 나눔.
        if(CameraFollow != null)
            CameraFollow.CameraSensitivity = sensitivity; //민감도 설정
        if(FindObjectOfType<Title_Camera>() != null)
            FindObjectOfType<Title_Camera>().CameraSensitivity = sensitivity;

        GeneralMgr.mouse_Sensitivity = slider_Mouse.value; //마우스 민감도 값을 저장
    }

    public void B_Quit()
    {
        Application.Quit();
    }

    public void SettingInit()
    {
        CameraFollow = FindObjectOfType<CameraFollow>();

        for (int i = 0; i < sliders_Sound.Length; i++)
        {
            if (GeneralMgr.volume[i] == -80)
                sliders_Sound[i].value = 0;

            else
                sliders_Sound[i].value = 20 + GeneralMgr.volume[i] / 2;//볼륨값이 -40 ~ 0임
        }
        slider_Mouse.value = GeneralMgr.mouse_Sensitivity; //설정에서 바꾼 값을 적용
    }
}
