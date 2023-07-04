using CommonData;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static CommonDataClass;

public struct stTransformInfo
{
    public Vector3 PrePosition;
    public Quaternion PreRotation;
    public Vector3 Position; 
    public Quaternion Rotation;
}

public class NetGameMgr : GameMgr
{
    public NetworkManager_Server Server;
    public NetworkManager_Client Client;
    public GameObject ui_ButtonTitle;
    public GameObject ui_Disconnected;

    public float acceleration = 80f;
    public float jumpPower = 60f;

    public GameObject objectGroup;
    public GameObject wallGroup;
    public GameObject castleGate;

    public GameObject[] PlayerGroup;
    public Text[] PlayerNameText;
    public GameObject ReadyPlayerNameText;
    [HideInInspector]
    public bool isConnectedServer = false;
    [HideInInspector]
    public int playerNumber = 0;
    [HideInInspector]
    public Vector3 moveVector;
    [HideInInspector]
    public stTransformInfo[] playerInfo;

    public Sprite spriteX;
    public GameObject StartButton;

    public Queue<stNumberMsg> destroyObjectMsg = new Queue<stNumberMsg>();
    public Queue<stNumberMsg> clearGame = new Queue<stNumberMsg>();

    [HideInInspector]
    public float syncTime = 0; //보간 처리를 위한 변수
    [HideInInspector]
    public float syncDelay; //값이 올 때까지 걸린 시간

    public int thisStageNumber = 0;

    private NetPlayerControl netPlayerControl;

    private byte[] jumpMsgByte;
    private byte[] selfDeadMsgByte;
    private int playerIndex = 0;
    public void ButtonTitle()
    {
        if(GeneralMgr.isServer)
        {
            Server.CloseSocket();
        }
        else
        {
            Client.DisConnect();
        }
        
        SceneManager.LoadScene(0);
    }
    
    public void ButtonnSelfDead()
    {
        Client.SendMsg(selfDeadMsgByte);
    }

    /// <summary>
    /// 서버에서 넘버링을 받아오는지 확인하는 코루틴
    /// </summary>
    /// <returns></returns>
    private IEnumerator WaitConnected()
    {
        yield return new WaitUntil(() => isConnectedServer);
        ReadyPlayerNameText.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = sprite_Star;
        if (playerNumber != 0)
        {
            StartButton.transform.GetChild(0).GetComponent<Text>().text = "Ready";
        }

        StartButton.SetActive(true);
        p_Object = PlayerGroup[playerNumber];
        p_Object.GetComponent<GetDamage_Player>().isClientPlayer = true;
        netPlayerControl = p_Object.GetComponent<NetPlayerControl>();
        p_Cam.P_Transform = PlayerGroup[playerNumber].transform;
        p_Cam.SetOffset();
        StartCoroutine(WaitDisconnected());

        stNumberMsg stJumpMsgData = new stNumberMsg();
        stJumpMsgData.MsgID = MessageID_ClientToServer.ClientToServerID;
        stJumpMsgData.MsgSubID = MessageID_ClientToServer.PlayerJump;
        stJumpMsgData.PacketSize = (ushort)Marshal.SizeOf(stJumpMsgData);
        stJumpMsgData.Number = (ushort)playerNumber;
        jumpMsgByte = new byte[Marshal.SizeOf(stJumpMsgData)];
        jumpMsgByte = GetNumberMsgToByte(stJumpMsgData);

        stNumberMsg stSelfDeadMsgData = new stNumberMsg();
        stSelfDeadMsgData.MsgID = MessageID_ClientToServer.ClientToServerID;
        stSelfDeadMsgData.MsgSubID = MessageID_ClientToServer.PlayerSelfDead;
        stSelfDeadMsgData.PacketSize = (ushort)Marshal.SizeOf(stSelfDeadMsgData);
        stSelfDeadMsgData.Number = (ushort)playerNumber;
        selfDeadMsgByte = new byte[Marshal .SizeOf(stSelfDeadMsgData)];
        selfDeadMsgByte = GetNumberMsgToByte(stSelfDeadMsgData);
    }

