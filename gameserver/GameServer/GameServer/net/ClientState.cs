

using System.Net.Sockets;

public class ClientState
{
    public Socket socket;
    public ByteArray readBuff = new ByteArray();

    public long lastPingTime = 0;

    public Player player;
}