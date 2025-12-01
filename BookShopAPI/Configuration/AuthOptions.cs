using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace BookShopAPI.Configuration
{
    public class AuthOptions
    {
        public const string ISSUER = "BookShop";
        public const string AUDIENCE = "MyAuthClient";
        const string KEY = "YourSuperSecretKeyThatIsAtLeast16CharactersLong!";

        public static SymmetricSecurityKey GetSymmetricSecurityKey() =>
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(KEY));

    }
}
