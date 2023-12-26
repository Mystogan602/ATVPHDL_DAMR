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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Diagnostics;

namespace HideFilePDF
{
    public partial class MainMenu : Form
    {
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
            if(labelChonFilePDFCanAn.Text == "Đường dẫn File cần ẩn")
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
    }
}
