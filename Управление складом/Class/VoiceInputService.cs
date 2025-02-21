using System;
using Vosk;
using NAudio.Wave;

namespace Управление_складом.Class
{
	public class VoiceInputService : IDisposable
	{
		private readonly object syncLock = new object();
		private Model modelRu;
		private WaveInEvent waveIn;
		private VoskRecognizer recognizerRu;
		private bool stopRequested;

		public bool IsRunning { get; private set; }

		// Событие, возвращающее распознанный текст
		public event Action<string> TextRecognized;

		public VoiceInputService(Model modelRu)
		{
			this.modelRu = modelRu ?? throw new ArgumentNullException(nameof(modelRu));
		}

		public void Start()
		{
			lock (syncLock)
			{
				if (IsRunning)
					return;
				stopRequested = false;
				IsRunning = true;

				waveIn = new WaveInEvent { WaveFormat = new WaveFormat(16000, 1) };
				recognizerRu = new VoskRecognizer(modelRu, 16000.0f);

				waveIn.DataAvailable += WaveIn_DataAvailable;
				waveIn.StartRecording();
			}
		}

		private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
		{
			bool needStop = false;
			string recognizedText = null;
			lock (syncLock)
			{
				if (stopRequested || recognizerRu == null)
					return;
				try
				{
					// Если получен окончательный результат, то отмечаем, что нужно остановить запись
					if (recognizerRu.AcceptWaveform(e.Buffer, e.BytesRecorded))
					{
						string json = recognizerRu.Result();
						recognizedText = ExtractTextFromJson(json);
						needStop = true;
					}
				}
				catch
				{
					needStop = true;
				}
			}
			if (needStop)
			{
				if (!string.IsNullOrWhiteSpace(recognizedText))
				{
					TextRecognized?.Invoke(recognizedText);
				}
				Stop();
			}
		}

		private string ExtractTextFromJson(string json)
		{
			const string pattern = "\"text\" : \"";
			int idx = json.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
			if (idx < 0)
				return "";
			idx += pattern.Length;
			int end = json.IndexOf("\"", idx);
			if (end < 0)
				return "";
			return json.Substring(idx, end - idx).Trim();
		}

		public void Stop()
		{
			lock (syncLock)
			{
				if (!IsRunning)
					return;
				stopRequested = true;

				if (waveIn != null)
				{
					try { waveIn.DataAvailable -= WaveIn_DataAvailable; waveIn.StopRecording(); }
					catch { }
					finally { waveIn.Dispose(); waveIn = null; }
				}
				if (recognizerRu != null)
				{
					try { recognizerRu.Dispose(); }
					catch { }
					finally { recognizerRu = null; }
				}
				IsRunning = false;
			}
		}

		public void Dispose()
		{
			Stop();
		}
	}
}
