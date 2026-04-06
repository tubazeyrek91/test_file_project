namespace DistributedStorage.Domain.Exceptions;

public class IntegrityException : Exception
{
    public IntegrityException(string message) : base(message)
    {
    }
}
