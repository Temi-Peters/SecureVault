// This is the login screen. It handles two situations:
// 1. First time the app is opened - creates a new master password
// 2. Every time after that - verifies the entered password against the stored hash

using System;
using System.Windows.Forms;

namespace SecureVault
{
    public partial class LoginForm : Form
    {
        // This property holds the encryption key after a successful login
        // It is passed to MainForm so the vault can encrypt and decrypt entries
        // It is stored in memory only and never written to disk
        public byte[]? DerivedKey { get; private set; }

        public LoginForm()
        {
            // Set up all the buttons, textboxes and labels defined in the designer file
            InitializeComponent();

            // Apply the dark colour scheme to this form
            ApplyTheme();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            // Get whatever the user typed in the password box
            // Trim removes any accidental spaces at the start or end
            // The ?? operator means if the result is null use an empty string instead
            string password = txtPassword.Text?.Trim() ?? string.Empty;

            // Do not proceed if the password box is empty
            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter your master password.");
                return;
            }

            // Check if the user is currently locked out before doing anything else
            // There is no point processing the password if they have to wait anyway
            int wait = LockoutManager.GetRemainingLockoutSeconds();
            if (wait > 0)
            {
                MessageBox.Show($"Too many failed attempts. Try again in {wait} seconds.");
                return;
            }

            // These will hold the salt and hash loaded from master.dat
            if (!PasswordUtils.LoadMaster(out byte[] salt, out byte[] storedVerifier,
                    out int iterations, out bool legacy))
            {
                // First run: create the master password
                if (!SecurityPolicy.ValidatePasswordComplexity(password, out string reason))
                {
                    MessageBox.Show("Password policy: " + reason);
                    return;
                }
                salt = PasswordUtils.GenerateSalt();
                byte[] newKey = PasswordUtils.DeriveEncryptionKey(password, salt, PasswordUtils.DefaultIterations);
                PasswordUtils.SaveMaster(salt, PasswordUtils.ComputeVerifier(newKey), PasswordUtils.DefaultIterations);
                MessageBox.Show("Master password created. Please log in again.");
                txtPassword.Clear();
                return;
            }

            byte[]? key = PasswordUtils.VerifyAndDeriveKey(password, salt, storedVerifier, iterations, legacy);
            if (key == null)
            {
                LockoutManager.RegisterFailedAttempt();
                int nextWait = LockoutManager.GetRemainingLockoutSeconds();
                if (nextWait > 0)
                {
                    MessageBox.Show($"Incorrect password. Try again in {nextWait} seconds.");
                }
                else
                {
                    MessageBox.Show("Incorrect password.");
                }
                txtPassword.Clear();
                return;
            }

            // A version 1 file stored the key itself. Now that the password is proven,
            // rewrite it in the version 2 layout so the key is no longer on disk.
            if (legacy)
            {
                PasswordUtils.SaveMaster(salt, PasswordUtils.ComputeVerifier(key), iterations);
            }

            LockoutManager.Reset();

            // Derive the encryption key from the password and salt
            // This key is used to encrypt and decrypt vault entries
            // It is kept in memory only for the duration of this session
            DerivedKey = key;

            // Set the result to OK so Program.cs knows login succeeded
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ApplyTheme()
        {
            // Set the background of the whole form to a dark grey colour
            // The numbers represent Red, Green, Blue values (0-255)
            this.BackColor = Color.FromArgb(30, 30, 30);

            // Loop through every control on the form and style it
            foreach (Control c in Controls)
            {
                // Make all labels white so they are visible on the dark background
                if (c is Label lbl)
                    lbl.ForeColor = Color.White;
                // Style buttons to match the dark theme
                if (c is Button btn)
                {
                    btn.BackColor = Color.FromArgb(58, 58, 58);
                    btn.ForeColor = Color.White;
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderSize = 0;
                }

                // Style read-only textboxes to blend into the dark background
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
