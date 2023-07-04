using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public enum GameState
{
    Loading, // 게임 시작시 로딩
    Play, // 플레이
    Pause, // 일시정지
    End, // 플레이가 끝나고 복귀
    Clear, // 게임 클리어
}

public static class GameSingletonItems
{
    public static bool isNetGame = false;
    public static bool isPlayerAlive = false;
    public static bool isSettingOpen = false;
    public const int bulletAmount = 10;
    public static GameState g_State;
    public static Queue<GameObject> destroyObj = new();

    public static WaitForSeconds WT_3sec = new(3);
    public static WaitForSeconds WT_1point5sec = new(1.5f);
    public static WaitForSeconds WT_1sec = new(1f);
    public static WaitForEndOfFrame WFEOF = new();

    /// <summary>
    /// 텍스트가 진해짐
    /// </summary>
    /// <param name="text"></param>
    /// <param name="fadeTime"></param>
    /// <returns></returns>
    public static IEnumerator FadeIn(Text text, float fadeTime)
    {
        Color color = text.color;
        float timer = 0;
        while (true)
        {
            if (timer > fadeTime)
            {
                color.a = 1;
                text.color = color;
                break;
            }
            color.a = timer / fadeTime;
            text.color = color;
            timer += Time.deltaTime;
            yield return WFEOF;
        }
    }
    /// <summary>
    /// 이미지가 진해짐
    /// </summary>
    /// <param name="fadeImage"></param>
    /// <param name="fadeTime"></param>
    /// <returns></returns>
    public static IEnumerator FadeIn(Image fadeImage, float fadeTime)
    {
        Color color = fadeImage.color;
        float timer = 0;
        while (true)
        {
            if (timer > fadeTime)
            {
                color.a = 1;
                fadeImage.color = color;
                break;
            }
            color.a = timer / fadeTime;
            fadeImage.color = color;
            timer += Time.deltaTime;
            yield return WFEOF;
        }

    }

    /// <summary>
    /// 텍스트가 옅어짐
    /// </summary>
    /// <param name="text"></param>
    /// <param name="fadeTime"></param>
    /// <returns></returns>
    public static IEnumerator FadeOut(Text text, float fadeTime)
    {
        Color color = text.color;
        float timer = 0;
        while (true)
        {
            if (timer > fadeTime)
            {
                color.a = 0;
                text.color = color;
                break;
            }
            color.a = 1 - timer / fadeTime;
            text.color = color;
            timer += Time.deltaTime;
            yield return WFEOF;
        }
    }
    /// <summary>
    /// 이미지가 옅어짐
    /// </summary>
    /// <param name="fadeImage"></param>
    /// <param name="fadeTime"></param>
    /// <returns></returns>
    public static IEnumerator FadeOut(Image fadeImage, float fadeTime)
    {
        Color color = fadeImage.color;
        float timer = 0;
        while (true)
        {
            if (timer > fadeTime)
            {
                color.a = 0;
                fadeImage.color = color;
                break;
            }
            color.a = 1- timer / fadeTime;
            fadeImage.color = color;
            timer += Time.deltaTime;
            yield return WFEOF;
        }
    }
}


public class GameMgr : MonoBehaviour //B=Button
{
    //[HideInInspector]
    //public float gameTimer = 0;
    
    [HideInInspector]
    public int AchieveGoldBirdCount = 0;

    public GameObject ui_WinPanel;
    public GameObject ui_PlayerHpBar;
    public GameObject ui_SettingPanel;
    public Transform startPos; // 시작점 지정
    public Text ui_StageNumber;
    public Text ui_RockCount;
    public Image fade; // fade이미지

    public Sprite sprite_Star; 
    public GameObject ui_Stars;

    private readonly float textFadeTime = 0.75f;
    private int curStageNum;
    protected CameraFollow p_Cam; // 플레이어 카메라
    [HideInInspector]
    public GameObject p_Object; //플레이어 오브젝트

