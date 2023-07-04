using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GeneralMgr : MonoBehaviour
{
    public bool ServerCheck;
    public static bool isServer;
    public static int unlockStageNumber; //해금된 스테이지
    public static int stagesStarsNumber;
    public static float[] volume = Enumerable.Repeat<float>(0,2).ToArray(); //배열 크기를 2, 값을 0으로 초기화
    public static float mouse_Sensitivity = 20;
    public static string serverIp = "127.0.0.1";

    public static GameObject GM;

    /// <summary>
    /// 스테이지 클리어 후 내역을 비교하여 결과를 저장
    /// </summary>
    /// <param name="currentStageNumber"></param>
    /// <param name="starsCount"></param>
    public static void SaveAfterStageCompare(int currentStageNumber, int starsCount)
    {
        if (currentStageNumber == unlockStageNumber && currentStageNumber<3)
        {
            unlockStageNumber = currentStageNumber + 1;
            PlayerPrefs.SetInt("StageSave", unlockStageNumber);
        }
        if (currentStageNumber -1 > 16) return; // 스테이지 16까지만 저장가능
        int pastStarsCount = (stagesStarsNumber >> (currentStageNumber -1) * 2) & 0b11; //3만 남기고 다 버림
        if (starsCount > pastStarsCount)
        {
            stagesStarsNumber += (starsCount - pastStarsCount) << (currentStageNumber - 1) * 2;
            PlayerPrefs.SetInt("StageStarsNumber", stagesStarsNumber);
        }
    }
    private void Awake()
    {
        if ( GM == null)
        {
            isServer = ServerCheck;
            if (PlayerPrefs.HasKey("StageSave") == false) //스테이지를 저장하는 변수가 없으면 1을 저장
            {
                unlockStageNumber = 1;
                PlayerPrefs.SetInt("StageSave", unlockStageNumber);
            }
            else unlockStageNumber = PlayerPrefs.GetInt("StageSave"); //스테이지 변수를 가져옴.

            if (PlayerPrefs.HasKey("StageStarsNumber") == false) //스테이지를 저장하는 변수가 없으면 1을 저장
            {
                stagesStarsNumber = 0;
                PlayerPrefs.SetInt("StageStarsNumber", stagesStarsNumber);
            }
            else stagesStarsNumber = PlayerPrefs.GetInt("StageStarsNumber");

            if (PlayerPrefs.HasKey("BGM") == false)
                PlayerPrefs.SetFloat("BGM", volume[0]);
            else
                volume[0] = PlayerPrefs.GetFloat("BGM");
            if (PlayerPrefs.HasKey("SFX") == false)
                PlayerPrefs.SetFloat("SFX", volume[1]);
            else
                volume[1] = PlayerPrefs.GetFloat("SFX");
            if (PlayerPrefs.HasKey("mouse_Sensitivity") == false)
                PlayerPrefs.SetFloat("mouse_Sensitivity", mouse_Sensitivity);
            else
                mouse_Sensitivity = PlayerPrefs.GetFloat("mouse_Sensitivity");
            if (PlayerPrefs.HasKey("serverIp") == false)
                PlayerPrefs.SetString("serverIp", serverIp);
            else
                serverIp = PlayerPrefs.GetString("serverIp");

            GM = this.gameObject;
            DontDestroyOnLoad(GM);
        }


    }
    private void OnApplicationQuit()
    {
        PlayerPrefs.SetInt("StageSave", unlockStageNumber);
        PlayerPrefs.SetInt("StageStarsNumber", stagesStarsNumber);
        PlayerPrefs.SetFloat("BGM", volume[0]);
        PlayerPrefs.SetFloat("SFX", volume[1]);
        PlayerPrefs.SetFloat("mouse_Sensitivity", mouse_Sensitivity);
        PlayerPrefs.SetString("serverIp", serverIp);
    }
    public void ButtonDataReset()
    {
        PlayerPrefs.DeleteAll();
        unlockStageNumber = 1;
        stagesStarsNumber = 0;
        volume = Enumerable.Repeat<float>(0, 2).ToArray();
        mouse_Sensitivity = 20;
        serverIp = "127.0.0.1";
        SceneManager.LoadScene(0);
    }
}
