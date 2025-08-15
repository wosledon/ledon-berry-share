using Microsoft.Web.WebView2.Wpf;
using Microsoft.Web.WebView2.Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Ledon.Berry.Shell
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 先尝试启动 API
            if (!ApiHost.Instance.TryStart(out var apiError))
            {
                MessageBox.Show($"后端 API 启动失败: {apiError}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            try
            {
                await webView.EnsureCoreWebView2Async();
                var apiPort = ApiHost.Instance.ListeningPort;
                if (apiPort is > 0)
                {
                    webView.Source = new Uri($"http://127.0.0.1:{apiPort}/");
                }
                else
                {
                    MessageBox.Show("未取得 API 端口，无法加载前端。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化前端失败: {ex.Message}", "异常", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // 允许用户选择是否关闭 API (此处直接关闭)
            ApiHost.Instance.TryStop();
        }
    }
}