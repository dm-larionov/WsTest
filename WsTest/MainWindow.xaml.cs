using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WsTest
{
	public class VideoInfo
	{
		public string Codec { get; private set; }
		public int Width { get; private set; }
		public int Heigth { get; private set; }
		public double FrameRate { get; private set; }
		public string FrameRateMode { get; private set; }
		public string ScanType { get; private set; }
		public TimeSpan Duration { get; private set; }
		public int Bitrate { get; private set; }
		public string AspectRatioMode { get; private set; }
		public double AspectRatio { get; private set; }

		public VideoInfo(MediaInfo mi)
		{
			Codec = mi.Get(StreamKind.Video, 0, "Format");
			Width = int.Parse(mi.Get(StreamKind.Video, 0, "Width"));
			Heigth = int.Parse(mi.Get(StreamKind.Video, 0, "Height"));
			Duration = TimeSpan.FromMilliseconds(int.Parse(mi.Get(StreamKind.Video, 0, "Duration")));
			Bitrate = int.Parse(mi.Get(StreamKind.Video, 0, "BitRate"));
			AspectRatioMode = mi.Get(StreamKind.Video, 0, "AspectRatio/String"); //as formatted string
			AspectRatio = double.Parse(mi.Get(StreamKind.Video, 0, "AspectRatio").Replace('.', ','));
			FrameRate = double.Parse(mi.Get(StreamKind.Video, 0, "FrameRate").Replace('.', ','));
			FrameRateMode = mi.Get(StreamKind.Video, 0, "FrameRate_Mode");
			ScanType = mi.Get(StreamKind.Video, 0, "ScanType");
		}
	}

	public class AudioInfo
	{
		public string Codec { get; private set; }
		public string CompressionMode { get; private set; }
		public string ChannelPositions { get; private set; }
		public TimeSpan Duration { get; private set; }
		public int Bitrate { get; private set; }
		public string BitrateMode { get; private set; }
		public int SamplingRate { get; private set; }

		public AudioInfo(MediaInfo mi)
		{
			Codec = mi.Get(StreamKind.Audio, 0, "Format");
			Duration = TimeSpan.FromMilliseconds(int.Parse(mi.Get(StreamKind.Audio, 0, "Duration")));
			Bitrate = int.Parse(mi.Get(StreamKind.Audio, 0, "BitRate"));
			BitrateMode = mi.Get(StreamKind.Audio, 0, "BitRate_Mode");
			CompressionMode = mi.Get(StreamKind.Audio, 0, "Compression_Mode");
			ChannelPositions = mi.Get(StreamKind.Audio, 0, "ChannelPositions");
			SamplingRate = int.Parse(mi.Get(StreamKind.Audio, 0, "SamplingRate"));
		}
	}

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private string[] VideoFormats =
		{
			"avi", "mp4", "wmv", "mov", "mkv"
		};

		private string[] VideoCodecs =
		{
			"avi", "h264", "h265", "quicktime"
		};


		private string FIO = null;
		private string TranslitFIO = null;
		private string SelectedFolder = null;
		private string SelectedPath = null;


		private string RootFolderName = null;
		private string AepName = null;
		private string RenderFolderName = null;
		private string RenderName = null;
		private string RenderFormats = null;
		private string RenderCount = null;
		private string RenderSizes = null;
		private string RenderDuration = null;
		private string RenderFramerate = null;
		private string ScenaryFolderName = null;
		private string ScenaryName = null;

		public MainWindow()
		{
			InitializeComponent();
			//InfoButton.Source = new BitmapImage(new Uri("./pic/info_icon.png", UriKind.Relative));
			string path = "conf.txt";
			if (File.Exists(path))
			{
				// асинхронное чтение
				using (StreamReader reader = new StreamReader(path))
				{
					RootFolderName = reader.ReadLine();
					AepName = reader.ReadLine();
					RenderFolderName = reader.ReadLine();
					RenderName = reader.ReadLine();
					RenderFormats = reader.ReadLine();
					RenderCount = reader.ReadLine();
					RenderSizes = reader.ReadLine();
					RenderDuration = reader.ReadLine();
					RenderFramerate = reader.ReadLine();
					ScenaryFolderName = reader.ReadLine();
					ScenaryName = reader.ReadLine();
				}

				ProjectName_TB.Text = RootFolderName;
				AepName_TB.Text = AepName;
				RenderFolderName_TB.Text = RenderFolderName;
				RenderNames_TB.Text = RenderName;
				RenderFormats_TB.Text = RenderFormats;
				RenderCount_TB.Text = RenderCount;
				RenderSizes_TB.Text = RenderSizes;
				RenderDuration_TB.Text = RenderDuration;
				RenderFramerate_TB.Text = RenderFramerate;
				ScenaryFolderName_TB.Text = ScenaryFolderName;
				ScenaryName_TB.Text = ScenaryName;
			}
		}

		private bool CheckMetadata(string filePath)
		{
			var fileName = filePath.Split('\\').Last();

			var isValidVideo = false;
			foreach (var format in VideoFormats)
			{
				if (fileName.EndsWith(format))
				{
					isValidVideo = true;
					break;
				}
			}

			if (isValidVideo == false)
				return false;

			var mi = new MediaInfo();
			mi.Open(@$"{filePath}");
			var videoInfo = new VideoInfo(mi);
			mi.Close();

			var videoSize = $"{videoInfo.Width}x{videoInfo.Heigth}";
			var validCodecs = RenderFormats_TB.Text
				.Replace("quicktime", "MOV")
				.Replace("h264", "AVC")
				.Replace("h265", "HEVC")
				.Split(" ");

			var videoCodec = videoInfo.Codec
				.Replace("ProRes", "quicktime")
				.Replace("AVC", "h264")
				.Replace("HEVC", "h265");

			var validName = RenderNames_TB.Text
				.Replace("{resolution}", videoSize)
				.Replace("{codec}", videoCodec);

			var isValidName = Path.GetFileNameWithoutExtension(fileName) == validName;
			var isValidFormat = RenderFormats_TB.Text.Split(' ').Contains(videoCodec);
			var isValidDuration = videoInfo.Duration.TotalSeconds == TimeSpan.Parse(RenderDuration_TB.Text).TotalSeconds;
			var isValidSize = RenderSizes_TB.Text.Split(' ').Contains(videoSize);
			var isValidFramerate = int.Parse(RenderFramerate_TB.Text) == int.Parse(videoInfo.FrameRate.ToString());

			if (isValidName == false)
			{
				ResultRenderName_LB.Content = "Неверно";
				ResultRenderName_LB.Background = new SolidColorBrush(Colors.Red);
			}

			if (isValidFormat == false)
			{

				ResultRenderFormatsName_LB.Content = "Неверно";
				ResultRenderFormatsName_LB.Background = new SolidColorBrush(Colors.Red);
			}

			if (isValidDuration == false)
			{
				ResultRenderDuration_LB.Content = "Неверно";
				ResultRenderDuration_LB.Background = new SolidColorBrush(Colors.Red);
			}

			if (isValidSize == false)
			{
				ResultRenderResolution_LB.Content = "Неверно";
				ResultRenderResolution_LB.Background = new SolidColorBrush(Colors.Red);
			}

			if (isValidFramerate == false)
			{
				ResultRenderDuration_LB.Content = "Неверно";
				ResultRenderDuration_LB.Background = new SolidColorBrush(Colors.Red);
			}

			if (isValidName && isValidFormat && isValidDuration && isValidSize && isValidFramerate)
				return true;
			else
				return false;
		}



		private void CheckWork_Click(object sender, RoutedEventArgs e)
		{
			ResultRootFolderName_LB.Content = "";
			ResultRootFolderName_LB.Background = new SolidColorBrush(Colors.Transparent);

			ResultProjectName_LB.Content = "";
			ResultProjectName_LB.Background = new SolidColorBrush(Colors.Transparent);

			ResultRenderFolderName_LB.Content = "";
			ResultRenderFolderName_LB.Background = new SolidColorBrush(Colors.Transparent);

			ResultRenderName_LB.Content = "";
			ResultRenderName_LB.Background = new SolidColorBrush(Colors.Transparent);

			ResultRenderFormatsName_LB.Content = "";
			ResultRenderFormatsName_LB.Background = new SolidColorBrush(Colors.Transparent);

			ResultRenderCount_LB.Content = "";
			ResultRenderCount_LB.Background = new SolidColorBrush(Colors.Transparent);

			ResultRenderDuration_LB.Content = "";
			ResultRenderDuration_LB.Background = new SolidColorBrush(Colors.Transparent);

			ResultRenderResolution_LB.Content = "";
			ResultRenderResolution_LB.Background = new SolidColorBrush(Colors.Transparent);

			ResultRenderFramerate_LB.Content = "";
			ResultRenderFramerate_LB.Background = new SolidColorBrush(Colors.Transparent);

			if (string.IsNullOrWhiteSpace(SelectedPath))
			{
				System.Windows.MessageBox.Show("Выберите проект для проверки", "Внимание!", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			if (string.IsNullOrWhiteSpace(RootFolderName)
				|| string.IsNullOrWhiteSpace(AepName)
				|| string.IsNullOrWhiteSpace(RenderFolderName)
				|| string.IsNullOrWhiteSpace(RenderName)
				|| string.IsNullOrWhiteSpace(RenderFormats)
				|| string.IsNullOrWhiteSpace(RenderCount)
				|| string.IsNullOrWhiteSpace(RenderSizes)
				|| string.IsNullOrWhiteSpace(RenderDuration)
				|| string.IsNullOrWhiteSpace(RenderFramerate)
				|| string.IsNullOrWhiteSpace(ScenaryFolderName)
				|| string.IsNullOrWhiteSpace(ScenaryName)
				|| string.IsNullOrWhiteSpace(ScenaryFolderName))
			{
				System.Windows.MessageBox.Show("Заполните все поля для проверки", "Внимание!", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			var files = Directory.GetFiles(SelectedPath);
			var folders = Directory.GetDirectories(SelectedPath);

			if (SelectedFolder == RootFolderName.Replace("{FIO}", TranslitFIO))
			{
				ResultRootFolderName_LB.Content = "Верно";
				ResultRootFolderName_LB.Background = new SolidColorBrush(Colors.Green);
			}
			else
			{
				ResultRootFolderName_LB.Content = "Неверно";
				ResultRootFolderName_LB.Background = new SolidColorBrush(Colors.Red);
			}

			var aepName = Path.GetFileNameWithoutExtension(files.FirstOrDefault(x => x.EndsWith(".aep")));
			if (AepName == aepName)
			{
				ResultProjectName_LB.Content = "Верно";
				ResultProjectName_LB.Background = new SolidColorBrush(Colors.Green);
			}
			else
			{
				ResultProjectName_LB.Content = "Неверно";
				ResultProjectName_LB.Background = new SolidColorBrush(Colors.Red);
			}

			if (folders.FirstOrDefault(x => x.Split("\\").Last() == RenderFolderName) != null)
			{
				ResultRenderFolderName_LB.Content = "Верно";
				ResultRenderFolderName_LB.Background = new SolidColorBrush(Colors.Green);

				var renderFolderFiles = Directory.GetFiles($@"{SelectedPath}\{RenderFolderName}");
				var renderFolderFolders = Directory.GetDirectories($@"{SelectedPath}\{RenderFolderName}");

				if (renderFolderFiles.Length == int.Parse(RenderCount_TB.Text)
					&& renderFolderFolders.Length == 0)
				{
					ResultRenderCount_LB.Content = "Верно";
					ResultRenderCount_LB.Background = new SolidColorBrush(Colors.Green);
				}
				else
				{
					ResultRenderCount_LB.Content = "Неверно";
					ResultRenderCount_LB.Background = new SolidColorBrush(Colors.Red);
				}

				var areRendersCorrect = true;
				foreach (var file in renderFolderFiles)
				{
					if (CheckMetadata(file) == false)
					{
						areRendersCorrect = false;
					}
				}

				if (ResultRenderName_LB.Content != "Неверно")
				{
					ResultRenderName_LB.Content = "Верно";
					ResultRenderName_LB.Background = new SolidColorBrush(Colors.Green);
				}

				if (ResultRenderFormatsName_LB.Content != "Неверно")
				{
					ResultRenderFormatsName_LB.Content = "Верно";
					ResultRenderFormatsName_LB.Background = new SolidColorBrush(Colors.Green);
				}

				if (ResultRenderDuration_LB.Content != "Неверно")
				{
					ResultRenderDuration_LB.Content = "Верно";
					ResultRenderDuration_LB.Background = new SolidColorBrush(Colors.Green);
				}

				if (ResultRenderResolution_LB.Content != "Неверно")
				{
					ResultRenderResolution_LB.Content = "Верно";
					ResultRenderResolution_LB.Background = new SolidColorBrush(Colors.Green);
				}

				if (ResultRenderFramerate_LB.Content != "Неверно")
				{
					ResultRenderFramerate_LB.Content = "Верно";
					ResultRenderFramerate_LB.Background = new SolidColorBrush(Colors.Green);
				}
			}
			else
			{
				ResultRenderFolderName_LB.Content = "Неверно";
				ResultRenderFolderName_LB.Background = new SolidColorBrush(Colors.Red);

				ResultRenderName_LB.Content = "Неверно";
				ResultRenderName_LB.Background = new SolidColorBrush(Colors.Red);

				ResultRenderFormatsName_LB.Content = "Неверно";
				ResultRenderFormatsName_LB.Background = new SolidColorBrush(Colors.Red);

				ResultRenderCount_LB.Content = "Неверно";
				ResultRenderCount_LB.Background = new SolidColorBrush(Colors.Red);

				ResultRenderDuration_LB.Content = "Неверно";
				ResultRenderDuration_LB.Background = new SolidColorBrush(Colors.Red);

				ResultRenderResolution_LB.Content = "Неверно";
				ResultRenderResolution_LB.Background = new SolidColorBrush(Colors.Red);

				ResultRenderFramerate_LB.Content = "Неверно";
				ResultRenderFramerate_LB.Background = new SolidColorBrush(Colors.Red);
			}

			if (folders.FirstOrDefault(x => x.Split("\\").Last() == ScenaryFolderName) != null)
			{
				ResultScenaryFolderName_LB.Content = "Верно";
				ResultScenaryFolderName_LB.Background = new SolidColorBrush(Colors.Green);

				var scenaryFolderFiles = Directory.GetFiles($@"{SelectedPath}\{ScenaryFolderName}");
				var scenaryFolderFolders = Directory.GetDirectories($@"{SelectedPath}\{ScenaryFolderName}");

				var scenaryFileName = scenaryFolderFiles.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x) == ScenaryName);
				if (scenaryFileName != null
					&& scenaryFolderFolders.Length == 0)
				{
					ResultScenaryName_LB.Content = "Верно";
					ResultScenaryName_LB.Background = new SolidColorBrush(Colors.Green);
				}
				else
				{
					ResultScenaryName_LB.Content = "Неверно";
					ResultScenaryName_LB.Background = new SolidColorBrush(Colors.Red);
				}
			}
			else
			{
				ResultScenaryFolderName_LB.Content = "Неверно";
				ResultScenaryFolderName_LB.Background = new SolidColorBrush(Colors.Red);

				ResultScenaryName_LB.Content = "Неверно";
				ResultScenaryName_LB.Background = new SolidColorBrush(Colors.Red);
			}
		}

		private void OpenFolder_Click(object sender, RoutedEventArgs e)
		{
			using var dialog = new FolderBrowserDialog
			{
				Description = "Выберите работу для проверки",
				UseDescriptionForTitle = true,
				ShowNewFolderButton = true
			};

			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				SelectedPath = dialog.SelectedPath;
				SelectedFolder = SelectedPath.Split('\\').Last();
				Path_LB.Content = SelectedFolder;
				FIO_TB.Text = "";
			}
		}

		private void ProjectName_TB_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			RootFolderName = ProjectName_TB.Text;
		}

		private void AepName_TB_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			AepName = AepName_TB.Text;
		}

		private void RenderFolderName_TB_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			RenderFolderName = RenderFolderName_TB.Text;
		}

		private void RenderNames_TB_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			RenderName = RenderNames_TB.Text;
		}

		private void RenderFormats_TB_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			RenderFormats = RenderFormats_TB.Text;
		}

		private void RenderCount_TB_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			RenderCount = RenderCount_TB.Text;
		}

		private void RenderDuration_TB_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			RenderDuration = RenderDuration_TB.Text;
		}

		private void RenderSizes_TB_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			RenderSizes = RenderSizes_TB.Text;
		}

		private void RenderFramerate_TB_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			RenderFramerate = RenderFramerate_TB.Text;
		}

		private void ScenaryFolderName_TB_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			ScenaryFolderName = ScenaryFolderName_TB.Text;
		}

		private void ScenaryName_TB_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			ScenaryName = ScenaryName_TB.Text;
		}

		private void SaveSettings_Click(object sender, RoutedEventArgs e)
		{
			using (TextWriter tw = new StreamWriter("conf.txt"))
			{
				tw.Flush();
				tw.WriteLine(RootFolderName);
				tw.WriteLine(AepName);
				tw.WriteLine(RenderFolderName);
				tw.WriteLine(RenderName);
				tw.WriteLine(RenderFormats);
				tw.WriteLine(RenderCount);
				tw.WriteLine(RenderSizes);
				tw.WriteLine(RenderDuration);
				tw.WriteLine(RenderFramerate);
				tw.WriteLine(ScenaryFolderName);
				tw.WriteLine(ScenaryName);
			}
		}

		private void FIO_TB_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			FIO = FIO_TB.Text;

			var transliter = new TranslitMethods.Translitter();
			TranslitFIO = transliter.Translit(FIO, TranslitMethods.TranslitType.Iso);
			TranslitFIO_LB.Content = TranslitFIO;
		}

		private void InfoButton_Click(object sender, RoutedEventArgs e)
		{
			new InfoWindow().ShowDialog();
		}
	}
}
