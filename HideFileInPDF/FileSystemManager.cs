using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography;

using System.Drawing;
using System.Drawing.Drawing2D;


namespace WinFormsApp1
{
    public class FileSystemManager
    {
        public const int BlockSize = 512;//byte
        public const int FATSize = 32 * BlockSize; // Kích thước của phần FAT (ví dụ: 16KB)
        int InfoSize =  FATSize ;
        public long FileSystemSize;
        public string FileSystemFilePath;

        private List<(string FileName, long StartBlock, long Size, long FileSize,string hashPassword,string salt)> FAT;
        private List<(string FileName, long StartBlock, long Size, long FileSize, string hashPassword, string salt)> BACKUP;
        private static readonly Random random = new Random();



        private byte[] hashedPassword = new byte [BlockSize];
        private const int HashedPasswordBlock = 0; // Block đầu tiên

       

        public int getFATSize()
        {
            return FATSize;
        }
        public FileSystemManager()
        {

            FAT = new List<(string, long, long, long,string,string)>();

            BACKUP = new List<(string, long, long, long,string,string)>();

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
        public List<string> GetFilesFromFAT()
        {
            List<string> fileNames = new List<string>();

            foreach (var entry in FAT)
            {
                // Thêm tên file vào danh sách
                fileNames.Add(entry.FileName);
            }

            return fileNames;
        }
        public List<string> GetFilesFromBACKUP()
        {
            List<string> fileNames = new List<string>();
            MessageBox.Show("IN BACKUP");
            if (BACKUP != null)
            {
                MessageBox.Show("BACKUP ! NULL");
                foreach (var entry in BACKUP)
                {
                    // Thêm tên file vào danh sách
                    MessageBox.Show("IN LOOP FILE BACKUP");

                    fileNames.Add(entry.FileName);
                }
                

            }
            else
            {
                MessageBox.Show("BACKUP NULL");
            }
            



            return fileNames;
        }

        private long GetStartPositionForNewData(FileStream fileSystemStream)
        {
            long currentPosition = InfoSize; // Bắt đầu từ sau bảng FAT
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
        private long GetStartPositionForBackUp(FileStream fileSystemStream)
        {
            long currentPosition = FATSize+BlockSize;
            // Lặp qua các block để tìm vị trí trống đầu tiên
            while (currentPosition < InfoSize)
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


            return fileSystemStream.Length;
        }
        private long GetStartPositionForNewEntry(FileStream fileSystemStream)
        {
            long currentPosition = BlockSize;
            // Lặp qua các block để tìm vị trí trống đầu tiên
            while (currentPosition < BlockSize+FATSize)
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
            return InfoSize;
        }

        public long GetTotalUsedSpace()
        {
            long totalUsedSpace = InfoSize;

            using (FileStream fileSystemStream = new FileStream(FileSystemFilePath, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[BlockSize];
                // Lặp qua từng block của hệ thống tập tin
                for (long currentPosition = totalUsedSpace; currentPosition < fileSystemStream.Length; currentPosition += BlockSize)
                {
                    int bytesRead = ReadBlock(fileSystemStream, currentPosition, buffer);

                    // Nếu block không trống, cộng vào tổng không gian đã sử dụng
                    if (!IsBlockEmpty(buffer, bytesRead))
                    {
                        totalUsedSpace += bytesRead;
                    }
                }
            }

            return totalUsedSpace;
        }

        private void WriteFATAndData(string filePath,string hashPassword,string salt)
        {
            // Cập nhật thông tin FAT ở đầu tệp MYFS
            string fileName = Path.GetFileName(filePath);
            long fileSizeInBlocks;
            long fileSize;
            long startBlock;
            long startPosition;
            using (FileStream fs = new FileStream(FileSystemFilePath, FileMode.Open, FileAccess.ReadWrite))
            {

                // Xác định vị trí bắt đầu trong hệ thống tập tin cho dữ liệu mới
                startPosition = GetStartPositionForNewData(fs);
                startBlock = startPosition / BlockSize;
                long remainingSpace = Math.Max(0, fs.Length - startPosition);

                using (FileStream sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    // Kiểm tra xem không gian trống còn đủ lớn cho dữ liệu mới không
                    if (remainingSpace >= sourceStream.Length)
                    {
                        fileSizeInBlocks = (long)Math.Ceiling(sourceStream.Length / (double)BlockSize);
                        fileSize = sourceStream.Length;
                        // Cập nhật thông tin entry FAT mới
                        long entryOffset = GetStartPositionForNewEntry(fs); // Giả sử mỗi entry FAT có kích thước là 512 byte
                        if (entryOffset >= BlockSize+FATSize)
                        {
                            MessageBox.Show("Hệ thống entry đã đầy. Không thể Import thêm file.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        else
                        {
                            fs.Seek(entryOffset, SeekOrigin.Begin);

                            byte[] entryData = new byte[BlockSize]; // Độ dài của mỗi entry FAT
                            Encoding.UTF8.GetBytes(fileName).CopyTo(entryData, 0); // Tên file
                            BitConverter.GetBytes(startBlock).CopyTo(entryData, 256); // Start block
                            BitConverter.GetBytes((long)Math.Ceiling(fileSize / (double)BlockSize)).CopyTo(entryData, 256 + 8); // Size in blocks
                            BitConverter.GetBytes(fileSize).CopyTo(entryData, 256 + 8 * 2); // File size
                            Encoding.UTF8.GetBytes(hashPassword).CopyTo(entryData, 256 + 8 * 3); // Password hash
                            Encoding.UTF8.GetBytes(salt).CopyTo(entryData, 256 + 8 * 3 + 32); // Salt
                            fs.Write(entryData, 0, entryData.Length);
                            FAT.Add((fileName, startBlock, fileSizeInBlocks, fileSize, hashPassword, salt));

                        }
                    }
                    else
                    {
                        MessageBox.Show("Không đủ không gian trống cho dữ liệu mới.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }
            // Thực hiện quá trình Import vào hệ thống tập tin
            WriteDataInFileSystem(filePath, startPosition);
        }
        private void WriteFATInBackup(FileStream fileSystemStream, string fileName)
        {
            var fileEntry = FAT.FirstOrDefault(entry => entry.FileName == fileName);

            // Cập nhật thông tin FAT ở đầu tệp MYFS
            long fileSizeInBlocks;
            long fileSize;
            long startBlock;
            long startPosition;
            // Xác định vị trí bắt đầu trong hệ thống tập tin cho dữ liệu mới

            startBlock = fileEntry.StartBlock;
            fileSizeInBlocks = (long)Math.Ceiling(fileEntry.FileSize / (double)BlockSize);

            fileSize = fileEntry.FileSize;
            // Cập nhật thông tin entry FAT mới
            long entryOffset = GetStartPositionForBackUp(fileSystemStream); // Giả sử mỗi entry FAT có kích thước là 512 byte
            if (entryOffset >= InfoSize)
            {
                MessageBox.Show("Hệ thống entry đã đầy. Không thể Import thêm file.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else
            {


                // Đặt vị trí bắt đầu trong vung backup
                fileSystemStream.Seek(entryOffset, SeekOrigin.Begin);

                byte[] entryData = new byte[BlockSize]; // Độ dài của mỗi entry FAT
                Encoding.UTF8.GetBytes(fileName).CopyTo(entryData, 0); // Tên file
                BitConverter.GetBytes(startBlock).CopyTo(entryData, 256); // Start block
                BitConverter.GetBytes((long)Math.Ceiling(fileSize / (double)BlockSize)).CopyTo(entryData, 256 + 8); // Size in blocks
                BitConverter.GetBytes(fileSize).CopyTo(entryData, 256 + 8 * 2); // File size
                fileSystemStream.Write(entryData, 0, entryData.Length);

            }

        }
        private void WriteDataInFileSystem(string filePath, long startPosition)
        {
            using (FileStream fileSystemStream = new FileStream(FileSystemFilePath, FileMode.Open, FileAccess.ReadWrite))
            {
                // Đặt vị trí bắt đầu trong hệ thống tập tin
                fileSystemStream.Seek(startPosition, SeekOrigin.Begin);
                using (FileStream sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    // Đọc toàn bộ nội dung của file nguồn và ghi vào hệ thống tập tin
                    byte[] buffer = new byte[BlockSize];
                    int bytesRead;

                    while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        int bytesToWrite = Math.Min(bytesRead, BlockSize);
                        fileSystemStream.Write(buffer, 0, bytesToWrite);
                    }
                    MessageBox.Show("File đã được Import thành công.");
                }
            }

        }

        public void ReadHashedPassword()
        {
            using (FileStream fs = new FileStream(FileSystemFilePath, FileMode.Open, FileAccess.Read))
            {
                fs.Seek(0, SeekOrigin.Begin); // Di chuyển đến block đầu tiên 
                // Đọc dữ liệu 
                byte[] HashedData = new byte[BlockSize];
                hashedPassword = new byte[BlockSize];
                int bytesRead = fs.Read(HashedData, 0, HashedData.Length);
                // Kiểm tra xem có đọc đủ dữ liệu không
                if (bytesRead == BlockSize)
                {
                    if (HashedData != null)
                    {
                        Array.Copy(HashedData, 0, hashedPassword, 0, BlockSize);
                    }
                }
            }
        }
        public void ReadFAT()
        {
            // Mở file hệ thống để đọc bảng FAT
            using (FileStream fs = new FileStream(FileSystemFilePath, FileMode.Open, FileAccess.Read))
            {
                fs.Seek(BlockSize, SeekOrigin.Begin); // Di chuyển đến block thứ hai (sau block chứa hashed password)

                // Đọc dữ liệu FAT từ tệp MYFS
                byte[] fatData = new byte[FATSize];
                int bytesRead = fs.Read(fatData, 0, fatData.Length);

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

                        long startBlock = BitConverter.ToInt64(entryData, 256); // Đọc start block
                        long sizeInBlocks = BitConverter.ToInt64(entryData, 256 + 8); // Đọc size in blocks
                        long fileSize = BitConverter.ToInt64(entryData, 256 + 8 * 2); // Đọc file size

                        string hassPassword = Encoding.UTF8.GetString(entryData, 256 + 8 * 3, 32).TrimEnd('\0'); // Đọc passwordHash
                        string salt = Encoding.UTF8.GetString(entryData, 256 + 8 * 3 + 32, 32).TrimEnd('\0'); // Đọc salt




                        if (!string.IsNullOrEmpty(fileName))
                        {
                            // Thêm thông tin vào bảng FAT
                            FAT.Add((fileName, startBlock, sizeInBlocks, fileSize, hassPassword, salt));
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Lỗi khi đọc dữ liệu từ bảng FAT.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        public void ReadBACKUP()
        {
            // Mở file hệ thống để đọc bảng FAT
            using (FileStream fs = new FileStream(FileSystemFilePath, FileMode.Open, FileAccess.Read))
            {
                fs.Seek(BlockSize+FATSize, SeekOrigin.Begin); // Di chuyển đến block thứ hai (sau block chứa hashed password)

                // Đọc dữ liệu BACKUP từ tệp MYFS
                byte[] fatData = new byte[FATSize];
                int bytesRead = fs.Read(fatData, 0, fatData.Length);

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

                        long startBlock = BitConverter.ToInt64(entryData, 256); // Đọc start block
                        long sizeInBlocks = BitConverter.ToInt64(entryData, 256 + 8); // Đọc size in blocks
                        long fileSize = BitConverter.ToInt64(entryData, 256 + 8 * 2); // Đọc file size
                        string hassPassword = Encoding.UTF8.GetString(entryData, 256 + 8 * 3, 32).TrimEnd('\0'); // Đọc passwordHash
                        string salt = Encoding.UTF8.GetString(entryData, 256 + 8 * 3 + 32, 32).TrimEnd('\0'); // Đọc salt

                        if (!string.IsNullOrEmpty(fileName))
                        {
                            // Thêm thông tin vào BACKUP
                            BACKUP.Add((fileName, startBlock, sizeInBlocks, fileSize,"",""));
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Lỗi khi đọc dữ liệu từ bảng FAT.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public void InitializeFileSystem(long size, string filePath)
        {
            FileSystemSize = size;
            FileSystemFilePath = filePath;
            FAT.Clear(); // Xóa dữ liệu cũ trong bảng FAT
            hashedPassword = null;
            // Tạo file hệ thống với kích thước và đường dẫn cụ thể
            using (FileStream fs = new FileStream(FileSystemFilePath, FileMode.Create, FileAccess.Write))
            {
                // Đặt kích thước của tệp MYFS
                fs.SetLength(FileSystemSize);
            }
        }


        public void SelectFileSystem(long size, string filePath)
        {
            if (string.IsNullOrEmpty(FileSystemFilePath))
            {
                // Kiểm tra xem tệp MyFS đã tồn tại hay chưa
                if (File.Exists(filePath))
                {
                    FileSystemFilePath = filePath;
                    FileSystemSize = size;
                    FAT.Clear(); // Xóa dữ liệu cũ trong bảng FAT
                    hashedPassword = null;
                    ReadFAT();
                    ReadHashedPassword();
                }
                else
                {
                    MessageBox.Show("Tệp MyFS.DRS không tồn tại.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        public void ImportFile(string filePath,string hashPassword,string salt)
        {
            // Kiểm tra xem file hệ thống đã được khởi tạo chưa
            if (string.IsNullOrEmpty(FileSystemFilePath) || !File.Exists(FileSystemFilePath))
            {
                MessageBox.Show("Hệ thống tập tin chưa được khởi tạo.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Ghi thông tin vào bảng FAT và Data của MyFS
            WriteFATAndData(filePath,hashPassword,salt);
        }

        public void ExportFile(string fileName, string exportFilePath)
        {
            // Kiểm tra xem file hệ thống đã được khởi tạo chưa
            if (string.IsNullOrEmpty(FileSystemFilePath) || !File.Exists(FileSystemFilePath))
            {
                MessageBox.Show("Hệ thống tập tin chưa được khởi tạo.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Kiểm tra xem file cần xuất có tồn tại trong bảng FAT không
            var fileEntry = FAT.FirstOrDefault(entry => entry.FileName == fileName);
            if (fileEntry == default)
            {
                MessageBox.Show("Không có file được chọn hoặc file không tồn tại trong hệ thống tập tin.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            

            using (FileStream fileSystemStream = new FileStream(FileSystemFilePath, FileMode.Open, FileAccess.Read))
            using (FileStream exportStream = new FileStream(exportFilePath, FileMode.Create, FileAccess.Write))
            {
                // Xác định vị trí bắt đầu trong hệ thống tập tin cho file cần xuất
                long startPosition = fileEntry.StartBlock * BlockSize;

                // Đặt vị trí bắt đầu trong hệ thống tập tin
                fileSystemStream.Seek(startPosition, SeekOrigin.Begin);

                // Đọc dữ liệu từ hệ thống tập tin và ghi vào file xuất
                byte[] buffer = new byte[BlockSize];
                long remainingBytes = fileEntry.FileSize;
                int bytesRead;

                while (remainingBytes > 0 && (bytesRead = fileSystemStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    // Giới hạn số byte cần ghi để không vượt quá kích thước của file
                    int bytesToWrite = (int)Math.Min(remainingBytes, bytesRead);
                    exportStream.Write(buffer, 0, bytesToWrite);
                    remainingBytes -= bytesToWrite;
                }
            }

            MessageBox.Show("File đã được Export thành công.");
        }
        public void SetPassword(string newPassword)
        {
            using (FileStream fs = new FileStream(FileSystemFilePath, FileMode.Open, FileAccess.ReadWrite))
            {

                if (!string.IsNullOrEmpty(newPassword))
                {
                    // Yêu cầu mật khẩu khi khởi tạo hệ thống tập tin
                    using (SHA256 sha256 = SHA256.Create())
                    {
                        hashedPassword = sha256.ComputeHash(Encoding.UTF8.GetBytes(newPassword));
                    }
                    fs.Seek(HashedPasswordBlock, SeekOrigin.Begin);
                    // Lưu hashed password vào block đầu tiên
                    fs.Write(hashedPassword, 0, hashedPassword.Length);
                }
            }
        }
        public void ChangePassword(string oldPassword, string newPassword)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                if (CheckPassword(oldPassword))
                {
                    byte[] newPasswordHash = new byte[BlockSize];
                    // Hash mật khẩu mới
                    byte[] enteredPasswordHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(newPassword));
                    Array.Copy(enteredPasswordHash, newPasswordHash, enteredPasswordHash.Length);
                    // Lưu hashed password mới vào block đầu tiên
                    using (FileStream fs = new FileStream(FileSystemFilePath, FileMode.Open, FileAccess.Write))
                    {
                        fs.Seek(HashedPasswordBlock, SeekOrigin.Begin);
                        fs.Write(enteredPasswordHash, 0, enteredPasswordHash.Length);
                    }

                    hashedPassword = newPasswordHash; // Cập nhật hashed password trong bộ nhớ
                }
                else
                {
                    MessageBox.Show("Mật khẩu cũ không đúng.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        public bool CheckPassword(string enteredPassword)
        {
            ReadHashedPassword();
            using (SHA256 sha256 = SHA256.Create())
            {
                if (string.IsNullOrEmpty(enteredPassword))
                {
                    using (FileStream fs = new FileStream(FileSystemFilePath, FileMode.Open, FileAccess.Read))
                    {
                        byte[] buffer = new byte[BlockSize];
                        int bytesRead = ReadBlock(fs, HashedPasswordBlock * BlockSize, buffer);
                        return IsBlockEmpty(buffer, bytesRead);
                    }
                }
                else
                {
                    byte[] enteredPasswordHash = new byte[BlockSize];

                    byte[] tempHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(enteredPassword));
                    Array.Copy(tempHash, enteredPasswordHash, tempHash.Length);
                    return hashedPassword.SequenceEqual(enteredPasswordHash);
                }
            }
        }
        public void ClearPassword()
        {
            using (FileStream fs = new FileStream(FileSystemFilePath, FileMode.Open, FileAccess.Write))
            {
                // Xóa hashed password từ block đầu tiên
                byte[] emptyBuffer = new byte[BlockSize];
                fs.Seek(HashedPasswordBlock, SeekOrigin.Begin);
                fs.Write(emptyBuffer, 0, emptyBuffer.Length);
            }

            // Đặt lại trạng thái mật khẩu
            hashedPassword = null;
        }


        private long GetStartPositionOfFileName(FileStream fileSystemStream, string fileName)
        {
            long currentPosition = BlockSize; // Start after the FAT table

           

            //Loop through the file system to find the position of the file by name
            while (currentPosition < fileSystemStream.Length)
            {
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

                currentPosition += BlockSize;
            }

            // If the file name is not found, return the end of the file
            return fileSystemStream.Length;
        }
        public void DeleteFileNoRecovery(string fileName)
        {
            // Kiểm tra xem file hệ thống đã được khởi tạo chưa
            if (string.IsNullOrEmpty(FileSystemFilePath) || !File.Exists(FileSystemFilePath))
            {
                MessageBox.Show("Hệ thống tập tin chưa được khởi tạo.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Kiểm tra xem file cần xóa có tồn tại trong bảng FAT không

            var fileEntry = FAT.FirstOrDefault(entry =>  entry.FileName == fileName);
            
            if (fileEntry == default)
            {
                MessageBox.Show("Không có file được chọn hoặc file không tồn tại trong hệ thống tập tin.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                using (FileStream fileSystemStream = new FileStream(FileSystemFilePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    
                    // Xác định vị trí bắt đầu trong hệ thống tập tin cho file cần xóa
                    long startPosition = fileEntry.StartBlock * BlockSize;
                    long startPositionOfFileName= GetStartPositionOfFileName(fileSystemStream, fileName);

                    // Đặt vị trí bắt đầu trong hệ thống tập tin
                    fileSystemStream.Seek(startPosition, SeekOrigin.Begin);

                    // Ghi dữ liệu rỗng vào vị trí của file để xóa nó
                    byte[] emptyBuffer = new byte[fileEntry.FileSize];
                    fileSystemStream.Write(emptyBuffer, 0, emptyBuffer.Length);

                    // Đặt vị trí bắt đầu trong hệ thống tập tin de xoa ten File
                    fileSystemStream.Seek(startPositionOfFileName, SeekOrigin.Begin);

                    // Ghi dữ liệu rỗng vào vị trí của file để xóa nó
                    byte[] emptyBufferFileName = new byte[512];
                    fileSystemStream.Write(emptyBufferFileName, 0, emptyBufferFileName.Length);

                    // Di chuyển dữ liệu từ file tiếp theo lên vị trí của file bị xóa
                    for (int i = FAT.IndexOf(fileEntry) + 1; i < FAT.Count; i++)
                    {
                        var nextFile = FAT[i];
                        long nextFileStartPosition = nextFile.StartBlock * BlockSize;
                        long nextFileEntry = startPositionOfFileName + BlockSize;

                        // Đọc dữ liệu từ file tiếp theo và ghi vào vị trí của file trước đó
                        byte[] buffer = new byte[nextFile.FileSize];
                        fileSystemStream.Seek(nextFileStartPosition, SeekOrigin.Begin);
                        int bytesRead = fileSystemStream.Read(buffer, 0, buffer.Length);

                        // Đọc dữ liệu từ entry tiếp theo và ghi vào vị trí của entry trước đó
                        byte[] bufferEntry = new byte[BlockSize];
                        fileSystemStream.Seek(nextFileEntry, SeekOrigin.Begin);
                        int bytesReadEntry = fileSystemStream.Read(bufferEntry, 0, bufferEntry.Length);

                        fileSystemStream.Seek(startPosition, SeekOrigin.Begin);
                        fileSystemStream.Write(buffer, 0, bytesRead);
                        //
                        nextFile.StartBlock = startPosition / BlockSize;
                        fileSystemStream.Seek(startPositionOfFileName, SeekOrigin.Begin);
                        BitConverter.GetBytes(nextFile.StartBlock).CopyTo(bufferEntry, 256); // Start block
                        fileSystemStream.Write(bufferEntry, 0, bytesReadEntry);

                        // Cập nhật vị trí của file trong bảng FAT
                        startPosition = GetStartPositionForNewData(fileSystemStream);
                        startPositionOfFileName = nextFileEntry;

                    }

                    // Ghi dữ liệu rỗng vào vị trí cuối cùng để xóa file cuối cùng
                    emptyBuffer = new byte[FAT.LastOrDefault().FileSize];
                    fileSystemStream.Seek(startPosition, SeekOrigin.Begin);
                    fileSystemStream.Write(emptyBuffer, 0, emptyBuffer.Length);

                    // Ghi dữ liệu rỗng vào vị trí cuối cùng để xóa entry cuối cùng
                    emptyBuffer = new byte[BlockSize];
                    fileSystemStream.Seek(startPositionOfFileName, SeekOrigin.Begin);
                    fileSystemStream.Write(emptyBuffer, 0, emptyBuffer.Length);

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

       
        public void DeleteFile(string fileName)
        {
            // Kiểm tra xem file hệ thống đã được khởi tạo chưa
            if (string.IsNullOrEmpty(FileSystemFilePath) || !File.Exists(FileSystemFilePath))
            {
                MessageBox.Show("Hệ thống tập tin chưa được khởi tạo.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Kiểm tra xem file cần xóa có tồn tại trong bảng FAT không

            var fileEntry = FAT.FirstOrDefault(entry => entry.FileName == fileName);

            if (fileEntry == default)
            {
                MessageBox.Show("Không có file được chọn hoặc file không tồn tại trong hệ thống tập tin.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                using (FileStream fileSystemStream = new FileStream(FileSystemFilePath, FileMode.Open, FileAccess.ReadWrite))
                {

                    // Xác định vị trí bắt đầu trong hệ thống tập tin cho file cần xóa
                    long startPositionOfFileName = GetStartPositionOfFileName(fileSystemStream, fileName);


                    // Ghi FAT vao vung backup
                  

                    WriteFATInBackup(fileSystemStream,fileName);


                    // Đặt vị trí bắt đầu trong hệ thống tập tin de xoa ten File
                    fileSystemStream.Seek(startPositionOfFileName, SeekOrigin.Begin);

                    // Ghi dữ liệu rỗng vào vị trí của file để xóa nó
                    byte[] emptyBufferFileName = new byte[BlockSize];
                    fileSystemStream.Write(emptyBufferFileName, 0, emptyBufferFileName.Length);
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
        public byte[] ReadFileFromDRS(string fileName)
        {
            var fileEntry = FAT.FirstOrDefault(entry => entry.FileName == fileName);
            if (fileEntry == default)
            {
                MessageBox.Show("Không có file được chọn hoặc file không tồn tại trong hệ thống tập tin.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            using (FileStream fileSystemStream = new FileStream(FileSystemFilePath, FileMode.Open, FileAccess.Read))
            {
                // Xác định vị trí bắt đầu trong hệ thống tập tin cho file cần đọc
                long startPosition = fileEntry.StartBlock * BlockSize;

                // Đặt vị trí bắt đầu trong hệ thống tập tin
                fileSystemStream.Seek(startPosition, SeekOrigin.Begin);

                // Đọc dữ liệu từ hệ thống tập tin và trả về dưới dạng mảng byte
                byte[] buffer = new byte[fileEntry.FileSize];
                int bytesRead = fileSystemStream.Read(buffer, 0, buffer.Length);

                if (bytesRead != buffer.Length)
                {
                    MessageBox.Show("Lỗi khi đọc dữ liệu từ hệ thống tập tin.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }

                return buffer;
            }
        }
        public void WriteBytesToDRSAndUpdateFAT(byte[] data, string fileName, string hashPassword,string salt)
        {
            using (FileStream fs = new FileStream(FileSystemFilePath, FileMode.Open, FileAccess.ReadWrite))
            {
                long startPosition = GetStartPositionForNewData(fs);
                long startBlock = startPosition / BlockSize;
                long remainingSpace = Math.Max(0, fs.Length - startPosition);

                // Kiểm tra xem không gian trống còn đủ lớn cho dữ liệu mới không
                if (remainingSpace >= data.Length)
                {
                    // Cập nhật thông tin entry FAT mới
                    long entryOffset = GetStartPositionForNewEntry(fs);
                    if (entryOffset >= FATSize)
                    {
                        MessageBox.Show("Hệ thống entry đã đầy. Không thể Import thêm file.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    else
                    {
                        fs.Seek(entryOffset, SeekOrigin.Begin);

                        byte[] entryData = new byte[BlockSize];
                        Encoding.UTF8.GetBytes(fileName).CopyTo(entryData, 0);
                        BitConverter.GetBytes(startBlock).CopyTo(entryData, 256);
                        BitConverter.GetBytes((long)Math.Ceiling(data.Length / (double)BlockSize)).CopyTo(entryData, 256 + 8);
                        BitConverter.GetBytes(data.Length).CopyTo(entryData, 256 + 8 * 2);
                        Encoding.UTF8.GetBytes(hashPassword).CopyTo(entryData, 256 + 8 * 3);
                        Encoding.UTF8.GetBytes(salt).CopyTo(entryData, 256 + 8 * 3 + 32);
                        fs.Write(entryData, 0, entryData.Length);

                        // Ghi dữ liệu từ mảng byte vào hệ thống tập tin
                        fs.Seek(startPosition, SeekOrigin.Begin);
                        fs.Write(data, 0, data.Length);

                        // Cập nhật bảng FAT
                        FAT.Add((fileName, startBlock, (long)Math.Ceiling(data.Length / (double)BlockSize), data.Length, hashPassword, null)); // Không lưu salt ở đây

                        MessageBox.Show("Dữ liệu đã được ghi vào file DRS và cập nhật bảng FAT thành công.");
                    }
                }
                else
                {
                    MessageBox.Show("Không đủ không gian trống cho dữ liệu mới.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        public static string GenerateSalt()
        {
            int saltLength = 8; // Độ dài cố định cho muối
            string allowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()-=_+";
            StringBuilder salt = new StringBuilder();

            for (int i = 0; i < saltLength; i++)
            {
                int index = random.Next(0, allowedChars.Length);
                salt.Append(allowedChars[index]);
            }

            return salt.ToString();
        }

        public static string HashPassword(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password + salt);
                byte[] hashBytes = sha256.ComputeHash(passwordBytes);

                // Chuyển đổi giá trị hash thành chuỗi hex với độ dài 32 byte (64 ký tự hex)
                StringBuilder stringBuilder = new StringBuilder(hashBytes.Length * 2);
                foreach (byte b in hashBytes)
                {
                    stringBuilder.Append(b.ToString("x2"));
                }

                return stringBuilder.ToString();
            }
        }

        public byte[] EncryptBytes(byte[] plainBytes, string hashPassword)
        {
            using (Aes aesAlg = Aes.Create())
            {
                // Chỉnh kích thước của khóa và muối
                byte[] key = Encoding.UTF8.GetBytes(hashPassword).Take(32).ToArray();
                byte[] salt = new byte[aesAlg.BlockSize / 8];

                aesAlg.Key = key;
                aesAlg.IV = salt;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(plainBytes, 0, plainBytes.Length);
                        csEncrypt.FlushFinalBlock();
                    }
                    return msEncrypt.ToArray();
                }
            }
        }

        public byte[] DecryptBytes(byte[] cipherBytes, string hashPassword)
        {
            using (Aes aesAlg = Aes.Create())
            {
                byte[] salt = new byte[aesAlg.BlockSize / 8]; // IV là 0 hết

                aesAlg.Key = Encoding.UTF8.GetBytes(hashPassword);
                aesAlg.IV = salt;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherBytes))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (MemoryStream msResult = new MemoryStream())
                        {
                            csDecrypt.CopyTo(msResult);
                            return msResult.ToArray();
                        }
                    }
                }
            }
        }
        
        public void recoveryFAT(string fileName)
        {
            // Kiểm tra xem file hệ thống đã được khởi tạo chưa
            if (string.IsNullOrEmpty(FileSystemFilePath) || !File.Exists(FileSystemFilePath))
            {
                MessageBox.Show("Hệ thống tập tin chưa được khởi tạo.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Kiểm tra xem file cần xóa có tồn tại trong bảng FAT không

            var fileEntry = BACKUP.FirstOrDefault(entry => entry.FileName == fileName);

            if (fileEntry == default)
            {
                MessageBox.Show("Không có file được chọn hoặc file không tồn tại trong hệ thống tập tin.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }
    }
}
