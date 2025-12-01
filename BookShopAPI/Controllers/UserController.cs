using AutoMapper;
using BookShopAPI.Configuration;
using BookShopAPI.Dto.User;
using BookShopAPI.Models;
using BookShopAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BookShopAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly BookShopContext _db;
        private readonly IMapper _mapper;
        private readonly IPasswordService _passwordService;

        public UserController(BookShopContext db, IMapper mapper, IPasswordService passwordService)
        {
            _db = db;
            _mapper = mapper;
            _passwordService = passwordService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserRequestDto loginUserRequest)
        {
            var user = await _db.Customers
                .Include(x => x.Role)
                .SingleOrDefaultAsync(x => x.Email.ToLower() == loginUserRequest.Login.ToLower());

            if (user == null)
                return NotFound("Login not found");
            if (!_passwordService.VerifyPassword(loginUserRequest.Password, user.PasswordHash))
                return BadRequest(_passwordService.HashPassword(loginUserRequest.Password));

            var tokenHandler = new JwtSecurityTokenHandler();

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role.RoleName)
            };

            var accessTokenDescripter = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(5),
                SigningCredentials = new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256)
            };

            var accessToken = tokenHandler.CreateToken(accessTokenDescripter);
            var encodedAccessToken = tokenHandler.WriteToken(accessToken);

            var token = new TokenResponseDto()
            {
                token = encodedAccessToken,
                Expires = accessToken.ValidTo
            };

            return Ok(token);

        }

        [HttpGet("get")]
        [Authorize]
        public async Task<IActionResult> GetUserInfo()
        {
            var currentUser = await _db.Customers
                .Include(x => x.Role)
                .SingleAsync(x => x.Email == User.Identity.Name);

            return Ok(_mapper.Map<UserResponseDto>(currentUser));
        }
    }
}
