using GorgoDresses.Common.Enums;
using GorgoDresses.Common.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using GorgoDresses.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace GorgoDresses.Common.Helpers;

public class JwtHelper
{
    private readonly IConfiguration configuration;
    public JwtHelper(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public string GenerateJwtToken(Guid userId, string email, IList<string> roles)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(configuration["jwtKey"]);
        var securityKey = new SymmetricSecurityKey(key);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var rolesClaims = roles.Select(x => new Claim("role", x)).ToList();
        var claims = new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, userId.ToString()),
            new Claim("roles", string.Join(',', roles)),
        });

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Audience = configuration["jwtAudience"],
            Issuer = configuration["jwtIssuer"],
            Subject = claims,
            Expires = DateTime.Now.AddMinutes(Convert.ToInt32(configuration["durationInMinutes"])),
            SigningCredentials = credentials
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public bool ValidateJwtToken(Guid userId, string token)
    {
        if (VerifyToken(token))
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var idClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.UniqueName);
            var rolesClaim = jwt.Claims.FirstOrDefault(c => c.Type == "roles");

            return idClaim.Value == userId.ToString() || rolesClaim.Value == UserRoleConstants.Administrator;
        }

        return false;
    }

    public Guid? GetUserId(string token)
    {
        if (VerifyToken(token))
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var idClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.UniqueName);

            return Guid.Parse(idClaim.Value);
        }

        return null;
    }

    public string RegenerateJwtToken(string oldToken)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(oldToken);
        var emailClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        var idClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.UniqueName);
        var rolesClaim = jwt.Claims.FirstOrDefault(c => c.Type == "roles");

        return GenerateJwtToken(Guid.Parse(idClaim.Value), emailClaim.Value, rolesClaim.Value.Split(','));
    }

    public bool IsAdmin(string token)
    {
        if (VerifyToken(token))
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var rolesClaim = jwt.Claims.FirstOrDefault(c => c.Type == "roles");
            return rolesClaim.Value == UserRoleConstants.Administrator;
        }

        return false;
    }

    public JwtSecurityToken ParseToken(string token)
    {
        if (!VerifyToken(token))
        {
            throw new AuthenticationException(ErrorCode.Unauthorized);
        }
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        return jwt;
    }

    public bool VerifyToken(string token)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["jwtKey"]));

        var validationParameters = new TokenValidationParameters()
        {
            IssuerSigningKey = key,
            ValidAudience = configuration["jwtAudience"],
            ValidIssuer = configuration["jwtIssuer"],
            ValidateLifetime = true,
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        SecurityToken validatedToken;

        try
        {
            tokenHandler.ValidateToken(token, validationParameters, out validatedToken);
        }
        catch (SecurityTokenException)
        {
            return false;
        }
        catch
        {
            return false;
        }

        return validatedToken != null;
    }
}
