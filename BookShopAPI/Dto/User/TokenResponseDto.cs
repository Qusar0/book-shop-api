namespace BookShopAPI.Dto.User
{
    public class TokenResponseDto
    {
        public string token { get; set; }
        public DateTime Expires { get; set; }
    }
}
