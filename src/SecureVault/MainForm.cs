using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SecureVault
{
    public partial class MainForm : Form
    {
        private byte[] encryptionKey;  // set after login
        private string passwordFile = "passwords.dat";
        private List<string> passwordEntries = new List<string>();

        public MainForm(byte[] key)
        {
            InitializeComponent();
            encryptionKey = key;
            Text = "Password Manager";
            LoadPasswords();  // load saved entries at startup
        }

        // Add a new password entry
        private void btnAdd_Click(object sender, EventArgs e)
        {
            string site = txtSite.Text.Trim();
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrWhiteSpace(site) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Site and Password cannot be empty.");
                return;
            }

            string entry = $"{site} | {username} | {password}";
            passwordEntries.Add(entry);

            SavePasswords();
            RefreshPasswordList();

            txtSite.Clear();
            txtUsername.Clear();
            txtPassword.Clear();
        }

        // Save encrypted passwords to file
        private void SavePasswords()
        {
            List<byte[]> encryptedList = passwordEntries
                .Select(entry => PasswordUtils.EncryptString(entry, encryptionKey))
                .ToList();

            using (var fs = new FileStream(passwordFile, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(encryptedList.Count); // number of entries
                foreach (var enc in encryptedList)
                {
                    bw.Write(enc.Length); // length of entry
                    bw.Write(enc);        // actual encrypted bytes
                }
            }
        }


        // Load encrypted passwords from file
        private void LoadPasswords()
        {
            passwordEntries.Clear();

            if (!File.Exists(passwordFile)) return;

            using (var fs = new FileStream(passwordFile, FileMode.Open))
            using (var br = new BinaryReader(fs))
            {
                try
                {
                    int count = br.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        int length = br.ReadInt32();
                        byte[] enc = br.ReadBytes(length);

                        // Ensure we actually read the expected number of bytes
                        if (enc.Length != length)
                            throw new EndOfStreamException("Password file is corrupted.");

                        string decrypted = PasswordUtils.DecryptBytes(enc, encryptionKey);
                        passwordEntries.Add(decrypted);
                    }
                }
                catch (EndOfStreamException)
                {
                    MessageBox.Show("Password file is corrupted or incomplete. It will be reset.");
                    passwordEntries.Clear();
                    File.Delete(passwordFile); // reset corrupted file
                }
            }

            RefreshPasswordList();
        }


        // Refresh ListBox with current entries
        private void RefreshPasswordList()
        {
            listBoxPasswords.Items.Clear();
            foreach (var entry in passwordEntries)
            {
                // only show the site for readability
                string site = entry.Split('|')[0].Trim();
                listBoxPasswords.Items.Add(site);
            }
        }

        // View selected password entry
        private void btnView_Click(object sender, EventArgs e)
        {
            if (listBoxPasswords.SelectedIndex == -1)
            {
                MessageBox.Show("Please select a password entry first.");
                return;
            }

            using (var confirmForm = new ConfirmPasswordForm())
            {
                if (confirmForm.ShowDialog() == DialogResult.OK && confirmForm.IsConfirmed)
                {
                    string entry = passwordEntries[listBoxPasswords.SelectedIndex];
                    string[] parts = entry.Split('|');
                    string site = parts[0].Trim();
                    string username = parts.Length > 1 ? parts[1].Trim() : "";
                    string password = parts.Length > 2 ? parts[2].Trim() : "";

                    MessageBox.Show(
                        $"Site: {site}\nUsername: {username}\nPassword: {password}",
                        "Stored Password");
                }
            }
        }

        // Change master password
        private void btnChangePassword_Click(object sender, EventArgs e)
        {
            using (var changeForm = new ChangePasswordForm())
            {
                if (changeForm.ShowDialog() == DialogResult.OK)
                {
                    MessageBox.Show("Master password changed successfully. Please remember your new password.");
                }
            }
        }
    }
}
