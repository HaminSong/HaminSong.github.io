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
using static NetworkManager_Server;
using System.Reflection;
using JetBrains.Annotations;
using System.Runtime.InteropServices.ComTypes;
using CommonData;
using static CommonDataClass;
using UnityEditor;
using TMPro;
using System.Linq;
using System.Net.Http;

/// <summary>
/// TCP ��Ʈ��ũ ��� Ŭ����
/// </summary>
public class TCPNetworkModule
{
    public TcpClient clientSocket;//Ŭ���̾�Ʈ ����(��� ���)
    public NetworkStream stream;//Ŭ���̾�Ʈ ��� ����

    public int ClientID;//Ŭ���̾�Ʈ �̸�

    public string strPlayerName;//Ŭ���̾�Ʈ �̸�
    //public bool isMode; // Mode( true : �÷��̾�, false: ����͸�)

    //���� ������ �������
    public byte[] buffer;
    //���� �����Ͱ� �߸� ��츦 ����Ͽ� �ӽù��ۿ� �����Ͽ� ����
    public byte[] tempBuffer;//�ӽù���
    public bool isTempByte;//�ӽù��� ����
    public int nTempByteSize;//�ӽù����� ũ��

    public bool isReady; //�غ�Ϸᴭ������

    public TCPNetworkModule(TcpClient clientSocket, int ClientID = 0)
    {
        this.clientSocket = clientSocket;
        this.stream = clientSocket.GetStream();

        this.ClientID = ClientID;

        //������ ������� �ʱ�ȭ
        this.buffer = new byte[4098];
        //�ӽù��� �ʱ�ȭ
        this.tempBuffer = new byte[4098];
        this.isTempByte = false;
        this.nTempByteSize = 0;


        

    }
}

/// <summary>
/// ��Ʈ��ũ ��� Ŭ����(TCP + UDP)
/// </summary>
public class NetworkModule : TCPNetworkModule
{
    public IPEndPoint EndPointSend;//Ŭ���̾�Ʈ EndPoint
    public UdpClient udpClient;
    public ushort udpPort;//Ŭ���̾�Ʈ�� port


    public NetworkModule(TcpClient clientSocket, ushort udpPort, int ClientID = 0) : base(clientSocket, ClientID)
    {
        this.udpPort = udpPort;
        IPEndPoint iPEndPoint = (IPEndPoint)clientSocket.Client.RemoteEndPoint;
        //tcpClient���� �޾ƿ� ������ �ּҰ��� ã�� �־��ְ� ��Ʈ�� ���Ҵ�
        EndPointSend = new IPEndPoint(iPEndPoint.Address, udpPort);
        udpClient = new UdpClient();
    }
}

public class NetworkManager_Server : MonoBehaviour
{
    public NetGameMgr netGameMgr;

    //������
    private Thread tcpListenerThread;
    private Thread udpListenerThread;

    private Thread MultiPlayerSyncThread;
    private Thread sendDestroyObjectMsgThread;
    private Thread sendBulletThrowMsgThread;
    //������ ����
    private TcpListener tcpListener ;
    private UdpClient udpSocketReceive;//���������� ��������� ������ �޴� UDP ������ �ϳ��� ��������
    public IPEndPoint IPEndPointReceive;//��� Ÿ��

    //Ŭ���̾�Ʈ ���
    public List<NetworkModule> connectedClients;//����� Ŭ���̾�Ʈ ���
    public List<NetworkModule> disconnectedClients;//���������� Ŭ���̾�Ʈ ���

    protected int udpPort;

    //���� ����
    public bool serverReady;
    //Ŭ���̾�Ʈ ID �ο� ��ȣ
    //int ClientIDIndex = 0;

    //�̺�Ʈ
    public Queue<NetworkModule> process_connectClient = new Queue<NetworkModule>();//Ŭ���̾�Ʈ ���� �̺�Ʈ
    public Queue<NetworkModule> process_disconnectClient = new Queue<NetworkModule>();//Ŭ���̾�Ʈ ���� ���� �̺�Ʈ

    public Queue<stPlayerDirectionMsg> process_PlayerDirection = new Queue<stPlayerDirectionMsg>();
    public Queue<stNumberMsg> process_PlayerJump = new Queue<stNumberMsg>();
    public Queue<int> readyPlayer = new Queue<int>();

