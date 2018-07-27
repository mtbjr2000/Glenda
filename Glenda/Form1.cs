using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Glenda
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            string type = cmbType.SelectedItem.ToString();

            if (type == "Debit")
            {
                FolderBrowserDialog folderDlg = new FolderBrowserDialog();
                DialogResult result = folderDlg.ShowDialog();

                if (result == DialogResult.OK)
                {
                    txtOutput.Clear();
                    txtInput.Text = folderDlg.SelectedPath;
                    Environment.SpecialFolder root = folderDlg.RootFolder;
                }
            }
            else
            {
                if (fileDlg.ShowDialog() == DialogResult.OK)
                {
                    txtOutput.Clear();
                    FileInfo file = new FileInfo(fileDlg.FileName);

                    if (file.Name.StartsWith("OCPC"))
                        txtInput.Text = fileDlg.FileName;
                    else
                        MessageBox.Show("Filename does not start OCPC", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnProcess_Click(object sender, EventArgs e)
        {
            if (txtInput.Text.Length > 0)
            {
                string type = cmbType.SelectedItem.ToString();

                if (type == "Debit")
                {
                    DirectoryInfo dir = new DirectoryInfo(txtInput.Text);
                    FileInfo[] files = dir.GetFiles("*.dbf");

                    files = files.Where(i => !i.Name.ToUpper().StartsWith("OCPC")).ToArray();
                    Debit(dir, files);
                }
                else
                {
                    FileInfo file = new FileInfo(txtInput.Text);
                    Credit(file);
                }
            }
        }

        private void Debit(DirectoryInfo dir, FileInfo[] files)
        {
            List<string> acctNumbers = new List<string>();

            foreach (FileInfo file in files)
            {
                txtOutput.AppendText(String.Format("Getting account numbers from {0} ==>", file.Name));

                using (var table = NDbfReader.Table.Open(file.FullName))
                {
                    List<string> items = new List<string>();

                    var reader = table.OpenReader(Encoding.GetEncoding(1250));

                    while (reader.Read())
                        items.Add(reader.GetString("ACCTNO"));

                    acctNumbers.AddRange(items.Distinct());
                    txtOutput.AppendText(items.Count.ToString() + "\n");
                }
            }

            acctNumbers = acctNumbers.Where(i => i != "100000000000").Distinct().OrderBy(i => i).ToList();

            if (acctNumbers.Count > 0)
            {
                var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("DEBIT");
                worksheet.Column(1).Width = 15;

                worksheet.Cell("A1").Value = "Account Number";

                for (int i = 0; i < acctNumbers.Count; i++)
                {
                    worksheet.Cell(i + 1, 1).Style.NumberFormat.Format = "000000000000";
                    worksheet.Cell(i + 1, 1).Value = acctNumbers[i];
                }

                string oldFileName = String.Format("{0}\\Debit_{1}.xlsx", dir.FullName, DateTime.Now.ToString("dd-MMM-yyyy-hhmmss"));
                string newFileName = String.Format("{0}\\Debit_{1}.xls", dir.FullName, DateTime.Now.ToString("dd-MMM-yyyy-hhmmss"));

                workbook.SaveAs(oldFileName);
                File.Copy(oldFileName, newFileName);
                File.Delete(oldFileName);

                txtOutput.AppendText(String.Format("\n{0} distinct account numbers outputted to:\n\n{1}", acctNumbers.Count, newFileName));
                Process.Start(newFileName);
            }
        }

        private void Credit(FileInfo file)
        {
            List<CreditDto> acctNumbers = new List<CreditDto>();

            txtOutput.AppendText(String.Format("Getting account numbers from {0}\n\n", file.Name));

            using (var table = NDbfReader.Table.Open(file.FullName))
            {
                var reader = table.OpenReader(Encoding.GetEncoding(1250));

                while (reader.Read())
                    acctNumbers.Add(new CreditDto { AccountNumber = reader.GetString("ACCTNO"), Amount = reader.GetDecimal("AMOUNT").Value });
            }

            decimal total = acctNumbers.Sum(i => i.Amount);
            decimal totalZero = acctNumbers.Where(i => i.AccountNumber == "000000000000").Sum(i => i.Amount);
            decimal lessTotal = total - totalZero;

            txtOutput.AppendText(String.Format("Total for All Accounts ==> {0:N}\n", total));
            txtOutput.AppendText(String.Format("Total for 000000000000 ==> {0:N}\n", totalZero));
            txtOutput.AppendText(String.Format("All less 000000000000 ==> {0:N}\n", lessTotal));
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            txtInput.Clear();
            cmbType.SelectedIndex = 0;
            txtOutput.Clear();
        }

        private void cmbType_SelectedIndexChanged(object sender, EventArgs e)
        {
            txtInput.Clear();
            txtOutput.Clear();
        }
    }
}
