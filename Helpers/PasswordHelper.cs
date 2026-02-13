using BCrypt.Net;
using static System.Net.Mime.MediaTypeNames;

namespace eProtokoll.Helpers
{
    public static class PasswordHelper
    {
        public static string Hash(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        public static bool Verify(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}