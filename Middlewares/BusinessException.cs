// En carpeta Exceptions
namespace tesisAPI.Exceptions
{
    public class BusinessException : Exception
    {
        public BusinessException(string message) : base(message) { 
        
        }
    }
}
