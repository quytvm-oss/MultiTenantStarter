using System.Security.Cryptography;
using System.Text;

namespace Modules.Identity.Helpers;

public static class TokenHasher
{
    public static string Sha256(string value) => 
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    
    public static string Fingerprint(string value) => Sha256(value)[..12];
}