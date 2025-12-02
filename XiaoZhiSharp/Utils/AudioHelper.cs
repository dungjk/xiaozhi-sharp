using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XiaoZhiSharp.Utils
{
    public class AudioHelper
    {
        /// <summary>
        /// Noise level detection
        /// </summary>
        public static bool IsAudioMute(byte[] buffer, int bytesRecorded)
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
    }
}
