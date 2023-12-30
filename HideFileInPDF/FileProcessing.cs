using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography;

using System.Drawing;
using System.Drawing.Drawing2D;
using libraryEncryptDecrypt;
using System.Reflection;
using System.Security.Policy;

namespace libraryFileProcessing
{
    public class FileProcessing
    {
        private EncryptDecrypt encryptDecrypt;

        public const int BlockSize = 512;//byte
        public const int FATSize = 32 * BlockSize; // Kích thước của phần FAT (ví dụ: 16KB)

        private static string pdfFilePath;
        private long PDFSize ;

        // Đọc nội dung của tệp PDF
        private static byte[] pdfBytes;
        // Tìm vị trí của lần xuất hiện của "/Info" trong nội dung của tệp PDF
        private int metadataPosition;
        // Find the position of the last occurrence of "%EOF" in the PDF content
        private static int eofPosition;
        private int NotDataSize = eofPosition + FATSize;

        private List<(string FileName, long StartByte, long FileSize, byte[] iv, byte[] salt, string hashedPassword)> FAT;

        public FileProcessing()
        {
            FAT = new List<(string, long, long, byte[], byte[], string)>();
            encryptDecrypt = new EncryptDecrypt(this);
        }

        public void SelectFilePDF(long size, string filePath)
        {
            pdfFilePath = filePath;
            PDFSize = size;
            pdfBytes = File.ReadAllBytes(pdfFilePath);
            metadataPosition = FindBytes(pdfBytes, Encoding.ASCII.GetBytes("/Info"), false, 1);
            eofPosition = FindBytes(pdfBytes, Encoding.ASCII.GetBytes("%EOF"), false, 2);
            FAT.Clear(); // Xóa dữ liệu cũ trong bảng FAT
        }

