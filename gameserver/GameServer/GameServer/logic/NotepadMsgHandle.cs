

using System.Data;

public partial class MsgHandler
{
    public static void MsgGetText(ClientState c,MsgBase msgBase)
    {
        MsgGetText msg =(MsgGetText) msgBase;

        Player player = c.player;
        
        if(player==null) return;

        msg.text = player.data.text;
        
        player.Send(msg);
    }

    public static void MsgSaveText(ClientState c,MsgBase msgBase)
    {
        MsgSaveText msg = (MsgSaveText)msgBase;

        Player player = c.player;
        
        if(player==null) return;

        player.data.text = msg.text;
        
        player.Send(msg);
    }
}