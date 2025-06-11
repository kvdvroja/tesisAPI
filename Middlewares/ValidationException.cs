namespace tesisAPI.Middlewares
{
    // Error de validación
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }
}
