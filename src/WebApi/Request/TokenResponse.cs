using DYApi.Models;
using System.Text.Json.Serialization;

namespace DYApi.Request
{
    public class TokenResponse
    {
        public string AccessToken { get; set; }
        public string TokenType { get; set; }
    }

    public class LoginTokenResponse : TokenResponse
    {
        public UserData User { get; set; }
    }


    public class UserData
    {
        public required string UserId { get; set; }
        public required string SessionKey { get; set; }
    }
}
