using OpusSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using XiaoZhiSharp.Utils;

namespace XiaoZhiSharp.Services
{
    public class AudioOpusService
    {
        // Opus Related components
        private OpusDecoder opusDecoder;   // decoder
        private OpusEncoder opusEncoder;   // encoder
        private readonly object _lock = new();
        private int _currentSampleRate;
        private int _currentChannels;

        /// <summary>
        /// encoder
        /// </summary>
        /// <param name="pcmData"></param>
        /// <param name="sampleRate"></param>
        /// <param name="channels"></param>
        /// <param name="frameDuration"></param>
        /// <param name="bitrate"></param>
        /// <returns></returns>
        public byte[] Encode(byte[] pcmData, int sampleRate=24000, int channels=1, int frameDuration = 60, int bitrate = 16)
        {
            lock (_lock)
            {
                if (opusEncoder == null || _currentSampleRate != sampleRate || _currentChannels != channels)
                {
                    opusEncoder?.Dispose();
                    opusEncoder = new OpusEncoder(sampleRate, channels, OpusPredefinedValues.OPUS_APPLICATION_VOIP);
                    _currentSampleRate = sampleRate;
                    _currentChannels = channels;
                }

                try
                {
                    // Calculate the frame size (number of samples, not the number of bytes).
                    int frameSize = sampleRate * frameDuration / 1000; // Default 60ms frame

                    // Ensure the input data length is correct (16-bit audio = 2 bytes/sample)
                    int expectedBytes = frameSize * channels * 2;

                    if (pcmData.Length != expectedBytes)
                    {
                        // Adjust data length or pad with zeros
                        byte[] adjustedData = new byte[expectedBytes];
                        if (pcmData.Length < expectedBytes)
                        {
                            // Insufficient data, copy existing data and fill with zeros
                            Array.Copy(pcmData, adjustedData, pcmData.Length);
                        }
                        else
                        {
                            // Too much data, truncate
                            Array.Copy(pcmData, adjustedData, expectedBytes);
                        }
                        pcmData = adjustedData;
                    }

                    short[] pcmShorts = new short[frameSize * channels];
                    for (int i = 0; i < pcmShorts.Length && i * 2 + 1 < pcmData.Length; i++)
                    {
                        pcmShorts[i] = BitConverter.ToInt16(pcmData, i * 2);
                    }

                    byte[] outputBuffer = new byte[4000]; // Opus maximum package size
                    int encodedLength = opusEncoder.Encode(pcmShorts, frameSize, outputBuffer, outputBuffer.Length);

                    byte[] result = new byte[encodedLength];
                    Array.Copy(outputBuffer, result, encodedLength);
                    return result;
                }
                catch (Exception ex)
                {
                    //LogConsole.WarningLine($"Opus encoding failed: {ex.Message}");
                    return Array.Empty<byte>();
                }
            }
        }

        /// <summary>
        /// 解码器
        /// </summary>
        /// <param name="opusData"></param>
        /// <param name="sampleRate"></param>
        /// <param name="channels"></param>
        /// <param name="frameDuration"></param>
        /// <param name="bitrate"></param>
        /// <returns></returns>
        public byte[] Decode(byte[] opusData, int sampleRate=24000, int channels=1, int frameDuration = 60, int bitrate = 16)
        {
            lock (_lock)
            {
                if (opusDecoder == null || _currentSampleRate != sampleRate || _currentChannels != channels)
                {
                    opusDecoder?.Dispose();
                    opusDecoder = new OpusDecoder(sampleRate, channels);
                    _currentSampleRate = sampleRate;
                    _currentChannels = channels;
                }
                try
                {
                    // Calculate the frame size (number of samples, not the number of bytes).
                    int frameSize = sampleRate * frameDuration / 1000; // Default 60ms frame
                    short[] pcmShorts = new short[frameSize * channels];
                    int decodedSamples = opusDecoder.Decode(opusData, opusData.Length, pcmShorts, frameSize, false);
                    if (decodedSamples <= 0)
                        return Array.Empty<byte>();
                    byte[] pcmData = new byte[decodedSamples * 2 * channels];
                    for (int i = 0; i < decodedSamples * channels; i++)
                    {
                        byte[] bytes = BitConverter.GetBytes(pcmShorts[i]);
                        Array.Copy(bytes, 0, pcmData, i * 2, 2);
                    }
                    return pcmData;
                }
                catch (Exception ex)
                {
                    //LogConsole.WarningLine($"Opus decoding failed: {ex.Message}");
                    return Array.Empty<byte>();
                }
            }
        }

        /// <summary>
        /// Converts PCM data to a float array
        /// </summary>
        /// <param name="byteArray"></param>
        /// <returns></returns>
        public float[] ConvertByteToFloatPcm(byte[] byteArray)
        {
            int byteLength = byteArray.Length;
            int floatLength = byteLength / 2;
            float[] floatArray = new float[floatLength];

            for (int i = 0; i < floatLength; i++)
            {
                // Read two bytes from a byte array and convert them to short type
                short sample = BitConverter.ToInt16(byteArray, i * 2);
                // Convert a short value to a float type, with a range of [-1, 1]
                floatArray[i] = sample / 32768.0f;
            }

            return floatArray;
        }
        /// <summary>
        /// Convert a float array to PCM data
        /// </summary>
        /// <param name="floatArray"></param>
        /// <returns></returns>
        public byte[] ConvertFloatToBytePcm(float[] floatArray)
        {
            int floatLength = floatArray.Length;
            byte[] byteArray = new byte[floatLength * 2];

            for (int i = 0; i < floatLength; i++)
            {
                // Convert a float value to a short value
                short sample = (short)(floatArray[i] * 32767);
                // Convert a short value to two bytes
                byte[] bytes = BitConverter.GetBytes(sample);
                // Store the two bytes in a byte array
                bytes.CopyTo(byteArray, i * 2);
            }

            return byteArray;
        }
    }
}
