using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace FormMain
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void EmbedButton_Click(object sender, EventArgs e)
        {
            // Hiển thị hộp thoại để chọn tệp PDF
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "PDF Files (*.pdf)|*.pdf";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Lấy đường dẫn của tệp PDF được chọn
                string pdfFilePath = openFileDialog.FileName;
                // Lấy file cần nhúng từ hệ thống
                OpenFileDialog fileToEmbedDialog = new OpenFileDialog();
                fileToEmbedDialog.Filter = "All Files (*.*)|*.*";
                if (fileToEmbedDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = fileToEmbedDialog.FileName;
                    // Tạo đường dẫn cho tệp đầu ra
                    string outputFilePath = Path.Combine(Path.GetDirectoryName(pdfFilePath), "output.pdf");
                    
                    try
                    {

                        // Nhúng văn bản vào tệp PDF
                        EmbedFileInPDF(pdfFilePath, filePath, outputFilePath);

                        // Hiển thị thông báo thành công
                        MessageBox.Show("Text embedded successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        // Hiển thị thông báo lỗi nếu có lỗi xảy ra
                        MessageBox.Show($"Error embedding file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void DecodeButton_Click(object sender, EventArgs e)
        {
            // Hiển thị hộp thoại để chọn tệp PDF
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "PDF Files (*.pdf)|*.pdf";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Lấy đường dẫn của tệp PDF được chọn
                string pdfFilePath = openFileDialog.FileName;
                string outputFilePath = Path.Combine(Path.GetDirectoryName(pdfFilePath), "file_input.pdf");
                try
                {
                    DecodeFileFromPDF(pdfFilePath, outputFilePath);
                }
                catch (Exception ex)
                {
                    // Hiển thị thông báo lỗi nếu có lỗi xảy ra
                    MessageBox.Show($"Error decoding text: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        static byte[] CreateSalt()
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

        private byte[] EncryptFile(string inputFile, string password)
        {
            byte[] fileBytes = File.ReadAllBytes(inputFile);
            byte[] salt = CreateSalt();
            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, 10000))
            using (var aesAlg = Aes.Create())
            {
                aesAlg.Key = deriveBytes.GetBytes(32); // 256-bit key
                aesAlg.IV = deriveBytes.GetBytes(16);  // 128-bit IV

                // Create a CryptoStream to perform encryption
                using (var msEncrypt = new MemoryStream())
                {
                    // Write the salt to the beginning of the MemoryStream
                    msEncrypt.Write(salt, 0, salt.Length);

                    using (var csEncrypt = new CryptoStream(msEncrypt, aesAlg.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        // Write the file data to the CryptoStream
                        csEncrypt.Write(fileBytes, 0, fileBytes.Length);
                        csEncrypt.FlushFinalBlock();
                    }

                    // Combine the salt and encrypted data
                    byte[] encryptedFileBytes = msEncrypt.ToArray();
                    return encryptedFileBytes;
                }
            }
        }
        private void EmbedFileInPDF(string inputPdfPath, string inputFile, string outputPdfPath)
        {
            // Kiểm tra tính hợp lệ của tệp PDF đầu vào
            if (!File.Exists(inputPdfPath) || Path.GetExtension(inputPdfPath).ToLower() != ".pdf")
            {
                MessageBox.Show("Invalid PDF file selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            if (!File.Exists(inputFile))
            {
                MessageBox.Show("Invalid file selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string password = DataToEmbedTextBox.Text;
            //byte[] fileBytes = File.ReadAllBytes(inputFile);
            byte[] fileBytes = EncryptFile(inputFile, password);
            // Đọc nội dung của tệp PDF
            byte[] pdfBytes = File.ReadAllBytes(inputPdfPath);

            // Tìm vị trí của lần xuất hiện của "/Info" trong nội dung của tệp PDF
            int metadataPosition = FindBytes(pdfBytes, Encoding.ASCII.GetBytes("/Info"), false, 1);

            if (metadataPosition == -1)
            {
                MessageBox.Show("Invalid PDF format (missing metadata).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Nhúng byte đầu tiên vào metadata
            byte[] firstByte = new byte[] { fileBytes[0] };
            byte[] remainingCombinedByte1 = new byte[pdfBytes.Length - metadataPosition];
            byte[] combinedBytes1 = new byte[pdfBytes.Length + firstByte.Length];

            // Sao chép phần đầu của nội dung PDF gốc (đến metadata)
            Array.Copy(pdfBytes, combinedBytes1, metadataPosition);

            // Sao chép phần còn lại của nội dung PDF gốc (sau metadata)
            Array.Copy(pdfBytes, metadataPosition, remainingCombinedByte1, 0, remainingCombinedByte1.Length);

            // Sao chép byte đầu tiên được nhúng vào metadata
            Array.Copy(firstByte, 0, combinedBytes1, metadataPosition, firstByte.Length);

            // Sao chép phần còn lại sau vị trí metadata
            Array.Copy(remainingCombinedByte1, 0, combinedBytes1, metadataPosition + firstByte.Length, remainingCombinedByte1.Length);

            // Lưu các byte còn lại vào một mảng khác
            int remainingLength = fileBytes.Length - 1;
            byte[] remainingBytes = new byte[remainingLength];
            Array.Copy(fileBytes, 1, remainingBytes, 0, remainingLength);
            byte[] combinedBytes2 = new byte[combinedBytes1.Length + remainingBytes.Length];

            // Sao chép nội dung PDF gốc
            Array.Copy(combinedBytes1, combinedBytes2, combinedBytes1.Length);

            // Sao chép các byte còn lại được nhúng vào cuối
            Array.Copy(remainingBytes, 0, combinedBytes2, combinedBytes1.Length, remainingBytes.Length);

            // Ghi nội dung đã được sửa đổi vào tệp PDF đầu ra
            File.WriteAllBytes(outputPdfPath, combinedBytes2);
        }
        private byte[] DecryptFile(byte[] encryptBytes, string password)
        {
            // Extract salt from the beginning of the encrypted data
            byte[] salt = new byte[16];
            Array.Copy(encryptBytes, salt, salt.Length);

            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, 10000))
            using (var aesAlg = Aes.Create())
            {
                aesAlg.Key = deriveBytes.GetBytes(32); // 256-bit key
                aesAlg.IV = deriveBytes.GetBytes(16);  // 128-bit IV

                // Create a CryptoStream to perform decryption
                using (var msDecrypt = new MemoryStream(encryptBytes, salt.Length, encryptBytes.Length - salt.Length))
                using (var csDecrypt = new CryptoStream(msDecrypt, aesAlg.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    // Read the decrypted data from the CryptoStream
                    using (var msOutput = new MemoryStream())
                    {
                        csDecrypt.CopyTo(msOutput);
                        return msOutput.ToArray();
                    }
                }
            }
        }
        private void DecodeFileFromPDF(string pdfFilePath, string outputFile)
        {
            // Kiểm tra tính hợp lệ của tệp PDF đầu vào
            if (!File.Exists(pdfFilePath) || Path.GetExtension(pdfFilePath).ToLower() != ".pdf")
            {
                MessageBox.Show("Invalid PDF file selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            try
            {
                // Đọc nội dung của tệp PDF
                byte[] pdfBytes = File.ReadAllBytes(pdfFilePath);

                // Tìm vị trí của lần xuất hiện của "/Info" trong nội dung của tệp PDF
                int metadataPosition = FindBytes(pdfBytes, Encoding.ASCII.GetBytes("/Info"), false, 1);

                if (metadataPosition == -1)
                {
                    MessageBox.Show("Invalid PDF format (missing metadata).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                byte[] firstByte = new byte[1];
                Array.Copy(pdfBytes, metadataPosition, firstByte, 0, 1);

                // Tìm vị trí của lần xuất hiện cuối cùng của "%EOF" trong nội dung của tệp PDF
                int eofPosition = FindBytes(pdfBytes, Encoding.ASCII.GetBytes("%EOF"), false,2);

                if (eofPosition == -1)
                {
                    MessageBox.Show("Invalid PDF format (missing %EOF).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                // Rút trích các byte còn lại từ cuối tệp PDF
                int remainingLength = pdfBytes.Length - eofPosition;
                byte[] remainingBytes = new byte[remainingLength];
                Array.Copy(pdfBytes, eofPosition, remainingBytes, 0, remainingLength);

                byte[] embeddedFileBytes = new byte[firstByte.Length + remainingBytes.Length];
                Array.Copy(firstByte, 0, embeddedFileBytes, 0, firstByte.Length);
                Array.Copy(remainingBytes, 0, embeddedFileBytes, firstByte.Length, remainingBytes.Length);

                string password = OutputTextBox.Text;
                embeddedFileBytes = DecryptFile(embeddedFileBytes, password);
                File.WriteAllBytes(outputFile, embeddedFileBytes);
            }
            catch (Exception ex)
            {
                // Hiển thị thông báo lỗi nếu có lỗi xảy ra
                MessageBox.Show($"Error decoding text: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int FindBytes(byte[] haystack, byte[] needle, bool reverse = false, int positionNeedle = 1)
        {
            // Hàm tìm kiếm vị trí của chuỗi byte (needle) trong mảng byte (haystack)
            // Parameters:
            //   - haystack: Mảng byte chứa dữ liệu cần tìm kiếm
            //   - needle: Mảng byte là chuỗi cần tìm kiếm
            //   - reverse: Cờ chỉ định xem tìm kiếm có nên thực hiện theo chiều xuôi hay ngược lại
            //   - positionNeedle: Số chỉ định vị trí của chuỗi cần tìm kiếm (bắt đầu là 1)
            // Returns:
            //   - Vị trí đầu tiên của chuỗi cần tìm kiếm nếu tìm thấy, ngược lại trả về -1

            // Xác định hướng tìm kiếm dựa trên giá trị của reverse
            int increment = reverse ? -1 : 1;
            int start = reverse ? haystack.Length - 1 : 0;
            int countNeedle = 0;

            // Duyệt qua mảng byte để tìm kiếm
            for (int i = start; reverse ? i >= 0 : i < haystack.Length - needle.Length + 1; i += increment)
            {
                // Kiểm tra xem có khớp chuỗi cần tìm kiếm không
                bool found = true;
                for (int j = 0; j < needle.Length; j++)
                {
                    if (haystack[i + j] != needle[j])
                    {
                        found = false;
                        break;
                    }
                }

                // Nếu khớp, tăng số lượng chuỗi cần tìm kiếm đã tìm thấy
                if (found)
                {
                    countNeedle++;

                    // Nếu đã tìm thấy đủ số lượng chuỗi cần tìm kiếm, trả về vị trí đầu tiên của chuỗi
                    if (countNeedle >= positionNeedle)
                    {
                        return i + needle.Length;
                    }
                }
            }

            // Trả về -1 nếu không tìm thấy chuỗi cần tìm kiếm
            return -1;
        }
    }
}
