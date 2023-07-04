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

    private void SettingStageStars() //�� ȹ�� ���� ǥ��
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
        for (int i = 0; i < GeneralMgr.unlockStageNumber; i++) //�رݵ� ���������� ������
        {
            ui_Button_Stage[i].SetActive(true);
        }
        for (int i = GeneralMgr.unlockStageNumber; i < ui_Button_Stage.Length; i++) //�رݵ��� ���� ���������� �Ⱥ�����
        {
            ui_Button_Stage[i].SetActive(false);
        }
    }

    public void B_ToStageNum(int stageNum)//�������� ����
    {
        if(GeneralMgr.unlockStageNumber < stageNum) //Ŭ������ ������������ �� �������� �ѹ��� �� ũ�� �ش� ���������� �̵� �Ұ�
            return;
        
        string thisSceneName = "Stage_" + stageNum.ToString();
        SceneManager.LoadScene(thisSceneName);//�ش� ���������� �̵�
    }
}
