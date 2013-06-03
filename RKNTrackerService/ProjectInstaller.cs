using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;

namespace RKNTrackerService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
			this.AfterInstall += new InstallEventHandler(ServiceInstaller_AfterInstall);
        }

		void ServiceInstaller_AfterInstall(object sender, InstallEventArgs e)
		{
			ServiceController sc = new ServiceController(this.RKNTrackerInstaller.ServiceName);
			sc.Start();
		}

		public override void Install(IDictionary stateServer)
		{
			Microsoft.Win32.RegistryKey system, currentControlSet, services, service, config;

			try
			{
				base.Install(stateServer);

				system = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("System");
				currentControlSet = system.OpenSubKey("CurrentControlSet");
				services = currentControlSet.OpenSubKey("Services");
				service = services.OpenSubKey(this.RKNTrackerInstaller.ServiceName, true);
				service.SetValue("Description", "Tracks your Anarchy Online characters so that the locations can be viewed on Rubi-Ka Network and in The Leet");
				config = service.CreateSubKey("Parameters");
			}
			catch (Exception e)
			{
				Console.WriteLine("An exception was thrown during service installation:\n" + e.ToString());
			}
		}

		public override void Uninstall(IDictionary stateServer)
		{
			Microsoft.Win32.RegistryKey system, currentControlSet, services, service;

			try
			{
				system = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("System");
				currentControlSet = system.OpenSubKey("CurrentControlSet");
				services = currentControlSet.OpenSubKey("Services");
				service = services.OpenSubKey(this.RKNTrackerInstaller.ServiceName, true);
				service.DeleteSubKeyTree("Parameters");
			}
			catch (Exception e)
			{
				Console.WriteLine("Exception encountered while uninstalling service:\n" + e.ToString());
			}
			finally
			{
				//Let the project installer do its job
				base.Uninstall(stateServer);
			}
		}
	}
}