    protected bool isRecivePlayerName = false;
    //public bool isReciveStartGameMsg = false;

    protected virtual void Awake()
    {
        udpPort = Constants.UDP_PORT_NUMBER - netGameMgr.thisStageNumber;
        //����� Ŭ���̾�Ʈ ��� �ʱ�ȭ
        connectedClients = new List<NetworkModule>();
        //���������� Ŭ���̾�Ʈ ��� �ʱ�ȭ
        disconnectedClients = new List<NetworkModule>();
    }


    // Start is called before the first frame update
    protected virtual void Start()
    {

    }

    // Update is called once per frame
    protected virtual void Update()
    {

    }

    /// <summary>
    /// ���� ���� ����ȭ�� �ʿ��� �����带 ���� �� ��������
    /// </summary>
    protected void GameServerThreadStart()
    {
        // ���ӵ� Ŭ���̾�Ʈ ������ �����ϴ� ������
        MultiPlayerSyncThread = new Thread(new ThreadStart(SendMultiPlayerSyncDataUDP));
        MultiPlayerSyncThread.IsBackground = true;
        MultiPlayerSyncThread.Start();

        sendDestroyObjectMsgThread = new Thread(new ThreadStart(SendDestroyObjectMsg));
        sendDestroyObjectMsgThread.IsBackground = true;
        sendDestroyObjectMsgThread.Start();

        sendBulletThrowMsgThread = new Thread(new ThreadStart(SendBulletThrowInfo));
        sendBulletThrowMsgThread.IsBackground = true;
        sendBulletThrowMsgThread.Start();
    }

