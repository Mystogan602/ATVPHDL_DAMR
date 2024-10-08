﻿using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using libraryFileProcessing;
using libraryEncryptDecrypt;
using System.Security.AccessControl;

namespace HideFilePDF
{
    public partial class MainMenu : Form
    {
        private Button selectedButton;
        private static string fileName = null;
        private static string pdfPath = null;
        private string filePath = null;
        private FileProcessing file = new FileProcessing();
        private EncryptDecrypt encryptDecrypt = new EncryptDecrypt();
        public MainMenu()
        {
            InitializeComponent();
            InitializeIU();
        }
        private void InitializeIU()
        {
            labelChonFilePDFCanAn.Text = "Đường dẫn File PDF để ẩn dữ liệu";
            labelChonFilePDFCanAn.ForeColor = Color.Gray;
            labelChonFilePDFDeAn.Text = "Đường dẫn File cần ẩn";
            labelChonFilePDFDeAn.ForeColor = Color.Gray;
        }
        public static string getFileName()
        {
            return fileName;
        }
        private void buttonChonFilePDF_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "PDF Files (*.pdf)|*.pdf";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                pdfPath = openFileDialog.FileName;

                // Lấy tên file và đuôi
                string fName = Path.GetFileName(pdfPath);
                // Hiển thị tên file
                labelChonFilePDFCanAn.Text = fName;
                labelChonFilePDFCanAn.ForeColor = SystemColors.ControlText;
                FileInfo PDFInfo = new FileInfo(pdfPath);
                long PDFSize = PDFInfo.Length;
                file.SelectFilePDF(PDFSize, pdfPath);

                DisplayFiles(pdfPath);
            }
        }

        private void buttonChonFileDeAn_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "All Files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                filePath = openFileDialog.FileName;
                // Lấy tên file 
                string fName = Path.GetFileName(filePath);
                labelChonFilePDFDeAn.Text = fName;
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
            else
            {
                try
                {
                    if (!file.CheckFAT())
                    {
                        file.GenerateFAT();
                    }
                    if (file.CheckFileName(labelChonFilePDFDeAn.Text) == true)
                    {
                        MessageBox.Show("Đã tồn tại tên file giống, nhập file khác hoặc đổi tên file !", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        byte[] salt = encryptDecrypt.CreateSalt();
                        string hashedPassword = encryptDecrypt.HashPassword(textBoxMatKhau.Text, salt);
                        byte[] fileData = File.ReadAllBytes(filePath);
                        byte[] fileBytes = encryptDecrypt.EncryptFile(fileData, hashedPassword, salt);
                        FileInfo PDFInfo = new FileInfo(pdfPath);
                        long PDFSize = PDFInfo.Length;
                        file.WriteFATAndData(filePath, PDFSize, fileBytes, salt, hashedPassword);
                        DisplayFiles(pdfPath);
                    }

                }
                catch (Exception ex)
                {
                    // Hiển thị thông báo lỗi nếu có lỗi xảy ra
                    MessageBox.Show($"Error embedding file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void buttonXuatFileAn_Click(object sender, EventArgs e)
        {
            if (textBoxMatKhau.Text == "")
            {
                MessageBox.Show("Vui lòng nhập mật khẩu", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else if (fileName == null)
            {
                MessageBox.Show("Vui lòng chọn file ẩn trong danh sách để xuất", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                byte[] salt = file.GetSalt(fileName);
                string hashedPassword = file.GetHashedPassword(fileName);
                if (encryptDecrypt.VerifyPassword(textBoxMatKhau.Text, hashedPassword, salt))
                {
                    string selectedFileName = fileName;
                    if (selectedFileName != null)
                    {
                        using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                        {
                            saveFileDialog.Title = "Chọn nơi để Export file";
                            saveFileDialog.Filter = "All Files|*.*";
                            // Xác định đuôi file của file được chọn và thiết lập Filter tương ứng trong SaveFileDialog
                            string fileExtension = Path.GetExtension(selectedFileName);
                            if (!string.IsNullOrEmpty(fileExtension))
                            {
                                saveFileDialog.Filter = $"{fileExtension} Files|*{fileExtension}|All Files|*.*";
                            }
                            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                            {
                                string exportFilePath = saveFileDialog.FileName;

                                // Truyền tên file cần xuất vào phương thức ExportFile
                                file.ExportFile(fileName, exportFilePath, hashedPassword, salt);
                            }
                        }

                    }
                    else
                    {
                        MessageBox.Show("Không có file được chọn.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Vui lòng nhập mật khẩu chính xác", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                }
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
            //FileProcessing fProcess = new FileProcessing();
            //List<string> files = fProcess.GetFilesFromFAT();
            List<string> files = file.GetFilesFromFAT();
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
                    file.DeleteFile(fileName);
                    DisplayFiles(pdfPath);
                };
                // Tạo Button chứa options thay đổi mật khẩu
                Button thaydoiPassFileButton = new Button();
                thaydoiPassFileButton.Text = "Đổi mật khẩu";
                thaydoiPassFileButton.BackColor = Color.DeepSkyBlue;
                thaydoiPassFileButton.Height = 60;
                thaydoiPassFileButton.Width = 115;
                thaydoiPassFileButton.Click += (sender, e) =>
                {
                    if (textBoxMatKhau.Text == "")
                    {
                        MessageBox.Show("Vui lòng nhập mật khẩu", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (textBoxNewPassword.Text == "")
                    {
                        MessageBox.Show("Vui lòng nhập mật khẩu mới", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    // Xử lý sự kiện khi nút được nhấn
                    fileName = fileNameButton.Text;
                    byte[] salt = file.GetSalt(fileName);
                    string OldHashedPassword = file.GetHashedPassword(fileName);
                    if (encryptDecrypt.VerifyPassword(textBoxMatKhau.Text, OldHashedPassword, salt))
                    {
                        if (textBoxMatKhau.Text == textBoxNewPassword.Text)
                        {
                            MessageBox.Show("Mật khẩu mới phải khác mật khẩu cũ", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        string newHashedPassword = encryptDecrypt.HashPassword(textBoxNewPassword.Text, salt);
                        file.UpdateDataAfterChangePassword(fileName, OldHashedPassword, newHashedPassword, salt);
                        DisplayFiles(pdfPath);
                    }
                    else
                    {
                        MessageBox.Show("Mật khẩu cũ không chính xác", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    }
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
            //Làm mới fileName
            fileName = null;
        }
    }
}