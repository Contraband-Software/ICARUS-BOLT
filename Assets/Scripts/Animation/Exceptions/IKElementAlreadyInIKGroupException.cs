using System;

public class IKElementAlreadyInIKGroupException : Exception
{
    public IKElementAlreadyInIKGroupException()
    {
    }

    public IKElementAlreadyInIKGroupException(string message)
        : base(message)
    {
    }

    public IKElementAlreadyInIKGroupException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
