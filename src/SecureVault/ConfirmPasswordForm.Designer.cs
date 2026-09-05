using System;

namespace SecureVault
{
    partial class ConfirmPasswordForm
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
            // Create all the controls that appear on the confirm password screen
            label1 = new Label();
            txtPassword = new TextBox();
            btnCancel = new Button();
            btnOK = new Button();

            // Pause layout while we set everything up
            SuspendLayout();

            // ---- Instruction Label ----
            // The text that tells the user what to do
            label1.AutoSize = true;
            label1.Location = new Point(108, 45);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(126, 15);
            label1.TabIndex = 0;
            label1.Text = "Enter Master Password";

            // ---- Password TextBox ----
            // Where the user types their master password to confirm their identity
            // UseSystemPasswordChar = true means it shows dots instead of letters
            txtPassword.Location = new Point(90, 80);
            txtPassword.Margin = new Padding(4, 3, 4, 3);
            txtPassword.Name = "txtPassword";
            txtPassword.Size = new Size(171, 23);
            txtPassword.TabIndex = 1;
            txtPassword.UseSystemPasswordChar = true;

            // ---- Cancel Button ----
            // Clicking this closes the form without confirming
            // and sets IsConfirmed to false in the main file
            btnCancel.Location = new Point(193, 121);
            btnCancel.Margin = new Padding(4, 3, 4, 3);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(68, 27);
            btnCancel.TabIndex = 4;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;

            // ---- OK Button ----
            // Clicking this verifies the entered password against the stored hash
            // If correct it sets IsConfirmed to true and closes the form
            btnOK.Location = new Point(90, 121);
            btnOK.Margin = new Padding(4, 3, 4, 3);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(56, 27);
            btnOK.TabIndex = 5;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;

            // ---- Form Settings ----
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;

            // Set the overall size of the confirm password window
            ClientSize = new Size(360, 201);

            // Add all controls to the form so they appear on screen
            Controls.Add(btnOK);
            Controls.Add(btnCancel);
            Controls.Add(txtPassword);
            Controls.Add(label1);
            Margin = new Padding(4, 3, 4, 3);
            Name = "ConfirmPasswordForm";
            Text = "Confirm Master Password";

            // Resume the layout engine now everything is configured
            ResumeLayout(false);
            PerformLayout();
        }

        // Declare all controls as private fields so they can be
        // accessed from anywhere in the ConfirmPasswordForm class
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOK;
    }
}
