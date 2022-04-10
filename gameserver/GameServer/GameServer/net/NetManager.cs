using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

public class NetManager
{
    public static Socket listenfd;

    public static Dictionary<Socket, ClientState> clients = new Dictionary<Socket, ClientState>();

    //Select的检查列表
    private static List<Socket> checkRead = new List<Socket>();
    //ping间隔
    public static long pingInterval = 30;

    public static void StartLoop(int listenPort)
    {
        listenfd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        IPAddress ipAdr = IPAddress.Parse("0.0.0.0");
        IPEndPoint ipEp = new IPEndPoint(ipAdr, listenPort);
        listenfd.Bind(ipEp);
        
        listenfd.Listen(0);
        
        Console.WriteLine("服务器启动成功");

        while (true)
        {
            ResetCheckRead();
            Socket.Select(checkRead,null,null,1000);

            for (int i = checkRead.Count-1; i >= 0; --i)
            {
                Socket s = checkRead[i];
                if (s==listenfd)
                {
                    ReadListenfd(s);
                }
                else
                {
                    ReadClientfd(s);
                }
            }
        }
    }


    private static void ResetCheckRead()
    {
        checkRead.Clear();
        checkRead.Add(listenfd);

        foreach (var s in clients.Values)
        {
            checkRead.Add(s.socket);
        }
    }

    private static void ReadListenfd(Socket listenfd)
    {
        try
        {
            Socket clientfd = listenfd.Accept();
            Console.WriteLine("Accept  "+clientfd.RemoteEndPoint.ToString());
            ClientState state = new ClientState();
            state.socket = clientfd;
            clients.Add(clientfd,state);
        }
        catch (SocketException e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private static void ReadClientfd(Socket clientfd)
    {
        ClientState state = clients[clientfd];
        ByteArray readBuff = state.readBuff;

        int count = 0;
        if (readBuff.remain<=0)
        {
            //---;
            readBuff.MoveBytes();
        }

        if (readBuff.remain<=0)
        {
            Close(state);
            return;
        }

        try
        {
            count = clientfd.Receive(readBuff.bytes, readBuff.writeIdx, readBuff.remain,0);
        }
        catch (Exception e)
        {
            Close(state);
            throw;
        }

        if (count<=0)
        {
            Close(state);
            return;
        }

        readBuff.writeIdx += count;
        //处理二进制消息
        
        //移动缓冲区
        readBuff.CheckAndMoveBytes();
    }

    private static void OnReceiveData(ClientState state)
    {
        ByteArray readBuff = state.readBuff;
        //消息长度
        if (readBuff.length<=2)
        {
            return;
        }

        Int16 bodyLength = readBuff.ReadInt16();
        //消息体
        if (readBuff.length<bodyLength)
        {
            return;
        }
        //解析协议名
        int nameCount = 0;
        string protoName = MsgBase.DecodeName(readBuff.bytes, readBuff.readIdx, out nameCount);

        if (string.IsNullOrEmpty(protoName))
        {
            Console.WriteLine("OnReceiveData Msgbase.DecodeName Fail");
            Close(state);
        }

        readBuff.readIdx += nameCount;
        //解析协议体
        int bodyCount = bodyLength - nameCount;
        MsgBase msgBase=MsgBase.Decode(protoName,readBuff.bytes,readBuff.readIdx,bodyCount);
        readBuff.readIdx += bodyCount;
        readBuff.CheckAndMoveBytes();
        
        //分发消息
        MethodInfo mi = typeof(MsgHandler).GetMethod(protoName);
        object[] o = { state,msgBase};
        if (mi!=null)
        {
            mi.Invoke(null, o);
        }
        else
        {
            Console.WriteLine("OnReceiveData Invoke fail "+protoName);
        }

        if (readBuff.length>2)
        {
            OnReceiveData(state);
        }

        //-----------------------------
    }

    public static void Send(ClientState cs,MsgBase msg)
    {
        if(cs==null)
            return;
        if(!cs.socket.Connected)
            return;

        byte[] nameBytes = MsgBase.EncodeName(msg);
        byte[] bodyBytes = MsgBase.Encode(msg);

        int len = nameBytes.Length + bodyBytes.Length;
        byte[] sendBytes = new byte[2 + len];

        sendBytes[0] = (byte) (len % 256);//低8位
        sendBytes[1]=(byte)(len/256);//高8位
        
        Array.Copy(nameBytes,0,sendBytes,2,nameBytes.Length);
        
        Array.Copy(bodyBytes,0,sendBytes,2+nameBytes.Length,bodyBytes.Length);

        try
        {
            cs.socket.BeginSend(sendBytes, 0, sendBytes.Length, 0, null, null);
        }
        catch (SocketException e)
        {
            
        }
    }


    public static void Close(ClientState state)
    {
        MethodInfo mei = typeof(EventHandler).GetMethod("OnDisconnect");
        object[] obj = {state};
        mei.Invoke(null,obj);
        
        state.socket.Close();
        clients.Remove(state.socket);
    }

    //定时器
    static void Timer()
    {
        MethodInfo mei = typeof(EventHandler).GetMethod("OnTimer");
        object[] obj = { };
        mei.Invoke(null, obj);
    }

    //获取时间戳
    public static long GetTimeStamp()
    {
        TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return Convert.ToInt64(ts.TotalMilliseconds);
    }
}