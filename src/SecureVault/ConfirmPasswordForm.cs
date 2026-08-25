using System;
using System.IO;
using System.Windows.Forms;

namespace SecureVault
{
    public partial class ConfirmPasswordForm : Form
    {
        public bool IsConfirmed { get; private set; } = false;

        public ConfirmPasswordForm()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            string enteredPassword = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(enteredPassword))
            {
                MessageBox.Show("Password cannot be blank.");
                return;
            }

            if (PasswordUtils.LoadMasterHash(out byte[] salt, out byte[] storedHash))
            {
                if (PasswordUtils.VerifyPassword(enteredPassword, salt, storedHash))
                {
                    IsConfirmed = true;
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    MessageBox.Show("Incorrect master password.");
                    txtPassword.Clear();
                }
            }
            else
            {
                MessageBox.Show("No master password set yet.");
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            IsConfirmed = false;
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
