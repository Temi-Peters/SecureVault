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
