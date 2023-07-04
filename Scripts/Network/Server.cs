using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine.UI;
using System.IO;
using System.Runtime.InteropServices;//�������� ���� �����
using System.Text.RegularExpressions;//Regex����� ���� �����
using CommonData;
//using UnityEditor.PackageManager;
using static CommonDataClass;
using System.Linq;
using UnityEngine.SceneManagement;

public class Server : NetworkManager_Server
{
    public static Queue<stBirdThrowMsg> stBirdThrowList;

    private Rigidbody[] playerRigidbodyGroup = new Rigidbody[Constants.MAX_USER_COUNT];

    //�α�
    private List<string> logList;//data

    // ������ �÷��̾� ����
    private Dictionary<int, GameObject> dicCreatePlayerID = new Dictionary<int, GameObject>();//data


    protected override void Awake()
    {
        base.Awake();

        if (!GeneralMgr.isServer) //������ �ƴ� ���
        {
            GetComponent<Server>().enabled = false;
            return;
        }

        if(stBirdThrowList  == null) stBirdThrowList = new Queue<stBirdThrowMsg>(); else stBirdThrowList.Clear();

        //���� ���� �ʱ�ȭ
        serverReady = false;
        //�α� �ʱ�ȭ
        logList = new List<string>();

        int indexCount = 0;
        Transform wallGroupTransform = netGameMgr.wallGroup.transform;
        for (int i = 0; i < wallGroupTransform.childCount; i++)
        {
            for (int k = 0; k < wallGroupTransform.GetChild(i).childCount; k++)
            {
                wallGroupTransform.GetChild(i).GetChild(k).GetComponent<GetDamage_Object>().indexNumber = indexCount++;
            }
        }

        ObjectIndexNumberInit(netGameMgr.objectGroup);

        ServerCreate();
    }


    protected override void Start()
    {
        for (int i = 0; i < netGameMgr.PlayerGroup.Length; i++)
        {
            playerRigidbodyGroup[i] = netGameMgr.PlayerGroup[i].GetComponent<Rigidbody>();
        }


        StartCoroutine(WaitGamePlay());
    }

