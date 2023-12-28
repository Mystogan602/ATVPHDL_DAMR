using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Diagnostics;
using libraryFileProcessing;

namespace HideFilePDF
{
    public partial class MainMenu : Form
    {
        private Button selectedButton;
        private static string fileName = null;
        public MainMenu()
        {
            InitializeComponent();
            InitializeIU();
        }
        private void InitializeIU()
        {
            labelChonFilePDFCanAn.Text = "Đường dẫn File cần ẩn";
            labelChonFilePDFCanAn.ForeColor = Color.Gray;
            labelChonFilePDFDeAn.Text = "Đường dẫn File PDF ẩn dữ liệu";
            labelChonFilePDFDeAn.ForeColor = Color.Gray;
        }
        public static string getFileName()
        {
            return fileName;
        }
        private void buttonCHonFilePDFCanAn_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "PDF Files (*.pdf)|*.pdf";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string fullPath = openFileDialog.FileName;

                // Lấy tên file và đuôi
                string fileName = Path.GetFileName(fullPath);
                // Hiển thị tên file
                labelChonFilePDFCanAn.Text = fileName;
                labelChonFilePDFCanAn.ForeColor = SystemColors.ControlText;
                DisplayFiles(fullPath);
            }
        }

        private void buttonChonFilePDFDeAn_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "PDF Files (*.pdf)|*.pdf";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string fullPath = openFileDialog.FileName;
                // Lấy tên file 
                string fileName = Path.GetFileName(fullPath);
                labelChonFilePDFDeAn.Text = fileName;
                labelChonFilePDFDeAn.ForeColor = SystemColors.ControlText;
            }
        }
        private void buttonThoat_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void buttonAn_Click(object sender, EventArgs e)
        {
            if (labelChonFilePDFCanAn.Text == "Đường dẫn File cần ẩn")
            {
                MessageBox.Show("Vui lòng nhập đường dẫn file cần ẩn.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else if (labelChonFilePDFDeAn.Text == "Đường dẫn File PDF ẩn dữ liệu")
            {
                MessageBox.Show("Vui lòng nhập đường dẫn File PDF ẩn dữ liệu.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else if (textBoxMatKhau.Text == "")
            {
                MessageBox.Show("Vui lòng nhập mật khẩu", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void buttonXuatFileAn_Click(object sender, EventArgs e)
        {
            if (labelChonFilePDFDeAn.Text == "Đường dẫn File PDF ẩn dữ liệu")
            {
                MessageBox.Show("Vui lòng nhập đường dẫn File PDF ẩn dữ liệu.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else if (textBoxMatKhau.Text == "")
            {
                MessageBox.Show("Vui lòng nhập mật khẩu", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void buttonXemFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string fullPath = openFileDialog.FileName;
                if (File.Exists(fullPath))
                {
                    try
                    {
                        // Mở file bằng ứng dụng mặc định
                        Process.Start(fullPath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Không thể mở file: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Đường dẫn file không hợp lệ hoặc file không tồn tại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        //Biểu diễn danh sách file ẩn
        private void DisplayFiles(string fPath)
        {
            // Xóa danh sách hiện tại (nếu có)
            flowLayoutPanelListFileHide.Controls.Clear();
            FileProcessing fProcess = new FileProcessing();
            //List<string> files = fProcess.GetFilesFromFAT();
            List<string> files = new List<string>
            {
                @"C:\Documents\File1.txt",
                @"D:\Images\Image1.jpg",
                @"E:\Documents\File2.docx",
                @"F:\Videos\Video1.mp4",
                @"C:\Documents\File2.txt",
                @"D:\Images\Image2.jpg",
                @"E:\Documents\File3.docx",
                @"F:\Videos\Video2.mp4",
                // Thêm các đường dẫn tệp tin khác nếu cần
            };
            foreach (string filePath in files)
            {
                // Tạo Panel chứa TextBox và Button
                Panel filePanel = new Panel();
                filePanel.BorderStyle = BorderStyle.FixedSingle;
                filePanel.Width = 500;
                filePanel.Height = 80;
                // Tạo TableLayoutPanel
                TableLayoutPanel tableLayout = new TableLayoutPanel();
                tableLayout.Dock = DockStyle.Fill;

                // Tạo Button chứa tên file
                Button fileNameButton = new Button();
                fileNameButton.Text = Path.GetFileName(filePath);
                fileNameButton.BackColor = SystemColors.ActiveCaption;
                fileNameButton.Height = 60;
                fileNameButton.Width = 300;
                fileNameButton.Click += (sender, e) =>
                {
                    // Xử lý sự kiện khi nút được nhấn
                    if (selectedButton != null)
                    {
                        // Nếu có Button trước đó được chọn, đặt lại màu của nó
                        selectedButton.BackColor = SystemColors.ActiveCaption;
                    }
                    fileName = fileNameButton.Text;
                    fileNameButton.BackColor = Color.LightGreen; // Đổi màu khi được chọn
                    selectedButton = fileNameButton; // Lưu trữ Button mới được chọn
                };
                // Tạo Button chứa options xóa
                Button xoaFileButton = new Button();
                xoaFileButton.Text = "Xóa";
                xoaFileButton.BackColor = Color.DeepSkyBlue;
                xoaFileButton.Height = 60;
                xoaFileButton.Width = 60;
                xoaFileButton.Click += (sender, e) =>
                {
                    // Xử lý sự kiện khi nút được nhấn
                    fileName = fileNameButton.Text;
                };
                // Tạo Button chứa options thay đổi mật khẩu
                Button thaydoiPassFileButton = new Button();
                thaydoiPassFileButton.Text = "Đổi mật khẩu";
                thaydoiPassFileButton.BackColor = Color.DeepSkyBlue;
                thaydoiPassFileButton.Height = 60;
                thaydoiPassFileButton.Width = 115;
                thaydoiPassFileButton.Click += (sender, e) =>
                {
                    // Xử lý sự kiện khi nút được nhấn
                    fileName = fileNameButton.Text;
                };

                // Thêm TextBox và Button vào TableLayoutPanel
                tableLayout.Controls.Add(fileNameButton, 0, 0);
                tableLayout.Controls.Add(xoaFileButton, 1, 0);
                tableLayout.Controls.Add(thaydoiPassFileButton, 2, 0);

                // Thêm TableLayoutPanel vào Panel
                filePanel.Controls.Add(tableLayout);

                // Thêm Panel vào FlowLayoutPanel
                flowLayoutPanelListFileHide.Controls.Add(filePanel);
            }

            // Kích thước của FlowLayoutPanel sẽ tự động thay đổi để hiển thị thanh cuộn nếu cần
            flowLayoutPanelListFileHide.AutoScroll = true;
        }


    }
}
