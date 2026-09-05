using System;

namespace SecureVault
{
    partial class ChangePasswordForm
    {
        // Keeps track of all components so they can be cleaned up when the form closes
        private System.ComponentModel.IContainer components = null;

        // Runs automatically when the form closes to free up memory
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            // Create all the controls that appear on the change password screen
            this.lblNewPassword = new System.Windows.Forms.Label();
            this.txtNewPassword = new System.Windows.Forms.TextBox();
            this.lblConfirmPassword = new System.Windows.Forms.Label();
            this.txtConfirmPassword = new System.Windows.Forms.TextBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();

            // Pause layout while we set everything up
            this.SuspendLayout();

            // ---- New Password Label ----
            // The static text that says "New Password:" on the left side
            this.lblNewPassword.AutoSize = true;
            this.lblNewPassword.Location = new System.Drawing.Point(30, 30);
            this.lblNewPassword.Name = "lblNewPassword";
            this.lblNewPassword.Size = new System.Drawing.Size(98, 16);
            this.lblNewPassword.TabIndex = 0;
            this.lblNewPassword.Text = "New Password:";

            // ---- Confirm Password Label ----
            // The static text that says "Confirm Password:" on the left side
            this.lblConfirmPassword.AutoSize = true;
            this.lblConfirmPassword.Location = new System.Drawing.Point(30, 70);
            this.lblConfirmPassword.Name = "lblConfirmPassword";
            this.lblConfirmPassword.Size = new System.Drawing.Size(115, 16);
            this.lblConfirmPassword.TabIndex = 2;
            this.lblConfirmPassword.Text = "Confirm Password:";

            // ---- Old Password TextBox ----
            // Where the user types their current master password to verify identity
            this.txtOldPassword = new System.Windows.Forms.TextBox();
            this.txtOldPassword.Location = new System.Drawing.Point(150, 30);
            this.txtOldPassword.Name = "txtOldPassword";
            this.txtOldPassword.Size = new System.Drawing.Size(200, 20);
            this.txtOldPassword.UseSystemPasswordChar = true;

            // ---- New Password TextBox ----
            // Where the user types their desired new master password
            this.txtNewPassword = new System.Windows.Forms.TextBox();
            this.txtNewPassword.Location = new System.Drawing.Point(150, 70);
            this.txtNewPassword.Name = "txtNewPassword";
            this.txtNewPassword.Size = new System.Drawing.Size(200, 20);
            this.txtNewPassword.UseSystemPasswordChar = true;
            // ---- Confirm Password TextBox ----
            // Where the user types their new password a second time to confirm
            // Both new password fields must match before the change is accepted
            this.txtConfirmPassword = new System.Windows.Forms.TextBox();
            this.txtConfirmPassword.Location = new System.Drawing.Point(150, 20);
            this.txtConfirmPassword.Name = "txtConfirmPassword";
            this.txtConfirmPassword.Size = new System.Drawing.Size(200, 20);
            this.txtConfirmPassword.UseSystemPasswordChar = true;

            // ---- Save Button ----
            // Clicking this validates all fields and saves the new master password
            this.btnSave.Location = new System.Drawing.Point(160, 110);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(80, 28);
            this.btnSave.TabIndex = 4;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;

            // Wire up the click event to the btnOK_Click handler in the main file
            this.btnSave.Click += new System.EventHandler(this.btnOK_Click);

            // ---- Cancel Button ----
            // Clicking this closes the form without making any changes
            this.btnCancel.Location = new System.Drawing.Point(260, 110);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(80, 28);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;

            // Close the form directly when cancel is clicked
            this.btnCancel.Click += (s, e) => this.Close();

            // ---- Form Settings ----
            // AcceptButton means pressing Enter triggers the Save button
            this.AcceptButton = this.btnSave;
            this.CancelButton = this.btnCancel;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;

            // Set the overall size of the change password window
            this.ClientSize = new System.Drawing.Size(380, 170);

            // Add all controls to the form so they appear on screen
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.txtConfirmPassword);
            this.Controls.Add(this.lblConfirmPassword);
            this.Controls.Add(this.txtNewPassword);
            this.Controls.Add(this.lblNewPassword);
            this.Name = "ChangePasswordForm";
            this.Text = "Change Master Password";

            // Resume the layout engine now everything is configured
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        // Declare all controls as private fields so they can be
        // accessed from anywhere in the ChangePasswordForm class
        private System.Windows.Forms.Label lblNewPassword;
        private System.Windows.Forms.TextBox txtNewPassword;
        private System.Windows.Forms.Label lblConfirmPassword;
        private System.Windows.Forms.TextBox txtConfirmPassword;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.TextBox txtOldPassword;
    }
}