    private void FixedUpdate()
    {
        int dequeueCount = 0;
        while (process_PlayerDirection.Count > 0)
        {
            stPlayerDirectionMsg stPlayerDirectionMsgData = process_PlayerDirection.Dequeue();
            int clientNumber = stPlayerDirectionMsgData.ClientID;
            if (playerRigidbodyGroup[clientNumber].GetComponent<NetPlayerControl>().isGround)
                playerRigidbodyGroup[clientNumber].AddForceAtPosition(stPlayerDirectionMsgData.MoveVector * netGameMgr.acceleration, playerRigidbodyGroup[clientNumber].transform.position + Vector3.up * 0.1f);
            if (dequeueCount++ > 4) break;
        }
        dequeueCount = 0;
        while (process_PlayerJump.Count > 0)
        {
            stNumberMsg stLogInMsgData = process_PlayerJump.Dequeue();
            int clientNumber = stLogInMsgData.Number;
            if (playerRigidbodyGroup[clientNumber].GetComponent<NetPlayerControl>().isGround)
                playerRigidbodyGroup[clientNumber].AddForce(Vector3.up * netGameMgr.jumpPower, ForceMode.Impulse);
            if (dequeueCount++ > 4) break;
        }
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

        //if (isReciveStartGameMsg)
        //{
        //    isReciveStartGameMsg = false;
        //    //gameController.GetComponent<GameController>().isReciveStart = true;
        //}

        if (isRecivePlayerName)
        {
            isRecivePlayerName=false;
            for (int i =0; i<connectedClients.Count; i++)
            {
                netGameMgr.ReadyPlayerNameText.transform.GetChild(connectedClients[i].ClientID).gameObject.SetActive(true);
                netGameMgr.ReadyPlayerNameText.transform.GetChild(connectedClients[i].ClientID).GetComponent<Text>().text = connectedClients[i].strPlayerName;
            }
        }

        //����� Ŭ���̾�Ʈ ����
        if (process_connectClient.Count > 0)
        {
            
            //ť���� Ŭ���̾�Ʈ ����
            NetworkModule connectClient = process_connectClient.Dequeue();
            //����� Ŭ���̾�Ʈ���� ID�ο� �޽��� ����
            SendLogInMsg(connectClient, (ushort)udpPort, connectClient.udpPort);

            // ������ �÷��̾� ���� ����!!
            dicCreatePlayerID.Add(connectClient.ClientID, netGameMgr.PlayerGroup[connectClient.ClientID]);

            //TCP�� ����� Ŭ���̾�Ʈ�� IP��������
            IPEndPoint tcpEndPoint = (IPEndPoint)connectedClients[connectedClients.Count - 1].clientSocket.Client.RemoteEndPoint;
            logList.Add(tcpEndPoint.Address.ToString());

        }
        //���� ���� Ŭ���̾�Ʈ ����
        if (process_disconnectClient.Count > 0)
        {
            //�α� ���
            logList.Add("[�ý���] Ŭ���̾�Ʈ ���� ����");

            //ť���� Ŭ���̾�Ʈ ����
            NetworkModule disconnectClient = process_disconnectClient.Dequeue();

            netGameMgr.ReadyPlayerNameText.transform.GetChild(disconnectClient.ClientID).gameObject.SetActive(false);
            if(disconnectClient.ClientID != 0)
                netGameMgr.ReadyPlayerNameText.transform.GetChild(disconnectClient.ClientID).GetChild(0).GetComponent<Image>().sprite = netGameMgr.spriteX;

            stNumberMsg stNumberMsgData = new stNumberMsg();
            stNumberMsgData.MsgID = MessageID_LogIn.LoginID;
            stNumberMsgData.MsgSubID = MessageID_LogIn.ChangePlayerInfo;
            stNumberMsgData.PacketSize = (ushort)Marshal.SizeOf(stNumberMsgData);
            stNumberMsgData.Number = (ushort)disconnectClient.ClientID;

            BroadcastByte(GetNumberMsgToByte(stNumberMsgData));

            // ���� ������ �÷��̾� ���� ����!!
            dicCreatePlayerID.Remove(disconnectClient.ClientID);

            //Ŭ�� ���� ������ 0�̸�
            if (connectedClients.Count == 0) netGameMgr.isConnectedServer = false;
        }

        if(readyPlayer.Count > 0)
        {
            int readyPlayerNumber = readyPlayer.Dequeue();
            netGameMgr.ReadyPlayerNameText.transform.GetChild(readyPlayerNumber).GetChild(0).GetComponent<Image>().sprite = netGameMgr.sprite_Star;
            
        }

        //�α׸���Ʈ�� �׿��ٸ�
        if (logList.Count > 0)
        {
            //����
            WriteLog(logList[0]);
            logList.RemoveAt(0);
        }
    }

    /// <summary>
    /// ������Ʈ �ʱ� ������ �ϱ� ���� �Լ�
    /// </summary>
    private void ObjectIndexNumberInit(GameObject objectGroup)
    {
        for (int childNumber = 0; childNumber < objectGroup.transform.childCount; childNumber++)
        {
            Transform objectGroupChild = objectGroup.transform.GetChild(childNumber);
            int indexCount = 0;
            if (objectGroupChild.GetChild(0).CompareTag("Boom"))
            {
                for (int i = 0; i < objectGroupChild.childCount; i++)
                {
                    objectGroupChild.GetChild(i).GetComponent<Explosion>().indexNumber = indexCount++;
                }
            }
            else
            {
                for (int i = 0; i < objectGroupChild.childCount; i++)
                {
                    objectGroupChild.GetChild(i).GetComponent<GetDamage_Object>().indexNumber = indexCount++;
                }
            }
        }
    }

    private IEnumerator WaitGamePlay()
    {
        yield return new WaitUntil(() => GameSingletonItems.g_State == GameState.Play);
        GameServerThreadStart();
        StartCoroutine(netGameMgr.WaitDisconnected());
        netGameMgr.fade.gameObject.SetActive(false);
        int connectedClientsCount = 0;
        for (int i = 0; i < Constants.MAX_USER_COUNT; i++)
        {
            if (i >= connectedClients.Count || connectedClients[connectedClientsCount].ClientID != i)
            {
                netGameMgr.PlayerGroup[i].SetActive(false);
                netGameMgr.PlayerNameText[i].gameObject.SetActive(false);
                continue;
            }

            netGameMgr.PlayerNameText[i].text = connectedClients[connectedClientsCount].strPlayerName;
            netGameMgr.PlayerGroup[i].GetComponent<NetPlayerControl>().isAlive = true;
            StartCoroutine(SendPlayerDamaged(connectedClientsCount));
            StartCoroutine(SendPlayerAliveMsg(connectedClientsCount));
            StartCoroutine(SendCastleGateCracked());
            connectedClientsCount++;

        }
        netGameMgr.ReadyPlayerNameText.SetActive(false);
        netGameMgr.ui_ButtonTitle.SetActive(false);
    }

