using System;
using System.Windows.Forms;

namespace SecureVault
{
    static class Program
    {
        // STAThread tells Windows this application uses a single UI thread
        // This is required for all Windows Forms applications to work correctly
        [STAThread]
        static void Main()
        {
            // Makes buttons, textboxes and other controls look like
            // a modern Windows application rather than the old Windows XP style
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Make sure the folder the vault files live in exists before any
            // form tries to read or write them. If that fails there is no
            // point going further, so say why and stop.
            try
            {
                DataPaths.Initialise();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "SecureVault could not set up its data folder:\n" +
                    DataPaths.DataDirectory + "\n\n" + ex.Message,
                    "SecureVault", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Show the login screen first before anything else
            // The using keyword ensures the form is removed from memory
            // properly once it is no longer needed
            using (var loginForm = new LoginForm())
            {
                // ShowDialog opens the form and pauses here until it closes
                // DialogResult.OK means the user logged in successfully
                if (loginForm.ShowDialog() == DialogResult.OK)
                {
                    // Login was successful so open the main password vault
                    // We pass the encryption key that was derived from the
                    // master password during login so the vault can decrypt entries
                    Application.Run(new MainForm(loginForm.DerivedKey));
                }
                else
                {
                    // User closed or cancelled the login screen
                    // so close the application entirely
                    Application.Exit();
                }
            }
        }
    }
}
