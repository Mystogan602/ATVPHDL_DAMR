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
    internal class EncryptDecrypt
    {
        private string password;
        private string filePath;
        private byte[] saltGlobal;
        public void setPassword(string password)
        {
            this.password = password;
        }
        public void setFilePath(string filePath)
        {
            this.filePath = filePath;
        }
        public void setSaltGlobal(byte[] saltGlobal)
        {
            this.saltGlobal = saltGlobal;
        }
        public string getPassword()
        {
            return this.password;
        }
        public string getFilePath()
        {
            return this.filePath;
        }
        public byte[] getSalt()
        {
            return this.saltGlobal;
        }
        static byte[] GenerateSalt()
        {
            byte[] salt = new byte[16];
            using (RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider())
            {
                rngCsp.GetBytes(salt);
            }
            return salt;
        }
        // Hash the combined password and salt
        static byte[] HashWithSalt(byte[] data)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(data);
            }
        }
        static byte[] createKey(string password, byte[] salt)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] passwordAndSalt = new byte[passwordBytes.Length + salt.Length];
            passwordBytes.CopyTo(passwordAndSalt, 0);
            salt.CopyTo(passwordAndSalt, passwordBytes.Length);

            // Hash the combined password and salt
            byte[] hashedKeyWithSalt = HashWithSalt(passwordAndSalt);
            return hashedKeyWithSalt;
        }
        public byte[] encryption(string data)
        {

            // Encryption key and initialization vector (IV)
            string iv = "1234567890123456"; // Change this to a secure IV (must be 16 bytes)

            try
            {
                byte[] salt = GenerateSalt();
                saltGlobal = salt;
                byte[] encryptionKey = createKey("123", salt);
                string executablePath = AppDomain.CurrentDomain.BaseDirectory;




                // Read the plaintext from the input file
               


                // Create AES encryption algorithm
                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.IV = Encoding.UTF8.GetBytes(iv);
                    aesAlg.Key = encryptionKey;
                    // Create an encryptor to perform the stream transform
                    ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                    // Create a memory stream to hold the encrypted data
                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        // Create a crypto stream that transforms the file data using encryption
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {
                                // Write the plaintext to the crypto stream
                                swEncrypt.Write(data);
                            }
                        }
                        return msEncrypt.ToArray();
                        

                        // Write the encrypted data to a new file

                    }

                }

            }
            catch (Exception ex)
            {
                return null;
            }

        }
        public void decryption(string fileName)
        {
            string executablePath = AppDomain.CurrentDomain.BaseDirectory;
            string keyFileName = fileName + ".dat";
            string saltFileName = fileName + "s" + ".dat";
            string iv = "1234567890123456";
            if (File.Exists(keyFileName))
            {
                string keyFile = Path.Combine(executablePath, keyFileName);
                byte[] key = File.ReadAllBytes(keyFile);
                string saltFile = Path.Combine(executablePath, saltFileName);
                byte[] salt = File.ReadAllBytes(saltFile);
                byte[] userPassword = createKey(password, salt);
                bool areEqual = userPassword.SequenceEqual(key);

                if (areEqual)
                {
                    try
                    {

                        // Read the encrypted data from the input file
                        byte[] encryptedData = File.ReadAllBytes(fileName);

                        // Create AES decryption algorithm
                        using (Aes aesAlg = Aes.Create())
                        {
                            aesAlg.IV = Encoding.UTF8.GetBytes(iv);
                            aesAlg.Key = key;

                            // Create a decryptor to perform the stream transform
                            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                            // Create a memory stream to hold the decrypted data
                            using (MemoryStream msDecrypt = new MemoryStream(encryptedData))
                            {
                                // Create a crypto stream that transforms the file data using decryption
                                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                                {
                                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                                    {
                                        // Read the decrypted data from the crypto stream
                                        string decryptedText = srDecrypt.ReadToEnd();

                                        // Write the decrypted text to an output file
                                        File.WriteAllText(fileName, decryptedText);
                                        File.Delete(keyFileName);
                                        MessageBox.Show("File được giải mã thành công !");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi : " + ex.Message);
                    }

                }
                else
                {
                    MessageBox.Show("sai mật khẩu !");
                }


            }
            else
            {
                MessageBox.Show("File phải được mã trước khi giải mã !");
            }
        }
    }
}