    /// <summary>
    /// ���� ���� ��ư
    /// </summary>
    public void ServerCreate()
    {
        // TCP���� ��� ������ ����
        tcpListenerThread = new Thread(new ThreadStart(ListenForIncommingRequeset));
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Start();

        udpListenerThread = new Thread(new ThreadStart(UdpListenForIncommingRequeset));
        udpListenerThread.IsBackground = true;
        udpListenerThread.Start();
    }
    /// <summary>
    /// �ı��� ������Ʈ ������ �����ִ� ������
    /// </summary>
    public void SendDestroyObjectMsg()
    {
        try
        {
            while (true)
            {
                if (netGameMgr.destroyObjectMsg.Count > 0)
                {
                    stNumberMsg stLogInMsgData = netGameMgr.destroyObjectMsg.Dequeue();
                    BroadcastByte(GetNumberMsgToByte(stLogInMsgData));
                }
                Thread.Sleep(10);
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("SocketException " + socketException.ToString());
        }
    }

    /// <summary>
    /// �ҷ��� ������ ������ ������ ������
    /// </summary>
    public void SendBulletThrowInfo()
    {
        try
        {
            while (true)
            {
                if(Server.stBirdThrowList.Count > 0)
                {
                    stBirdThrowMsg stBirdThrowMsgData = Server.stBirdThrowList.Dequeue();
                    BroadcastByte(GetBirdThrowMsgToByte(stBirdThrowMsgData));
                }
                Thread.Sleep(20);
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("SocketException " + socketException.ToString());
        }
    }

    /// <summary>
    /// ���ӵ� Ŭ���̾�Ʈ ������ �����ϴ� ������
    /// </summary>
    public void SendMultiPlayerSyncDataUDP()
    {
        int milliSecond = 1000 / Constants.FRAME_PER_SECOND;
        try
        {
            // ������ ���ú� �׽� ���(Update)
            while (true)
            {
                if (connectedClients.Count <= 0) continue;

                stMultiPlayerSyncMsg MultiPlayerSyncMsgData = new stMultiPlayerSyncMsg();
                MultiPlayerSyncMsgData.SetMultiPlayerInfo();

                for(int i=0; i< Constants.MAX_USER_COUNT; i++)
                {
                    MultiPlayerSyncMsgData.MultiPlayerInfo[i].position = netGameMgr.playerInfo[i].Position;
                    MultiPlayerSyncMsgData.MultiPlayerInfo[i].rotation = netGameMgr.playerInfo[i].Rotation;
                }



                //Ŭ���̾�Ʈ �ϳ��� ����ȭ ������
                foreach (NetworkModule client in connectedClients)
                {
                    if(client != null)
                    {
                        //����
                        SendMsgUDP(client, GetMultiPlayerSyncMsgToByte(MultiPlayerSyncMsgData));
                    }
                    
                }
                //���� �ֱ�
                Thread.Sleep(milliSecond);
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("SocketException " + socketException.ToString());
        }
    }


    public void SendEndGameMsg()
    {
        //�÷��̾� ������ ��Ȱ��ȭ
        //���� ����� Ŭ���̾�Ʈ�� �����ִٸ�
        if (connectedClients.Count > 0)
        {
            stHeader stHeaderMsgData = new stHeader();

            stHeaderMsgData.MsgID = MessageID_ServerToClient.ServerToClientID; // �޽��� ID
            stHeaderMsgData.MsgSubID = MessageID_ServerToClient.EndGame; // �޽��� ID
            stHeaderMsgData.PacketSize = (ushort)Marshal.SizeOf(stHeaderMsgData);//�޽��� ũ��; // �������κ� �޽��� ũ��

            //��� Ŭ���̾�Ʈ����
            foreach (NetworkModule client2 in connectedClients)
            {
                if (client2 != null)
                {
                    //����
                    SendMsg(client2, GetHeaderToByte(stHeaderMsgData));
                }

            }
        }
    }

    /// <summary>
    /// ���� ������ ����
    /// </summary>
    public void ListenForIncommingRequeset()
    {
        try
        {
            // ���� ����
            tcpListener = new TcpListener(IPAddress.Any/*������ ���� ������ IP*/, Constants.TCP_PORT_NUMBER + netGameMgr.thisStageNumber);
            tcpListener.Start();

            // ���� ���� ON
            serverReady = true;

            // ������ ���ú� �׽� ���(Update)
            while (true)
            {
                // ������ ������ ���ٸ�
                if(!serverReady)
                    break;

                //���� �õ����� Ŭ���̾�Ʈ Ȯ��
                if(tcpListener != null)
                {
                    
                    if(tcpListener.Pending())
                    {
                        TcpClient tcpClient = tcpListener.AcceptTcpClient();
                        if (GameSingletonItems.g_State != GameState.Loading || connectedClients.Count >= Constants.MAX_USER_COUNT)
                        {
                            tcpClient.Close();
                            continue;
                        }
                        bool isMiddleEmpty = false;
                        int clientIdNumber = connectedClients.Count;
                        for (int i = 0; i < connectedClients.Count; i++)
                        {
                            if (connectedClients[i].ClientID != i)
                            {
                                //�߰��� ���� �׸��� �ִٸ� �װ��� �߰�
                                clientIdNumber = i;
                                isMiddleEmpty = true;
                                break;
                            }
                        }

                        //�߰��� ���� �׸��� ���ٸ� ���� �߰�
                        if (isMiddleEmpty == false)
                        {
                            //����� Ŭ���̾�Ʈ ��Ͽ� ����
                            connectedClients.Add(new NetworkModule(tcpClient, (ushort)(Constants.UDP_PORT_NUMBER + netGameMgr.thisStageNumber * Constants.MAX_USER_COUNT + clientIdNumber), clientIdNumber));
                            //Ŭ���̾�Ʈ ���� ó��(UI)
                            process_connectClient.Enqueue(connectedClients[clientIdNumber]/*��� �߰��� Ŭ���̾�Ʈ*/);
                        }
                        else
                        {
                            connectedClients.Insert(clientIdNumber, new NetworkModule(tcpClient, (ushort)(Constants.UDP_PORT_NUMBER + netGameMgr.thisStageNumber * Constants.MAX_USER_COUNT + clientIdNumber), clientIdNumber));
                            process_connectClient.Enqueue(connectedClients[clientIdNumber]/*��� �߰��� Ŭ���̾�Ʈ*/);
                        }
                        ////����� Ŭ���̾�Ʈ ��Ͽ� ����
                        //connectedClients.Add(new TCPNetworkModule(tcpListener.AcceptTcpClient(), ClientIDIndex));
                        ////Ŭ���̾�Ʈ ���� ó��(UI)
                        //process_connectClient.Enqueue(connectedClients[connectedClients.Count - 1]/*��� �߰��� Ŭ���̾�Ʈ*/);

                        // ���ӵǴ� ���ڸ�ŭ ���� ��Ų��. ���� 1000�� �̻� �����ϰ���? ��
                        //ClientIDIndex++;
                    }
                    
                }

                //���ӵ� Ŭ���̾�Ʈ ����� ��ȣ�ۿ� ó��
                foreach (NetworkModule client in connectedClients)
                {
                    if (client != null)
                    {
                        //Ŭ���̾�Ʈ ���� �����
                        if (!IsConnected(client.clientSocket))
                        {
                            //Ŭ���̾�Ʈ ���� ���� ó��(UI)
                            process_disconnectClient.Enqueue(client/*���� ������ Ŭ���̾�Ʈ*/);

                            ///�̰����� �ٷ� Ŭ���̾�Ʈ�� �����ϰ� �Ǹ� �����尣�� ������ ���̷� ������ �߻��ϹǷ� ���������� Ŭ���̾�Ʈ ������� �����Ѵ�.
                            //���������� Ŭ���̾�Ʈ ��Ͽ� �߰�
                            disconnectedClients.Add(client);

                            continue;
                        }

                        //�޽����� ���Դٸ�
                        if (client.stream.DataAvailable)
                        {
                            //�޽��� ���� ���� �ʱ�ȭ
                            Array.Clear(client.buffer, 0, client.buffer.Length);

                            //�޽����� �д´�.
                            int messageLength = client.stream.Read(client.buffer, 0, client.buffer.Length);

                            //���� ó���ϴ� ����
                            byte[] pocessBuffer = new byte[messageLength + client.nTempByteSize];//���� �о�� �޽����� ���� �޽����� ����� ���ؼ� ó���� ���� ����
                                                                                          //���Ҵ� �޽����� �ִٸ�
                            if (client.isTempByte)
                            {
                                //�� �κп� ���Ҵ� �޽��� ����
                                Array.Copy(client.tempBuffer, 0, pocessBuffer, 0, client.nTempByteSize);
                                //���� ���� �޽��� ����
                                Array.Copy(client.buffer, 0, pocessBuffer, client.nTempByteSize, messageLength);
                            }
                            else
                            {
                                //���Ҵ� �޽����� ������ ���� �о�� �޽����� ����
                                Array.Copy(client.buffer, 0, pocessBuffer, 0, messageLength);
                            }

                            //ó���ؾ� �ϴ� �޽����� ���̰� 0�� �ƴ϶��
                            if (client.nTempByteSize + messageLength > 0)
                            {
                                //���� �޽��� ó��
                                OnIncomingData(client, pocessBuffer);
                            }
                        }
                        else if(client.nTempByteSize > 0)
                        {
                            byte[] pocessBuffer = new byte[client.nTempByteSize];//���� �о�� �޽����� ���� �޽����� ����� ���ؼ� ó���� ���� ����
                            //�� �κп� ���Ҵ� �޽��� ����
                            Array.Copy(client.tempBuffer, 0, pocessBuffer, 0, client.nTempByteSize);
                            OnIncomingData(client, pocessBuffer);
                        }

                    }
                }

                ///���������� �ݺ��ϸ� ������ index�� ������
                //���������� Ŭ���̾�Ʈ ��� ó��
                for (int i = disconnectedClients.Count - 1; i >= 0; i--)
                {
                    //���ӵ� Ŭ���̾�Ʈ ��Ͽ��� ����
                    connectedClients.Remove(disconnectedClients[i]);

                    //ó���� ���������� Ŭ���̾�Ʈ ��Ͽ��� ����
                    disconnectedClients.Remove(disconnectedClients[i]);
                }

                //����� Ŭ���̾�Ʈ ���(connectedClients)�� �߰��� �Ǿ� foreach���� Ÿ�� ������ ������ �ȵ��� client�� null�� �Ǵ� ������ �߻��Ͽ� �����̸� �ش�
                Thread.Sleep(1);
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("SocketException " + socketException.ToString());
        }
    }

    /// <summary>
    /// UDP ���� ������ �Լ�
    /// </summary>
    public void UdpListenForIncommingRequeset()
    {
        try
        {
            IPEndPointReceive = new IPEndPoint(IPAddress.Any, udpPort);
            udpSocketReceive = new UdpClient(IPEndPointReceive);
            byte[] udpBuffer = new byte[2048];
            // ������ ���ú� �׽� ���(Update)
            while (true)
            {
                udpBuffer = udpSocketReceive.Receive(ref IPEndPointReceive);

                process_PlayerDirection.Enqueue(GetstPlayerDirectionMsgfromByte(udpBuffer));

                Array.Clear(udpBuffer, 0, udpBuffer.Length);
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("UDPSocketException " + socketException.ToString());
        }
    }

    /// <summary>
    /// ���� �޽��� ó��
    /// </summary>
    /// <param name="data"></param>
    private void OnIncomingData(TCPNetworkModule client, byte[] data)
    {
        // �������� ũ�Ⱑ ����� ũ�⺸�ٵ� ������
        if (data.Length < Constants.HEADER_SIZE)
        {
            Array.Copy(data, 0, client.tempBuffer, client.nTempByteSize, data.Length);     // ���� ���� ���ۿ� ���� �޽��� ����
            client.isTempByte = true;
            client.nTempByteSize += data.Length;
            return;
        }

        //����κ� �߶󳻱�(�����ϱ�)
        byte[] headerDataByte = new byte[Constants.HEADER_SIZE];
        Array.Copy(data, 0, headerDataByte, 0, headerDataByte.Length); //��� ������ ��ŭ ������ ����
        //��� ������ ����üȭ(������)
        stHeader headerData = HeaderfromByte(headerDataByte);

        // ����� ������� ���� �޽����� ����� ������
        if (headerData.PacketSize > data.Length)
        {
            Array.Copy(data, 0, client.tempBuffer, client.nTempByteSize, data.Length);     // ���� ���� ���ۿ� ���� �޽��� ����
            client.isTempByte = true;
            client.nTempByteSize += data.Length;
            return;
        }

        //����� �޽���ũ�⸸ŭ�� �޽��� �����ϱ�
        byte[] msgData = new byte[headerData.PacketSize]; //��Ŷ �и��� ���� ���� ���� ����� ��Ŷ �����ŭ ���� ����
        Array.Copy(data, 0, msgData, 0, headerData.PacketSize); //������ ���ۿ� ��Ŷ ���� ����

        //����� �޽�����
        if (headerData.MsgID == MessageID_LogIn.LoginID)//�α��� �޽���
        {
            if (headerData.MsgSubID == MessageID_LogIn.LogInMsg)//�α��� ID �޽��� ���� 
            {
                stPlayerInfoMsg stPlayerInfoMsgData = GetPlayerInfoMsgfromByte(msgData);
                connectedClients[stPlayerInfoMsgData.LogInID].strPlayerName = stPlayerInfoMsgData.UserName;
                isRecivePlayerName = true;
                SendAllPlayerName();
                if (stPlayerInfoMsgData.LogInID == 0)
                {
                    connectedClients[0].isReady = true;
                }
                else
                {
                    stNumberMsg stNumberMsgData = new stNumberMsg();
                    stNumberMsgData.MsgID = MessageID_ServerToClient.ServerToClientID;
                    stNumberMsgData.MsgSubID = MessageID_ServerToClient.ReadyGame;
                    stNumberMsgData.PacketSize = (ushort)Marshal.SizeOf(stNumberMsgData);
                    for(int i = 0; i<connectedClients.Count; i++)
                    {
                        if (i == 0) continue;
                        if (connectedClients[i].isReady)
                        {
                            stNumberMsgData.Number = (ushort)i;
                            SendMsg(connectedClients[stPlayerInfoMsgData.LogInID], GetNumberMsgToByte(stNumberMsgData));
                        }
                    }

                }
            }
        }
        if (headerData.MsgID == MessageID_ClientToServer.ClientToServerID)//������ ����ȭ �޽���(Ŭ���̾�Ʈ -> ����)
        {
            if(headerData.MsgSubID == MessageID_ClientToServer.StartGame)
            {
                stNumberMsg stNumberMsgData = GetNumberMsgfromByte(msgData);
                //0��° �÷��̾ ���� ���� ����
                if (stNumberMsgData.Number == 0)
                {
                    GameSingletonItems.g_State = GameState.Play;
                    netGameMgr.isConnectedServer = true;
                    stNumberMsgData.MsgID = MessageID_ServerToClient.ServerToClientID;
                    stNumberMsgData.MsgSubID = MessageID_ServerToClient.StartGame;
                    BroadcastByte(GetNumberMsgToByte(stNumberMsgData));
                }
            }
            else if(headerData.MsgSubID == MessageID_ClientToServer.ReadyGame)
            {
                stNumberMsg stNumberMsgData = GetNumberMsgfromByte(msgData);
                if (stNumberMsgData.Number != 0)
                {
                    readyPlayer.Enqueue(stNumberMsgData.Number);
                    stNumberMsgData.MsgID = MessageID_ServerToClient.ServerToClientID;
                    stNumberMsgData.MsgSubID = MessageID_ServerToClient.ReadyGame;
                    connectedClients[stNumberMsgData.Number].isReady = true;
                    BroadcastByte(GetNumberMsgToByte(stNumberMsgData));
                }
            }
            //else if (headerData.MsgSubID == MessageID_ClientToServer.PlayerDirection)
            //{
            //    process_PlayerDirection.Enqueue(GetstPlayerDirectionMsgfromByte(msgData));
            //}
            else if(headerData.MsgSubID == MessageID_ClientToServer.PlayerJump)
            {
                process_PlayerJump.Enqueue(GetNumberMsgfromByte(msgData));
            }
        }
        else//�ĺ����� ���� ID
        {

        }

        // ��� �޽����� ó���Ǽ� ���� �޽����� ���� ��� 
        if (data.Length == msgData.Length)
        {
            client.isTempByte = false;
            client.nTempByteSize = 0;
        }
        // �޽��� ó�� �� �޽����� �����ִ� ���
        else
        {
            //�ӽ� ���� û��
            Array.Clear(client.tempBuffer, 0, client.tempBuffer.Length);

            //������ ���ۿ� ��Ŷ ���� ����
            Array.Copy(data, msgData.Length, client.tempBuffer, 0, data.Length - (msgData.Length));// �ӽ� ���� ���ۿ� ���� �޽��� ����
            client.isTempByte = true;
            client.nTempByteSize = data.Length - (msgData.Length);
        }
    }

    /// <summary>
    /// Ŭ���̾�Ʈ ���� Ȯ��
    /// </summary>
    /// <param name="client"></param>
    /// <returns></returns>
    private bool IsConnected(TcpClient client)
    {
        try
        {
            if(client != null && client.Client != null && client.Client.Connected)
            {
                if(client.Client.Poll(0, SelectMode.SelectRead))
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
    /// ����ȭ ������ ���� ������
    /// </summary>
    public void SyncDataThread_Send()
    {
        //try
        //{
        //    // ������ ���ú� �׽� ���(Update)
        //    while (true)
        //    {
        //        if (connectedClients.Count <= 0) continue;

        //        //������ ��� Ŭ���̾�Ʈ����
        //        foreach (TCPNetworkModule targetClient in connectedClients)
        //        {
        //            //����ȭ �����Ͱ� ���� ���ٸ�
        //            if (targetClient == null || targetClient.dataSync.strClientName == null) continue;

        //            //Ŭ���̾�Ʈ �ϳ��� ����ȭ ������
        //            foreach (TCPNetworkModule client in connectedClients)
        //            {
        //                if (client.dataSync.strClientName == null)
        //                    continue;
        //                //����
        //                SendMsg(targetClient, GetDataSyncMsgToByte(client.dataSync));
        //            }
        //        }

        //        //���� �ֱ�
        //        Thread.Sleep(33);
        //    }
        //}
        //catch (SocketException socketException)
        //{
        //    Debug.Log("SocketException " + socketException.ToString());
        //}
    }

    protected void SendLogInMsg(NetworkModule client, ushort udpPortServer,ushort udpPortClient)
    {
        // ���� ���� ����ü �ʱ�ȭ
        stLogInMsg stLogInMsgData = new stLogInMsg();

        //�޽��� �ۼ�
        stLogInMsgData.MsgID = MessageID_LogIn.LoginID;//�޽��� ID
        stLogInMsgData.MsgSubID = MessageID_LogIn.LogInMsg;//�޽��� ID
        stLogInMsgData.PacketSize = (ushort)Marshal.SizeOf(stLogInMsgData);//�޽��� ũ��
        stLogInMsgData.LogInID = (ushort)client.ClientID;
        stLogInMsgData.udpPortServer = udpPortServer;
        stLogInMsgData.udpPortClient = udpPortClient;

        SendMsg(client, GetLogInMsgToByte(stLogInMsgData));
    }

    /// <summary>
    /// �Ű����� �޽��� ������
    /// </summary>
    public void SendMsg(NetworkModule client, byte[] message)
    {
        //������ �����°� �ƴ϶��
        if (!serverReady)
            return;

        //����
        client.stream.Write(message, 0, message.Length);
        client.stream.Flush();
    }
    /// <summary>
    /// �Ű����� �޽��� ������
    /// </summary>
    public void SendMsgUDP(NetworkModule client, byte[] message)
    {
        //������ �����°� �ƴ϶��
        if (!serverReady)
            return;

        //����
        client.udpClient.Send(message, message.Length, client.EndPointSend);
    }

    /// <summary>
    /// ��ε�ĳ��Ʈ �޽��� ����
    /// </summary>
    public void BroadcastByte(byte[] message)
    {
        //����� Ŭ���̾�Ʈ���� �޽��� ����
        foreach (NetworkModule client in connectedClients)
        {
            if (client != null)
                SendMsg(client, message);
        }
    }

    /// <summary>
    /// ���� �ݱ�
    /// </summary>
    public void CloseSocket()
    {
        //������ ������ ���ٸ�
        if (!serverReady)
        {
            return;
        }
        else//�ʱ�ȭ
        {
            //���� ���� �� �ʱ�ȭ
            //if (tcpListener != null)
            //{

            if (tcpListener != null)
            {
                tcpListener.Stop(); tcpListener = null;
            }
            //���� �ʱ�ȭ
            serverReady = false;

            if(udpSocketReceive != null)
            {
                udpSocketReceive.Close();
                udpSocketReceive = null;
            }

            if (tcpListenerThread != null)
            {
                //������ �ʱ�ȭ
                tcpListenerThread.Abort();
                tcpListenerThread = null;
            }
            if(udpListenerThread != null)
            {
                udpListenerThread.Abort();
                udpListenerThread = null;
            }

            if(MultiPlayerSyncThread != null)
            {
                MultiPlayerSyncThread.Abort();
                MultiPlayerSyncThread = null;
            }
            if (sendDestroyObjectMsgThread != null)
            {
                sendDestroyObjectMsgThread.Abort();
                sendDestroyObjectMsgThread = null;
            }
            if (sendBulletThrowMsgThread != null)
            {
                sendBulletThrowMsgThread.Abort();
                sendBulletThrowMsgThread = null;
            }

            //����� Ŭ���̾�Ʈ �ʱ�ȭ
            foreach (NetworkModule client in connectedClients)
            {
                if (client != null)
                {
                    client.stream.Close();
                    client.stream = null;
                    client.clientSocket.Close();
                    client.isTempByte = false;
                    client.nTempByteSize = 0;
                }
            }
            connectedClients.Clear();



            //}
        }

        Debug.Log("���� �ݱ� �Ϸ�");
    }

    /// <summary>
    /// ���� �����
    /// </summary>
    private void OnApplicationQuit()
    {
        CloseSocket();
    }
    private void SendAllPlayerName()
    {
        stAllPlayerNameMsg stAllPlayerNameMsg = new stAllPlayerNameMsg();
        stAllPlayerNameMsg.MsgID = MessageID_ServerToClient.ServerToClientID;
        stAllPlayerNameMsg.MsgSubID = MessageID_ServerToClient.AllPlayerName;
        stAllPlayerNameMsg.PacketSize = (ushort)Marshal.SizeOf(stAllPlayerNameMsg);
        stAllPlayerNameMsg.SetUserName();

        int connectedClientsCount = 0;
        for (int i = 0; i < Constants.MAX_USER_COUNT; i++)
        {
            if (i >= connectedClients.Count)
            {
                stAllPlayerNameMsg.UserName[i].name = "@!destroy!@";
                continue;
            }
            if (connectedClients[connectedClientsCount].ClientID == i)
            {
                stAllPlayerNameMsg.UserName[i].name = connectedClients[connectedClientsCount].strPlayerName;
                connectedClientsCount++;
            }
            else
                stAllPlayerNameMsg.UserName[i].name = "@!destroy!@";
        }
        //����� Ŭ���̾�Ʈ���� �޽��� ����
        BroadcastByte(GetAllPlayerNameMsgToByte(stAllPlayerNameMsg));
    }
}
