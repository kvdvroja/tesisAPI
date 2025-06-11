namespace tesisAPI.Middlewares
{
    // Recurso no encontrado
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }
}
