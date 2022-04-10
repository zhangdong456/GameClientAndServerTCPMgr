using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using UnityEngine;

public enum NetEvent
{
     ConnectSucc=1,
     ConnectFail=2,
     Close=3,
}

public static class NetManager
{
     private static Socket socket;
     //接收缓冲区
     private static ByteArray readBuff;
     //写入队列
     private static Queue<ByteArray> writeQueue;

     //消息列表
     private static List<MsgBase> msgList = new List<MsgBase>();
     private static int msgCount = 0;
     private readonly static int MAX_MESSAGE_FIRE = 10;

     //事件委托类型
     public delegate void EventListener(String err);

     public delegate void MsgListener(MsgBase msgBase);

     //事件监听列表
     private static Dictionary<NetEvent, EventListener> eventListeners = new Dictionary<NetEvent, EventListener>();
     private static Dictionary<string, MsgListener> msgListeners = new Dictionary<string, MsgListener>();

     //是否正在链接
     private static bool isConnecting = false;
     //是否正在关闭
     private static bool isClosing = false;
     
     //-------------------
     public static bool isUsePing = true;

     public static int pingInterval = 30;
     public static int pongInterval = 120;

     private static float lastPintTime = 0;

     private static float lastPongTime = 0;
     
     //-------------------

     public static void Connect(string ip,int port)
     {
          if (socket!=null&&socket.Connected)
          {
               Debug.Log("Connect fail,already connected");
               return;
          }

          if (isConnecting)
          {
               Debug.Log("Connect fail,isConnecting");
               return;
          }
          InitState();

          socket.NoDelay = true;
          isConnecting = true;
          socket.BeginConnect(ip, port, ConnectCallback, socket);

     }

     private static void InitState()
     {
          socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
          readBuff = new ByteArray();
          writeQueue = new Queue<ByteArray>();
          isConnecting = false;
          isClosing = false;

          msgList = new List<MsgBase>();
          msgCount = 0;

          lastPintTime = Time.time;
          lastPongTime = Time.time;

          //监听服务器回复的心跳包
          if (!msgListeners.ContainsKey("MsgPong"))
          {
               AddMsgListener("MsgPong",OnMsgPong);
          }
     }

     private static void ConnectCallback(IAsyncResult ar)
     {
          try
          {
               Socket socket = (Socket) ar.AsyncState;
               socket.EndConnect(ar);
               FireEvent(NetEvent.ConnectSucc,"Socket Connect Sucess");
               isConnecting = false;
               //开始接收数据
               socket.BeginReceive(readBuff.bytes, readBuff.writeIdx, readBuff.remain, 0, ReceiveCallback, socket);
          }
          catch (SocketException e)
          {
               FireEvent(NetEvent.ConnectFail,e.ToString());
               isConnecting = false;
          }
     }

     private static void ReceiveCallback(IAsyncResult ar)
     {
          try
          {
               Socket socket = (Socket) ar.AsyncState;
               int count = socket.EndReceive(ar);
               if (count==0)
               {
                    Close();
                    return;
               }

               readBuff.writeIdx += count;
               //处理而二进制信息
               OnReceiveData();
               if (readBuff.remain<8)
               {
                    readBuff.MoveBytes();
                    readBuff.Resize(readBuff.length*2);
               }

               socket.BeginReceive(readBuff.bytes, readBuff.writeIdx, readBuff.remain, 0, ReceiveCallback, socket);
          }
          catch (SocketException e)
          {
               Console.WriteLine(e);
               throw;
          }
     }

     private static void OnReceiveData()
     {
          if (readBuff.length<=2)
          {
               return;
          }

          int readIdx = readBuff.readIdx;
          byte[] bytes = readBuff.bytes;
          Int16 bodyLength=(Int16)((bytes[readIdx+1]<<8)|bytes[readIdx]);
          if (readBuff.length<bodyLength+2)
          {
               return;
          }

          readBuff.readIdx += 2;
          //解析协议名
          int nameCount = 0;
          string protoName = MsgBase.DecodeName(readBuff.bytes, readBuff.readIdx, out nameCount);
          if (string.IsNullOrEmpty(protoName))
          {
               return;
          }

          readBuff.readIdx += nameCount;
          //解析协议体
          int bodyCount = bodyLength - nameCount;
          MsgBase msgBase=MsgBase.Decode(protoName,readBuff.bytes,readBuff.readIdx,bodyCount);
          readBuff.readIdx += bodyCount;
          readBuff.CheckAndMoveBytes();

          lock (msgList)
          {
               msgList.Add(msgBase);
          }

          msgCount++;
          if (readBuff.length>2)
          {
               OnReceiveData();
          }
     }

