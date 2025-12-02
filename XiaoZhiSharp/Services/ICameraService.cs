using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XiaoZhiSharp.Services
{
    public interface ICameraService
    {
        /// <summary>
        /// Take pictures and obtain image data
        /// </summary>
        Task<byte[]?> CapturePhotoAsync();
    }
}
