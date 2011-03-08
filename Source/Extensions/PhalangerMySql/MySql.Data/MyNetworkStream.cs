using System;
using System.Net.Sockets;

internal class MyNetworkStream : NetworkStream
{
    private const int MaxRetryCount = 2;
    private Socket socket;

    public MyNetworkStream(Socket socket, bool ownsSocket) : base(socket, ownsSocket)
    {
        this.socket = socket;
    }

    public override void Flush()
    {
        int num = 0;
        Exception exception = null;
        do
        {
            try
            {
                base.Flush();
                return;
            }
            catch (Exception exception2)
            {
                exception = exception2;
                this.HandleOrRethrowException(exception2);
            }
        }
        while (++num < 2);
        throw exception;
    }

    private void HandleOrRethrowException(Exception e)
    {
        for (Exception exception = e; exception != null; exception = exception.InnerException)
        {
            if (exception is SocketException)
            {
                SocketException exception2 = (SocketException) exception;
                if (this.IsWouldBlockException(exception2))
                {
                    this.socket.Blocking = true;
                    return;
                }
                if (this.IsTimeoutException(exception2))
                {
                    throw new TimeoutException(exception2.Message, e);
                }
            }
        }
        throw e;
    }

    private bool IsTimeoutException(SocketException e)
    {
        return (e.SocketErrorCode == SocketError.TimedOut);
    }

    private bool IsWouldBlockException(SocketException e)
    {
        return (e.SocketErrorCode == SocketError.WouldBlock);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int num = 0;
        Exception exception = null;
        do
        {
            try
            {
                return base.Read(buffer, offset, count);
            }
            catch (Exception exception2)
            {
                exception = exception2;
                this.HandleOrRethrowException(exception2);
            }
        }
        while (++num < 2);
        throw exception;
    }

    public override int ReadByte()
    {
        int num = 0;
        Exception exception = null;
        do
        {
            try
            {
                return base.ReadByte();
            }
            catch (Exception exception2)
            {
                exception = exception2;
                this.HandleOrRethrowException(exception2);
            }
        }
        while (++num < 2);
        throw exception;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        int num = 0;
        Exception exception = null;
        do
        {
            try
            {
                base.Write(buffer, offset, count);
                return;
            }
            catch (Exception exception2)
            {
                exception = exception2;
                this.HandleOrRethrowException(exception2);
            }
        }
        while (++num < 2);
        throw exception;
    }
}

