using System;
using System.Windows.Forms;

namespace SecureVault
{
    public partial class ChangePasswordForm : Form
    {
        public ChangePasswordForm()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            string oldPass = txtOldPassword.Text;
            string newPass = txtNewPassword.Text?.Trim();
            string confirmPass = txtConfirmPassword.Text;

            if (string.IsNullOrWhiteSpace(oldPass) ||
                string.IsNullOrWhiteSpace(newPass) ||
                string.IsNullOrWhiteSpace(confirmPass))
            {
                MessageBox.Show("All fields are required.");
                return;
            }
            string reason;
            if (!SecurityPolicy.ValidatePasswordComplexity(newPass, out reason))
            {
                MessageBox.Show("New password does not meet complexity rules: " + reason);
                return;
            }

            if (newPass != confirmPass)
            {
                MessageBox.Show("New passwords do not match.");
                return;
            }

            // Load the stored master hash
            if (!PasswordUtils.LoadMasterHash(out byte[] salt, out byte[] storedHash))
            {
                MessageBox.Show("No existing master password found.");
                return;
            }

            // Verify old password
            if (!PasswordUtils.VerifyPassword(oldPass, salt, storedHash))
            {
                MessageBox.Show("Old password is incorrect.");
                return;
            }

            // Create new hash + salt
            byte[] newSalt = PasswordUtils.GenerateSalt();
            byte[] newHash = PasswordUtils.HashPassword(newPass, newSalt);

            PasswordUtils.SaveMasterHash(newSalt, newHash);

            MessageBox.Show("Master password successfully changed.");
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
