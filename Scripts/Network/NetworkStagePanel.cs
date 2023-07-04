using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkStagePanel : MonoBehaviour
{
    public void ButtonNetStage(int stageNum)
    {
        string sceneName = "Net_Stage_" + stageNum.ToString();
        SceneManager.LoadScene(sceneName);
    }
}
