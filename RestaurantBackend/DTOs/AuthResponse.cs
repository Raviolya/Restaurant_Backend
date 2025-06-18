namespace RestaurantBackend.DTOs
{
    public class AuthResponse
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public DateTime Expiration { get; set; }
        public UserResponseDto User { get; set; } 
    }


}