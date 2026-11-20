namespace task2.Exceptions
{
    public class ConflictException : AppException
    {
        public ConflictException(string message) : base(message, 409) { }
    }
}
