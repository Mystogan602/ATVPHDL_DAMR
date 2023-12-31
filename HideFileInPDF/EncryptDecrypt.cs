using libraryFileProcessing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace libraryEncryptDecrypt
{
    public class EncryptDecrypt
    {
        public byte[] CreateSalt()
        {
            // Tạo một mảng byte có kích thước là 16
            byte[] salt = new byte[16];

            // Sử dụng RNGCryptoServiceProvider để tạo dữ liệu ngẫu nhiên cho salt
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }

            // Trả về mảng byte chứa salt
            return salt;
        }
        public string HashPassword(string password, byte[] salt)
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, 10000))
            using (var sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(deriveBytes.GetBytes(32));  // 256-bit key
                return Convert.ToBase64String(hashedBytes);  // Lưu mật khẩu băm dưới dạng Base64
            }
        }
        public bool VerifyPassword(string inputPassword, string hashedPassword, byte[] salt)
        {
            // Băm mật khẩu nhập vào với salt từ tệp tin và so sánh với mật khẩu băm từ tệp tin
            string checkpass = HashPassword(inputPassword, salt);
            return hashedPassword == HashPassword(inputPassword, salt);
        }
        public byte[] ReencryptedFile(string oldHashedPassword, string newHashedPassword, byte[] encryptedFileContent, byte[] salt)
        {

            // Decrypt the file content using the old key
            byte[] decryptedFileContent = DecryptFile(encryptedFileContent, oldHashedPassword,salt);

            // Encrypt the file content using the new key
            byte[] reencryptedFileContent = EncryptFile(decryptedFileContent, newHashedPassword,salt);
            
            return reencryptedFileContent;
        }

        private byte[] GenerateKey(string hashedPassword, byte[] salt)
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(hashedPassword, salt, 10000))
            {
                return deriveBytes.GetBytes(32); // 32 bytes for a 256-bit key
            }
        }
        public byte[] EncryptFile(byte[] fileData, string hashedPassword, byte[] salt)
        {
            // Generate encryption key using PBKDF2
            byte[] key = GenerateKey(hashedPassword, salt);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.GenerateIV();

                // Encrypt the file content
                byte[] encryptedBytes = EncryptFileContent(fileData, aesAlg);
                // Combine IV and encrypted content
                byte[] result = new byte[aesAlg.IV.Length + encryptedBytes.Length];
                Array.Copy(aesAlg.IV, result, aesAlg.IV.Length);
                Array.Copy(encryptedBytes, 0, result, aesAlg.IV.Length, encryptedBytes.Length);

                return result;
            }
        }

        private byte[] EncryptFileContent(byte[] fileData, Aes aesAlg)
        {
            using (MemoryStream msInput = new MemoryStream(fileData))
            using (MemoryStream msOutput = new MemoryStream())
            using (CryptoStream cryptoStream = new CryptoStream(msOutput, aesAlg.CreateEncryptor(), CryptoStreamMode.Write))
            {
                msInput.CopyTo(cryptoStream);
                cryptoStream.FlushFinalBlock();
                return msOutput.ToArray();
            }
        }
        public byte[] DecryptFile(byte[] encryptedFile, string hashedPassword, byte[] salt)
        {
            // Generate decryption key using PBKDF2
            byte[] key = GenerateKey(hashedPassword, salt);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;

                // Extract IV from the beginning of the encrypted file
                byte[] iv = new byte[aesAlg.IV.Length];
                Array.Copy(encryptedFile, iv, iv.Length);
                aesAlg.IV = iv;
                // Decrypt the content (excluding IV)
                byte[] decryptedBytes = DecryptFileContent(encryptedFile, aesAlg);

                return decryptedBytes;
            }
        }

        private byte[] DecryptFileContent(byte[] encryptedFile, Aes aesAlg)
        {
            using (MemoryStream msInput = new MemoryStream(encryptedFile, aesAlg.IV.Length, encryptedFile.Length - aesAlg.IV.Length))
            using (MemoryStream msOutput = new MemoryStream())
            using (CryptoStream cryptoStream = new CryptoStream(msInput, aesAlg.CreateDecryptor(), CryptoStreamMode.Read))
            {
                cryptoStream.CopyTo(msOutput);
                return msOutput.ToArray();
            }
        }
    }
}