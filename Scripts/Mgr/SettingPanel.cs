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

    public void MouseControl() //���콺 �ΰ��� ����
    {
        float sensitivity = slider_Mouse.value * 0.1f; // �ΰ����� 0~2���� �����̴� ���� 20�̶� 10���� ����.
        if(CameraFollow != null)
            CameraFollow.CameraSensitivity = sensitivity; //�ΰ��� ����
        if(FindObjectOfType<Title_Camera>() != null)
            FindObjectOfType<Title_Camera>().CameraSensitivity = sensitivity;

        GeneralMgr.mouse_Sensitivity = slider_Mouse.value; //���콺 �ΰ��� ���� ����
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
                sliders_Sound[i].value = 20 + GeneralMgr.volume[i] / 2;//�������� -40 ~ 0��
        }
        slider_Mouse.value = GeneralMgr.mouse_Sensitivity; //�������� �ٲ� ���� ����
    }
}
