﻿using FabricAirTask.Data.Abstract;
using FabricAirTask.Dto;
using FabricAirTask.Entity;
using FabricAirTask.Services.Abstract;
using Microsoft.IdentityModel.Tokens;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace FabricAirTask.Services.Concrete
{
    public class AuthService : IAuthService
    {

        private readonly IUserRepo _userRepo;
        private readonly IConfiguration _configuration;

        public AuthService(IHttpContextAccessor contextAccessor, IUserRepo userRepo, IConfiguration configuration)
        {

            _userRepo = userRepo;
            _configuration = configuration;
        }
        public async Task<string> Login(LoginDto loginDto)
        {
            var user = await _userRepo.GetByEmail(loginDto.Email);

            if (user == null)
            {
                return "User is not Found";
            }
            else if (!VerifyPasswordHash(loginDto.Password, user.PasswordHash, user.PasswordSalt))
            {
                return "Please Chech your Password";
            }
            else
            {
                var token = CreateToken(user);
                return "Login is Successful" + "Token is : " + token;
            }
        }

        public async Task<string> Register(User user, string password)
        {
            var response = await _userRepo.UserExist(user.Email);
            if (response)
            {
                return "User is already exists";
            }
            CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);

            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;
            user.DateCreated = DateTime.Now;
            _userRepo.AddUser(user);
            var token = CreateToken(user);
            return "User Created" + token;

        }
        private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512(passwordSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return computedHash.SequenceEqual(passwordHash);
            }
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }
        private string CreateToken(User user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_configuration.GetSection("AppSettings:SecretKey").Value));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken
                (
                    claims: claims,
                    expires: DateTime.Now.AddDays(1),
                    signingCredentials: creds
                );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }
    }
}
