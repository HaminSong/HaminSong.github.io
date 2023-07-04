using System.Runtime.InteropServices;
using System;
using UnityEngine;

namespace CommonData
{
    
    /// <summary>
    /// ��� Ŭ����
    /// </summary>
    static public class Constants
    {
        public const int HEADER_ID_SIZE = 4;//��� ������� 6(4(ID) + 2(Size))
        public const int HEADER_SIZE = HEADER_ID_SIZE + 2;//��� ������� 6(4(ID) + 2(Size))

        public const int MAX_NAME_LENGTH = 32;
        public const int MAX_USER_COUNT = 4;//�ִ� ����� ��

        public const int FRAME_PER_SECOND = 30;//1�ʴ� ������
        public const int TCP_PORT_NUMBER = 9100;
        public const int UDP_PORT_NUMBER = 9200;
    }

    /// <summary>
    /// �α��� �޽���
    /// </summary>
    static public class MessageID_LogIn
    {
        //ID
        public const ushort LoginID = 0x00;
        //SubID
        public const ushort LogInMsg = 0x00;                        //ID �ο�
        public const ushort ChangePlayerInfo = 0x01;                //�÷��̾� ���� ���� (�̸�,Mode[�÷��̾�,����͸�])
    }

    /// <summary>
    /// ������ ����ȭ �޽���(Ŭ���̾�Ʈ -> ����)
    /// </summary>
    static public class MessageID_ClientToServer
    {
        //ID
        public const ushort ClientToServerID    = 0x01;

        //SubID
        public const ushort PlayerPosition      = 0x00;         //�÷��̾� ��ġ ����
        public const ushort StartGame           = 0x01;         // ���� ����
        public const ushort FinishGame          = 0x02;         // ���� ����
        public const ushort ReadyGame           = 0x03;         // �غ� �Ϸ�
        public const ushort PlayerDirection     = 0x04;         // �÷��̾� �̵� ����
        public const ushort PlayerJump          = 0x05;         // �÷��̾� ����
        public const ushort PlayerSelfDead      = 0x06;         // �÷��̾� �ڻ�
    }

    /// <summary>
    /// ������ ����ȭ �޽���(���� -> Ŭ���̾�Ʈ)
    /// </summary>
    static public class MessageID_ServerToClient
    {
        //ID
        public const ushort ServerToClientID    = 0x02;
        //SubID
        public const ushort AllPlayerName       = 0x00;         // ���� ����� ����!! (ù��° �÷��̾� : true, ������ �÷��̾� : false)
                                                                // public const ushort InitTokenInfo = 0x00;           // �ʱ� ��ū ����(���ӿ�����Ʈ ���� - enable : true) - �̺�Ʈ
        public const ushort StartGame           = 0x01;         // ���� ����(ĳ���� ������ ���� ����) - �̺�Ʈ
        public const ushort EndGame             = 0x02;         // ���� ����(ĳ���� ������ ���� �Ұ�) - �̺�Ʈ                           
        public const ushort ReadyGame           = 0x03;
        public const ushort PlayerSyncInfo      = 0x04;         // ��ü ���� ����
        public const ushort PlayerAliveInfo     = 0x05;
        public const ushort PlayerDamagedInfo   = 0x06;
        public const ushort BirdThrow           = 0x07;
        public const ushort CastleGateCracked   = 0x08;
        public const ushort ClearGame           = 0x09;
                                                     
        public const ushort WallDestroy         = 0xA0;
        public const ushort BoomDestroy         = 0xA1;
        public const ushort WindMillDestroy     = 0xA2;
        public const ushort TrapDestroy         = 0xA3;
        public const ushort CastleGateDestroy   = 0xA4;

    }

    /// <summary>
    /// ��Ÿ �޽���
    /// </summary>
    static public class MessageID_ETC
    {
        //ID
        public const ushort ETCID = 0x02;                   //�޽���
        //SubID
        public const ushort Message = 0x00;                 //�޽���
    }
}

