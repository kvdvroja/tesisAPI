namespace tesisAPI.Middlewares
{
    // Conflictos de estado, duplicados, etc.
    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message) { }
    }
}
