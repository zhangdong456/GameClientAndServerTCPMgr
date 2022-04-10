

public class MsgGetText : MsgBase
{
    public MsgGetText()
    {
        protoName = "MsgGetText";
    }

    //服务端回
    public string text = "";
}

public class MsgSaveText : MsgBase
{
    public MsgSaveText()
    {
        protoName = "MsgSaveText";
    }

    //客户端发
    public string text = "";

    //服务端回
    public int result = 0;
}