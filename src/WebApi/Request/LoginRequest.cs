namespace DYApi.Request
{
    public class LoginRequest
    {
        public string? UserName { get; set; }
        public string? Password { get; set; }
    }

    public class WechatMiniProRequest: LoginRequest
    {
        public string? Code { get; set; }
    }
    public class RegisterRequest
    {
        public required string UserName { get; set; }
        public required string Password { get; set; }
    }
}
