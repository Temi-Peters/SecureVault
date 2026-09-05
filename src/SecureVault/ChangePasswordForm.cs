using System;
using System.Windows.Forms;

namespace SecureVault
{
    public partial class ChangePasswordForm : Form
    {
        // Set on success so the vault can be re-encrypted under the new key
        public byte[]? NewKey { get; private set; }

        public ChangePasswordForm()
        {
            // Set up all the buttons and textboxes from the designer file
            InitializeComponent();

            // Apply the dark colour scheme
            ApplyTheme();
        }
        private void btnOK_Click(object sender, EventArgs e)
        {
            // Get whatever the user typed in each field
            string oldPass = txtOldPassword.Text;

            // Trim removes any accidental spaces at the start or end
            string newPass = txtNewPassword.Text?.Trim();
            string confirmPass = txtConfirmPassword.Text;

            // Make sure none of the fields are left empty
            if (string.IsNullOrWhiteSpace(oldPass) ||
                string.IsNullOrWhiteSpace(newPass) ||
                string.IsNullOrWhiteSpace(confirmPass))
            {
                MessageBox.Show("All fields are required.");
                return;
            }

            // Check the new password meets the complexity rules
            // before doing anything else
            string reason;
            if (!SecurityPolicy.ValidatePasswordComplexity(newPass, out reason))
            {
                MessageBox.Show("New password does not meet complexity rules: " + reason);
                return;
            }

            // Make sure the new password and confirmation match
            if (newPass != confirmPass)
            {
                MessageBox.Show("New passwords do not match.");
                return;
            }

            // Try to load the currently stored master password hash from disk
            if (!PasswordUtils.LoadMaster(out byte[] salt, out byte[] storedVerifier, out int iterations, out bool legacy))
            {
                MessageBox.Show("No existing master password found.");
                return;
            }

            // Verify the old password is correct before allowing the change
            // This prevents someone changing the password on an unlocked device
            if (PasswordUtils.VerifyAndDeriveKey(oldPass, salt, storedVerifier, iterations, legacy) == null)
            {
                MessageBox.Show("Old password is incorrect.");
                return;
            }

            // Generate a brand new random salt for the new password
            // Never reuse the old salt even if it would be valid
            byte[] newSalt = PasswordUtils.GenerateSalt();

            // Hash the new password with the new salt
            byte[] newKey = PasswordUtils.DeriveEncryptionKey(newPass, newSalt, PasswordUtils.DefaultIterations);
            // Save the new salt and hash to disk, overwriting the old ones
            PasswordUtils.SaveMaster(newSalt, PasswordUtils.ComputeVerifier(newKey), PasswordUtils.DefaultIterations);
            NewKey = newKey;

            MessageBox.Show("Master password successfully changed.");

            // Set the result to OK so the calling form knows it succeeded
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            // User cancelled so set result to Cancel and close
            DialogResult = DialogResult.Cancel;
            Close();
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