    /// <summary>
    /// 모든 연결이 끊기면 서버, 클라 초기화
    /// </summary>
    /// <returns></returns>
    public IEnumerator WaitDisconnected()
    {
        if (GeneralMgr.isServer)
        {
            yield return new WaitUntil(() => isConnectedServer == false);
        }
        else
        {
            yield return new WaitUntil(() => Client.clientReady == false);
            isConnectedServer = false;
        }
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        ui_Disconnected.SetActive(true);
        Color color = fade.color;
        color.a = 1;
        fade.color = color;
        fade.gameObject.SetActive(true );
        if (GeneralMgr.isServer)
        {
            FindObjectOfType<Server>().CloseSocket();
            yield return GameSingletonItems.WT_1sec;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            FindObjectOfType<NetworkManager_Client>().DisConnect();
            yield return GameSingletonItems.WT_3sec;
            SceneManager.LoadScene(0);
        }
    }

    /// <summary>
    /// 서버, 클라에서 플레이가 죽었는지 체크하고 리스폰 시켜줌
    /// </summary>
    /// <param name="playerIndex"></param>
    /// <returns></returns>
    public IEnumerator CheckNetPlayerState(int playerIndex)
    {
        yield return new WaitUntil(() => GameSingletonItems.g_State == GameState.Play);
        
        if (PlayerGroup[playerIndex].gameObject.activeSelf == false)
        {
            yield break;
        }

        NetPlayerControl netPlayerControl = PlayerGroup[playerIndex].GetComponent<NetPlayerControl>();
        WaitUntil WU_PlayerDeath = new (() => netPlayerControl.isAlive == false);

        if(GeneralMgr.isServer)
        {
            PlayerGroup[playerIndex].transform.position = startPos.transform.GetChild(playerIndex).position + Vector3.up * 3;
        }
        bool isClientPlayer = playerIndex == playerNumber;
        if (isClientPlayer)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            ReadyPlayerNameText.SetActive(false);
            ui_ButtonTitle.SetActive(false);
        }
        while (true)
        {
            PlayerGroup[playerIndex].SetActive(true);
            PlayerGroup[playerIndex].GetComponent<GetDamage_Player>().HpReset();
            if (GeneralMgr.isServer == false)
            {
                if (isClientPlayer)
                {
                    p_Cam.SetOffset();
                    ui_PlayerHpBar.SetActive(true);
                    ui_PlayerHpBar.GetComponent<Slider>().value = 1;
                    GameSingletonItems.isPlayerAlive = true;
                    yield return StartCoroutine(GameSingletonItems.FadeOut(fade, 1));
                }
            }
            else
            {
                GameSingletonItems.isPlayerAlive = true;
            }
            
            netPlayerControl.isAlive = true;
            yield return WU_PlayerDeath;

            if (GeneralMgr.isServer == false)
            {
                if (isClientPlayer)
                {
                    Setting(true);
                    ui_PlayerHpBar.SetActive(false);
                    GameSingletonItems.isPlayerAlive = false;
                }
                yield return GameSingletonItems.WT_1point5sec;
                if (castleGate.activeSelf == false)
                {
                    p_Cam.SetCameraShakeTimer(0);
                    GameSingletonItems.isPlayerAlive = true;
                    break;
                }
                PlayerGroup[playerIndex].SetActive(false);
                
                if (isClientPlayer)
                    yield return StartCoroutine(GameSingletonItems.FadeIn(fade, 1));
                //else
                //    yield return GameSingletonItems.WT_1sec;
            }
            else
            {
                yield return GameSingletonItems.WT_1point5sec;
                netPlayerControl.GetComponent<Rigidbody>().velocity = Vector3.zero;
                netPlayerControl.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
                PlayerGroup[playerIndex].SetActive(false);
                if (PlayerGroup[playerIndex].transform == p_Cam.P_Transform)
                    yield return StartCoroutine(GameSingletonItems.FadeIn(fade, 1));
                
            }
            
            PlayerGroup[playerIndex].transform.position = startPos.transform.GetChild(playerIndex).position + Vector3.up * 3;
            yield return GameSingletonItems.WT_1sec;
        }
    }

    public IEnumerator WaitClear()
    {
        if (GeneralMgr.isServer)
        {
            yield return new WaitUntil(() => GameSingletonItems.g_State == GameState.Clear);
            stNumberMsg stClearGame = new stNumberMsg();
            stClearGame.MsgID = MessageID_ServerToClient.ServerToClientID;
            stClearGame.MsgSubID = MessageID_ServerToClient.ClearGame;
            stClearGame.PacketSize = (ushort)Marshal.SizeOf(stClearGame);
            stClearGame.Number = (ushort)playerNumber;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            FindObjectOfType<Server>().BroadcastByte(GetNumberMsgToByte(stClearGame));
        }
        else
        {
            yield return new WaitUntil(() => clearGame.Count > 0);
            Setting(true);
            ui_PlayerHpBar.SetActive(false);
            yield return StartCoroutine(GameSingletonItems.FadeIn(fade, 1));
            yield return GameSingletonItems.WT_1point5sec;
            stNumberMsg stClearGame = clearGame.Dequeue();

            if (playerNumber == stClearGame.Number)
            {
                PlayerGroup[stClearGame.Number].GetComponent<NetPlayerControl>().isAlive = true;
                //GameSingletonItems.isPlayerAlive = true;
                GameSingletonItems.g_State = GameState.Play;
            }
            else
                GameSingletonItems.g_State = GameState.Clear;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            p_Cam.P_Transform = PlayerGroup[stClearGame.Number].transform;
        }
        yield return StartCoroutine(GameSingletonItems.FadeOut(fade, 1));
        yield return GameSingletonItems.WT_1point5sec;
        ui_ButtonTitle.SetActive(true);

        yield return new WaitForSeconds(60);
        isConnectedServer = false;
    }

    private void P_InputMove(ref Vector3 moveVec) //플레이어 움직임
    {
        Vector3 p_Forward = p_Object.transform.position - p_Cam.transform.position;
        p_Forward.y = 0;
        float vertical = Input.GetAxis("Vertical");
        float horizontal = Input.GetAxis("Horizontal");
        moveVec = p_Forward * vertical + Quaternion.AngleAxis(90, Vector3.up) * p_Forward * horizontal;
        moveVec.Normalize();
    }

    protected override void Awake()
    {
        base.Awake();

        ui_Disconnected.SetActive(false);
        StartButton.SetActive(false);
        ui_ButtonTitle.SetActive(true);
        if (GeneralMgr.isServer == false)
        {
            StartCoroutine(WaitConnected());
        }
        StartCoroutine(WaitClear());

        playerInfo = new stTransformInfo[Constants.MAX_USER_COUNT];
        for (int i = 0; i < playerInfo.Length; i++)
        {
            playerInfo[i].Position = Vector3.zero;
            playerInfo[i].Rotation = Quaternion.identity;
            playerInfo[i].PrePosition = Vector3.zero;
            playerInfo[i].PreRotation = Quaternion.identity;
        }

        for(int i = 0; i < ReadyPlayerNameText.transform.childCount; i++)
        {
            ReadyPlayerNameText.transform.GetChild(i).gameObject.SetActive(false);
        }


        for (int i = 0; i < PlayerGroup.Length; i++)
        {
            if (GeneralMgr.isServer == false)
            {
                PlayerGroup[i].GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
                PlayerGroup[i].GetComponent<SphereCollider>().radius += 0.2f;
            }
            PlayerGroup[i].GetComponent<Rigidbody>().useGravity = GeneralMgr.isServer;
            PlayerGroup[i].SetActive(true);
            PlayerNameText[i].gameObject.SetActive(true);
        }
    }

    protected override void Start()
    {
        base.Start();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        for (int i = 0; i< Constants.MAX_USER_COUNT; i++)
        {
            StartCoroutine(CheckNetPlayerState(i));
        }
        p_Cam.P_Transform = PlayerGroup[playerIndex].transform;
        p_Cam.SetOffset();
    }

    protected override void Update()
    {
        if (GameSingletonItems.g_State == GameState.Play || GameSingletonItems.g_State == GameState.Clear)
        {
            syncTime += Time.deltaTime;
            for (int i = 0; i < PlayerGroup.Length; i++)
            {
                if (GeneralMgr.isServer == false)
                {
                    if (PlayerGroup[i].activeSelf == false) continue;
                    PlayerGroup[i].transform.SetPositionAndRotation(Vector3.Lerp(playerInfo[i].PrePosition, playerInfo[i].Position, syncTime / syncDelay),
                        Quaternion.Lerp(playerInfo[i].PreRotation, playerInfo[i].Rotation, syncTime / syncDelay));
                }
                else
                {
                    playerInfo[i].Position = PlayerGroup[i].transform.position;
                    playerInfo[i].Rotation = PlayerGroup[i].transform.rotation;
                }
            }

        }
        if (GeneralMgr.isServer)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Setting(); // 셋팅창을 켜고 끄는 기능
            }
            int processCount = 0;
            while (GameSingletonItems.destroyObj.Count > 0)
            {
                stNumberMsg stDestroyMsgData = new stNumberMsg();
                stDestroyMsgData.MsgID = MessageID_ServerToClient.ServerToClientID;
                stDestroyMsgData.PacketSize = (ushort)Marshal.SizeOf(stDestroyMsgData);
                GameObject obj = GameSingletonItems.destroyObj.Dequeue(); // 큐안에 오브젝트가 있으면 각 프레임마다 오브젝트 파괴
                if (obj.CompareTag("Wall"))
                {
                    stDestroyMsgData.MsgSubID = MessageID_ServerToClient.WallDestroy;
                    stDestroyMsgData.Number = (ushort)obj.GetComponent<GetDamage_Object>().indexNumber;
                }
                else if (obj.CompareTag("Boom"))
                {
                    stDestroyMsgData.MsgSubID = MessageID_ServerToClient.BoomDestroy;
                    stDestroyMsgData.Number = (ushort)obj.GetComponent<Explosion>().indexNumber;
                }
                else if (obj.CompareTag("WindMill"))
                {
                    stDestroyMsgData.MsgSubID = MessageID_ServerToClient.WindMillDestroy;
                    stDestroyMsgData.Number = (ushort)obj.GetComponent<GetDamage_Object>().indexNumber;
                }
                else if (obj.CompareTag("Trap"))
                {
                    stDestroyMsgData.MsgSubID = MessageID_ServerToClient.TrapDestroy;
                    stDestroyMsgData.Number = (ushort)obj.GetComponent<GetDamage_Object>().indexNumber;
                }
                else if (obj.CompareTag("CastleGate"))
                {
                    stDestroyMsgData.MsgSubID = MessageID_ServerToClient.CastleGateDestroy;
                    stDestroyMsgData.Number = (ushort)obj.GetComponent<GetDamage_Object>().indexNumber;
                }
                destroyObjectMsg.Enqueue(stDestroyMsgData);
                if (processCount++ > 3) break;
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                while (true)
                {
                    playerIndex++;
                    if (playerIndex >= Constants.MAX_USER_COUNT)
                        playerIndex = 0;
                    if (PlayerGroup[playerIndex].activeSelf) break;
                }
                p_Cam.P_Transform = PlayerGroup[playerIndex].transform;
            }
        }
        else
        {
            if (GameSingletonItems.g_State == GameState.Play)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    Setting(); // 셋팅창을 켜고 끄는 기능
                }
                if (Input.GetKeyDown(KeyCode.Space) && netPlayerControl.isGround)
                {
                    Client.SendMsg(jumpMsgByte);
                }
                if (netPlayerControl.isAlive)
                {
                    P_InputMove(ref moveVector);
                }
                else
                {
                    moveVector = Vector3.zero;
                }
            }
        }
    }
}
