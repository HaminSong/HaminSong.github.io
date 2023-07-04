using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;
using UnityEngine.UI;
using System.Runtime.InteropServices;//마샬링을 위한 어셈블리
using System.Text.RegularExpressions;//Regex사용을 위한 어셈블리
using CommonData;
using static CommonDataClass;
using TMPro;
using Random = UnityEngine.Random;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;

public class NetworkManager_Client : MonoBehaviour
{
    public NetGameMgr netGameMgr;
    //쓰레드           
    private Thread tcpListenerThread;
    private Thread udpListenerThread;

    private Thread tcpSendMoveVectorThread;
    private Thread timerThread;

    //소켓
    private TcpClient socketConnection;
    private NetworkStream stream;
    public UdpClient udpSocketReceive;
    public UdpClient udpSocketSend;
    public IPEndPoint IPEndPointReceive;
    public IPEndPoint IPEndPointSend;

    public stLogInMsg clientLogIn;

    private string strPlayerName;

    //상태
    public bool clientReady;

    //ip
    public GameObject LogInUICanvas;
    private string ip;
    //받은 데이터 저장공간
    byte[] buffer;
    //받은 데이터가 잘릴 경우를 대비하여 임시버퍼에 저장하여 관리
    byte[] tempBuffer;//임시버퍼
    bool isTempByte;//임시버퍼 유무
    int nTempByteSize;//임시버퍼의 크기

    //클라이언트에서 플레이어가 어디로 움직이고 싶은지 나타내는 벡터
    [HideInInspector]
    public Vector3 p_MoveVec = Vector3.zero;

    public Queue<stAllPlayerNameMsg> process_AllPlayerNameMsg = new Queue<stAllPlayerNameMsg>();
    public Queue<stPlayerAliveInfo> process_PlayerAliveInfo = new Queue<stPlayerAliveInfo>();
    public Queue<stPlayerDamaged> process_PlayerDamaged = new Queue<stPlayerDamaged>();
    public Queue<stNumberMsg> process_ObjectDestroy = new Queue<stNumberMsg>();
    public Queue<stBirdThrowMsg> process_BirdThrow = new Queue<stBirdThrowMsg>();
    public Queue<int> readyPlayer = new Queue<int>();
    public Queue<int> disconnectedPlayer = new Queue<int>();

    private GameObject[] wallArray;
    private GameObject[] boomArray;
    private GameObject[] windmillArray;
    private GameObject[] trapArray;


    private bool receive_CastleGateCracked = false;
    private float elapsedTime = 0;//게임 시작 후 경과 시간을 알기 위한 변수
    // Start is called before the first frame update

    void Awake()
    {
        if (GeneralMgr.isServer)//서버인 경우
        {
            LogInUICanvas.SetActive(false);
            GetComponent<NetworkManager_Client>().enabled = false;

            return;
        }

        //서버 ip를 저장해놨다가 인풋필드에 넣어줌
        LogInUICanvas.transform.GetChild(0).GetComponent<InputField>().text = GeneralMgr.serverIp;

        if (netGameMgr.wallGroup != null)
        {
            int objectCount = 0;
            int objectChildCount = netGameMgr.wallGroup.transform.childCount;
            int objectIndex;
            for (objectIndex = 0; objectIndex < objectChildCount; objectIndex++)
            {
                objectCount += netGameMgr.wallGroup.transform.GetChild(objectIndex).childCount;
            }

            wallArray = new GameObject[objectCount];
            objectCount = 0;
            for (objectIndex = 0; objectIndex < objectChildCount; objectIndex++)
            {
                for (int k = 0; k < netGameMgr.wallGroup.transform.GetChild(objectIndex).childCount; k++)
                {
                    wallArray[objectCount++] = netGameMgr.wallGroup.transform.GetChild(objectIndex).GetChild(k).gameObject;
                }
            }
        }
        if (netGameMgr.objectGroup == null) return; //오브젝트그룹이 없거나 자식이 없으면 안함

        ObjectArrayInit(netGameMgr.objectGroup, ref boomArray, 0);
        ObjectArrayInit(netGameMgr.objectGroup, ref windmillArray, 1);
        ObjectArrayInit(netGameMgr.objectGroup, ref trapArray, 2);
    }

