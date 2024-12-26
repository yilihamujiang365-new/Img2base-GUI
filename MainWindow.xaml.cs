using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using System.Management;

namespace Img2base_GUI
{
    public partial class MainWindow : Window
    {
        private BitmapImage selectedImage; // 存储图片
        private string convertedText = ""; // 存储文本

        public MainWindow()
        {
            InitializeComponent();
            
            about();
        }
        private void about()
        {
            // 获取程序集信息
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var assemblyName = assembly.GetName();
            var softwareName = assemblyName.Name; // 软件名称
            var version = assemblyName.Version.ToString(); // 软件版本
            var developer = "Yilihamujiang365@outlook.com"; // 替换为开发者名字
            var programmingLanguage = "C#"; // 开发语言

            string osversion = GetRealOSVersion();

            // 自动生成关于窗口的信息
            MessageBox.Show(
                $"软件名称: {softwareName}\n" +
                $"版本: {version}\n" +
                $"开发者: {developer}\n" +
                $"开发语言: {programmingLanguage}\n" +
                $".NET 版本: {Environment.Version}\n" +
                $"操作系统版本: {osversion}\n" +
                $"界面类型: WPF",
                "关于本软件",
                MessageBoxButton.OK,
                MessageBoxImage.Information);     



        }


        string GetRealOSVersion()
        {
            // 示例中使用 WMI 获取真实版本
            string version = "";
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
            {
                foreach (var obj in searcher.Get())
                {
                    version = obj["Version"]?.ToString();
                    break;
                }
            }
            return version;
        }

        // 浏览按钮 - 加载图片
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // 加载图片并显示
                    selectedImage = new BitmapImage(new Uri(openFileDialog.FileName));
                    imageControl.Source = selectedImage;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"加载图片失败: {ex.Message}");
                }
            }
        }

        // 保存图片按钮
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (selectedImage == null)
            {
                MessageBox.Show("没有可保存的图片！");
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "PNG 文件|*.png"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // 保存图片到选定路径
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(selectedImage));
                    using (var stream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                    {
                        encoder.Save(stream);
                    }
                    MessageBox.Show("图片已成功保存！");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存图片失败: {ex.Message}");
                }
            }
        }


        // 转换按钮 - 自动判断图片或文本，进行相应转换
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (selectedImage != null)
            {
                // 图片转文本
                try
                {
                    byte[] imageBytes;
                    using (var memoryStream = new MemoryStream())
                    {
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(selectedImage));
                        encoder.Save(memoryStream);
                        imageBytes = memoryStream.ToArray();
                    }

                    // 根据选中的进制进行转换
                    string baseSelected = ((ComboBoxItem)baseComboBox.SelectedItem)?.Content.ToString();
                    convertedText = ConvertBytesToBase(imageBytes, baseSelected);

                    // 显示转换结果
                    richtextboxControl.Document.Blocks.Clear();
                    richtextboxControl.Document.Blocks.Add(new Paragraph(new Run(convertedText)));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"转换失败: {ex.Message}");
                }
            }
            else
            {
                // 文本转图片
                try
                {
                    TextRange textRange = new TextRange(richtextboxControl.Document.ContentStart, richtextboxControl.Document.ContentEnd);
                    string inputText = textRange.Text.Trim();

                    if (string.IsNullOrEmpty(inputText))
                    {
                        MessageBox.Show("文本框为空，无法转换！");
                        return;
                    }

                    // 自动检测文本的进制
                    int detectedBase = DetectBase(inputText);
                    byte[] imageBytes = ConvertTextToBytes(inputText, detectedBase);

                    // 转换字节数组为图片
                    using (var memoryStream = new MemoryStream(imageBytes))
                    {
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.StreamSource = memoryStream;
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.EndInit();
                        imageControl.Source = image;
                        selectedImage = image;
                    }

                    MessageBox.Show($"成功将文本转为图片！（检测为 {detectedBase} 进制）");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"转换失败: {ex.Message}");
                }
            }
        }

        // 清空照片按钮
        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            imageControl.Source = null;
            selectedImage = null;
            MessageBox.Show("照片已清空！");
        }

        // 进制选择更改事件
        private void baseComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (selectedImage != null || !string.IsNullOrEmpty(convertedText))
            {
                Button_Click_2(null, null); // 自动重新转换
            }
        }

        // 将字节数组转换为指定进制
        private string ConvertBytesToBase(byte[] bytes, string baseSelected)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in bytes)
            {
                switch (baseSelected)
                {
                    case "二进制":
                        sb.Append(Convert.ToString(b, 2).PadLeft(8, '0')).Append(" ");
                        break;
                    case "八进制":
                        sb.Append(Convert.ToString(b, 8)).Append(" ");
                        break;
                    case "十进制":
                        sb.Append(b).Append(" ");
                        break;
                    case "十六进制":
                        sb.Append(b.ToString("X2")).Append(" ");
                        break;
                }
            }
            return sb.ToString().Trim();
        }

        // 自动检测文本的进制
        private int DetectBase(string text)
        {
            text = text.Trim().Replace(" ", "");

            if (text.All(c => c == '0' || c == '1'))
                return 2; // 二进制
            if (text.All(c => c >= '0' && c <= '7'))
                return 8; // 八进制
            if (text.All(c => char.IsDigit(c)))
                return 10; // 十进制
            if (text.All(c => char.IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
                return 16; // 十六进制

            throw new Exception("无法检测文本的进制，请确保文本格式正确！");
        }

        // 将文本转换为字节数组
        private byte[] ConvertTextToBytes(string text, int detectedBase)
        {
            string[] segments = text.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return segments.Select(segment => Convert.ToByte(segment, detectedBase)).ToArray();
        }

        // 复制按钮
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            TextRange textRange = new TextRange(richtextboxControl.Document.ContentStart, richtextboxControl.Document.ContentEnd);
            string text = textRange.Text.Trim();
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show("没有可复制的内容！");
                return;
            }
            Clipboard.SetText(text);
            MessageBox.Show("内容已复制到剪贴板！");
        }

        // 保存TXT按钮
        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            TextRange textRange = new TextRange(richtextboxControl.Document.ContentStart, richtextboxControl.Document.ContentEnd);
            string text = textRange.Text.Trim();
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show("没有可保存的内容！");
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "文本文件|*.txt"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllText(saveFileDialog.FileName, text);
                MessageBox.Show("文件已保存！");
            }
        }
        // 加载TXT按钮 - 异步加载文件内容，避免卡顿
        private async void Button_Click_5(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "文本文件|*.txt"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // 异步读取文件内容
                    string fileContent = await Task.Run(() => File.ReadAllText(openFileDialog.FileName, Encoding.UTF8));

                    // 显示文件内容到富文本框中
                    richtextboxControl.Document.Blocks.Clear();
                    richtextboxControl.Document.Blocks.Add(new Paragraph(new Run(fileContent)));

                    MessageBox.Show("文件加载成功！");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"加载文件失败: {ex.Message}");
                }
            }
        }

    }
}