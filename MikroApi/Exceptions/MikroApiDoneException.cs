using System;

namespace DanilovSoft.MikroApi;

public class MikroApiDoneException : MikroApiException
{
    public MikroApiDoneException()
    {
    }

    public MikroApiDoneException(string? message) : base(message)
    {
    }

    public MikroApiDoneException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
