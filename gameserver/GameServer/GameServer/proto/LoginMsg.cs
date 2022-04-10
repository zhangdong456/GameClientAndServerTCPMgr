


public class MsgRegister : MsgBase
{
    public MsgRegister()
    {
        protoName = "MsgRegister";
    }

    //客户端发
    public string id = "";
    public string pw = "";

    //0成功 1失败
    public int result = 0;
}

public class MsgLogin : MsgBase
{
    public MsgLogin()
    {
        protoName = "MsgLogin";
    }

    public string id = "";
    public string pw = "";

    public int result = 0;
}

public class MsgKick : MsgBase
{
    public MsgKick()
    {
        protoName = "MsgKick";
    }

    //踢下线的原因Id
    public int reason = 0;
}