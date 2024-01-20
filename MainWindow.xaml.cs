using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PrompReader {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>

	public static partial class Arg {
		public static string[] argv;
	}

	public partial class MainWindow : Window {

		


		public MainWindow() {
			InitializeComponent();

			TextBoxParameters.PreviewDragOver += (sender, args) => {
				bool res = args.Data.GetDataPresent(DataFormats.FileDrop);
				args.Effects = (res ? DragDropEffects.Copy : DragDropEffects.None);
				// これを忘れずに
				args.Handled = true;
			};

			if (Arg.argv.Length != 0) {
				string? parameters = ReadPngParameters(Arg.argv);
				if (parameters != null) {
					TextBoxParameters.Text = parameters;
				}
			}

			TextBoxParameters.PreviewDrop += (sender, args) => {
				if (!args.Data.GetDataPresent(DataFormats.FileDrop)) { return; }

				string[] fileNames = (string[])args.Data.GetData(DataFormats.FileDrop);

				string? parameters = ReadPngParameters(fileNames);
				if (parameters != null) {
					TextBoxParameters.Text = parameters;
				}
			};
		}
		
		private string? ReadPngParameters(string[] ListFilePath) {
			/*
			 https://www.setsuki.com/hsp/ext/png.htm
			 https://hoshi-sano.hatenablog.com/entry/2013/08/18/112550
			 https://ja.wikipedia.org/wiki/Portable_Network_Graphics
			 https://edom18.hateblo.jp/entry/2022/02/14/082055
			 */
			if ((ListFilePath == null) || (ListFilePath.Length < 0)) { return null; }
			string filePath = ListFilePath[0];
			string key = "parameters";
			using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read)) {
				byte[] buffer = new byte[8];
				fs.Read(buffer, 0, 8); // Read PNG header

				while (fs.Position < fs.Length) {
					// Read chunk length (4 bytes)
					buffer = new byte[4];
					fs.Read(buffer, 0, 4);
					Array.Reverse(buffer);
					int chunkLength = BitConverter.ToInt32(buffer, 0);

					// Read chunk type (4 bytes)
					buffer = new byte[4];
					fs.Read(buffer, 0, 4);
					string chunkType = Encoding.ASCII.GetString(buffer);

					if (chunkType == "tEXt") {
						buffer = new byte[chunkLength];
						fs.Read(buffer, 0, chunkLength);

						string textData = Encoding.ASCII.GetString(buffer);
						if (textData.StartsWith(key)) {
							return textData.Substring(key.Length).TrimStart('\0');
						}
					}

					else if (chunkType == "iTXt") {
						buffer = new byte[chunkLength];
						fs.Read(buffer, 0, chunkLength);
						string? res = ParseiTXtChunk(buffer, key);
						if (res != null) { return res; }
                    }


					else {
						// Skip chunk data and CRC
						fs.Seek(chunkLength + 4, SeekOrigin.Current);
					}
				}
			}
			return null;
		}


		static string? ParseiTXtChunk(byte[] data, string key) {
			int index = 0;
			string keyword = GetString(data, ref index);
			if(keyword != key) { return null; }
			byte compressionFlag = data[index++];
			byte compressionMethod = data[index++];
			string languageTag = GetString(data, ref index);
			string translatedKeyword = GetString(data, ref index);
			string text = GetString(data, ref index);

			Debug.WriteLine("Keyword: " + keyword);
			Debug.WriteLine("Compression Flag: " + compressionFlag);
			Debug.WriteLine("Compression Method: " + compressionMethod);
			Debug.WriteLine("Language Tag: " + languageTag);
			Debug.WriteLine("Translated Keyword: " + translatedKeyword);
			Debug.WriteLine("Text: " + text);
			return text;
		}

		static string GetString(byte[] data, ref int index) {
			int start = index;
			while ((data[index++] != 0)&& (index<data.Length)) ;
			return Encoding.UTF8.GetString(data, start, index - start - 1);
		}
	}


}
