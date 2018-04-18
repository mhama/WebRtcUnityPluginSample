using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovieStats
{
    const int maxSamples = 100;
    private Deque<float> frameLoadTimes = new Deque<float>(maxSamples + 1);
    private Deque<float> frameShowTimes = new Deque<float>(maxSamples + 1);
    private Deque<float> frameSkipTimes = new Deque<float>(maxSamples + 1);
    System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

    public MovieStats()
    {
        sw.Start();
    }

    public void CountFrameLoad()
    {
        AddSampleToQueue(frameLoadTimes);
    }
    public void CountFrameSkip()
    {
        AddSampleToQueue(frameSkipTimes);
    }
    public void CountFrameShow()
    {
        AddSampleToQueue(frameShowTimes);
    }
    void AddSampleToQueue(Deque<float> queue)
    {
        lock (this)
        {
            queue.AddFront(sw.ElapsedMilliseconds * 0.001f);
            if (queue.Count >= maxSamples)
            {
                queue.RemoveBack();
            }
        }
    }
    public float fpsLoad()
    {
        return CalcFps(frameLoadTimes);
    }
    public float fpsSkip()
    {
        return CalcFps(frameSkipTimes);
    }
    public float fpsShow()
    {
        return CalcFps(frameShowTimes);
    }
    public float CalcFps(Deque<float> queue)
    {
        int count = 0;
        float firstTime = 0;
        float lastTime = 0;
        lock (this)
        {
            count = queue.Count;
            if (count >= 2)
            {
                firstTime = queue.Get(0);
                lastTime = queue.Get(count - 1);
            }
        }
        if (count <= 1)
        {
            return 0;
        }
        float fps = (count - 1) / (firstTime - lastTime);
        return fps;
    }
}
