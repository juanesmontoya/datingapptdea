using System.Security.Cryptography;
using System.Threading.Tasks;
using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using API.DTOs;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers

{
    public class AccountController : BaseApiController
    {
        private readonly DataContext _context;

        public AccountController (DataContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AppUser>> Register(RegisterDTO registerDTO)
        {
            if(await UserExists(registerDTO.Username)) return BadRequest("Ya existe el nombre de usuario");

            using var hmac = new HMACSHA512();

            var user = new AppUser
            {
                UserName = registerDTO.Username.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDTO.Password)),
                PasswordSalt = hmac.Key                
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        [HttpPost("login")]
        private async Task<ActionResult<AppUser>> Login(LoginDTO loginDTO)
        {
            var user = await _context.Users.SingleOrDefaultAsync(user => user.UserName == loginDTO.Username);

            if(user == null) return Unauthorized("Usuario invalido");

            using var hmac = new HMACSHA512(user.PasswordSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDTO.Password));

            for(int i = 0; i < computedHash.Length; i++)
            {
                if(computedHash[i] != user.PasswordHash[i]) return Unauthorized("Password invalido");
            }

            return user;
        }

        private async Task<bool> UserExists(string username)
        {
            return await _context.Users.AnyAsync(variable => variable.UserName == username.ToLower());
        }
    }
}