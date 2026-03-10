using Business.Data;
using Business.Model;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Business.Utilities
{
    public interface IJwtUtils
    {
        public string GenerateJwtToken(User account);
        public SecurityToken? ValidateJwtToken(string token);
        //public RefreshToken GenerateRefreshToken(string ipAddress);
        public string GetSecret();
    }

    public class JwtUtils : IJwtUtils
    {
        private readonly DBContext _context;
        private readonly AppSettings _appSettings;

        public JwtUtils(
            DBContext context,
            IOptions<AppSettings> appSettings)
        {
            _context = context;
            _appSettings = appSettings.Value;
        }

        public string GenerateJwtToken(User user)
        {
            // generate token that is valid for 3 months
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            
            // Note: Role is now company-specific and sent with each request via X-Company-Id header
            // Token only contains user identity
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("id", user.id.ToString()),
                    new Claim("email", user.email)
                }),
                Expires = DateTime.UtcNow.AddMonths(3),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public SecurityToken? ValidateJwtToken(string token)
        {
            if (token == null)
            {
                Console.WriteLine("[JWT DEBUG] Token is null");
                return null;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            Console.WriteLine($"[JWT DEBUG] Validating token (length: {token.Length})");
            Console.WriteLine($"[JWT DEBUG] Secret key length: {_appSettings.Secret?.Length ?? 0}");
            
            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                Console.WriteLine($"[JWT DEBUG] Token validated successfully for user: {jwtToken.Claims.FirstOrDefault(x => x.Type == "id")?.Value}");
                
                // return account id from JWT token if validation successful
                return jwtToken;
            }
            catch (SecurityTokenExpiredException ex)
            {
                Console.WriteLine($"[JWT DEBUG] Token EXPIRED: {ex.Message}");
                return null;
            }
            catch (SecurityTokenInvalidSignatureException ex)
            {
                Console.WriteLine($"[JWT DEBUG] Token INVALID SIGNATURE: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[JWT DEBUG] Token validation FAILED: {ex.GetType().Name} - {ex.Message}");
                return null;
            }
        }

        //public RefreshToken GenerateRefreshToken(string userId)
        //{
        //    var refreshToken = new RefreshToken
        //    {
        //        // token is a cryptographically strong random sequence of values
        //        Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(64)),
        //        // token is valid for 7 days
        //        Expires = DateTime.UtcNow.AddDays(7),
        //        Created = DateTime.UtcNow,
        //        CreatedById = userId
        //    };

        //    // ensure token is unique by checking against db
        //    var tokenIsUnique = !_context.User.Any(a => a.RefreshTokens.Any(t => t.Token == refreshToken.Token));

        //    if (!tokenIsUnique)
        //        return GenerateRefreshToken(userId);

        //    return refreshToken;
        //}

        public string GetSecret()
        {
            return _appSettings.Secret;
        }
    }
}
