using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Windows.Forms;
using System.Collections;

namespace HideFilePDF
{
    class dataHider
    {
        static private int encAESSize = 16;
        static private int saltSize = 16;
        public static void HideDataInPdf(string PDFPath, string dataToHide )
        {
            EncryptDecrypt encryptDecrypt = new EncryptDecrypt();
            try
            {
                string outputFilePath = Path.Combine(Path.GetDirectoryName(PDFPath), "output.pdf");

                // Đọc toàn bộ nội dung của file PDF thành một mảng byte
                byte[] pdfBytes = File.ReadAllBytes(PDFPath);
   


                // Xử lý byte theo nhu cầu của bạn, ví dụ, thêm dữ liệu vào cuối file
                //byte[] newData = System.Text.Encoding.UTF8.GetBytes(dataToHide);
                byte[] newData = new byte[16];
                newData=encryptDecrypt.encryption(dataToHide);
                string byteString = BitConverter.ToString(newData);
                MessageBox.Show(byteString);
                byte[] newPdfBytes = new byte[pdfBytes.Length + encAESSize+saltSize];

                Buffer.BlockCopy(pdfBytes, 0, newPdfBytes, 0, pdfBytes.Length);

                Buffer.BlockCopy(newData, 0, newPdfBytes, pdfBytes.Length, encAESSize + saltSize);

                // Ghi byte đã xử lý vào file mới

                File.WriteAllBytes(outputFilePath, newPdfBytes);

                MessageBox.Show("Đã ẩn dữ liệu thành công!");
            }     
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}");



                //commit
                
                


            }
        }
    }
}
