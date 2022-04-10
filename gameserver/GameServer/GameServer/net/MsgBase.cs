
using System;
using System.Web.Script.Serialization;

public class MsgBase
{
    public string protoName = "";

    //解码器
    private static JavaScriptSerializer Js = new JavaScriptSerializer();
    
    public static byte[] Encode(MsgBase msgBase)
    {
        string s = Js.Serialize(msgBase);
        return System.Text.Encoding.UTF8.GetBytes(s);
    }

    public static MsgBase Decode(string protoName,byte[] bytes,int offest,int count)
    {
        string s = System.Text.Encoding.UTF8.GetString(bytes, offest, count);
        MsgBase msgBase = (MsgBase)Js.Deserialize(s,Type.GetType(protoName));
        return msgBase;
    }

    //编码协议名
    public static byte[] EncodeName(MsgBase msgBase)
    {
        byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(msgBase.protoName);
        Int16 len = (Int16) nameBytes.Length;

        byte[] bytes = new byte[2 + len];
        
        bytes[0]=(byte)(len%256);//int16位右移8位 取余  相当于取的就是右移的8位  存储低8位
        bytes[1]=(byte)(len/256);//int16 右移8位 相当于取高8位的数值            存储高8位
        
        Array.Copy(nameBytes,0,bytes,2,len);

        return bytes;
    }
    //解析协议名
    public static string DecodeName(byte[] bytes,int offest,out int count)
    {
        count = 0;
        if (offest + 2 > bytes.Length)
            return "";
        Int16 len=(Int16)((bytes[offest+1]<<8)|bytes[offest]);
        if (len<=0)
        {
            return "";
        }

        if (offest+2+len>bytes.Length)
        {
            return "";
        }

        count = 2 + len;
        string name = System.Text.Encoding.UTF8.GetString(bytes, offest + 2, len);
        return name;

    }
}
