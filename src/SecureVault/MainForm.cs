// This is the main password vault screen that appears after a successful login
// It allows the user to add, view and delete password entries
// It also handles session key rotation, clipboard clearing and the dark theme

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
// We specify the full name here because there are two Timer classes available
// and we want the Windows Forms one, not the System.Threading one
using Timer = System.Windows.Forms.Timer;

namespace SecureVault
{
    public partial class MainForm : Form
    {
        // The master key is the encryption key derived from the master password
        // It is used to encrypt and decrypt all vault entries
        // It never changes while the app is running and is never written to disk
        private byte[] masterKey;
        // The full path of the file where encrypted passwords are stored on disk
        private readonly string passwordFile = DataPaths.PasswordFile;
        // This list holds all the decrypted password entries in memory
        // while the app is running
        private readonly List<string> passwordEntries = new();
        // This timer fires 15 seconds after a password is copied
        // to clear it from the clipboard automatically
        private readonly Timer clipboardClearTimer;
        // Stores the value that was most recently copied to the clipboard
        // so we can check it is still there before clearing
        private string? lastClipboardSecret;
        public MainForm(byte[] key)
        {
            // Set up all the buttons, labels and listbox from the designer file
            InitializeComponent();
            // Store the master key passed in from the login form
            masterKey = key;
            // Set the title bar text of the window
            Text = "Password Manager";
            // Load any previously saved password entries from disk
            LoadPasswords();
            // Set up the clipboard clear timer
            // It does not start automatically, only when a password is copied
            clipboardClearTimer = new Timer();
            clipboardClearTimer.Interval = 15_000; // 15 seconds
            clipboardClearTimer.Tick += ClipboardClearTimer_Tick;
            // Apply the dark colour scheme
            ApplyTheme();
        }
        // This method runs when the clipboard clear timer fires
        private void ClipboardClearTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                // Only clear the clipboard if it still contains what we copied
                // If the user has copied something else in the meantime
                // we do not want to clear that
                if (!string.IsNullOrEmpty(lastClipboardSecret) &&
                    Clipboard.ContainsText() &&
                    Clipboard.GetText() == lastClipboardSecret)
                {
                    Clipboard.Clear();
                }
            }
            catch
            {
                // The clipboard can sometimes be locked by Windows or another app
                // If that happens we just move on rather than crashing
            }
            finally
            {
                // Always clear our stored reference and stop the timer
                // regardless of whether the clipboard clear succeeded
                lastClipboardSecret = null;
                clipboardClearTimer.Stop();
            }
        }
        // Checks whether a site string looks like a valid URL
        // Returns true if it matches the URL pattern, false otherwise
        private Boolean IsSiteValid(string site)
        {
            if (string.IsNullOrEmpty(site)) return false;
            // This pattern matches most common URL formats
            // including with or without http, www, and various domain endings
            const string pattern = @"(?:http[s]?://.)?(?:www\.)?[-a-zA-Z0-9@%.*+~#=]{2,256}\.[a-z]{2,6}\b(?:[-a-zA-Z0-9@:%*+.~#?&//=]*)";
            return Regex.IsMatch(site, pattern, RegexOptions.IgnoreCase);
        }
        // This method runs when the user clicks the Add Entry button
        private void btnAdd_Click(object sender, EventArgs e)
        {
            string site = txtSite.Text.Trim();
            // Check the site field contains a valid URL before proceeding
            if (!IsSiteValid(site))
            {
                MessageBox.Show("Please enter a valid URL for the site.");
                return;
            }
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();
            // Check the site and password fields are not empty
            // Username is optional so we do not check it here
            if (string.IsNullOrWhiteSpace(site) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Site and Password cannot be empty.");
                return;
            }
            // Combine the three fields into a single string separated by pipes
            // This is the format used to store each entry
            string entry = $"{site} | {username} | {password}";
            // Add to the in-memory list
            passwordEntries.Add(entry);
            // Save the updated list to disk in encrypted form
            SavePasswords();
            // Refresh the listbox on screen to show the new entry
            RefreshPasswordList();
            // Clear the input fields ready for the next entry
            txtSite.Clear();
            txtUsername.Clear();
            txtPassword.Clear();
        }
        // This method runs when the user clicks the Delete Entry button
        private void btnDelete_Click(object sender, EventArgs e)
        {
            int index = listBoxPasswords.SelectedIndex;
            // Check something is actually selected before trying to delete
            if (index < 0)
            {
                MessageBox.Show("Please select an entry to delete.");
                return;
            }
            // Ask the user to confirm before permanently deleting
            var confirm = MessageBox.Show(
                "Are you sure you want to delete this entry?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            // If the user clicked No, do nothing
            if (confirm != DialogResult.Yes)
                return;
            // Remove the entry from the in-memory list
            passwordEntries.RemoveAt(index);
            // Save the updated list back to disk
            SavePasswords();
            // Refresh the listbox to reflect the deletion
            RefreshPasswordList();
        }
        // Encrypts all entries and saves them to passwords.dat
        private void SavePasswords()
        {
            // Encrypt each entry individually using the master key
            // Select creates a new list where each entry has been encrypted
            List<byte[]> encryptedList = passwordEntries
                .Select(entry => PasswordUtils.EncryptString(entry, masterKey))
                .ToList();
            // Open the file for writing, creating it if it does not exist
            using var fs = new FileStream(passwordFile, FileMode.Create);
            using var bw = new BinaryWriter(fs);
            // Write how many entries there are first
            // so when we read back we know how many to expect
            bw.Write(encryptedList.Count);
            // Write each encrypted entry with its length prefix
            foreach (var enc in encryptedList)
            {
                bw.Write(enc.Length);
                bw.Write(enc);
            }
        }
        // Reads and decrypts all entries from passwords.dat
        private void LoadPasswords()
        {
            // Clear the in-memory list before loading
            passwordEntries.Clear();
            // If the file does not exist yet there is nothing to load
            if (!File.Exists(passwordFile))
                return;
            try
            {
                using var fs = new FileStream(passwordFile, FileMode.Open);
                using var br = new BinaryReader(fs);
                // Read how many entries were saved
                int count = br.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    // Read the length of this entry first
                    int length = br.ReadInt32();
                    // Read exactly that many bytes
                    byte[] enc = br.ReadBytes(length);
                    // If we got fewer bytes than expected the file is corrupted
                    if (enc.Length != length)
                        throw new EndOfStreamException();
                    // Decrypt the entry using the master key
                    string decrypted = PasswordUtils.DecryptBytes(enc, masterKey);
                    // Add the decrypted entry to the in-memory list
                    passwordEntries.Add(decrypted);
                }
            }
            catch
            {
                // If anything goes wrong reading the file, warn the user
                // and reset rather than crashing the application
                MessageBox.Show("Password file is corrupted and will be reset.");
                passwordEntries.Clear();
                File.Delete(passwordFile);
            }
            // Update the listbox to show whatever was loaded
            RefreshPasswordList();
        }
        // Updates the listbox on screen to show the current list of sites
        private void RefreshPasswordList()
        {
            listBoxPasswords.Items.Clear();
            foreach (var entry in passwordEntries)
            {
                // Each entry is stored as "site | username | password"
                // We only show the site name in the list for security
                string site = entry.Split('|')[0].Trim();
                listBoxPasswords.Items.Add(site);
            }
        }
        // This method runs when the user clicks the View Entry button
        private void btnView_Click(object sender, EventArgs e)
        {
            // Check something is selected in the list
            if (listBoxPasswords.SelectedIndex == -1)
            {
                MessageBox.Show("Please select a password entry first.");
                return;
            }
            // Ask the user to re-enter their master password before showing
            // the entry, as an extra layer of security
            using var confirmForm = new ConfirmPasswordForm();
            if (confirmForm.ShowDialog() != DialogResult.OK || !confirmForm.IsConfirmed)
                return;
            // Get the full entry for the selected item
            string entry = passwordEntries[listBoxPasswords.SelectedIndex];
            // Split it back into its three parts
            string[] parts = entry.Split('|');
            string site = parts[0].Trim();
            string username = parts.Length > 1 ? parts[1].Trim() : "";
            string password = parts.Length > 2 ? parts[2].Trim() : "";
            // Open the view password form with the decrypted details
            using (var viewForm = new ViewPasswordForm(site, username, password))
            {
                viewForm.ShowDialog();
            }
        }
        // This method runs when the user clicks the Change Master Password button
        private void btnChangePassword_Click(object sender, EventArgs e)
        {
            using var changeForm = new ChangePasswordForm();
            if (changeForm.ShowDialog() == DialogResult.OK && changeForm.NewKey != null)
            {
                // The entries are already decrypted in memory, so re-encrypting the vault
                // under the new key is one save. Without this, the vault would be unreadable
                // at the next login because it would still be encrypted under the old key.
                masterKey = changeForm.NewKey;
                SavePasswords();
                MessageBox.Show("Master password changed successfully.");
            }
        }
        // Applies the dark colour scheme to all controls on this form
        private void ApplyTheme()
        {
            this.BackColor = Color.FromArgb(30, 30, 30);
            foreach (Control c in Controls)
            {
                if (c is Label lbl)
                    lbl.ForeColor = Color.White;
                if (c is Button btn)
                {
                    btn.BackColor = Color.FromArgb(58, 58, 58);
                    btn.ForeColor = Color.White;
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderSize = 0;
                }
                if (c is TextBox tb && tb.ReadOnly)
                {
                    tb.BackColor = this.BackColor;
                    tb.ForeColor = Color.White;
                    tb.BorderStyle = BorderStyle.None;
                }
            }
        }
    }

}
