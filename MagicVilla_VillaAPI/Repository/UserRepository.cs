using AutoMapper;
using MagicVilla_ClassLibrary.Models;
using MagicVilla_ClassLibrary.Models.Dto;
using MagicVilla_VillaAPI.Repository.IRepostiory;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MagicVilla_VillaAPI.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly RoleManager<IdentityRole> _roleManager;
        private string secretKey;

        public UserRepository(ApplicationDbContext db, IConfiguration configuration,
                     UserManager<ApplicationUser> userManager, IMapper mapper, RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _mapper = mapper;
            _roleManager = roleManager;
            secretKey = configuration.GetValue<string>("ApiSettings:Secret");
        }

        public bool IsUniqueUser(string username)
        {
            var user = _db.ApplicationUsers.FirstOrDefault(x => x.UserName == username);
            if (user == null)
            {
                return true;
            }
            return false;
        }

        public async Task<TokenDTO> Login(LoginRequestDTO loginRequestDTO)
        {
            var user = _db.ApplicationUsers
                  .FirstOrDefault(u => u.UserName.ToLower() == loginRequestDTO.UserName.ToLower());

            bool isValid = await _userManager.CheckPasswordAsync(user, loginRequestDTO.Password);

            if (user == null || isValid == false)
            {
                return new TokenDTO()
                {
                    AccessToken = "",
                };
            }

            var jwtTokenId = $"JTI{Guid.NewGuid()}";
            var accessToken = await GetAccessToken(user, jwtTokenId);
            var refreshToken = await CreateNewRefreshToken(user.Id, jwtTokenId);
            TokenDTO tokenDto = new TokenDTO()
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
            return tokenDto;
        }

        public async Task<UserDTO> Register(RegisterationRequestDTO registerationRequestDTO)
        {
            ApplicationUser user = new()
            {
                UserName = registerationRequestDTO.UserName,
                Email = registerationRequestDTO.UserName,
                NormalizedEmail = registerationRequestDTO.UserName.ToUpper(),
                Name = registerationRequestDTO.Name
            };

            try
            {
                var result = await _userManager.CreateAsync(user, registerationRequestDTO.Password);
                if (result.Succeeded)
                {
                    if (!_roleManager.RoleExistsAsync(registerationRequestDTO.Role).GetAwaiter().GetResult())
                    {
                        await _roleManager.CreateAsync(new IdentityRole(registerationRequestDTO.Role));
                    }

                    await _userManager.AddToRoleAsync(user, registerationRequestDTO.Role);
                    var userToReturn = _db.ApplicationUsers
                        .FirstOrDefault(u => u.UserName == registerationRequestDTO.UserName);
                    return _mapper.Map<UserDTO>(userToReturn);

                }
            }
            catch (Exception e)
            {

            }
            return new UserDTO();
        }

        protected async Task<string> GetAccessToken(ApplicationUser user, string jwtTokenId)
        {
            //if user was found generate JWT Token

            var roles = await _userManager.GetRolesAsync(user);
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                     new Claim(ClaimTypes.Name, user.UserName.ToString()),
                     new Claim(ClaimTypes.Role, roles.FirstOrDefault()),
                     new Claim(JwtRegisteredClaimNames.Jti, jwtTokenId),
                     new Claim(JwtRegisteredClaimNames.Sub, user.Id)
                }),
                Expires = DateTime.UtcNow.AddMinutes(1),
                SigningCredentials = new(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenStr = tokenHandler.WriteToken(token);
            return tokenStr;
        }

        //ขั้นตอนการ refresh token
        public async Task<TokenDTO> RefreshAccessToken(TokenDTO tokenDTO)
        {
            // Find an existing refresh token
            // ค้นหารีเฟรชโทเคนจาก database ว่ามีหรือไม่
            var existingRefreshToken = await _db.RefreshTokens.FirstOrDefaultAsync(u => u.Refresh_Token == tokenDTO.RefreshToken);
            if (existingRefreshToken == null)
            {
                return new TokenDTO();
            }

            #region A ตรวจสอบความถูกต้องของโทเคนที่ส่งมาเข้า
            // Compare data from existing refresh and access token provided and if there is any missmatch then consider it as a fraud
            // ตรวจสอบว่า accesstoken ที่ส่งเข้ามาถูกต้องหรือไม่
            var accessTokenData = GetAccessTokenData(tokenDTO.AccessToken);
            if (!accessTokenData.isSuccessful || accessTokenData.userId != existingRefreshToken.UserId
                || accessTokenData.tokenId != existingRefreshToken.JwtTokenId)
            {
                existingRefreshToken.IsValid = false;
                _db.SaveChanges();
                return new TokenDTO();
            }

            // When someone tries to use not valid refresh token, fraud possible
            //เมื่อมีคนพยายามใช้โทเค็นรีเฟรชที่ไม่ถูกต้อง อาจเกิดการบุกรุกได้
            if (!existingRefreshToken.IsValid)
            {
                var chainRecords = _db.RefreshTokens.Where(u => u.UserId == existingRefreshToken.UserId
                && u.JwtTokenId == existingRefreshToken.JwtTokenId)
                    .ExecuteUpdate(u => u.SetProperty(refreshToken => refreshToken.IsValid, false));
                
                return new TokenDTO();
            }

            // If just expired then mark as invalid and return empty
            // ถ้าโทเคนหมดอายุ ให้ส่งค่าว่างกลับไป
            if (existingRefreshToken.ExpiresAt < DateTime.UtcNow)
            {
                existingRefreshToken.IsValid = false;
                _db.SaveChanges();
                return new TokenDTO();
            }
            #endregion A ตรวจสอบความถูกต้องของโทเคนที่ส่งมาเข้า

            #region B สร้าง accessToken refreshToken ให้ใหม่
            // replace old refresh with a new one with updated expire date
            // ให้อัพเดท refreshTokenใหม่ คือ ExpiresAt และ Refresh_Token
            var newRefreshToken = await CreateNewRefreshToken(existingRefreshToken.UserId, existingRefreshToken.JwtTokenId);

            // revoke existing refresh token
            // เปลี่ยนสถานะของเดิมให้เป็น false (ยกเลิกการใช้งาน)
            existingRefreshToken.IsValid = false;
            _db.SaveChanges();

            // generate new access token
            // สร้าง accessToken ใหม่
            var applicationUser = _db.ApplicationUsers.FirstOrDefault(u => u.Id == existingRefreshToken.UserId);
            if (applicationUser == null)
                return new TokenDTO();

            var newAccessToken = await GetAccessToken(applicationUser, existingRefreshToken.JwtTokenId);

            return new TokenDTO()
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
            };
            #endregion B สร้าง accessToken refreshToken ให้ใหม่
        }

        protected (bool isSuccessful, string userId, string tokenId) GetAccessTokenData(string accessToken)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwt = tokenHandler.ReadJwtToken(accessToken);
                var jwtTokenId = jwt.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Jti).Value;
                var userId = jwt.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Sub).Value;
                return (true, userId, jwtTokenId);

            }
            catch
            {
                return (false, null, null);
            }
        }

        protected async Task<string> CreateNewRefreshToken(string userId, string tokenId)
        {
            RefreshToken refreshToken = new()
            {
                IsValid = true,
                UserId = userId,
                JwtTokenId = tokenId,
                ExpiresAt = DateTime.UtcNow.AddMinutes(1),
                Refresh_Token = Guid.NewGuid() + "-" + Guid.NewGuid(),
            };

            await _db.RefreshTokens.AddAsync(refreshToken);
            await _db.SaveChangesAsync();
            return refreshToken.Refresh_Token;
        }
    }
}