    void Start()
    {
        //UDP 초기화
        udpSocketReceive = null;
        udpSocketSend = null;

        //받은 데이터 저장공간 초기화
        buffer = new byte[4096];
        //임시버퍼 초기화
        tempBuffer = new byte[4096];
        isTempByte = false;
        nTempByteSize = 0;
    }

    private int processCount = 0;
    // Update is called once per frame
    void Update()
    {
        if (clientReady)
        {
            LogInUICanvas.SetActive(false);
        }
        else
        {
            LogInUICanvas.SetActive(true);
        }

        if (receive_CastleGateCracked)
        {
            receive_CastleGateCracked = false;
           netGameMgr.castleGate.GetComponent<GetDamage_CastleGate>().CrackedGate();
        }

        while(readyPlayer.Count > 0)
        {
            int readyPlayerNumber = readyPlayer.Dequeue();

            netGameMgr.ReadyPlayerNameText.transform.GetChild(readyPlayerNumber).GetChild(0).GetComponent<Image>().sprite = netGameMgr.sprite_Star;
        }
        while (disconnectedPlayer.Count > 0)
        {
            int disconnectedPlayerNumber = disconnectedPlayer.Dequeue();
            if (GameSingletonItems.g_State == GameState.Loading)
            {
                if(disconnectedPlayerNumber != 0)
                    netGameMgr.ReadyPlayerNameText.transform.GetChild(disconnectedPlayerNumber).GetChild(0).GetComponent<Image>().sprite = netGameMgr.spriteX;
                netGameMgr.ReadyPlayerNameText.transform.GetChild(disconnectedPlayerNumber).gameObject.SetActive(false);
            }
        }

        while (process_BirdThrow.Count > 0)
        {
            stBirdThrowMsg stBirdThrowMsgData = process_BirdThrow.Dequeue();
            windmillArray[stBirdThrowMsgData.IndexNumber].GetComponent<ThrowBird>().BulletThrow(stBirdThrowMsgData.Direction);
            if (processCount++ > 3) break;
        }

        processCount = 0;
        while (process_ObjectDestroy.Count > 0)
        {
            stNumberMsg objectDestroy = process_ObjectDestroy.Dequeue();
            if (objectDestroy.MsgSubID == MessageID_ServerToClient.WallDestroy)
            {
                wallArray[objectDestroy.Number].GetComponent<GetDamage_Object>().ObjectDestroy();
            }
            else if (objectDestroy.MsgSubID == MessageID_ServerToClient.BoomDestroy)
            {
                boomArray[objectDestroy.Number].GetComponent<Explosion>().ExplosionEffectOn();
            }
            else if (objectDestroy.MsgSubID == MessageID_ServerToClient.WindMillDestroy)
            {
                windmillArray[objectDestroy.Number].GetComponent<GetDamage_Object>().ObjectDestroy();
            }
            else if (objectDestroy.MsgSubID == MessageID_ServerToClient.TrapDestroy)
            {
                trapArray[objectDestroy.Number].GetComponent<GetDamage_Object>().ObjectDestroy();
            }
            else if (objectDestroy.MsgSubID == MessageID_ServerToClient.CastleGateDestroy)
            {
                netGameMgr.castleGate.GetComponent<GetDamage_Object>().ObjectDestroy();
                GameSingletonItems.isPlayerAlive = true;
            }
            if (processCount++ > 3) break;
        }
        processCount = 0;
        while (process_PlayerAliveInfo.Count > 0)
        {

            stPlayerAliveInfo stPlayerAliveInfoData = process_PlayerAliveInfo.Dequeue();
            netGameMgr.PlayerGroup[stPlayerAliveInfoData.LogInID].GetComponent<NetPlayerControl>().isAlive = stPlayerAliveInfoData.isAlive;
            if (processCount++ > 3) break;
        }
        processCount = 0;
        while (process_AllPlayerNameMsg.Count > 0)
        {
            stAllPlayerNameMsg stAllPlayerNameMsg = process_AllPlayerNameMsg.Dequeue();
            for (int i = 0; i < Constants.MAX_USER_COUNT; i++)
            {
                netGameMgr.PlayerNameText[i].text = stAllPlayerNameMsg.UserName[i].name;
                if (stAllPlayerNameMsg.UserName[i].name != "@!destroy!@")
                {
                    netGameMgr.ReadyPlayerNameText.transform.GetChild(i).GetComponent<Text>().text = stAllPlayerNameMsg.UserName[i].name;
                    netGameMgr.ReadyPlayerNameText.transform.GetChild(i).gameObject.SetActive(true);
                    netGameMgr.PlayerGroup[i].SetActive(true);
                    netGameMgr.PlayerNameText[i].gameObject.SetActive(true);
                }
                else
                {
                    netGameMgr.PlayerGroup[i].SetActive(false);
                    netGameMgr.PlayerNameText[i].gameObject.SetActive(false);
                }
            }
            if (processCount++ > 3) break;
        }
        processCount = 0;
        while (process_PlayerDamaged.Count > 0)
        {
            stPlayerDamaged stPlayerDamagedData = process_PlayerDamaged.Dequeue();
            netGameMgr.p_Object.GetComponent<GetDamage_Player>().GetDamaged(stPlayerDamagedData.Damage);
        }
    }

