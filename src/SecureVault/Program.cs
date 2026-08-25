using System;
using System.Windows.Forms;

namespace SecureVault
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Show login form first
            using (var loginForm = new LoginForm())
            {
                if (loginForm.ShowDialog() == DialogResult.OK)
                {
                    // If login succeeds, pass derived key to MainForm
                    Application.Run(new MainForm(loginForm.DerivedKey));
                }
                else
                {
                    // Exit if user cancels login
                    Application.Exit();
                }
            }
        }
    }
}
