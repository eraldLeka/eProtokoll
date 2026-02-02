using Microsoft.AspNetCore.Identity;
using BCrypt.Net;

namespace eProtokoll.Services
{
    public class BCryptPasswordHasher<TUser> : IPasswordHasher<TUser> where TUser : class
    {
        public string HashPassword(TUser user, string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        public PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword)
        {
            bool isValid = BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword);
            return isValid ? PasswordVerificationResult.Success : PasswordVerificationResult.Failed;
        }
    }
}