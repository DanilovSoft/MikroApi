using System;

namespace DanilovSoft.MikroApi;

public class MikroApiConnectionException : MikroApiException
{
    public MikroApiConnectionException()
    {
    }

    public MikroApiConnectionException(string message) : base(message)
    {
    }

    public MikroApiConnectionException(string message, Exception? innerException) : base(message, innerException)
    {
    }
}
