using System;
using System.IO;
using System.Linq;
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

        // Biến middleIndex để lưu vị trí giữa của dữ liệu cần nhúng
        int middleIndex = 1;

        private void EmbedButton_Click(object sender, EventArgs e)
        {
            // Hiển thị hộp thoại để chọn tệp PDF
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "PDF Files (*.pdf)|*.pdf";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Lấy đường dẫn của tệp PDF được chọn
                string pdfFilePath = openFileDialog.FileName;
                // Lấy văn bản cần nhúng từ TextBox
                string inputText = DataToEmbedTextBox.Text;
                // Tạo đường dẫn cho tệp đầu ra
                string outputFilePath = Path.Combine(Path.GetDirectoryName(pdfFilePath), "output.pdf");

                try
                {
                    // Chia văn bản thành hai phần dựa trên vị trí giữa
                    string textToEmbed1 = inputText.Substring(0, middleIndex);
                    string textToEmbed2 = inputText.Substring(middleIndex);

                    // Nhúng văn bản vào tệp PDF
                    EmbedTextInPDF(pdfFilePath, textToEmbed1, textToEmbed2, outputFilePath);

                    // Hiển thị thông báo thành công
                    MessageBox.Show("Text embedded successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    // Hiển thị thông báo lỗi nếu có lỗi xảy ra
                    MessageBox.Show($"Error embedding text: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                try
                {
                    // Giải mã văn bản từ tệp PDF
                    string decodedText = DecodeTextFromPDF(pdfFilePath);
                    // Hiển thị văn bản giải mã vào TextBox
                    OutputTextBox.Text = decodedText;
                }
                catch (Exception ex)
                {
                    // Hiển thị thông báo lỗi nếu có lỗi xảy ra
                    MessageBox.Show($"Error decoding text: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void EmbedTextInPDF(string inputPdfPath, string textToEmbed1, string textToEmbed2, string outputPdfPath)
        {
            // Kiểm tra tính hợp lệ của tệp PDF đầu vào
            if (!File.Exists(inputPdfPath) || Path.GetExtension(inputPdfPath).ToLower() != ".pdf")
            {
                MessageBox.Show("Invalid PDF file selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Đọc nội dung của tệp PDF
            byte[] pdfBytes = File.ReadAllBytes(inputPdfPath);

            // Tìm vị trí của lần xuất hiện của "/Info" trong nội dung của tệp PDF
            int metadataPosition = FindBytes(pdfBytes, Encoding.ASCII.GetBytes("/Info"), false, 1);

            if (metadataPosition == -1)
            {
                MessageBox.Show("Invalid PDF format (missing metadata).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Nhúng văn bản vào metadata
            byte[] textBytes1 = Encoding.UTF8.GetBytes(textToEmbed1);
            byte[] remainingCombinedByte1 = new byte[pdfBytes.Length - metadataPosition];
            byte[] combinedBytes1 = new byte[pdfBytes.Length + textBytes1.Length];

            // Sao chép phần đầu của nội dung PDF gốc (đến metadata)
            Array.Copy(pdfBytes, combinedBytes1, metadataPosition);

            // Sao chép phần còn lại của nội dung PDF gốc (sau metadata)
            Array.Copy(pdfBytes, metadataPosition, remainingCombinedByte1, 0, remainingCombinedByte1.Length);

            // Sao chép văn bản được nhúng vào metadata
            Array.Copy(textBytes1, 0, combinedBytes1, metadataPosition, textBytes1.Length);

            // Sao chép phần còn lại sau vị trí metadata
            Array.Copy(remainingCombinedByte1, 0, combinedBytes1, metadataPosition + textToEmbed1.Length, remainingCombinedByte1.Length);

            // Nhúng văn bản thứ hai vào cuối tệp PDF
            byte[] textBytes2 = Encoding.UTF8.GetBytes(textToEmbed2);
            byte[] combinedBytes2 = new byte[combinedBytes1.Length + textBytes2.Length];

            // Sao chép nội dung PDF gốc
            Array.Copy(combinedBytes1, combinedBytes2, combinedBytes1.Length);

            // Sao chép văn bản được nhúng vào cuối
            Array.Copy(textBytes2, 0, combinedBytes2, combinedBytes1.Length, textBytes2.Length);

            // Ghi nội dung đã được sửa đổi vào tệp PDF đầu ra
            File.WriteAllBytes(outputPdfPath, combinedBytes2);
        }

        private string DecodeTextFromPDF(string pdfFilePath)
        {
            // Kiểm tra tính hợp lệ của tệp PDF đầu vào
            if (!File.Exists(pdfFilePath) || Path.GetExtension(pdfFilePath).ToLower() != ".pdf")
            {
                MessageBox.Show("Invalid PDF file selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return string.Empty;
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
                    return string.Empty;
                }

                // Rút trích văn bản từ metadata
                int text1Length = middleIndex;
                byte[] textBytes1 = new byte[text1Length];
                Array.Copy(pdfBytes, metadataPosition, textBytes1, 0, text1Length);
                string decodedText1 = Encoding.UTF8.GetString(textBytes1);

                // Tìm vị trí của lần xuất hiện cuối cùng của "%EOF" trong nội dung của tệp PDF
                int eofPosition = FindBytes(pdfBytes, Encoding.ASCII.GetBytes("%EOF"), true);

                if (eofPosition == -1)
                {
                    MessageBox.Show("Invalid PDF format (missing %EOF).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return string.Empty;
                }

                // Rút trích văn bản từ cuối tệp PDF
                int text2Length = pdfBytes.Length - eofPosition;
                byte[] textBytes2 = new byte[text2Length];
                Array.Copy(pdfBytes, eofPosition, textBytes2, 0, text2Length);
                string decodedText2 = Encoding.UTF8.GetString(textBytes2);

                // Kết hợp cả hai văn bản đã giải mã
                string decodedText = decodedText1 + decodedText2;

                return decodedText;
            }
            catch (Exception ex)
            {
                // Hiển thị thông báo lỗi nếu có lỗi xảy ra
                MessageBox.Show($"Error decoding text: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return string.Empty;
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