    private IEnumerator SendPlayerDamaged(int playerNumber)
    {
        stPlayerDamaged stPlayerDamagedData = new stPlayerDamaged();
        stPlayerDamagedData.MsgID = MessageID_ServerToClient.ServerToClientID;
        stPlayerDamagedData.MsgSubID = MessageID_ServerToClient.PlayerDamagedInfo;
        stPlayerDamagedData.PacketSize = (ushort)Marshal.SizeOf(stPlayerDamagedData);

        GetDamage_Player getDamage_Player = netGameMgr.PlayerGroup[playerNumber].GetComponent<GetDamage_Player>();
        float preHp = getDamage_Player.GetCurHpValue();
        WaitUntil WU_Damaged = new WaitUntil(() => preHp != getDamage_Player.GetCurHpValue());

        NetworkModule client = null;

        for(int i = 0; i < connectedClients.Count; i++)
        {
            if (connectedClients[i].ClientID == playerNumber)
            {
                client = connectedClients[i];
                break;
            }
        }
        if (client == null) yield break;

        while (true)
        {
            yield return WU_Damaged;
            stPlayerDamagedData.Damage = preHp - getDamage_Player.GetCurHpValue();
            preHp = getDamage_Player.GetCurHpValue();
            SendMsg(client, GetPlayerDamagedToByte(stPlayerDamagedData));
        }
    }

    private IEnumerator SendPlayerAliveMsg(int playerNumber)
    {
        NetPlayerControl netPlayerControl = netGameMgr.PlayerGroup[playerNumber].GetComponent<NetPlayerControl>();
        WaitUntil WU_PlayerDeath = new(() => netPlayerControl.isAlive == false || GameSingletonItems.g_State == GameState.Clear);
        WaitUntil WU_PlayerRevive = new(() => netPlayerControl.isAlive == true || GameSingletonItems.g_State == GameState.Clear);
        stPlayerAliveInfo stPlayerAliveInfoData = new stPlayerAliveInfo();
        stPlayerAliveInfoData.MsgID = MessageID_ServerToClient.ServerToClientID;
        stPlayerAliveInfoData.MsgSubID = MessageID_ServerToClient.PlayerAliveInfo;
        stPlayerAliveInfoData.PacketSize = (ushort)Marshal.SizeOf(stPlayerAliveInfoData);
        stPlayerAliveInfoData.LogInID = (ushort)playerNumber;
        stPlayerAliveInfoData.isAlive = false;
        while (true)
        {
            yield return WU_PlayerDeath;
            BroadcastByte(GetPlayerAliveInfoToByte(stPlayerAliveInfoData));
            if (GameSingletonItems.g_State == GameState.Clear) break;
            yield return WU_PlayerRevive;
        }
    }

    private IEnumerator SendCastleGateCracked()
    {
        GetDamage_CastleGate getDamage_CastleGate = netGameMgr.castleGate.GetComponent<GetDamage_CastleGate>();
        stHeader stCastleGateCrackedMsgData = new stHeader();
        stCastleGateCrackedMsgData.MsgID = MessageID_ServerToClient.ServerToClientID;
        stCastleGateCrackedMsgData.MsgSubID = MessageID_ServerToClient.CastleGateCracked;
        stCastleGateCrackedMsgData.PacketSize = (ushort)Marshal.SizeOf(stCastleGateCrackedMsgData);
        getDamage_CastleGate.isCracked = false;
        yield return new WaitUntil(() => getDamage_CastleGate.isCracked);
        BroadcastByte(GetHeaderToByte(stCastleGateCrackedMsgData));
    }

    /// <summary>
    /// �޽��� Ȯ��
    /// </summary>
    /// <param name="message"></param>
    public void WriteLog(/*Time*/string message)
    {
        //\0 ���� : ������ Byte�� �ԷµǸ� ������ �޽����� ���õ��� ����
        message = Regex.Replace(message, "[\\0]", string.Empty);
        //�޽��� Ȯ��
        Debug.Log(message);
    }
}