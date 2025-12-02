using XiaoZhiSharp.Utils;
using NAudio.Wave;
using OpusSharp.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XiaoZhiSharp.Services
{
    public class AudioWaveService : IAudioService, IDisposable
    {
        // NAudio audio output related components
        private IWavePlayer? _waveOut;
        private BufferedWaveProvider? _waveOutProvider = null;
        // NAudio audio input related components
        private WaveInEvent? _waveIn;

        public event IAudioService.PcmAudioEventHandler? OnPcmAudioEvent;
        // Audio parameters
        public int SampleRate { get; set; } = Global.SampleRate_WaveOut;
        public int SampleRate_WaveIn { get; set; } = Global.SampleRate_WaveIn;
        public int Bitrate { get; set; } = 16;
        public int Channels { get; set; } = 1;
        public int FrameDuration { get; set; } = 60;
        public int FrameSize
        {
            get
            {
                return SampleRate * FrameDuration / 1000; // Frame size
            }
        }
        public bool IsPlaying { get; private set; }
        public bool IsRecording { get; private set; } = false;
        public int VadCounter { get; private set; } = 0; // Counters for voice activity detection
        public AudioWaveService()
        {
            Initialize();
        }
        public void Initialize()
        {
            // Initialize audio output related components
            var waveFormat = new WaveFormat(SampleRate, Bitrate, Channels);
            _waveOut = new WaveOutEvent();
            _waveOutProvider = new BufferedWaveProvider(waveFormat);
            _waveOut.Init(_waveOutProvider);
            // Increase the buffer size, for example, to 10 seconds of audio data.
            _waveOutProvider.BufferLength = SampleRate * Channels * 2 * 10;

            // Initialize audio input components
            _waveIn = new WaveInEvent();
            _waveIn.WaveFormat = new WaveFormat(48000, Bitrate, Channels);
            //_waveIn.WaveFormat = new WaveFormat(SampleRate, Bitrate, Channels);
            _waveIn.DataAvailable += waveIn_DataAvailable;
            _waveIn.RecordingStopped += waveIn_RecordingStopped;

            // Start audio playback thread
            Thread threadWave = new Thread(() =>
            {
                while (true)
                {
                    if (!IsPlaying)
                    {
                        if (_waveOutProvider.BufferedDuration > TimeSpan.FromSeconds(1))
                        {
                            StartPlaying();
                        }
                    }
                    while (IsPlaying)
                    {
                        // More logic can be added, such as buffer checks, etc.
                        Thread.Sleep(10);
                    }
                    StopPlaying();
                }
            });
            threadWave.Start();
        }
        public void StartRecording()
        {
            if (_waveIn != null)
            {
                if (!IsRecording)
                {
                    _waveIn.StartRecording();
                    IsRecording = true;
                    VadCounter = 0;
                    //LogConsole.WriteLine("Start recording");
                }
            }
        }
        public void StopRecording()
        {
            if (_waveIn != null)
            {
                if (IsRecording)
                {
                    _waveIn.StopRecording();
                    //LogConsole.WriteLine("End of recording");
                    IsRecording = false;
                    VadCounter = 0;
                }
            }
        }
        /// <summary>
        /// Noise level detection
        /// </summary>
        private bool IsAudioMute(byte[] buffer, int bytesRecorded)
        {
            double rms = 0;
            int sampleCount = bytesRecorded / 2; // 2 bytes per sample

            for (int i = 0; i < sampleCount; i++)
            {
                short sample = BitConverter.ToInt16(buffer, i * 2);
                rms += sample * sample;
            }

            rms = Math.Sqrt(rms / sampleCount);
            rms /= short.MaxValue; // Normalize to the range of 0 - 1

            double MuteThreshold = 0.01; // Silence threshold
            return rms < MuteThreshold;
        }
        private void waveIn_RecordingStopped(object? sender, StoppedEventArgs e)
        {
        }
        private float[] ConvertBytesToFloats(byte[] byteData)
        {
            int sampleCount = byteData.Length / 2; // Assuming it's 16-bit audio
            float[] floatData = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                short sample = BitConverter.ToInt16(byteData, i * 2);
                floatData[i] = sample / (float)short.MaxValue;
            }

            return floatData;
        }
        private void waveIn_DataAvailable(object? sender, WaveInEventArgs e)
        {
            Task.Run(() =>
            {
                byte[] pcmBytes48000 = e.Buffer;
                if (!IsAudioMute(pcmBytes48000, e.BytesRecorded))
                {
                    if(Global.IsDebug)
                        Console.Title = "Recording-" + VadCounter;
                    byte[] pcmBytes = ConvertPcmSampleRate(pcmBytes48000, 48000, SampleRate_WaveIn, Channels, Bitrate);

                    if (OnPcmAudioEvent != null)
                    {
                        OnPcmAudioEvent(pcmBytes);
                    }
                }
                else
                {
                    VadCounter ++;
                    if (Global.IsDebug)
                        Console.Title = "Mute-" + VadCounter;
                }
            });
        }
        private byte[] ConvertPcmSampleRate(byte[] pcmData, int originalSampleRate, int targetSampleRate, int channels, int bitsPerSample)
        {
            // Create raw audio format
            WaveFormat originalFormat = new WaveFormat(originalSampleRate, bitsPerSample, channels);

            // Wrap byte[] data into a MemoryStream
            using (MemoryStream memoryStream = new MemoryStream(pcmData))
            {
                // Create raw audio stream
                using (RawSourceWaveStream originalStream = new RawSourceWaveStream(memoryStream, originalFormat))
                {
                    // Create target audio format
                    WaveFormat targetFormat = new WaveFormat(targetSampleRate, bitsPerSample, channels);

                    // Resampling
                    using (MediaFoundationResampler resampler = new MediaFoundationResampler(originalStream, targetFormat))
                    {
                        resampler.ResamplerQuality = 60; // Set resampling quality

                        // Calculate the approximate length of the resampled data
                        long estimatedLength = (long)(pcmData.Length * (double)targetSampleRate / originalSampleRate);
                        byte[] resampledData = new byte[estimatedLength];

                        int totalBytesRead = 0;
                        int bytesRead;
                        byte[] buffer = new byte[resampler.WaveFormat.AverageBytesPerSecond];
                        while ((bytesRead = resampler.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            Array.Copy(buffer, 0, resampledData, totalBytesRead, bytesRead);
                            totalBytesRead += bytesRead;
                        }

                        // Adjust the array length to the actual number of bytes read.
                        Array.Resize(ref resampledData, totalBytesRead);

                        return resampledData;
                    }
                }
            }
        }
        public void StartPlaying()
        {
            if (!IsPlaying)
            {
                _waveOut?.Play();
                IsPlaying = true;
            }
        }
        public void StopPlaying()
        {
            if (IsPlaying)
            {
                _waveOut?.Stop();
                IsPlaying = false;
            }
        }
        public void AddOutSamples(byte[] pcmData)
        {
            if (_waveOutProvider != null)
            {
                // Add sample data
                _waveOutProvider.AddSamples(pcmData, 0, pcmData.Length);
            }
        }
        public void AddOutSamples(float[] pcmData)
        {
            if (_waveOutProvider != null)
            {
                byte[] byteAudioData = FloatArrayToByteArray(pcmData);

                // Check the available space in the buffer.
                while (_waveOutProvider.BufferedBytes + byteAudioData.Length > _waveOutProvider.BufferLength)
                {
                    // Wait a while to allow the buffer to have enough space.
                    System.Threading.Thread.Sleep(10);
                }

                // Add sample data
                _waveOutProvider.AddSamples(byteAudioData, 0, byteAudioData.Length);
            }
        }
        private static byte[] FloatArrayToByteArray(float[] floatArray)
        {
            // Initialize a byte array twice the length of the float array, since each short occupies 2 bytes.
            byte[] byteArray = new byte[floatArray.Length * 2];

            for (int i = 0; i < floatArray.Length; i++)
            {
                // Map float values ​​to short ranges
                short sample = (short)(floatArray[i] * short.MaxValue);

                // Split the short value into two bytes
                byteArray[i * 2] = (byte)(sample & 0xFF);
                byteArray[i * 2 + 1] = (byte)(sample >> 8);
            }

            return byteArray;
        }
        private static float[] ByteArrayToFloatArray(byte[] byteArray)
        {
            // Check if the length of the byte array is a multiple of 4
            if (byteArray.Length % 4 != 0)
            {
                throw new ArgumentException("The length of the byte array must be a multiple of 4.");
            }

            // Calculate the length of an array of floating point numbers
            int floatCount = byteArray.Length / 4;
            float[] floatArray = new float[floatCount];

            // Loop through the byte array, taking 4 bytes each time and converting them into a floating point number
            for (int i = 0; i < floatCount; i++)
            {
                floatArray[i] = BitConverter.ToSingle(byteArray, i * 4);
            }

            return floatArray;
        }
        public void Dispose()
        {
            IsPlaying = false;
            IsRecording = false;
            _waveIn?.Dispose();
            _waveOut?.Dispose();
        }

    }
}
