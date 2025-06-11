namespace tesisAPI.DTOs
{
    public class AuthenticationRequestBody
    {
        public string? UserName { get; set; } = string.Empty;
        public string? Password { get; set; } = string.Empty;
    }
}
