// This form shows the details of a single selected password entry
// It includes the site name, username and the password itself
// The password is hidden by default and can only be revealed temporarily
// It also handles copying the password to the clipboard and clearing it automatically

using System;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace SecureVault
{
    public partial class ViewPasswordForm : Form
    {
        // The actual password stored here in memory only
        // It is never written to disk from this form
        private readonly string password;
        // Timer that automatically clears the clipboard 15 seconds
        // after the password is copied
        private Timer clipboardTimer;

        // Stores what we copied to the clipboard so we can check
        // it is still there before clearing it
        private string? clipboardValue;

        // Tracks whether the password is currently visible or hidden
        private bool passwordVisible = false;

        // Timer that automatically hides the password 10 seconds
        // after the user clicks Show
        private Timer revealTimer;

        public ViewPasswordForm(string site, string username, string password)
        {
            // Set up all the labels and buttons from the designer file
            InitializeComponent();

            // Fill in the site and username labels with the actual values
            lblSite.Text = site;
            lblUsername.Text = username;

            // Put the password in the textbox but hide it behind dots
            // ReadOnly means the user cannot type in or modify this box
            // ShortcutsEnabled = false prevents copying via keyboard shortcuts
            // TabStop = false means pressing Tab skips over this box
            txtPassword.Text = password;
            txtPassword.ReadOnly = true;
            txtPassword.UseSystemPasswordChar = true;
            txtPassword.ShortcutsEnabled = false;
            txtPassword.TabStop = false;

            // Store the password so we can use it for copying later
            this.password = password;

            // Show a message in the security label at the bottom
            lblSecurityStatus.Text = "Clipboard clears in 10s";

            // Set up the clipboard clear timer
            // It does not start until the user copies a password
            clipboardTimer = new Timer();
            clipboardTimer.Interval = 15_000; // 15 seconds
            clipboardTimer.Tick += ClipboardTimer_Tick;

            // Set up the reveal timer
            // It does not start until the user clicks Show
            revealTimer = new Timer();
            revealTimer.Interval = 10_000; // 10 seconds
            revealTimer.Tick += RevealTimer_Tick;

            // Apply the dark colour scheme
            ApplyTheme();
        }
        // This method runs when the user clicks the Show or Hide button
        private void btnToggleVisibility_Click(object sender, EventArgs e)
        {
            // Flip the visibility state
            passwordVisible = !passwordVisible;

            // If passwordVisible is true show the actual characters
            // If false show dots instead
            txtPassword.UseSystemPasswordChar = !passwordVisible;

            // Update the button text to reflect the current state
            btnToggleVisibility.Text = passwordVisible ? "Hide" : "Show";

            if (passwordVisible)
            {
                // Restart the reveal timer so the user always gets
                // a full 10 seconds from the last time they clicked Show
                revealTimer.Stop();
                revealTimer.Start();
            }
        }

        // This method runs when the reveal timer fires after 10 seconds
        private void RevealTimer_Tick(object? sender, EventArgs e)
        {
            // Hide the password again automatically
            txtPassword.UseSystemPasswordChar = true;
            btnToggleVisibility.Text = "Show";
            passwordVisible = false;
            revealTimer.Stop();
        }

        // This method runs when the user clicks the Copy Password button
        private void btnCopyPassword_Click(object sender, EventArgs e)
        {
            // Copy the password to the Windows clipboard
            Clipboard.SetText(password);

            // Store what we copied so we can verify it is still there
            // before clearing it 15 seconds later
            clipboardValue = password;

            // Reset and start the clipboard clear timer
            clipboardTimer.Stop();
            clipboardTimer.Start();

            // Tell the user the password has been copied and will be cleared
            MessageBox.Show(
                "Password copied to clipboard.\nIt will be cleared automatically.",
                "Security Notice",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // This method runs when the clipboard timer fires after 15 seconds
        private void ClipboardTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                // Only clear the clipboard if it still contains what we copied
                // The user might have copied something else since then
                // and we do not want to clear that
                if (!string.IsNullOrEmpty(clipboardValue) &&
                    Clipboard.ContainsText() &&
                    Clipboard.GetText() == clipboardValue)
                {
                    Clipboard.Clear();
                }
            }
            catch
            {
                // The clipboard can be locked by Windows or another application
                // If that happens we just move on rather than crashing
            }
            finally
            {
                // Always clear our stored reference and stop the timer
                // regardless of whether the clipboard clear succeeded
                clipboardValue = null;
                clipboardTimer.Stop();
            }
        }
        // This method runs when the user clicks the Close button
        private void btnClose_Click(object sender, EventArgs e)
        {
            // Run the security cleanup before closing
            SecureCleanup();
            Close();
        }

        // Clears all sensitive data from the form before it closes
        private void SecureCleanup()
        {
            // Clear the password textbox so nothing remains in the UI
            txtPassword.Text = string.Empty;
            passwordVisible = false;

            // Stop both timers so no callbacks fire after the form is gone
            revealTimer.Stop();
            clipboardTimer.Stop();
        }

        // This runs automatically if the user clicks on another window
        // while this form is open
        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);

            // Hide the password immediately when the user switches away
            // This prevents the password being left visible on an unattended screen
            txtPassword.UseSystemPasswordChar = true;
            btnToggleVisibility.Text = "Show";
            passwordVisible = false;
            revealTimer.Stop();
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
