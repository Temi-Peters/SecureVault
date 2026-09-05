using System;
using System.IO;
using System.Windows.Forms;

namespace SecureVault
{
    public partial class ConfirmPasswordForm : Form
    {
        // This property tells the calling form whether the user
        // successfully confirmed their master password
        // It starts as false and is only set to true if verification succeeds
        public bool IsConfirmed { get; private set; } = false;
        public ConfirmPasswordForm()
        {
            // Set up all the buttons and textboxes from the designer file
            InitializeComponent();

            // Apply the dark colour scheme
            ApplyTheme();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            // Get whatever the user typed in the password box
            string enteredPassword = txtPassword.Text;

            // Do not proceed if the password box is empty
            if (string.IsNullOrWhiteSpace(enteredPassword))
            {
                MessageBox.Show("Password cannot be blank.");
                return;
            }

            // Try to load the stored master password hash from disk
            if (PasswordUtils.LoadMasterHash(out byte[] salt, out byte[] storedHash))
            {
                // Check if the entered password matches the stored hash
                if (PasswordUtils.VerifyPassword(enteredPassword, salt, storedHash))
                {
                    // Password is correct so mark as confirmed
                    IsConfirmed = true;

                    // Set result to OK so the calling form knows verification passed
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    // Wrong password so clear the box and let them try again
                    MessageBox.Show("Incorrect master password.");
                    txtPassword.Clear();
                }
            }
            else
            {
                // master.dat does not exist which should not normally happen
                // since the user had to log in to get here
                MessageBox.Show("No master password set yet.");
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            // User cancelled so mark as not confirmed
            IsConfirmed = false;

            // Set result to Cancel so the calling form knows to abort
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