    /// <summary>
    /// 오브젝트 초기 설정을 하기 위한 함수
    /// </summary>
    /// <param name="objectGroup"></param>
    /// <param name="gameObjects"></param>
    /// <param name="childNumber"></param>
    private void ObjectArrayInit(GameObject objectGroup, ref GameObject[] gameObjects, int childNumber)
    {
        if (objectGroup.transform.childCount < childNumber + 1) return;

        int objectChildCount = objectGroup.transform.GetChild(childNumber).childCount;
        gameObjects = new GameObject[objectChildCount];
        for (int i = 0; i < objectChildCount; i++)
        {
            gameObjects[i] = objectGroup.transform.GetChild(childNumber).GetChild(i).gameObject;
        }
    }


    /// <summary>
    /// 서버 연결
    /// </summary>
    public void ConnectToTcpServer()
    {
        strPlayerName = GameObject.Find("PlayerName").GetComponent<InputField>().text;
        ip = GameObject.Find("ServerIP").GetComponent<InputField>().text;
        if (strPlayerName.Contains("@!destroy!@")) return;
        if (Encoding.Default.GetByteCount(strPlayerName) > Constants.MAX_NAME_LENGTH) return;

        GeneralMgr.serverIp = ip;
        //isPlayer = GameObject.Find("TogglePlayer").GetComponent<Toggle>().isOn;

        // TCP클라이언트 스레드 시작
        tcpListenerThread = new Thread(new ThreadStart(ListenForIncommingRequeset));
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Start();

        udpListenerThread = new Thread(new ThreadStart(UdpListenForIncommingRequeset));
        //udpListenerThread = new Thread(new ThreadStart(TcpListenForPlayerSyncMsg)); 
        udpListenerThread.IsBackground = true;
        udpListenerThread.Start();

        tcpSendMoveVectorThread = new Thread(new ThreadStart(SendPlayerMoveVector));
        tcpSendMoveVectorThread.IsBackground = true;

        timerThread = new Thread(new ThreadStart(TimerThread));
        timerThread.IsBackground = true;
    }

