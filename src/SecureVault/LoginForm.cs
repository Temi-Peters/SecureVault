using System;
using System.Windows.Forms;

namespace SecureVault
{
    public partial class LoginForm : Form
    {
        public byte[] DerivedKey { get; private set; }

        public LoginForm()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string password = txtPassword.Text?.Trim();

            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter your master password.");
                return;
            }

            // Check lockout first
            int wait = LockoutManager.GetRemainingLockoutSeconds();
            if (wait > 0)
            {
                MessageBox.Show($"Too many failed attempts. Try again in {wait} seconds.");
                return;
            }

            byte[] salt, storedHash;

            if (!PasswordUtils.LoadMasterHash(out salt, out storedHash))
            {
                // first run: create master password
                string reason;
                if (!SecurityPolicy.ValidatePasswordComplexity(password, out reason))
                {
                    MessageBox.Show("Password policy: " + reason);
                    return;
                }

                salt = PasswordUtils.GenerateSalt();
                storedHash = PasswordUtils.HashPassword(password, salt);
                PasswordUtils.SaveMasterHash(salt, storedHash);
                MessageBox.Show("Master password created. Please log in with it.");
                // Do not auto-login: require the user to re-enter for confirmation
                return;
            }

            // Verify provided password
            if (!PasswordUtils.VerifyPassword(password, salt, storedHash))
            {
                // record failed attempt and show message (with optional remaining wait)
                LockoutManager.RegisterFailedAttempt();
                int nextWait = LockoutManager.GetRemainingLockoutSeconds();
                if (nextWait > 0)
                {
                    MessageBox.Show("Incorrect password. Further attempts are blocked for " + nextWait + " seconds.");
                }
                else
                {
                    MessageBox.Show("Incorrect password.");
                }
                txtPassword.Clear();
                return;
            }

            // Success: reset lockout
            LockoutManager.Reset();

            // Derive session key and proceed
            DerivedKey = PasswordUtils.DeriveEncryptionKey(password, salt);
            DialogResult = DialogResult.OK;
            Close();
        }

    }
}
