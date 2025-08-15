using System.Configuration;
using System.Data;
using System.Windows;
using System;

namespace Ledon.Berry.Shell
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		protected override void OnExit(ExitEventArgs e)
		{
			// 应用退出：尝试停止 API 进程
			ApiHost.Instance.TryStop();
			base.OnExit(e);
		}
	}
}

