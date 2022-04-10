using System;

public class ByteArray 
{
    //默认大小
    private const int DEFAULT_SIZE = 1024;
    //初始大小
    private int initSize = 0;
    //缓冲区
    public byte[] bytes;
    //读写位置
    public int readIdx = 0;
    
    public int writeIdx = 0;
    //容量
    private int capacity = 0;

    //剩余空间
    public int remain {
        get
        {
            return capacity - writeIdx;
        }
    }
    //数据长度
    public int length
    {
        get { return writeIdx - readIdx; }
    }

    public ByteArray(int size=DEFAULT_SIZE)
    {
        bytes = new byte[size];
        capacity = size;
        initSize = size;
        readIdx = 0;
        writeIdx = 0;
    }

    public ByteArray(byte[] defaultBytes)
    {
        bytes = defaultBytes;
        capacity = defaultBytes.Length;
        initSize = defaultBytes.Length;
        readIdx = 0;
        writeIdx = defaultBytes.Length;
    }

    public int Write(byte[] bs,int offest,int count)
    {
        if(remain<count)
            Resize(length+count);
        Array.Copy(bs,offest,bytes,writeIdx,count);
        writeIdx += count;
        return count;
    }

    public int Read(byte[] bs,int offest,int count)
    {
        count = Math.Min(count, length);
        Array.Copy(bytes,0,bs,offest,count);
        readIdx += count;
        CheckAndMoveBytes();
        return count;
    }

    //重新设置尺寸
    public void Resize(int size)
    {
        if(size<length) return;
        if(size<initSize) return;

        int n = 1;
        while (n<size)
        {
            n *= 2;
        }

        capacity = n;
        byte[] newBytes = new byte[capacity];
        Array.Copy(bytes,readIdx,newBytes,0,writeIdx-readIdx);
        bytes = newBytes;
        writeIdx = length;
        readIdx = 0;
    }

    public void CheckAndMoveBytes()
    {
        if (length<8)
        {
            MoveBytes();
        }
    }

    public void MoveBytes()
    {
        Array.Copy(bytes,readIdx,bytes,0,length);
        writeIdx = length;
        readIdx = 0;
    }

    public Int16 ReadInt16()
    {
        if (length < 2) return 0;
        Int16 ret=(Int16)((bytes[1]<<8)|bytes[0]);
        readIdx += 2;
        CheckAndMoveBytes();
        return ret;
    }

    public int ReadInt32()
    {
        if (length < 4) return 0;
        Int32 ret=(Int32)((bytes[3]<<24)|
                          (bytes[2]<<16)|
                          (bytes[1]<<8)|
                          bytes[0]);
        readIdx += 4;
        CheckAndMoveBytes();
        return ret;
    }
}