public class CommonDataClass 
{
    /// <summary>
    /// ��� ����ü ������
    /// </summary>
    [StructLayout(LayoutKind.Sequential/*�����¼������(Queue)*/, Pack = 1/*�����͸� ���� ����*/)]
    public struct stHeader
    {
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgID; // �޽��� ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgSubID; // �޽��� ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2/*ushort size*/)]
        public UInt16 PacketSize; // �������κ� �޽��� ũ��
    }
    /// <summary>
    /// ��� ����ü ������ �Լ�(Byte->����ü)
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static stHeader HeaderfromByte(byte[] arr)
    {
        //����ü �ʱ�ȭ
        stHeader str = default(stHeader);
        int size = Marshal.SizeOf(str);//����ü Size

        //Size��ŭ �޸� �Ҵ�(�޸� �ڸ� ������)
        IntPtr ptr = Marshal.AllocHGlobal(size);

        //�����͸� �����Ͽ� �޸𸮿� �ֱ�(������ ���ųֱ�)
        Marshal.Copy(arr, 0, ptr, size);

        //����ü�� �ֱ�(���ų��� ������ ���� �ؼ� ����ü�� �ֱ�)
        str = (stHeader)Marshal.PtrToStructure(ptr, str.GetType());
        //�Ҵ��� �޸� ����
        Marshal.FreeHGlobal(ptr);

        //����ü ����
        return str;
    }
    /// <summary>
    /// �α��� ����ü ������ �Լ�(����ü->Byte)
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static byte[] GetHeaderToByte(stHeader str)
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(str, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);