        public bool CheckFAT()
        {
            // Check if the metadata position and the next byte in the modified PDF differ from the expected value
            byte expectedMetadataValue = 32; // Assuming the initial value is 20 (space character)
            byte byteMetadataPos = pdfBytes[metadataPosition];
            if (byteMetadataPos != expectedMetadataValue)
            {
                // Byte at metadataPosition is different, indicating modification
                return true;
            }
            else
            {
                // Byte at metadataPosition is equal to expected value, check next byte
                if (pdfBytes[metadataPosition + 1] == expectedMetadataValue)
                {
                    return true;
                }
                else
                {
                    if (eofPosition < PDFSize)
                    {
                        return pdfBytes[eofPosition] == 0;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
        public void GenerateFAT()
        {
            if (metadataPosition == -1)
            {
                MessageBox.Show("Invalid PDF format (missing metadata).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (eofPosition == -1)
            {
                MessageBox.Show("Invalid PDF format (missing %EOF).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            // Mở tệp để ghi thêm nội dung
            using (FileStream fileStream = new FileStream(pdfFilePath, FileMode.Append, FileAccess.Write))
            {
                // Tạo một mảng byte chứa giá trị 0
                byte[] zeroBytes = new byte[FATSize - 1];

                // Ghi mảng byte vào cuối tệp
                fileStream.Write(zeroBytes, 0, zeroBytes.Length);
            }
        }
        private byte[] GenerateEntry(string fileName, long fileSize, byte[] iv, byte[] salt, string hashedPassword)
        {
            byte[] entryData = new byte[BlockSize]; // Độ dài của mỗi entry FAT
            Encoding.UTF8.GetBytes(fileName).CopyTo(entryData, 0); // Tên file
            BitConverter.GetBytes(PDFSize).CopyTo(entryData, 256); // Start byte
            BitConverter.GetBytes(fileSize).CopyTo(entryData, 256 + 16); // File size
            Array.Copy(iv, 0, entryData, 256 + 16 + 32, iv.Length);// Copy iv
            Array.Copy(salt, 0, entryData, 256 + 16 + 32 * 2, salt.Length);//salt
            Encoding.UTF8.GetBytes(hashedPassword).CopyTo(entryData, 256 + 16+32*2); // Password hash
            return entryData;
        }

        public void WriteFAT(string inputFile, byte[] iv, byte[] salt, string hashedPassword)
        {
            // Cập nhật thông tin FAT ở đầu tệp MYFS
            string fileName = Path.GetFileName(inputFile);
            FileInfo fileInfo = new FileInfo(inputFile);
            long fileSize = fileInfo.Length;

            // Cập nhật thông tin entry FAT mới
            using (FileStream fs = new FileStream(pdfFilePath, FileMode.Open, FileAccess.ReadWrite))
            {
                long entryOffset = GetStartPositionForNewEntry(fs); // Giả sử mỗi entry FAT có kích thước là 512 byte
                if (entryOffset >= NotDataSize)
                {
                    MessageBox.Show("Hệ thống entry đã đầy. Không thể Import thêm file.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                else
                {
                    if (entryOffset == metadataPosition)
                    {
                        byte[] entryData = GenerateEntry(fileName, fileSize, iv, salt, hashedPassword); // Độ dài của mỗi entry FAT
                        ModifyFirstByteFAT(entryData);

                        entryOffset = eofPosition;
                        fs.Seek(entryOffset, SeekOrigin.Begin);
                        fs.Write(entryData, 1, entryData.Length);
                        FAT.Add((fileName, PDFSize, fileSize, iv, salt, hashedPassword));
                    }
                    else
                    {
                        fs.Seek(entryOffset, SeekOrigin.Begin);
                        byte[] entryData = GenerateEntry(fileName, fileSize, iv, salt, hashedPassword);// Độ dài của mỗi entry FAT
                        fs.Write(entryData, 0, entryData.Length);
                        FAT.Add((fileName, PDFSize, fileSize, iv, salt, hashedPassword));
                    }
                }
            }
        }
        public void ReadFAT()
        {
            // Mở file hệ thống để đọc bảng FAT
            using (FileStream fs = new FileStream(pdfFilePath, FileMode.Open, FileAccess.Read))
            {
                fs.Seek(metadataPosition, SeekOrigin.Begin);

                // Đọc dữ liệu FAT từ file PDF
                byte[] fatData = new byte[FATSize];
                int bytesRead = fs.Read(fatData, 0, 1);
                fs.Seek(eofPosition, SeekOrigin.Begin);
                bytesRead += fs.Read(fatData, 1, fatData.Length-1);

                // Kiểm tra xem có đọc đủ dữ liệu không
                if (bytesRead == FATSize)
                {
                    int numberOfEntries = FATSize / BlockSize;

                    for (int i = 0; i < numberOfEntries; i++)
                    {
                        byte[] entryData = new byte[BlockSize];
                        Array.Copy(fatData, i * BlockSize, entryData, 0, BlockSize);

                        // Đọc thông tin từ mỗi entry
                        string fileName = Encoding.UTF8.GetString(entryData, 0, 256).TrimEnd('\0'); // Đọc tên file
                        long startByte = BitConverter.ToInt64(entryData, 256); // Đọc start byte
                        long fileSize = BitConverter.ToInt64(entryData, 256 + 16); // Đọc file size
                        byte[] iv = new byte[32];
                        byte[] salt = new byte[32];
                        Array.Copy(entryData, 256 + 16 + 32, iv, 0, 32);// Đọc iv
                        Array.Copy(entryData, 256 + 16 + 32 * 2, salt, 0, 32); // Đọc salt
                        string hashedPassword = Encoding.UTF8.GetString(entryData, 256 + 16 + 3*32,32).TrimEnd('\0'); // Đọc passwordHash
                        if (!string.IsNullOrEmpty(fileName) && fileName != " ")
                        {
                            // Thêm thông tin vào bảng FAT
                            FAT.Add((fileName, startByte, fileSize, iv, salt,hashedPassword));
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Lỗi khi đọc dữ liệu từ bảng FAT.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        public List<string> GetFilesFromFAT()
        {
            ReadFAT();
            List<string> fileNames = new List<string>();

            foreach (var entry in FAT)
            {
                // Thêm tên file vào danh sách
                fileNames.Add(entry.FileName);
            }

            return fileNames;
        }
        private static int FindBytes(byte[] haystack, byte[] needle, bool reverse = false, int positionNeedle = 1)
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

        private int ReadBlock(FileStream fs, long position, byte[] buffer)
        {
            fs.Seek(position, SeekOrigin.Begin);
            return fs.Read(buffer, 0, buffer.Length);
        }
        private bool IsBlockEmpty(byte[] buffer, int bytesRead)
        {
            for (int i = 0; i < bytesRead; i++)
            {
                if (buffer[i] != 0)
                {
                    return false;
                }
            }

            return true;
        }
        private long GetStartPositionForNewEntry(FileStream fileSystemStream)
        {
            if (!CheckFAT())
            {
                return metadataPosition;
            }
            else
            {
                long currentPosition = eofPosition + BlockSize - 1;
                // Lặp qua các block để tìm vị trí trống đầu tiên
                while (currentPosition < NotDataSize)
                {
                    byte[] buffer = new byte[BlockSize];
                    int bytesRead = ReadBlock(fileSystemStream, currentPosition, buffer);

                    // Nếu block hiện tại là block trống, trả về vị trí bắt đầu của block
                    if (IsBlockEmpty(buffer, bytesRead))
                    {
                        return currentPosition;
                    }

                    currentPosition += BlockSize;
                }

                // Nếu không tìm thấy không gian trống, trả về vị trí cuối cùng của FAT
                return NotDataSize;
            }
        }

        public void EmbedFileInPDF(string inputFile, string hashedPassword, byte[] salt)
        {
            if (!File.Exists(inputFile))
            {
                MessageBox.Show("Invalid file selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            byte[] fileBytes = encryptDecrypt.EncryptFile(inputFile, hashedPassword, salt);

            using (FileStream fileStream = new FileStream(pdfFilePath, FileMode.Append, FileAccess.Write))
            {
                // Ghi file vào cuối tệp
                fileStream.Write(fileBytes, 0, fileBytes.Length);
            }
        }
        private byte[] ModifyFirstByteFAT(byte[] Bytes)
        {
            // Extract relevant parts of the original PDF content
            byte[] preMetadataBytes = pdfBytes[..metadataPosition];
            byte[] postMetadataBytes = pdfBytes[metadataPosition..];

            // Combine the parts with the encrypted file bytes
            byte[] modifiedPdfContent = preMetadataBytes
                .Concat(Bytes[..1])  // Add the first byte
                .Concat(postMetadataBytes)
                .ToArray();

            return modifiedPdfContent;
        }
        public void ExportFile(string fileName, string exportFilePath, string hashedPassword, byte[] salt)
        {
            // Kiểm tra xem file cần xuất có tồn tại trong bảng FAT không
            var fileEntry = FAT.FirstOrDefault(entry => entry.FileName == fileName);
            if (fileEntry == default)
            {
                MessageBox.Show("Không có file được chọn hoặc file không tồn tại trong hệ thống tập tin.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            using (FileStream fileSystemStream = new FileStream(pdfFilePath, FileMode.Open, FileAccess.Read))
            using (FileStream exportStream = new FileStream(exportFilePath, FileMode.Create, FileAccess.Write))
            {
                // Xác định vị trí bắt đầu trong hệ thống tập tin cho file cần xuất
                long startPosition = fileEntry.StartByte;

                // Đặt vị trí bắt đầu trong hệ thống tập tin
                fileSystemStream.Seek(startPosition, SeekOrigin.Begin);

                // Đọc dữ liệu từ hệ thống tập tin và ghi vào file xuất
                byte[] buffer = new byte[fileEntry.FileSize];
                byte[] embeddedFileBytes = encryptDecrypt.DecryptFile(fileName, buffer, hashedPassword, salt);
                int bytesRead = fileSystemStream.Read(embeddedFileBytes, 0, embeddedFileBytes.Length);
                exportStream.Write(embeddedFileBytes, 0, bytesRead);
            }

            MessageBox.Show("File đã được Export thành công.");
        }
        public byte[] GetIV(string fileName)
        {
            // Kiểm tra xem file có tồn tại trong bảng FAT không
            var fileEntry = FAT.FirstOrDefault(entry => entry.FileName == fileName);
            if (fileEntry == default)
            {
                MessageBox.Show("Không có file được chọn hoặc file không tồn tại trong hệ thống tập tin.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            else
            {
                return fileEntry.iv;
            }
        }
        public byte[] GetSalt(string fileName)
        {
            // Kiểm tra xem file có tồn tại trong bảng FAT không
            var fileEntry = FAT.FirstOrDefault(entry => entry.FileName == fileName);
            if (fileEntry == default)
            {
                MessageBox.Show("Không có file được chọn hoặc file không tồn tại trong hệ thống tập tin.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            else
            {
                return fileEntry.salt;
            }
        }
        public string GetHashedPassword(string fileName)
        {
            // Kiểm tra xem file có tồn tại trong bảng FAT không
            var fileEntry = FAT.FirstOrDefault(entry => entry.FileName == fileName);
            if (fileEntry == default)
            {
                MessageBox.Show("Không có file được chọn hoặc file không tồn tại trong hệ thống tập tin.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            else
            {
                return fileEntry.hashedPassword;
            }
        }

        private long GetStartPositionOfFileName(FileStream fileSystemStream, string fileName)
        {
            long currentPosition = metadataPosition;

            //Loop through the file system to find the position of the file by name
            while (currentPosition < fileSystemStream.Length)
            {
                byte[] buffer = new byte[BlockSize];
                int bytesRead;
                if (currentPosition == metadataPosition)
                {
                    byte[] buffer1 = new byte[1];
                    bytesRead = ReadBlock(fileSystemStream, currentPosition, buffer1);
                    currentPosition = eofPosition;
                    byte[] buffer2 = new byte[BlockSize - 1];
                    bytesRead += ReadBlock(fileSystemStream, currentPosition, buffer2);
                }
                else
                {
                    bytesRead = ReadBlock(fileSystemStream, currentPosition, buffer);
                }

                // Check if the current block contains the file name
                // Tên file
                string blockContent = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                if (blockContent.Contains(fileName))
                {

                    // Return the position if the file name is found in the block
                    return currentPosition;
                }

                currentPosition += BlockSize;
            }

            // If the file name is not found, return the end of the file
            return fileSystemStream.Length;
        }
        private long GetStartPositionForNewData(FileStream fileSystemStream)
        {
            long currentPosition = NotDataSize; // Bắt đầu từ sau bảng FAT
            // Lặp qua các block để tìm vị trí trống đầu tiên
            while (currentPosition < fileSystemStream.Length)
            {
                byte[] buffer = new byte[BlockSize];
                int bytesRead = ReadBlock(fileSystemStream, currentPosition, buffer);

                // Nếu block hiện tại là block trống, trả về vị trí bắt đầu của block
                if (IsBlockEmpty(buffer, bytesRead))
                {
                    return currentPosition;
                }

                currentPosition += BlockSize;
            }

            // Nếu không tìm thấy không gian trống, trả về vị trí cuối cùng của tệp
            return fileSystemStream.Length;
        }
        private void ClearDataAtPosition(FileStream fileStream, long position, int size)
        {
            byte[] emptyBuffer = new byte[size];
            fileStream.Seek(position, SeekOrigin.Begin);
            fileStream.Write(emptyBuffer, 0, emptyBuffer.Length);
        }

        private byte[] ReadDataFromFile(FileStream fileStream, long position, int size)
        {
            byte[] buffer = new byte[size];
            fileStream.Seek(position, SeekOrigin.Begin);
            fileStream.Read(buffer, 0, buffer.Length);
            return buffer;
        }

        private void WriteDataToFile(FileStream fileStream, long position, byte[] data, int offset = 0)
        {
            fileStream.Seek(position, SeekOrigin.Begin);
            fileStream.Write(data, offset, data.Length - offset);
        }
        public void DeleteFile(string fileName)
        {
            // Kiểm tra xem file cần xóa có tồn tại trong bảng FAT không

            var fileEntry = FAT.FirstOrDefault(entry => entry.FileName == fileName);

            if (fileEntry == default)
            {
                MessageBox.Show("Không có file được chọn hoặc file không tồn tại trong hệ thống tập tin.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                using (FileStream fileSystemStream = new FileStream(pdfFilePath, FileMode.Open, FileAccess.ReadWrite))
                {

                    // Xác định vị trí bắt đầu trong hệ thống tập tin cho file cần xóa
                    long startPosition = fileEntry.StartByte;
                    long startPositionOfFileName = GetStartPositionOfFileName(fileSystemStream, fileName);

                    // Ghi dữ liệu rỗng vào vị trí của file để xóa nó
                    ClearDataAtPosition(fileSystemStream, startPosition, (int)fileEntry.FileSize);
                    ClearDataAtPosition(fileSystemStream, startPositionOfFileName, BlockSize);

                    // Di chuyển dữ liệu từ file tiếp theo lên vị trí của file bị xóa
                    for (int i = FAT.IndexOf(fileEntry) + 1; i < FAT.Count; i++)
                    {
                        var nextFile = FAT[i];
                        long nextFileStartPosition = nextFile.StartByte;
                        long nextFileEntry = startPositionOfFileName + BlockSize;

                        // Đọc dữ liệu từ file tiếp theo và ghi vào vị trí của file trước đó
                        byte[] buffer = ReadDataFromFile(fileSystemStream, nextFileStartPosition, (int)nextFile.FileSize);

                        // Đọc dữ liệu từ entry tiếp theo và ghi vào vị trí của entry trước đó
                        byte[] bufferEntry = ReadDataFromFile(fileSystemStream, nextFileEntry, BlockSize);

                        WriteDataToFile(fileSystemStream, startPosition, buffer);
                        //
                        nextFile.StartByte = startPosition;
                        WriteDataToFile(fileSystemStream, startPositionOfFileName, BitConverter.GetBytes(nextFile.StartByte), 256);

                        // Cập nhật vị trí của file trong bảng FAT
                        startPosition = GetStartPositionForNewData(fileSystemStream);
                        startPositionOfFileName = nextFileEntry;

                    }

                    // Ghi dữ liệu rỗng vào vị trí cuối cùng để xóa file cuối cùng
                    ClearDataAtPosition(fileSystemStream, startPosition, (int)FAT.LastOrDefault().FileSize);

                    // Ghi dữ liệu rỗng vào vị trí cuối cùng để xóa entry cuối cùng
                    ClearDataAtPosition(fileSystemStream, startPositionOfFileName, BlockSize);

                }

                // Update FAT to mark the file as deleted
                FAT.Remove(fileEntry);

                MessageBox.Show("File đã được xóa thành công.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xóa file: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
