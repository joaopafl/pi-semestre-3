using System.Security.Cryptography;
using System.Text;

namespace Pi_Odonto.Helpers
{
    public static class PasswordHelper
    {
        public static string HashPassword(string password)
        {
            // Usar BCrypt é mais seguro, mas se não tiver a lib, pode usar SHA256
            using (var sha256 = SHA256.Create())
            {
                var salt = "PiOdonto2025!@#"; // Salt fixo (em produção, use salt único por usuário)
                var saltedPassword = password + salt;
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            var hashInput = HashPassword(password);
            return hashInput.Equals(hashedPassword);
        }

        public static string GenerateRandomPassword(int length = 8)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        internal static bool VerifyPassword(object senha1, string? senha2)
        {
            throw new NotImplementedException();
        }
    }
}