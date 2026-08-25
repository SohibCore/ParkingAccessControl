using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace ParkingApp
{
    public class LicenseService
    {
        private const string LicenseFilePath = "license.dat";

        private static readonly string[] ValidKeys = new[]
        {
            "SOHIB-PARK-A7X9",
            "SOHIB-PARK-B3K2",
            "SOHIB-PARK-C8M1"
        };

        public bool IsActivated()
        {
            return File.Exists(LicenseFilePath);
        }

        public bool Activate(string enteredKey = "ParkingAccessControl")
        {
            string normalized = enteredKey.Trim().ToUpper();

            if (ValidKeys.Contains(normalized))
            {
                string hash = MakeHashCode(normalized);
                File.WriteAllText(LicenseFilePath, hash);
                return true;
            }
            return false;
        }
        private string MakeHashCode(string x)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(x));
            return Convert.ToBase64String(bytes);
        }
    }
}
