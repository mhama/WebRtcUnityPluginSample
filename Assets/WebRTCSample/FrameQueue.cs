using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// FramePacket のキュー。
// 指定サイズ以上Pushすると、指定サイズ以下になるよう末尾のデータから削除される。
// スレッドセーフ。
public class FrameQueue
{
    private Deque<FramePacket> frames = new Deque<FramePacket>();
    private FramePacketPool bufferPool = new FramePacketPool();
    private int maxQueueCount;
    MovieStats stats = new MovieStats();

    public FrameQueue(int _maxQueueCount)
    {
        maxQueueCount = _maxQueueCount;
    }

    public void Push(FramePacket frame)
    {
        stats.CountFrameLoad();
        FramePacket trashBuf = null;
        lock (this)
        {
            frames.AddFront(frame);
            if (frames.Count >= maxQueueCount)
            {
                stats.CountFrameSkip();
                trashBuf = frames.RemoveBack();
            }
        }
        // lock内でPushしないのは、thisとbufferPoolの両方のlockを同時にとらないようにする配慮。
        if (trashBuf != null)
        {
            bufferPool.Push(trashBuf);
        }
    }

    public FramePacket Pop()
    {
        lock (this)
        {
            if (frames.IsEmpty)
            {
                return null;
            }
            stats.CountFrameShow();
            return frames.RemoveBack();
        }
    }

    public FramePacket GetDataBufferWithContents(int width, int height, byte[] src, int size)
    {
        return bufferPool.GetDataBufferWithContents(width, height, src, size);
    }

    public FramePacket GetDataBufferWithoutContents(int size)
    {
        return bufferPool.GetDataBuffer(size);
    }

    public void Pool(FramePacket buf)
    {
        bufferPool.Push(buf);
    }

    public int Count
    {
        get
        {
            lock (this)
            {
                return frames.Count;
            }
        }
    }

    public FramePacketPool FramePacketPool
    {
        get { return bufferPool; }
    }

    public MovieStats Stats
    {
        get { return stats; }
    }
}