        Marshal.FreeHGlobal(ptr);
        return arr;
    }

    /// <summary>
    /// �α��� ����ü ������
    /// </summary>
    [StructLayout(LayoutKind.Sequential/*�����¼������(Queue)*/, Pack = 1/*�����͸� ���� ����*/)]
    public struct stLogInMsg
    {
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgID; // �޽��� ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgSubID; // �޽��� ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2/*ushort size*/)]
        public UInt16 PacketSize; // �������κ� �޽��� ũ��

        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2/*ushort size*/)]
        public UInt16 LogInID; // �α���
        [MarshalAs(UnmanagedType.R4/*float*/, SizeConst = 2)]
        public UInt16 udpPortServer; // UDP port ����
        [MarshalAs(UnmanagedType.R4/*float*/, SizeConst = 2)]
        public UInt16 udpPortClient; // UDP port Ŭ��
    }
    /// <summary>
    /// �α��� ����ü ������ �Լ�(Byte->����ü)
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static stLogInMsg GetLogInMsgfromByte(byte[] arr)
    {
        //����ü �ʱ�ȭ
        stLogInMsg str = default(stLogInMsg);
        int size = Marshal.SizeOf(str);//����ü Size

        //Size��ŭ �޸� �Ҵ�(�޸� �ڸ� ������)
        IntPtr ptr = Marshal.AllocHGlobal(size);

        //�����͸� �����Ͽ� �޸𸮿� �ֱ�(������ ���ųֱ�)
        Marshal.Copy(arr, 0, ptr, size);

        //����ü�� �ֱ�(���ų��� ������ ���� �ؼ� ����ü�� �ֱ�)
        str = (stLogInMsg)Marshal.PtrToStructure(ptr, str.GetType());
        //�Ҵ��� �޸� ����
        Marshal.FreeHGlobal(ptr);

        //����ü ����
        return str;
    }
    /// <summary>
    /// �α��� ����ü ������ �Լ�(����ü->Byte)
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static byte[] GetLogInMsgToByte(stLogInMsg str)
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(str, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);

        Marshal.FreeHGlobal(ptr);
        return arr;
    }

    /// <summary>
    /// ���ڸ� �ѱ涧 ���� ����ü
    /// </summary>
    [StructLayout(LayoutKind.Sequential/*�����¼������(Queue)*/, Pack = 1/*�����͸� ���� ����*/)]
    public struct stNumberMsg
    {
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgID; // �޽��� ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgSubID; // �޽��� ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2/*ushort size*/)]
        public UInt16 PacketSize; // �������κ� �޽��� ũ��

        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2/*ushort size*/)]
        public UInt16 Number; // ����
    }
    /// <summary>
    /// ���ڸ� �ѱ�� ������ �Լ�(Byte->����ü)
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static stNumberMsg GetNumberMsgfromByte(byte[] arr)
    {
        //����ü �ʱ�ȭ
        stNumberMsg str = default(stNumberMsg);
        int size = Marshal.SizeOf(str);//����ü Size

        //Size��ŭ �޸� �Ҵ�(�޸� �ڸ� ������)
        IntPtr ptr = Marshal.AllocHGlobal(size);

        //�����͸� �����Ͽ� �޸𸮿� �ֱ�(������ ���ųֱ�)
        Marshal.Copy(arr, 0, ptr, size);

        //����ü�� �ֱ�(���ų��� ������ ���� �ؼ� ����ü�� �ֱ�)
        str = (stNumberMsg)Marshal.PtrToStructure(ptr, str.GetType());
        //�Ҵ��� �޸� ����
        Marshal.FreeHGlobal(ptr);

        //����ü ����
        return str;
    }
    /// <summary>
    /// ���ڸ� �ѱ�⸶���� �Լ�(����ü->Byte)
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static byte[] GetNumberMsgToByte(stNumberMsg str)
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(str, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);

        Marshal.FreeHGlobal(ptr);
        return arr;
    }

    /// <summary>
    /// �÷��̾� ���� ���� ������
    /// </summary>
    [StructLayout(LayoutKind.Sequential/*�����¼������(Queue)*/, Pack = 1/*�����͸� ���� ����*/)]
    public struct stPlayerAliveInfo
    {
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgID; // �޽��� ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgSubID; // �޽��� ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2/*ushort size*/)]
        public UInt16 PacketSize; // �������κ� �޽��� ũ��

        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2/*ushort size*/)]
        public UInt16 LogInID; // �α��� �ѹ�

        [MarshalAs(UnmanagedType.Bool/*ushort*/, SizeConst = 1/*ushort size*/)]
        public bool isAlive; // �α��� �ѹ�
    }
    /// <summary>
    /// �÷��̾� ���� ���� ������ �Լ�(Byte->����ü)
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static stPlayerAliveInfo GetPlayerAliveInfofromByte(byte[] arr)
    {
        //����ü �ʱ�ȭ
        stPlayerAliveInfo str = default(stPlayerAliveInfo);
        int size = Marshal.SizeOf(str);//����ü Size

        //Size��ŭ �޸� �Ҵ�(�޸� �ڸ� ������)
        IntPtr ptr = Marshal.AllocHGlobal(size);

        //�����͸� �����Ͽ� �޸𸮿� �ֱ�(������ ���ųֱ�)
        Marshal.Copy(arr, 0, ptr, size);

        //����ü�� �ֱ�(���ų��� ������ ���� �ؼ� ����ü�� �ֱ�)
        str = (stPlayerAliveInfo)Marshal.PtrToStructure(ptr, str.GetType());
        //�Ҵ��� �޸� ����
        Marshal.FreeHGlobal(ptr);

        //����ü ����
        return str;
    }
    /// <summary>
    /// �÷��̾� ���� ���� ������ �Լ�(����ü->Byte)
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static byte[] GetPlayerAliveInfoToByte(stPlayerAliveInfo str)
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(str, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);

        Marshal.FreeHGlobal(ptr);
        return arr;
    }

    /// <summary>
    /// ���� �̸� ����ü ������
    /// </summary>
    [StructLayout(LayoutKind.Sequential/*�����¼������(Queue)*/, Pack = 1/*�����͸� ���� ����*/)]
    public struct stPlayerInfoMsg
    {
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgID; // �޽��� ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgSubID; // �޽��� ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2/*ushort size*/)]
        public UInt16 PacketSize; // �������κ� �޽��� ũ��

        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2/*ushort size*/)]
        public UInt16 LogInID; // �α��� �ѹ�
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CommonData.Constants.MAX_NAME_LENGTH)]
        public string UserName; // ���� �̸�

        //[MarshalAs(UnmanagedType.Bool/*Bool*/, SizeConst = 1)]
        //public bool isMode; // Mode( true : �÷��̾�, false: ����͸�)

    }
    /// <summary>
    /// ���� �̸� ����ü ������ �Լ�(Byte->����ü)
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static stPlayerInfoMsg GetPlayerInfoMsgfromByte(byte[] arr)
    {
        //����ü �ʱ�ȭ
        stPlayerInfoMsg str = default(stPlayerInfoMsg);
        int size = Marshal.SizeOf(str);//����ü Size

        //Size��ŭ �޸� �Ҵ�(�޸� �ڸ� ������)
        IntPtr ptr = Marshal.AllocHGlobal(size);

        //�����͸� �����Ͽ� �޸𸮿� �ֱ�(������ ���ųֱ�)
        Marshal.Copy(arr, 0, ptr, size);

        //����ü�� �ֱ�(���ų��� ������ ���� �ؼ� ����ü�� �ֱ�)
        str = (stPlayerInfoMsg)Marshal.PtrToStructure(ptr, str.GetType());
        //�Ҵ��� �޸� ����
        Marshal.FreeHGlobal(ptr);

        //����ü ����
        return str;
    }
    /// <summary>
    /// ���� �̸� ����ü ������ �Լ�(����ü->Byte)
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static byte[] GetPlayerInfoMsgToByte(stPlayerInfoMsg str)
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(str, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);

        Marshal.FreeHGlobal(ptr);
        return arr;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct stPlayerName
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CommonData.Constants.MAX_NAME_LENGTH)]
        public string name;
    }

    /// <summary>
    /// �α��� ����ü ������
    /// </summary>
    [StructLayout(LayoutKind.Sequential/*�����¼������(Queue)*/, Pack = 1/*�����͸� ���� ����*/)]
    public struct stAllPlayerNameMsg
    {
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgID; // �޽��� ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgSubID; // �޽��� ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2/*ushort size*/)]
        public UInt16 PacketSize; // �������κ� �޽��� ũ��

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = CommonData.Constants.MAX_USER_COUNT)]
        public stPlayerName[] UserName; // ���� �̸�
        public void SetUserName(int UserCount = CommonData.Constants.MAX_USER_COUNT)
        {
            UserName = new stPlayerName[UserCount];
        }

    }
    /// <summary>
    /// �α��� ����ü ������ �Լ�(Byte->����ü)
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static stAllPlayerNameMsg GetAllPlayerNameMsgfromByte(byte[] arr)
    {
        //����ü �ʱ�ȭ
        stAllPlayerNameMsg str = default(stAllPlayerNameMsg);
        int size = Marshal.SizeOf(str);//����ü Size

        //Size��ŭ �޸� �Ҵ�(�޸� �ڸ� ������)
        IntPtr ptr = Marshal.AllocHGlobal(size);

        //�����͸� �����Ͽ� �޸𸮿� �ֱ�(������ ���ųֱ�)
        Marshal.Copy(arr, 0, ptr, size);

        //����ü�� �ֱ�(���ų��� ������ ���� �ؼ� ����ü�� �ֱ�)
        str = (stAllPlayerNameMsg)Marshal.PtrToStructure(ptr, str.GetType());
        //�Ҵ��� �޸� ����
        Marshal.FreeHGlobal(ptr);

        //����ü ����
        return str;
    }
    /// <summary>
    /// �α��� ����ü ������ �Լ�(����ü->Byte)
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static byte[] GetAllPlayerNameMsgToByte(stAllPlayerNameMsg str)
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(str, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);

        Marshal.FreeHGlobal(ptr);
        return arr;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct stPlayerDirectionMsg
    {
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2/*ushort size*/)]
        public UInt16 ClientID; // Ŭ���̾�Ʈ ���̵�

        [MarshalAs(UnmanagedType.ByValArray/*ushort*/, SizeConst = 3/*ushort size*/)]
        public Vector3 MoveVector; //�÷��̾ �̵��ϰ� ���� ����
    }
    /// <summary>
    /// ���� ����ȭ �޽��� ����ü ������ �Լ�(Byte->����ü)
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static stPlayerDirectionMsg GetstPlayerDirectionMsgfromByte(byte[] arr)
    {
        //����ü �ʱ�ȭ
        stPlayerDirectionMsg str = default(stPlayerDirectionMsg);
        int size = Marshal.SizeOf(str);//����ü Size

        if (size > arr.Length)
        {
            throw new Exception();
        }

        //Size��ŭ �޸� �Ҵ�(�޸� �ڸ� ������)
        IntPtr ptr = Marshal.AllocHGlobal(size);

        //�����͸� �����Ͽ� �޸𸮿� �ֱ�(������ ���ųֱ�)
        Marshal.Copy(arr, 0, ptr, size);

        //����ü�� �ֱ�(���ų��� ������ ���� �ؼ� ����ü�� �ֱ�)
        str = (stPlayerDirectionMsg)Marshal.PtrToStructure(ptr, str.GetType());
        //�Ҵ��� �޸� ����
        Marshal.FreeHGlobal(ptr);

        //����ü ����
        return str;
    }
    /// <summary>
    /// ���� ����ȭ �޽��� ����ü ������ �Լ�(����ü->Byte)
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static byte[] GetstPlayerDirectionMsgToByte(stPlayerDirectionMsg str)
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(str, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);

        Marshal.FreeHGlobal(ptr);
        return arr;
    }
    /// <summary>
    /// �÷��̾� ����ȭ �޽��� ����ü ������ ���� -> Ŭ���̾�Ʈ
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct stTransformInfo
    {
        [MarshalAs(UnmanagedType.ByValArray/*float*/, SizeConst = 3)]
        public Vector3 position; // Ŭ���̾�Ʈ ��ġ X��ǥ

        [MarshalAs(UnmanagedType.ByValArray/*float*/, SizeConst = 4)]
        public Quaternion rotation; // Ŭ���̾�Ʈ ��ġ X��ǥ
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct stMultiPlayerSyncMsg
    {
        [MarshalAs(UnmanagedType.ByValArray/*array*/, SizeConst = CommonData.Constants.MAX_USER_COUNT)]
        public stTransformInfo[] MultiPlayerInfo; // Ŭ���̾�Ʈ ����� ����


        public void SetMultiPlayerInfo(int nUserCnt = CommonData.Constants.MAX_USER_COUNT)
        {
            MultiPlayerInfo = new stTransformInfo[nUserCnt];
        }
    }
    /// <summary>
    /// ���� ����ȭ �޽��� ����ü ������ �Լ�(Byte->����ü)
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static stMultiPlayerSyncMsg GetMultiPlayerSyncMsgfromByte(byte[] arr)
    {
        //����ü �ʱ�ȭ
        stMultiPlayerSyncMsg str = default(stMultiPlayerSyncMsg);
        int size = Marshal.SizeOf(str);//����ü Size

        if (size > arr.Length)
        {
            throw new Exception();
        }

        //Size��ŭ �޸� �Ҵ�(�޸� �ڸ� ������)
        IntPtr ptr = Marshal.AllocHGlobal(size);

        //�����͸� �����Ͽ� �޸𸮿� �ֱ�(������ ���ųֱ�)
        Marshal.Copy(arr, 0, ptr, size);

        //����ü�� �ֱ�(���ų��� ������ ���� �ؼ� ����ü�� �ֱ�)
        str = (stMultiPlayerSyncMsg)Marshal.PtrToStructure(ptr, str.GetType());
        //�Ҵ��� �޸� ����
        Marshal.FreeHGlobal(ptr);

        //����ü ����
        return str;
    }
    /// <summary>
    /// ���� ����ȭ �޽��� ����ü ������ �Լ�(����ü->Byte)
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static byte[] GetMultiPlayerSyncMsgToByte(stMultiPlayerSyncMsg str)
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(str, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);

        Marshal.FreeHGlobal(ptr);
        return arr;
    }

    /// <summary>
    /// �÷��̾ ���� �������� ������
    /// </summary>
    [StructLayout(LayoutKind.Sequential/*�����¼������(Queue)*/, Pack = 1/*�����͸� ���� ����*/)]
    public struct stPlayerDamaged
    {
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgID; // �޽��� ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgSubID; // �޽��� ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2/*ushort size*/)]
        public UInt16 PacketSize; // �������κ� �޽��� ũ��

        [MarshalAs(UnmanagedType.R4/*ushort*/, SizeConst = 4/*ushort size*/)]
        public float Damage; // ���� ������
    }
    /// <summary>
    /// ����ü ������ �Լ�(Byte->����ü)
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static stPlayerDamaged GetPlayerDamagedfromByte(byte[] arr)
    {
        //����ü �ʱ�ȭ
        stPlayerDamaged str = default(stPlayerDamaged);
        int size = Marshal.SizeOf(str);//����ü Size

        //Size��ŭ �޸� �Ҵ�(�޸� �ڸ� ������)
        IntPtr ptr = Marshal.AllocHGlobal(size);

        //�����͸� �����Ͽ� �޸𸮿� �ֱ�(������ ���ųֱ�)
        Marshal.Copy(arr, 0, ptr, size);

        //����ü�� �ֱ�(���ų��� ������ ���� �ؼ� ����ü�� �ֱ�)
        str = (stPlayerDamaged)Marshal.PtrToStructure(ptr, str.GetType());
        //�Ҵ��� �޸� ����
        Marshal.FreeHGlobal(ptr);

        //����ü ����
        return str;
    }
    /// <summary>
    /// ����ü ������ �Լ�(����ü->Byte)
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static byte[] GetPlayerDamagedToByte(stPlayerDamaged str)
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(str, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);

        Marshal.FreeHGlobal(ptr);
        return arr;
    }
    /// <summary>
    /// �ҷ� ������
    /// </summary>
    [StructLayout(LayoutKind.Sequential/*�����¼������(Queue)*/, Pack = 1/*�����͸� ���� ����*/)]
    public struct stBirdThrowMsg
    {
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgID; // �޽��� ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgSubID; // �޽��� ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2/*ushort size*/)]
        public UInt16 PacketSize; // �������κ� �޽��� ũ��

        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 IndexNumber; // �����Ͽ콺 �ѹ�

        [MarshalAs(UnmanagedType.ByValArray/*float*/, SizeConst = 3)]
        public Vector3 Direction; // ���� ����
    }
    /// <summary>
    /// ����ü ������ �Լ�(Byte->����ü)
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static stBirdThrowMsg GetBirdThrowMsgfromByte(byte[] arr)
    {
        //����ü �ʱ�ȭ
        stBirdThrowMsg str = default(stBirdThrowMsg);
        int size = Marshal.SizeOf(str);//����ü Size

        //Size��ŭ �޸� �Ҵ�(�޸� �ڸ� ������)
        IntPtr ptr = Marshal.AllocHGlobal(size);

        //�����͸� �����Ͽ� �޸𸮿� �ֱ�(������ ���ųֱ�)
        Marshal.Copy(arr, 0, ptr, size);

        //����ü�� �ֱ�(���ų��� ������ ���� �ؼ� ����ü�� �ֱ�)
        str = (stBirdThrowMsg)Marshal.PtrToStructure(ptr, str.GetType());
        //�Ҵ��� �޸� ����
        Marshal.FreeHGlobal(ptr);

        //����ü ����
        return str;
    }
    /// <summary>
    /// ����ü ������ �Լ�(����ü->Byte)
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static byte[] GetBirdThrowMsgToByte(stBirdThrowMsg str)
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(str, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);

        Marshal.FreeHGlobal(ptr);
        return arr;
    }
}