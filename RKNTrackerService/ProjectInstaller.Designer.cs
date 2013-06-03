namespace RKNTrackerService
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
			this.RKNTrackerProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
			this.RKNTrackerInstaller = new System.ServiceProcess.ServiceInstaller();
			// 
			// RKNTrackerProcessInstaller
			// 
			this.RKNTrackerProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
			this.RKNTrackerProcessInstaller.Password = null;
			this.RKNTrackerProcessInstaller.Username = null;
			// 
			// RKNTrackerInstaller
			// 
			this.RKNTrackerInstaller.ServiceName = "RKN Tracker";
			this.RKNTrackerInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
			// 
			// ProjectInstaller
			// 
			this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.RKNTrackerProcessInstaller,
            this.RKNTrackerInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller RKNTrackerProcessInstaller;
        private System.ServiceProcess.ServiceInstaller RKNTrackerInstaller;
    }
}