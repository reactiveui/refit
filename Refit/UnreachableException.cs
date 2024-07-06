namespace Refit;

/// <summary>
/// The exception that is thrown when the program executes an instruction that was thought to be unreachable.
/// </summary>
public class UnreachableException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnreachableException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">Specified error message.</param>
    public UnreachableException(string message) : base(message){}

    /// <summary>
    /// Initializes a new instance of the <see cref="UnreachableException"/> class with the default error message.
    /// </summary>
    public UnreachableException()
    {}
}


