using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Title_Manager : MonoBehaviour
{
    public GameObject cannonBallPrefab;

    public Image ui_Fade;
    public Text ui_Story;

    public float speed = 10;
    public float storyTextTime = 1;


    [HideInInspector]
    public bool isStoryStart = false;

    
    public GameObject TitleCam;
    public GameObject titleGoldBird;
    public GameObject titleRock;
    public GameObject ui_TitleImage;
    public GameObject ui_Buttons;
    public GameObject ui_SettingPanel;
    public GameObject ui_StagePanel;
    public GameObject ui_TextSpace;
    public GameObject ui_NetStagePanel;
    private GameObject ui_StoryBoard;
    private GameObject[] cannonBalls = new GameObject[20];

    public void B_Setting()
    {
        if(ui_Buttons.activeSelf) 
        {
            ui_Buttons.SetActive(false);
            ui_SettingPanel.SetActive(true);
        }
        else 
        {
            ui_Buttons.SetActive(true);
            ui_SettingPanel.SetActive(false);
        }
    }
    public void B_StageSelect() //스테이지 셀렉트 판넬을 킴
    {
        if (GeneralMgr.isServer) return;
        if (ui_StagePanel.activeSelf)
        {
            ui_StagePanel.SetActive(false);
            ui_Buttons.SetActive(true);
        }
        else
        {
            ui_StagePanel.SetActive(true);
            ui_Buttons.SetActive(false);
        }
    }

    public void B_Net()
    {
        if (ui_NetStagePanel.activeSelf)
        {
            ui_NetStagePanel.SetActive(false);
            ui_Buttons.SetActive(true);
        }
        else
        {
            ui_NetStagePanel.SetActive(true);
            ui_Buttons.SetActive(false);
        }
    }

    public void StoryEnd()
    {
        ui_StoryBoard.gameObject.SetActive(false);
        ui_Buttons.SetActive(true);
        TitleCam.transform.position = Vector3.zero;
        isStoryStart = false;
        titleRock.SetActive(false);
        ui_TitleImage.SetActive(true);
    }

    private IEnumerator SpawnCannon(Vector3 cannonSpawnPosition, float spawnTime = 0.1f)
    {
        WaitForSeconds WT_Spawn = new WaitForSeconds(spawnTime);
        for (int i = 0; i < cannonBalls.Length; i++)
        {
            cannonBalls[i].transform.position = cannonSpawnPosition;
            cannonBalls[i].GetComponent<Rigidbody>().velocity = titleRock.transform.position - cannonSpawnPosition;
            cannonSpawnPosition.x *= -1;
            cannonBalls[i].SetActive(true);
            yield return WT_Spawn;
        }
    }

    public IEnumerator Story_Main_Coroutine()
    {
        IEnumerator tempIE = Rock_Auto_Move(titleGoldBird.transform.position);
        WaitUntil WU_Space = new WaitUntil(()=> Input.GetKeyDown(KeyCode.Space));
        ui_TitleImage.SetActive(false);
        ui_Buttons.SetActive(false);
        ui_SettingPanel.SetActive(false) ;
        ui_StagePanel.SetActive(false);
        ui_Story.text = "";
        ui_TextSpace.SetActive(false);
        yield return StartCoroutine(GameSingletonItems.FadeIn(ui_Fade, 1));
        isStoryStart = true;
        titleRock.SetActive(true);
        titleRock.GetComponent<Rigidbody>().isKinematic = false;
        titleRock.transform.position = new Vector3(0, -52f, -100);

        StartCoroutine(tempIE);

        yield return GameSingletonItems.WT_1point5sec;
        
        yield return StartCoroutine(GameSingletonItems.FadeOut(ui_Fade, 1));
        ui_StoryBoard.gameObject.SetActive(true);

        ui_Story.text = "[전설속에 존재한다는 황금알을 낳는 오리]";
        yield return StartCoroutine(GameSingletonItems.FadeIn(ui_Story, storyTextTime));
        yield return GameSingletonItems.WT_1point5sec;
        yield return StartCoroutine(GameSingletonItems.FadeOut(ui_Story, storyTextTime));

        ui_Story.text = "[그 황금알에서 태어난 황금오리]";
        yield return StartCoroutine(GameSingletonItems.FadeIn(ui_Story, storyTextTime));
        yield return GameSingletonItems.WT_1point5sec;
        yield return StartCoroutine(GameSingletonItems.FadeOut(ui_Story, storyTextTime));

        ui_Story.text = "[그 황금오리를 모아서 큰1돈을 벌거야!]";
        yield return StartCoroutine(GameSingletonItems.FadeIn(ui_Story, storyTextTime));
        yield return GameSingletonItems.WT_1point5sec;
        yield return StartCoroutine(GameSingletonItems.FadeOut(ui_Story, storyTextTime));

        yield return new WaitUntil(() => titleRock.GetComponent<Rigidbody>().isKinematic);

        Color color = ui_Story.color;
        color.a = 1;
        ui_Story.color = color;

        ui_TextSpace.SetActive(true);
        
        ui_Story.text = "황금오리다!!!";
        yield return StartCoroutine(GameSingletonItems.FadeIn(ui_Story, storyTextTime));
        yield return WU_Space;
        

        ui_Story.text = "으흐흐흐... 난 큰1돈을 번거야!";
        yield return StartCoroutine(GameSingletonItems.FadeIn(ui_Story, storyTextTime));
        yield return WU_Space;
        

        ui_Story.text = "(슈우웅)";
        yield return StartCoroutine(GameSingletonItems.FadeIn(ui_Story, storyTextTime));
        yield return WU_Space;

        ui_Story.text = "뭔 소리지?";
        yield return StartCoroutine(GameSingletonItems.FadeIn(ui_Story, storyTextTime));
        yield return WU_Space;

        ui_StoryBoard.gameObject.SetActive(false);
        StartCoroutine(SpawnCannon(new Vector3(100, 30, 150)));
        yield return GameSingletonItems.WT_1sec;
        yield return new WaitUntil(() => cannonBalls[0].activeSelf == false);

        ui_StoryBoard.gameObject.SetActive(true);
        ui_TextSpace.SetActive(false);
        ui_Story.text = "꾸우ㅔㅇㅔㅇㅔㅇㅔㅇㅔㄺ!!";
        yield return new WaitUntil(() => cannonBalls[cannonBalls.Length -1].activeSelf == true);
        yield return new WaitUntil(() => cannonBalls[cannonBalls.Length -1].activeSelf == false);
        yield return GameSingletonItems.WT_1point5sec;

        StoryEnd();
        yield return StartCoroutine(GameSingletonItems.FadeOut(ui_Fade, 1.5f));
    }

    private IEnumerator Rock_Auto_Move(Vector3 target)
    {
        Rigidbody rb = titleRock.GetComponent<Rigidbody>();
        rb.velocity = (target - titleRock.transform.position).normalized * 10;
        rb.angularVelocity = new Vector3(5, -1, -1);
        while (true)
        {
            if (Vector3.Distance(target, titleRock.transform.position) < 50)
            {
                rb.isKinematic = true;
                titleRock.GetComponent<AudioSource>().Stop();
                break;
            }

            rb.AddForce((target - titleRock.transform.position).normalized * speed);
            if(rb.velocity.magnitude > 25)
            {
                rb.velocity = rb.velocity.normalized * 25;
                
            }
            yield return GameSingletonItems.WFEOF;
        }
    }

    private void Start()
    {
        ui_StoryBoard = ui_Story.transform.parent.gameObject;
        ui_TitleImage.SetActive(true);
        ui_SettingPanel.SetActive(false);
        ui_StagePanel.SetActive(false);
        ui_NetStagePanel.SetActive(false) ;
        titleRock.SetActive(false);
        ui_StoryBoard.gameObject.SetActive(false);
        Color color = ui_Fade.color;
        color.a = 1f;
        ui_Fade.color = color;
        color = ui_Story.color;
        color.a = 0;
        ui_Story.color = color;
        StartCoroutine(GameSingletonItems.FadeOut(ui_Fade, 1));

        for(int i = 0; i< cannonBalls.Length; i++)
        {
            cannonBalls[i] = Instantiate(cannonBallPrefab);
            cannonBalls[i].SetActive(false);
        }
        ui_SettingPanel.GetComponent<SettingPanel>().SettingInit();
    }
}