     public static void Send(MsgBase msg)
     {
          if (socket==null||!socket.Connected)
          {
               return;
          }

          if (isConnecting||isClosing)
          {
               return;
          }

          byte[] nameBytes = MsgBase.EncodeName(msg);
          byte[] bodyBytes = MsgBase.Encode(msg);
          int len = nameBytes.Length + bodyBytes.Length;

          byte[] sendBytes = new byte[2 + len];
          
          //组装长度
          sendBytes[0]=(byte)(len%256);//存储低8位置
          sendBytes[1]=(byte)(len/256);//存储高8位
          //组装名字
          Array.Copy(nameBytes,0,sendBytes,2,nameBytes.Length);
          //组装消息体
          Array.Copy(bodyBytes,0,sendBytes,2+nameBytes.Length,bodyBytes.Length);

          ByteArray ba = new ByteArray(sendBytes);
          int count = 0;
          lock (writeQueue)
          {
               writeQueue.Enqueue(ba);
               count = writeQueue.Count;
          }

          if (count==1)
          {
               socket.BeginSend(sendBytes, 0, sendBytes.Length, 0, SendCallBack, socket);
          }
     }

     private static void SendCallBack(IAsyncResult ar)
     {
          Socket socket = (Socket) ar.AsyncState;
          if (socket==null||!socket.Connected)
          {
               return;
          }

          int count = socket.EndSend(ar);

          ByteArray ba;
          lock (writeQueue)
          {
               ba = writeQueue.First();
          }

          ba.readIdx += count;
          if (ba.length==0)
          {
               lock (writeQueue)
               {
                    writeQueue.Dequeue();
                    ba = writeQueue.First();
               }
          }

          if (ba!=null)
          {
               socket.BeginSend(ba.bytes, ba.readIdx, ba.length, 0, SendCallBack, socket);
          }else if (isClosing)
          {
               socket.Close();
          }
     }

     private static void Close()
     {
          if(socket==null||!socket.Connected)
               return;
          if(isConnecting)
               return;
          if(isClosing)
               return;
          if (writeQueue.Count>0)
          {
               isClosing = true;
          }
          else
          {
               socket.Close();
               FireEvent(NetEvent.Close,"");
          }
     }

     public static void Updata()
     {
          MsgUpdate();
          PingUpdate();
     }

     private static void PingUpdate()
     {
          if(!isUsePing)
               return;

          if (Time.time-lastPintTime>pingInterval)
          {
               MsgPing msgPing = new MsgPing();
               Send(msgPing);
               lastPintTime = Time.time;
          }

          if (Time.time-lastPongTime>pongInterval)
          {
               Debug.Log("心跳超时  关闭");
               Close();
          }
     }

     private static void OnMsgPong(MsgBase msgBase)
     {
          lastPongTime = Time.time;
     }

     private static void MsgUpdate()
     {
          if(msgCount==0)
               return;

          for (int i = 0; i < MAX_MESSAGE_FIRE; i++)
          {
               MsgBase msgBase = null;
               lock (msgList)
               {
                    if (msgList.Count>0)
                    {
                         msgBase = msgList[0];
                         msgList.RemoveAt(0);
                         msgCount--;
                    }
               }

               if (msgBase==null)
               {
                    break;
               }
               else
               {
                    FireMsg(msgBase.protoName,msgBase);
               }
          }
     }

     #region 网络事件

     public static void AddEventListener(NetEvent netEvent,EventListener listener)
     {
          if (eventListeners.ContainsKey(netEvent))
          {
               eventListeners[netEvent] += listener;
          }
          else
          {
               eventListeners.Add(netEvent,listener);
          }
     }

     public static void RemoveEventListener(NetEvent netEvent, EventListener listener)
     {
          if (eventListeners.ContainsKey(netEvent))
          {
               eventListeners[netEvent] -= listener;
               if (eventListeners[netEvent]==null)
               {
                    eventListeners.Remove(netEvent);
               }
          }
     }

     private static void FireEvent(NetEvent netEvent,String err)
     {
          if (eventListeners.ContainsKey(netEvent))
          {
               eventListeners[netEvent](err);
          }
     }

     #endregion

     #region 消息事件

     public static void AddMsgListener(string msgName,MsgListener listener)
     {
          if (msgListeners.ContainsKey(msgName))
          {
               msgListeners[msgName] += listener;
          }
          else
          {
               msgListeners.Add(msgName,listener);
          }
     }

     public static void RemoveMsgListener(string msgName,MsgListener listener)
     {
          if (msgListeners.ContainsKey(msgName))
          {
               msgListeners[msgName] -= listener;
               if (msgListeners[msgName]==null)
               {
                    msgListeners.Remove(msgName);
               }
          }
     }

     private static void FireMsg(string msgName,MsgBase msgBase)
     {
          if (msgListeners.ContainsKey(msgName))
          {
               msgListeners[msgName](msgBase);
          }
     }

     #endregion
    
}
