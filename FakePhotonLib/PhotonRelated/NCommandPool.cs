namespace FakePhotonLib.PhotonRelated;

internal class NCommandPool
{
    public readonly Stack<NCommand> pool = new();

    public NCommand Acquire(byte[] inBuff, ref int readingOffset)
    {
        NCommand ncommand;
        lock (pool)
        {
            if (pool.Count == 0)
            {
                ncommand = new(inBuff, ref readingOffset)
                {
                    returnPool = this
                };
            }
            else
            {
                ncommand = pool.Pop();
                ncommand.Initialize(inBuff, ref readingOffset);
            }
        }
        return ncommand;
    }

    public NCommand Acquire(NCommand.CommandType commandType, StreamBuffer payload, byte channel)
    {
        NCommand ncommand;
        lock (pool)
        {
            if (pool.Count == 0)
            {
                ncommand = new(commandType, payload, channel)
                {
                    returnPool = this
                };
            }
            else
            {
                ncommand = pool.Pop();
                ncommand.Initialize(commandType, payload, channel);
            }
        }
        return ncommand;
    }

    public void Release(NCommand nCommand)
    {
        nCommand.Reset();
        lock (pool)
            pool.Push(nCommand);
    }
}