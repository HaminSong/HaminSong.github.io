using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StagePanel : MonoBehaviour
{
    public GameObject[] ui_Button_Stage;
    public Sprite sprite_Star;

    private void Start()
    {
        StagePanelUpdate();
        SettingStageStars();
    }

    private void SettingStageStars() //별 획득 갯수 표시
    {
        for (int stageNum = 0; stageNum < GeneralMgr.unlockStageNumber; stageNum++)
        {
            int stageStars = (GeneralMgr.stagesStarsNumber >> stageNum * 2) & 0b11;
            for (int i = 0; i < stageStars; i++)
            {
                ui_Button_Stage[stageNum].transform.GetChild(0).GetChild(i).GetComponent<Image>().sprite = sprite_Star;
            }
        }
    }
    private void StagePanelUpdate()
    {
        for (int i = 0; i < GeneralMgr.unlockStageNumber; i++) //해금된 스테이지를 보여줌
        {
            ui_Button_Stage[i].SetActive(true);
        }
        for (int i = GeneralMgr.unlockStageNumber; i < ui_Button_Stage.Length; i++) //해금되지 않은 스테이지는 안보여줌
        {
            ui_Button_Stage[i].SetActive(false);
        }
    }

    public void B_ToStageNum(int stageNum)//스테이지 고르기
    {
        if(GeneralMgr.unlockStageNumber < stageNum) //클리어한 스테이지보다 고른 스테이지 넘버가 더 크면 해당 스테이지로 이동 불가
            return;
        
        string thisSceneName = "Stage_" + stageNum.ToString();
        SceneManager.LoadScene(thisSceneName);//해당 스테이지로 이동
    }
}
