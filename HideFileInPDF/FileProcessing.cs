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
using System.Drawing.Design;
using System.IO.Pipes;

namespace libraryFileProcessing
{
    public class FileProcessing
    {
        private EncryptDecrypt encryptDecrypt = new EncryptDecrypt();

        public const int BlockSize = 512;//byte
        public const int FATSize = 3 * BlockSize; // Kích thước của phần FAT (ví dụ: 16KB)

        private static string pdfFilePath;
        public long PDFSize ;

        // Đọc nội dung của tệp PDF
        private static byte[] pdfBytes;
        // Tìm vị trí của lần xuất hiện của "/Info" trong nội dung của tệp PDF
        private int metadataPosition;
        // Find the position of the last occurrence of "%EOF" in the PDF content
        private static int eofPosition;
        private int NotDataSize;

        private List<(string FileName, long StartByte, long FileSize, byte[] salt, string hashedPassword)> FAT;

        public FileProcessing()
        {
            FAT = new List<(string, long, long, byte[], string)>();
        }

        public void SelectFilePDF(long size, string filePath)
        {
            pdfFilePath = filePath;
            PDFSize = size;
            pdfBytes = File.ReadAllBytes(pdfFilePath);
            metadataPosition = FindBytes(pdfBytes, Encoding.ASCII.GetBytes("/Info"), false, 1);
            eofPosition = FindBytes(pdfBytes, Encoding.ASCII.GetBytes("%EOF"), false, 2);
            NotDataSize = eofPosition + FATSize;
            if (CheckFAT())
            {
                NotDataSize -= 1;
            }
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
        public long UpdatePDFSize()
        {
            FileInfo PDFInfo = new FileInfo(pdfFilePath);
            return PDFInfo.Length;
        }
        public bool CheckFAT()
        {
            if (eofPosition < PDFSize)
            {
                return true;
            }
            else
            {
                return false;
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
            // Mở tệp để ghi nội dung
            using (FileStream fileStream = new FileStream(pdfFilePath, FileMode.Open, FileAccess.ReadWrite))
            {
                // Di chuyển đến vị trí metadataPosition
                fileStream.Seek(metadataPosition, SeekOrigin.Begin);

                // Đọc dữ liệu từ vị trí hiện tại đến cuối tệp
                byte[] existingData = new byte[fileStream.Length - metadataPosition];
                fileStream.Read(existingData, 0, existingData.Length);

                // Đưa con trỏ về vị trí metadataPosition để ghi dữ liệu mới
                fileStream.Seek(metadataPosition, SeekOrigin.Begin);

                // Tạo một mảng byte chứa giá trị 0 
                byte[] newData = new byte[] { 0 };

                // Ghi mảng byte vào tệp tin
                fileStream.Write(newData, 0, newData.Length);

                // Ghi lại dữ liệu cũ
                fileStream.Write(existingData, 0, existingData.Length);
            }
            // Mở tệp để ghi thêm nội dung
            using (FileStream fileStream = new FileStream(pdfFilePath, FileMode.Append, FileAccess.Write))
            {
                // Tạo một mảng byte chứa giá trị 0
                byte[] zeroBytes = new byte[FATSize - 1];

                // Ghi mảng byte vào cuối tệp
                fileStream.Write(zeroBytes, 0, zeroBytes.Length);
            }
            PDFSize = UpdatePDFSize();
            eofPosition += 1;

        }
        private byte[] GenerateEntry(string fileName,long StartByte, long fileSize, byte[] salt, string hashedPassword)
        {
            byte[] entryData = new byte[BlockSize]; // Độ dài của mỗi entry FAT
            Encoding.UTF8.GetBytes(fileName).CopyTo(entryData, 0); // Tên file
            BitConverter.GetBytes(StartByte).CopyTo(entryData, 256); // Start byte
            BitConverter.GetBytes(fileSize).CopyTo(entryData, 256 + 16); // File size
            Array.Copy(salt, 0, entryData, 256 + 16*2, salt.Length);//salt
            Encoding.UTF8.GetBytes(hashedPassword).CopyTo(entryData, 256 + 16*3); // Password hash
            return entryData;
        }

        public void WriteFATAndData(string inputFile,long StartByte,byte[] fileBytes, byte[] salt, string hashedPassword)
        {
            // Cập nhật thông tin FAT ở đầu tệp MYFS
            string fileName = Path.GetFileName(inputFile);
            long fileSize = fileBytes.Length;
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
                        byte[] entryData = GenerateEntry(fileName, StartByte, fileSize, salt, hashedPassword); // Độ dài của mỗi entry FAT
                        WriteDataToFile(fs, entryOffset, entryData);
                    
                        FAT.Add((fileName, StartByte, fileSize, salt, hashedPassword));
                    }
                    else
                    {
                        byte[] entryData = GenerateEntry(fileName, StartByte,fileSize, salt, hashedPassword);// Độ dài của mỗi entry FAT
                        WriteDataToFile(fs, entryOffset, entryData);
                        FAT.Add((fileName, StartByte, fileSize, salt, hashedPassword));
                    }
                    MessageBox.Show("Đã Import thêm file.", "Thành công", MessageBoxButtons.OK);
                }
            }
            EmbedFileInPDF(inputFile, hashedPassword, salt, fileBytes);
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
                        byte[] salt = new byte[16];
                        Array.Copy(entryData, 256 + 16*2, salt, 0, 16); // Đọc salt
                        string hashedPassword = Encoding.UTF8.GetString(entryData, 256 + 16*3,64).TrimEnd('\0'); // Đọc passwordHash
                        if (!string.IsNullOrEmpty(fileName) && fileName != " ")
                        {
                            // Thêm thông tin vào bảng FAT
                            FAT.Add((fileName, startByte, fileSize, salt,hashedPassword));
                        }
                    }
                }

            }
        }
        public List<string> GetFilesFromFAT()
        {
            FAT.Clear();
            ReadFAT();
            List<string> fileNames = new List<string>();

            foreach (var entry in FAT)
            {
                // Thêm tên file vào danh sách
                fileNames.Add(entry.FileName);
            }

            return fileNames;
        }

        private int ReadBlock(FileStream fs, long position, byte[] buffer)
        {
            if (position == metadataPosition)
            {
                fs.Seek(position, SeekOrigin.Begin);
                int bytesRead = fs.Read(buffer, 0, 1);
                fs.Seek(eofPosition, SeekOrigin.Begin);
                bytesRead += fs.Read(buffer, 1, buffer.Length - 1);
                return bytesRead;
            }
            else {
                fs.Seek(position, SeekOrigin.Begin);
                return fs.Read(buffer, 0, buffer.Length);
            }
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
            long currentPosition = metadataPosition;
            byte[] buffer = new byte[BlockSize];
            int bytesRead = ReadBlock(fileSystemStream, currentPosition, buffer);
            // Nếu block hiện tại là block trống, trả về vị trí bắt đầu của block
            if (IsBlockEmpty(buffer, bytesRead))
            {
                return metadataPosition;
            }

            currentPosition = eofPosition + BlockSize -1;
            // Lặp qua các block để tìm vị trí trống đầu tiên
            while (currentPosition < NotDataSize)
            {
                buffer = new byte[BlockSize];
                bytesRead = ReadBlock(fileSystemStream, currentPosition, buffer);

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

        public void EmbedFileInPDF(string inputFile, string hashedPassword, byte[] salt, byte[] fileBytes)
        {
            if (!File.Exists(inputFile))
            {
                MessageBox.Show("Invalid file selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (FileStream fileStream = new FileStream(pdfFilePath, FileMode.Append, FileAccess.Write))
            {
                // Ghi file vào cuối tệp
                fileStream.Write(fileBytes, 0, fileBytes.Length);
            }
        }
        public void ExportFile(string fileName, string exportFilePath, string hashedPassword, byte[] salt)
        {
            // Kiểm tra xem file cần xuất có tồn tại trong bảng FAT không
            var fileEntry = FAT.FirstOrDefault(entry => entry.FileName == fileName);
            if (fileEntry == default)
            {
                MessageBox.Show("Không có file được chọn hoặc file không tồn tại", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (FileStream fileSystemStream = new FileStream(pdfFilePath, FileMode.Open, FileAccess.Read))
            using (FileStream exportStream = new FileStream(exportFilePath, FileMode.Create, FileAccess.Write))
            {
                byte[] buffer = ReadDataFromFile(fileSystemStream, fileEntry.StartByte, fileEntry.FileSize);
                byte[] embeddedFileBytes = encryptDecrypt.DecryptFile(buffer, hashedPassword, salt);
                exportStream.Write(embeddedFileBytes, 0, embeddedFileBytes.Length);
            }

            MessageBox.Show("File đã được Export thành công.");
        }

        public byte[] GetSalt(string fileName)
        {
            // Kiểm tra xem file có tồn tại trong bảng FAT không
            var fileEntry = FAT.FirstOrDefault(entry => entry.FileName == fileName);
            if (fileEntry == default)
            {
                MessageBox.Show("Không có file được chọn hoặc file không tồn tại", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show("Không có file được chọn hoặc file không tồn tại", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            byte[] buffer = new byte[BlockSize];
            int bytesRead = ReadBlock(fileSystemStream, currentPosition, buffer);

            // Check if the current block contains the file name
            // Tên file
            string blockContent = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            if (blockContent.Contains(fileName))
            {

                // Return the position if the file name is found in the block
                return currentPosition;
            }
            currentPosition = eofPosition + BlockSize - 1;
            //Loop through the file system to find the position of the file by name
            while (currentPosition < fileSystemStream.Length)
            {
                buffer = new byte[BlockSize];
                bytesRead = ReadBlock(fileSystemStream, currentPosition, buffer);

                // Check if the current block contains the file name
                // Tên file
                blockContent = Encoding.UTF8.GetString(buffer, 0, bytesRead);

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
        private void ClearDataAtPosition(FileStream fileStream, long position, int size)
        {
            byte[] emptyBuffer = new byte[size];
            if (position == metadataPosition)
            {
                fileStream.Seek(position, SeekOrigin.Begin);
                fileStream.Write(emptyBuffer, 0, 1);
                fileStream.Seek(eofPosition, SeekOrigin.Begin);
                fileStream.Write(emptyBuffer, 1, emptyBuffer.Length -1);
            }
            else
            {
                fileStream.Seek(position, SeekOrigin.Begin);
                fileStream.Write(emptyBuffer, 0, emptyBuffer.Length);
            }
        }
        public byte[] ReadDataFromFile(FileStream fileStream, long position, long size)
        {
            byte[] buffer = new byte[size];
            if (position == metadataPosition)
            {
                fileStream.Seek(position, SeekOrigin.Begin);
                fileStream.Read(buffer, 0, 1);
                fileStream.Seek(eofPosition, SeekOrigin.Begin);
                fileStream.Read(buffer, 1, buffer.Length - 1);
                return buffer;
            }
            else
            {
                fileStream.Seek(position, SeekOrigin.Begin);
                fileStream.Read(buffer, 0, buffer.Length);
                return buffer;
            }

        }
        private void WriteDataToFile(FileStream fileStream, long position, byte[] data, int offset = 0)
        {
            if (position == metadataPosition)
            {
                fileStream.Seek(position, SeekOrigin.Begin);
                fileStream.Write(data, 0, 1);
                fileStream.Seek(eofPosition, SeekOrigin.Begin);
                fileStream.Write(data, 1, data.Length - 1);
            }
            else
            {
                fileStream.Seek(position, SeekOrigin.Begin);
                fileStream.Write(data, offset, data.Length - offset);
            }

        }
        public void DeleteFile(string fileName)
        {
            // Kiểm tra xem file cần xóa có tồn tại trong bảng FAT không

            var fileEntry = FAT.FirstOrDefault(entry => entry.FileName == fileName);

            if (fileEntry == default)
            {
                MessageBox.Show("Không có file được chọn hoặc file không tồn tại", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                using (FileStream fileSystemStream = new FileStream(pdfFilePath, FileMode.Open, FileAccess.ReadWrite))
                {

                    // Xác định vị trí bắt đầu trong hệ thống tập tin cho file cần xóa
                    long startPosition = fileEntry.StartByte;
                    long startPositionOfFileName = GetStartPositionOfFileName(fileSystemStream, fileName);
                    for (int i = FAT.IndexOf(fileEntry) + 1; i < FAT.Count; i++)
                    {

                        var nextFile = FAT[i];
                        long nextFileStartPosition = nextFile.StartByte;
                        long nextFileEntry;
                        if (startPositionOfFileName == metadataPosition)
                        {
                            nextFileEntry = eofPosition + BlockSize - 1;
                        }
                        else
                        {
                            nextFileEntry = startPositionOfFileName + BlockSize;
                        }

                        // Đọc dữ liệu từ file tiếp theo và ghi vào vị trí của file trước đó
                        byte[] buffer = ReadDataFromFile(fileSystemStream, nextFileStartPosition, nextFile.FileSize);

                        // Đọc dữ liệu từ entry tiếp theo và ghi vào vị trí của entry trước đó
                        byte[] bufferEntry = ReadDataFromFile(fileSystemStream, nextFileEntry, BlockSize);
                        //Viết lại dữ liệu
                        WriteDataToFile(fileSystemStream, startPosition, buffer);
                        //viết lại bảng fat
                        
                        nextFile.StartByte = startPosition;
                        BitConverter.GetBytes(nextFile.StartByte).CopyTo(bufferEntry, 256); // Start byte
                        WriteDataToFile(fileSystemStream, startPositionOfFileName, bufferEntry);

                        // Cập nhật vị trí của file trong bảng FAT
                        startPosition += nextFile.FileSize;
                        startPositionOfFileName = nextFileEntry;
                    }

                    // Ghi dữ liệu rỗng vào vị trí cuối cùng để xóa entry cuối cùng
                    ClearDataAtPosition(fileSystemStream, startPositionOfFileName, BlockSize);
                    fileSystemStream.SetLength(startPosition);

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

        public void UpdateDataAfterChangePassword(string fileName, string oldHashedPassword, string newHashedPassword, byte[] salt)
        {
            // Kiểm tra xem file cần xóa có tồn tại trong bảng FAT không

            var fileEntry = FAT.FirstOrDefault(entry => entry.FileName == fileName);

            if (fileEntry == default)
            {
                MessageBox.Show("Không có file được chọn hoặc file không tồn tại ", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                using (FileStream fileSystemStream = new FileStream(pdfFilePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    byte[] encryptedFileContent = ReadDataFromFile(fileSystemStream, fileEntry.StartByte,fileEntry.FileSize);

                    // Encrypt the file content using the new key
                    byte[] reencryptedFileContent = encryptDecrypt.ReencryptedFile(oldHashedPassword, newHashedPassword, encryptedFileContent, salt);

                    long startPosition = fileEntry.StartByte;
                    long startPositionOfFileName = GetStartPositionOfFileName(fileSystemStream, fileName);
                    WriteDataToFile(fileSystemStream, startPosition, reencryptedFileContent);
                    //viết lại hashed pass trong bảng fat
                    fileEntry.hashedPassword=newHashedPassword;
                    byte[] bufferEntry = ReadDataFromFile(fileSystemStream, startPositionOfFileName, BlockSize);
                    Encoding.UTF8.GetBytes(fileEntry.hashedPassword).CopyTo(bufferEntry, 256 + 16 * 3);
                    WriteDataToFile(fileSystemStream, startPositionOfFileName, bufferEntry);
                }

                MessageBox.Show("File đã đổi mật khẩu thành công.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi đổi mật khẩu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}