    /// <summary>
    /// TCP클라이언트 쓰레드
    /// </summary>
    private void ListenForIncommingRequeset()
    {
        try
        {
            //연결
            socketConnection = new TcpClient(ip, Constants.TCP_PORT_NUMBER + netGameMgr.thisStageNumber);
            stream = socketConnection.GetStream();
            clientReady = true;

            //데이터 리시브 항시 대기
            while (true)
            {
                //연결 끊김 감지
                if (!IsConnected(socketConnection))
                {
                    //연결 해제
                    DisConnect();
                    break;
                }

                //연결 중
                if (clientReady)
                {
                    //메시지가 들어왔다면
                    if (stream.DataAvailable)
                    {
                        //메시지 저장 공간 초기화
                        Array.Clear(buffer, 0, buffer.Length);

                        //메시지를 읽는다.
                        int messageLength = stream.Read(buffer, 0, buffer.Length);

                        //실제 처리하는 버퍼
                        byte[] pocessBuffer = new byte[messageLength + nTempByteSize];//지금 읽어온 메시지에 남은 메시지의 사이즈를 더해서 처리할 버퍼 생성
                        //남았던 메시지가 있다면
                        if (isTempByte)
                        {
                            //앞 부분에 남았던 메시지 복사
                            Array.Copy(tempBuffer, 0, pocessBuffer, 0, nTempByteSize);
                            //지금 읽은 메시지 복사
                            Array.Copy(buffer, 0, pocessBuffer, nTempByteSize, messageLength);
                        }
                        else
                        {
                            //남았던 메시지가 없으면 지금 읽어온 메시지를 저장
                            Array.Copy(buffer, 0, pocessBuffer, 0, messageLength);
                        }

                        //처리해야 하는 메시지의 길이가 0이 아니라면
                        if (nTempByteSize + messageLength > 0)
                        {
                            //받은 메시지 처리
                            OnIncomingData(pocessBuffer);
                        }
                    }
                    else if (nTempByteSize > 0)
                    {
                        byte[] pocessBuffer = new byte[nTempByteSize];//지금 읽어온 메시지에 남은 메시지의 사이즈를 더해서 처리할 버퍼 생성
                        //앞 부분에 남았던 메시지 복사
                        Array.Copy(tempBuffer, 0, pocessBuffer, 0, nTempByteSize);
                        OnIncomingData(pocessBuffer);
                    }
                }
                else//socketReady == false
                {
                    //연결 해제시
                    break;
                }
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("TCPSocketException " + socketException.ToString());

            //클라이언트 연결 실패
            clientReady = false;
        }
    }

    /// <summary>
    /// 플레이어 이동을 지속적으로 보내기 위한 쓰레드
    /// </summary>
    public void SendPlayerMoveVector()
    {
        int milliSecond = 1000 / 50;
        stPlayerDirectionMsg stPlayerDirectionMsgData = new();
        stPlayerDirectionMsgData.ClientID = (ushort)netGameMgr.playerNumber;
        while (true)
        {
            if (GameSingletonItems.isPlayerAlive && GameSingletonItems.g_State == GameState.Play)
            {
                stPlayerDirectionMsgData.MoveVector = netGameMgr.moveVector;

                SendMsgUDP(GetstPlayerDirectionMsgToByte(stPlayerDirectionMsgData));
            }
            Thread.Sleep(milliSecond);
        }
    }

    /// <summary>
    /// udp연결
    /// </summary>
    private void UdpListenForIncommingRequeset()
    {
        try
        {
            //동기화 데이터 수신
            byte[] udpBuffer = new byte[2048];
            float preSyncDelay = 0; //이전 값이 올때까지의 시간
            // 데이터 리시브 항시 대기(Update)
            while (true)
            {
                if (udpSocketReceive == null) continue;

                
                udpBuffer = udpSocketReceive.Receive(ref IPEndPointReceive);

                //동기화 데이터 저장
                stMultiPlayerSyncMsg MultiPlayerSyncMsgData = GetMultiPlayerSyncMsgfromByte(udpBuffer);

                for (int p = 0; p < Constants.MAX_USER_COUNT; p++)
                {
                    netGameMgr.playerInfo[p].PrePosition = netGameMgr.playerInfo[p].Position;
                    netGameMgr.playerInfo[p].PreRotation = netGameMgr.playerInfo[p].Rotation;
                    netGameMgr.playerInfo[p].Position = MultiPlayerSyncMsgData.MultiPlayerInfo[p].position;
                    netGameMgr.playerInfo[p].Rotation = MultiPlayerSyncMsgData.MultiPlayerInfo[p].rotation;
                }

                if (udpBuffer != null) //통신 값을 받았다면
                {
                    netGameMgr.syncDelay = elapsedTime - preSyncDelay;
                    preSyncDelay = elapsedTime;
                    netGameMgr.syncTime = 0;
                }
                Array.Clear(udpBuffer,0,udpBuffer.Length);
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("UDPSocketException " + socketException.ToString());
        }
    }

    //private void TcpListenForPlayerSyncMsg()
    //{
    //    float preSyncDelay = 0; //이전 값이 올때까지의 시간
    //    void InputPlayerInfo(stMultiPlayerSyncMsg stMultiPlayerSyncMsgData)
    //    {
    //        for (int p = 0; p < Constants.MAX_USER_COUNT; p++)
    //        {
    //            netGameMgr.playerInfo[p].PrePosition = netGameMgr.playerInfo[p].Position;
    //            netGameMgr.playerInfo[p].PreRotation = netGameMgr.playerInfo[p].Rotation;
    //            netGameMgr.playerInfo[p].Position = stMultiPlayerSyncMsgData.MultiPlayerInfo[p].position;
    //            netGameMgr.playerInfo[p].Rotation = stMultiPlayerSyncMsgData.MultiPlayerInfo[p].rotation;
    //        }
    //        netGameMgr.syncDelay = elapsedTime - preSyncDelay;
    //        preSyncDelay = elapsedTime;
    //        netGameMgr.syncTime = 0;
    //    }
    //    try
    //    {
            
    //        TcpClient _socketConnection = new TcpClient(ip, Constants.TCP_PORT_NUMBER - netGameMgr.thisStageNumber);
    //        NetworkStream _stream = _socketConnection.GetStream();
    //        int stByteSize = Marshal.SizeOf(typeof(stMultiPlayerSyncMsg));
    //        byte[] _buffer = new byte[1024];
    //        byte[] _tempBuffer = new byte[4096];

    //        byte[] syncByte = new byte[stByteSize];
    //        int _tempBufferSize = 0;
    //        while (true)
    //        {
    //            if (_stream.DataAvailable)
    //            {
    //                Array.Clear(_buffer, 0, _buffer.Length);
    //                int messageLength = _stream.Read(_buffer, 0, _buffer.Length);
    //                byte[] processByte = new byte[messageLength + _tempBufferSize];
    //                if (_tempBufferSize > 0)
    //                {
    //                    Array.Copy(_tempBuffer, 0, processByte, 0, _tempBufferSize);
    //                    Array.Copy(_buffer, 0, processByte, _tempBufferSize, messageLength);
    //                }
    //                else
    //                {
    //                    Array.Copy(_buffer, 0, processByte, 0, messageLength);
    //                }
    //                if (processByte.Length >= stByteSize)
    //                {
    //                    Array.Copy(processByte, 0, syncByte, 0, syncByte.Length);
    //                    InputPlayerInfo(GetMultiPlayerSyncMsgfromByte(syncByte));
    //                    _tempBufferSize = processByte.Length - stByteSize;
                        
    //                    Array.Clear(_tempBuffer, 0, _tempBuffer.Length);
    //                    if (_tempBufferSize >= stByteSize)
    //                    {
    //                        int divideShare = processByte.Length / stByteSize;
    //                        _tempBufferSize = processByte.Length - stByteSize * divideShare;
    //                        Array.Copy(processByte, stByteSize * divideShare, _tempBuffer, 0, _tempBufferSize);
    //                    }
    //                    //else if (_tempBufferSize > 0)
    //                    //{
    //                    //    Array.Copy(_buffer, stByteSize, _tempBuffer, 0, stByteSize);
    //                    //}
    //                }
    //                else
    //                {
    //                    Array.Copy(processByte, 0, _tempBuffer, _tempBufferSize, processByte.Length);
    //                    _tempBufferSize += processByte.Length;
    //                }
    //            }
    //            else if(_tempBufferSize >= stByteSize)
    //            {
    //                Array.Copy(_tempBuffer, 0, syncByte, 0, syncByte.Length);
    //                InputPlayerInfo(GetMultiPlayerSyncMsgfromByte(syncByte));
    //                _tempBufferSize = _tempBuffer.Length - stByteSize;
    //            }
    //        }
    //    }
    //    catch(SocketException socketException)
    //    {
    //        Debug.Log("TCPSocketException " + socketException.ToString());
    //    }
    //}

    /// <summary>
    /// 게임 시작하고 나서 경과된 시간을 재기 위한 타이머
    /// </summary>
    private void TimerThread()
    {
        while(true)
        {
            elapsedTime += 0.001f;
            Thread.Sleep(1);
        }
    }

    /// <summary>
    /// 접속 확인
    /// </summary>
    /// <param name="client"></param>
    /// <returns></returns>
    private bool IsConnected(TcpClient client)
    {
        try
        {
            if (client != null && client.Client != null && client.Client.Connected)
            {
                if (client.Client.Poll(0, SelectMode.SelectRead))
                {
                    return !(client.Client.Receive(new byte[1], SocketFlags.Peek) == 0);
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 받은 메시지 처리
    /// </summary>
    /// <param name="data"></param>
    private void OnIncomingData(byte[] data)
    {
        // 데이터의 크기가 헤더의 크기보다도 작으면
        if (data.Length < Constants.HEADER_SIZE)
        {
            Array.Copy(data, 0, tempBuffer, nTempByteSize, data.Length);     // 임지 저장 버퍼에 지금 메시지 저장
            isTempByte = true;
            nTempByteSize += data.Length;
            return;
        }

        //헤더부분 잘라내기(복사하기)
        byte[] headerDataByte = new byte[Constants.HEADER_SIZE];
        Array.Copy(data, 0, headerDataByte, 0, headerDataByte.Length); //헤더 사이즈 만큼 데이터 복사
        //헤더 데이터 구조체화(마샬링)
        CommonDataClass.stHeader headerData = CommonDataClass.HeaderfromByte(headerDataByte);

        // 헤더의 사이즈보다 남은 메시지의 사이즈가 작으면
        if (headerData.PacketSize > data.Length)
        {
            Array.Copy(data, 0, tempBuffer, nTempByteSize, data.Length);     // 임지 저장 버퍼에 지금 메시지 저장
            isTempByte = true;
            nTempByteSize += data.Length;
            return;
        }

        //헤더의 메시지크기만큼만 메시지 복사하기
        byte[] msgData = new byte[headerData.PacketSize]; //패킷 분리를 위한 현재 읽은 헤더의 패킷 사이즈만큼 버퍼 생성
        Array.Copy(data, 0, msgData, 0, headerData.PacketSize); //생성된 버퍼에 패킷 정보 복사

        //헤더의 메시지가
        if (headerData.MsgID == MessageID_LogIn.LoginID)//로그인
        {
            if (headerData.MsgSubID == MessageID_LogIn.LogInMsg)//로그인 ID 메시지 수신 
            {
                ReceiveLogInMsg(msgData);
                clientLogIn = GetLogInMsgfromByte(msgData);

                IPEndPointReceive = new IPEndPoint(IPAddress.Any, clientLogIn.udpPortClient);
                udpSocketReceive = new UdpClient(IPEndPointReceive);
                IPEndPointSend = new IPEndPoint(IPAddress.Parse(ip), clientLogIn.udpPortServer);
                udpSocketSend = new UdpClient();
            }
            else if(headerData.MsgSubID == MessageID_LogIn.ChangePlayerInfo)
            {
                disconnectedPlayer.Enqueue(GetNumberMsgfromByte(msgData).Number);
            }
        }
        if (headerData.MsgID == MessageID_ServerToClient.ServerToClientID)//동기화 데이터
        {
            if (headerData.MsgSubID == MessageID_ServerToClient.AllPlayerName)         // 게임 제어권 전송!! (첫번째 플레이어 : true, 나머지 플레이어 : false)
            {
                process_AllPlayerNameMsg.Enqueue(GetAllPlayerNameMsgfromByte(msgData));
            }
            else if (headerData.MsgSubID == MessageID_ServerToClient.StartGame)               // 게임 시작(캐릭터 움직임 제어 가능) - 이벤트
            {
                GameSingletonItems.g_State = GameState.Play;
                timerThread.Start();
            }
            else if (headerData.MsgSubID == MessageID_ServerToClient.ReadyGame)               // 게임 시작(캐릭터 움직임 제어 가능) - 이벤트
            {
                readyPlayer.Enqueue(GetNumberMsgfromByte(msgData).Number);
            }
            //else if (headerData.MsgSubID == MessageID_ServerToClient.EndGame)                 // 게임 끝(캐릭터 움직임 제어 불가) - 이벤트
            //{
            //    // 메시지 큐 필요(Update에서 수정 필요)
            //    processGameStateMsg.Enqueue(MessageID_ServerToClient.EndGame);
            //}
            else if(headerData.MsgSubID == MessageID_ServerToClient.PlayerAliveInfo)
            {
                process_PlayerAliveInfo.Enqueue(GetPlayerAliveInfofromByte(msgData));
            }
            else if (headerData.MsgSubID == MessageID_ServerToClient.PlayerDamagedInfo)
            {
                process_PlayerDamaged.Enqueue(GetPlayerDamagedfromByte(msgData));
            }
            else if (headerData.MsgSubID == MessageID_ServerToClient.BirdThrow)
            {
                process_BirdThrow.Enqueue(GetBirdThrowMsgfromByte(msgData));
            }
            else if (headerData.MsgSubID == MessageID_ServerToClient.CastleGateCracked)
            {
                receive_CastleGateCracked = true;
            }
            else if(headerData.MsgSubID == MessageID_ServerToClient.ClearGame)
            {
                netGameMgr.clearGame.Enqueue(GetNumberMsgfromByte(msgData));
            }
            else if(headerData.MsgSubID >= MessageID_ServerToClient.WallDestroy && headerData.MsgSubID <= MessageID_ServerToClient.CastleGateDestroy)
            {
                process_ObjectDestroy.Enqueue(GetNumberMsgfromByte(msgData));
            }
        }
        else//식별되지 않은 ID
        {

        }

        // 모든 메시지가 처리되서 남은 메시지가 없을 경우 
        if (data.Length == msgData.Length)
        {
            isTempByte = false;
            nTempByteSize = 0;
        }
        // 메시지 처리 후 메시지가 남아있는 경우
        else
        {
            //임시 버퍼 청소
            Array.Clear(tempBuffer, 0, tempBuffer.Length);            

            //생성된 버퍼에 패킷 정보 복사
            Array.Copy(data, msgData.Length, tempBuffer, 0, data.Length - (msgData.Length));// 임시 저장 버퍼에 남은 메시지 저장
            isTempByte = true;
            nTempByteSize = data.Length - (msgData.Length);
        }
    }

    /// <summary>
    /// 매개변수 메시지 보내기
    /// </summary>
    public void SendMsg(byte[] message)
    {
        //연결된 상태가 아니라면
        if (!clientReady)
            return;

        //전송
        stream.Write(message, 0, message.Length);
        stream.Flush();
    }

    public void SendMsgUDP(byte[] message)
    {
        //연결된 상태가 아니라면
        if (!clientReady)
            return;

        //전송
        udpSocketSend.Send(message, message.Length, IPEndPointSend);
    }

    /// <summary>
    /// 연결 해제
    /// </summary>
    public void DisConnect()
    {
        //상태 초기화
        clientReady = false;

        if (stream != null)
        {
            //stream 초기화
            stream.Close();
            stream = null;
        }
        if (socketConnection != null)
        {
            //소켓 초기화
            socketConnection.Close();
            socketConnection = null;
        }

        if(tcpListenerThread != null)
        {
            //쓰레드 초기화
            tcpListenerThread.Abort();
            tcpListenerThread = null;
        }

        if(udpListenerThread != null)
        {
            udpListenerThread.Abort();
            udpListenerThread = null;
        }

        if(udpSocketReceive !=null)
        {
            udpSocketReceive.Close();
            udpSocketReceive = null;
        }

        if (udpSocketSend != null)
        {
            udpSocketSend.Close();
            udpSocketSend = null;
        }

        if(timerThread != null)
        {
            timerThread.Abort();
            timerThread = null;
        }

        if(tcpSendMoveVectorThread != null)
        {
            tcpSendMoveVectorThread.Abort();
            tcpSendMoveVectorThread = null;
        }
    }
    
    /// <summary>
    /// 어플 종료시
    /// </summary>
    private void OnApplicationQuit()
    {
        DisConnect();
    }

    private void ReceiveLogInMsg(byte[] msgData)
    {
        stLogInMsg stLogInMsgData = GetLogInMsgfromByte(msgData);
        netGameMgr.playerNumber = stLogInMsgData.LogInID;

        netGameMgr.isConnectedServer = true;

        tcpSendMoveVectorThread.Start();

        stPlayerInfoMsg stPlayerInfoMsg = new stPlayerInfoMsg();
        stPlayerInfoMsg.MsgID = MessageID_LogIn.LoginID;
        stPlayerInfoMsg.MsgSubID = MessageID_LogIn.LogInMsg;
        stPlayerInfoMsg.PacketSize = (ushort)Marshal.SizeOf(stPlayerInfoMsg);
        stPlayerInfoMsg.LogInID = stLogInMsgData.LogInID;
        stPlayerInfoMsg.UserName = strPlayerName;

        SendMsg(GetPlayerInfoMsgToByte(stPlayerInfoMsg));
    }

    /// <summary>
    /// 게임 시작 버튼 클릭
    /// </summary>
    public void GameStart_Click()
    {
        //게임 시작 메시지 전송
        if (clientReady)
        {
            if(netGameMgr.playerNumber == 0)
            {
                for(int i = 0; i< netGameMgr.ReadyPlayerNameText.transform.childCount; i++)
                {
                    if(netGameMgr.ReadyPlayerNameText.transform.GetChild(i).GetChild(0).GetComponent<Image>().sprite == netGameMgr.spriteX
                        && netGameMgr.ReadyPlayerNameText.transform.GetChild(i).gameObject.activeSelf)
                    {
                        return;
                    }
                }
            }
            netGameMgr.StartButton.SetActive(false);
            // 정보 변경 구조체 초기화
            CommonDataClass.stNumberMsg stNumberMsgData = new CommonDataClass.stNumberMsg();

            //메시지 작성
            stNumberMsgData.MsgID = MessageID_ClientToServer.ClientToServerID;//메시지 ID
            if(netGameMgr.playerNumber == 0)
                stNumberMsgData.MsgSubID = MessageID_ClientToServer.StartGame; //플레이어 위치 정보
            else
                stNumberMsgData.MsgSubID = MessageID_ClientToServer.ReadyGame; //플레이어 위치 정보
            stNumberMsgData.PacketSize = (ushort)Marshal.SizeOf(stNumberMsgData);//메시지 크기
            stNumberMsgData.Number = (ushort)netGameMgr.playerNumber;

            //구조체 바이트화 및 전송
            SendMsg(CommonDataClass.GetNumberMsgToByte(stNumberMsgData));
        }
    }
    /// <summary>
    /// 게임 끝 버튼 클릭
    /// </summary>
    public void GameEnd_Click()
    {
        //게임 끝 메시지 전송
        if (clientReady)
        {
            // 정보 변경 구조체 초기화
            CommonDataClass.stHeader stGameStartSyncMsg = new CommonDataClass.stHeader();

            //메시지 작성
            stGameStartSyncMsg.MsgID = MessageID_ClientToServer.ClientToServerID;//메시지 ID
            stGameStartSyncMsg.MsgSubID = MessageID_ClientToServer.FinishGame; //플레이어 위치 정보
            stGameStartSyncMsg.PacketSize = (ushort)Marshal.SizeOf(stGameStartSyncMsg);//메시지 크기
            //stGameStartSyncMsg.strClientName = clientLogIn.strLogInID;

            //구조체 바이트화 및 전송
            SendMsg(CommonDataClass.GetHeaderToByte(stGameStartSyncMsg));
        }
    }
}
