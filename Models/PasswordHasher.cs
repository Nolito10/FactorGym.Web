using System;
using System.Security.Cryptography;
using System.Text;

namespace FactorFitGym.Web.Models
{
    public static class PasswordHasher
    {
        public static string Hash(string password)
        {
            if (string.IsNullOrEmpty(password)) 
                return string.Empty;

            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}