    private IEnumerator CheckPlayerState()
    {
        int playerCount = 1;
        WaitUntil WU_PlayerDeath = new WaitUntil(() => GameSingletonItems.isPlayerAlive == false);
        GameSingletonItems.g_State = GameState.Play;
        while (true)
        {
            ui_RockCount.text = playerCount + "번째 돌";
            yield return StartCoroutine(GameSingletonItems.FadeIn(ui_StageNumber, textFadeTime));
            yield return StartCoroutine(GameSingletonItems.FadeIn(ui_RockCount, textFadeTime));
            yield return StartCoroutine(GameSingletonItems.FadeOut(ui_RockCount, textFadeTime));
            yield return StartCoroutine(GameSingletonItems.FadeOut(ui_StageNumber, textFadeTime));

            ui_PlayerHpBar.SetActive(true);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            CreatPlayer();
            GameSingletonItems.isPlayerAlive = true;

            yield return StartCoroutine(GameSingletonItems.FadeOut(fade, 1));

            yield return WU_PlayerDeath; //죽거나 게임을 클리어하면 다음 내용 진행

            if (GameSingletonItems.g_State != GameState.Clear)
            {
                playerCount++;
            }

            Setting(true);
            ui_PlayerHpBar.SetActive(false);
            yield return GameSingletonItems.WT_1point5sec;
            p_Object.SetActive(false);
            yield return StartCoroutine(GameSingletonItems.FadeIn(fade, 1));

            if (GameSingletonItems.g_State == GameState.Clear)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;

                GeneralMgr.SaveAfterStageCompare(curStageNum, AchieveGoldBirdCount);
                ui_WinPanel.SetActive(true);
                GameClearInfoPrint();

                yield break; //클리어하면 코루틴 나감
            }
        }
    }

    private void GameClearInfoPrint()
    {
        for (int i = 0; i < AchieveGoldBirdCount; i++)
        {
            if (i == 3) break;
            ui_Stars.transform.GetChild(i).GetComponent<Image>().sprite = sprite_Star;
        }
    }

    private void CreatPlayer() //플레이어를 생산
    {
        if (p_Object == null) return;
        p_Object.GetComponent<PlayerControl>().PlayerReset();
        p_Object.transform.position = startPos.position; //선택한 유닛을 시작 위치로 옮김
        p_Object.SetActive(true);
        p_Cam.SetOffset(); // 카메라의 offset을 설정

        ui_PlayerHpBar.SetActive(true);
    }

    protected void Setting(bool isMustClose = false)
    {
        if (GameSingletonItems.isSettingOpen || isMustClose)
        {
            GameSingletonItems.isSettingOpen = false;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            ui_SettingPanel.SetActive(false);
        }
        else
        {
            GameSingletonItems.isSettingOpen = true;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            ui_SettingPanel.SetActive(true);
        }
    }

    public void B_Reset()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // 씬 재시작
    }
    public void B_NextStage()//다음 스테이지 번호를 받아서 넣음
    {
        if (curStageNum + 1 >= 4) return;
        SceneManager.LoadScene(curStageNum + 1);
    }

    public void B_Title() //타이틀로 버튼
    {
        SceneManager.LoadScene(0);
    }

    public void B_SelfDead()
    {
        p_Object.GetComponent<GetDamage_Player>().GetDamaged();
    }

    protected virtual void Awake()
    {
        GameSingletonItems.isNetGame = GetComponent<NetGameMgr>() != null;

        ui_SettingPanel.GetComponent<SettingPanel>().SettingInit();
        ui_PlayerHpBar.SetActive(false);
        ui_SettingPanel.SetActive(false);
        p_Cam = FindObjectOfType<CameraFollow>();
        GameSingletonItems.destroyObj.Clear();
        GameSingletonItems.g_State = GameState.Loading;

        if (GameSingletonItems.isNetGame) return;
        ui_WinPanel.SetActive(false);
        curStageNum = SceneManager.GetActiveScene().buildIndex;
        ui_StageNumber.text = "Stage " + curStageNum;
        p_Object = FindObjectOfType<PlayerControl>().gameObject;
        p_Object.GetComponent<PlayerControl>().cam_Transform = p_Cam.transform;
        p_Cam.P_Transform = p_Object.transform; //카메라의 player position을 선택한 유닛으로 지정
        
    }

    protected virtual void Start() //게임 상태를 초기화해주며 fade in
    {
        GameSingletonItems.isSettingOpen = false;

        if (GameSingletonItems.isNetGame) return;

        Color color = ui_StageNumber.color;
        color.a = 0;
        ui_StageNumber.color = color;
        color = ui_RockCount.color;
        color.a = 0;
        ui_RockCount.color = color;
        StartCoroutine(CheckPlayerState());
    }

    protected virtual void Update()
    {
        if (GameSingletonItems.g_State == GameState.Play && Input.GetKeyDown(KeyCode.Escape))
        {
            Setting(); // 셋팅창을 켜고 끄는 기능
        }

        int dequeueCount = 0;
        while (GameSingletonItems.destroyObj.Count > 0)
        {
            GameObject obj = GameSingletonItems.destroyObj.Dequeue(); // 큐안에 오브젝트가 있으면 각 프레임마다 오브젝트 파괴
            Destroy(obj);
            dequeueCount++;
            if (dequeueCount > 3) break;
        }
    }
}
