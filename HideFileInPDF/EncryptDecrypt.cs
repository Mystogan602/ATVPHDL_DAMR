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
        private FileProcessing fileP;
        public EncryptDecrypt(FileProcessing file) {
            fileP = file;
        }
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
            return hashedPassword == HashPassword(inputPassword, salt);
        }
        //public void ChangePassword(string oldPassword, string newPassword)
        //{
        //    using (SHA256 sha256 = SHA256.Create())
        //    {
        //        if (CheckPassword(oldPassword))
        //        {
        //            byte[] newPasswordHash = new byte[BlockSize];
        //            // Hash mật khẩu mới
        //            byte[] enteredPasswordHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(newPassword));
        //            Array.Copy(enteredPasswordHash, newPasswordHash, enteredPasswordHash.Length);
        //            // Lưu hashed password mới vào block đầu tiên
        //            using (FileStream fs = new FileStream(FileSystemFilePath, FileMode.Open, FileAccess.Write))
        //            {
        //                fs.Seek(HashedPasswordBlock, SeekOrigin.Begin);
        //                fs.Write(enteredPasswordHash, 0, enteredPasswordHash.Length);
        //            }

        //            hashedPassword = newPasswordHash; // Cập nhật hashed password trong bộ nhớ
        //        }
        //        else
        //        {
        //            MessageBox.Show("Mật khẩu cũ không đúng.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //        }
        //    }
        //}

        public byte[] EncryptFile(string inputFile, string hashedPassword, byte[] salt)
        {
            // Generate encryption key using PBKDF2
            byte[] key = GenerateKey(hashedPassword, salt);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.GenerateIV();

                // Encrypt the file content
                byte[] encryptedBytes = EncryptFileContent(inputFile, aesAlg);

                fileP.WriteFAT(inputFile, aesAlg.IV, salt, hashedPassword);

                return encryptedBytes;
            }
        }

        private byte[] GenerateKey(string hashedPassword, byte[] salt)
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(hashedPassword, salt, 10000))
            {
                return deriveBytes.GetBytes(32); // 32 bytes for a 256-bit key
            }
        }

        private byte[] EncryptFileContent(string inputFile, Aes aesAlg)
        {
            using (FileStream fsInput = new FileStream(inputFile, FileMode.Open))
            using (MemoryStream msOutput = new MemoryStream())
            using (CryptoStream cryptoStream = new CryptoStream(msOutput, aesAlg.CreateEncryptor(), CryptoStreamMode.Write))
            {
                fsInput.CopyTo(cryptoStream);
                cryptoStream.FlushFinalBlock();
                return msOutput.ToArray();
            }
        }
        public byte[] DecryptFile(string fileName, byte[] encryptedFile, string hashedPassword, byte[] salt)
        {
            // Generate decryption key using PBKDF2
            byte[] key = GenerateKey(hashedPassword, salt);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;

                // Extract IV from the beginning of the encrypted file
                aesAlg.IV = fileP.GetIV(fileName);

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