// <copyright file="HardwareDetector.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    using System;
    using System.Management;

    public class HardwareCapabilities
    {
        public bool HasNvidiaGPU { get; set; }
        public bool HasAmdGPU { get; set; }
        public bool HasIntelGPU { get; set; }
        public string NvidiaGpuModel { get; set; }
        public string AmdGpuModel { get; set; }
        public string IntelGpuModel { get; set; }
        public long NvidiaGpuMemory { get; set; }
        public long AmdGpuMemory { get; set; }
        public long IntelGpuMemory { get; set; }
        public bool SupportsNvenc { get; set; }
        public bool SupportsAmf { get; set; }
        public bool SupportsQsv { get; set; }
        public int CpuCoreCount { get; set; }
        public long SystemMemory { get; set; }

        public bool HasHardwareAcceleration =>
            SupportsNvenc || SupportsAmf || SupportsQsv;
    }

    public static class HardwareDetector
    {
        private static HardwareCapabilities cachedCapabilities;

        public static HardwareCapabilities Detect()
        {
            if (cachedCapabilities != null)
            {
                return cachedCapabilities;
            }

            var capabilities = new HardwareCapabilities();

            try
            {
                // 检测 GPU
                DetectGpus(capabilities);

                // 检测 CPU 核心数
                capabilities.CpuCoreCount = Environment.ProcessorCount;

                // 检测系统内存
                capabilities.SystemMemory = GetSystemMemory();

                // 检测编码支持
                DetectEncodingSupport(capabilities);

                cachedCapabilities = capabilities;
            }
            catch (Exception ex)
            {
                Diagnostics.Debug.Log($"Hardware detection failed: {ex.Message}");
            }

            return capabilities;
        }

        private static void DetectGpus(HardwareCapabilities capabilities)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string name = obj["Name"]?.ToString() ?? string.Empty;
                        long memory = 0;

                        if (obj["AdapterRAM"] != null)
                        {
                            try
                            {
                                memory = Convert.ToInt64(obj["AdapterRAM"]);
                            }
                            catch (FormatException)
                            {
                                Diagnostics.Debug.Log($"Warning: Could not parse GPU memory for '{name}'");
                                memory = 0;
                            }
                        }

                        // 跳过无效的显卡名称
                        if (string.IsNullOrWhiteSpace(name) || name == "Microsoft Basic Display Adapter")
                        {
                            continue;
                        }

                        // 检查 NVIDIA GPU
                        if (name.IndexOf("nvidia", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("geforce", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("rtx", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("gtx", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("gt", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("quadro", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("tesla", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            capabilities.HasNvidiaGPU = true;
                            capabilities.NvidiaGpuModel = name;
                            capabilities.NvidiaGpuMemory = memory;
                            Diagnostics.Debug.Log($"Detected NVIDIA GPU: {name} ({memory / 1024 / 1024}MB)");
                        }
                        // 检查 AMD GPU
                        else if (name.IndexOf("amd", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 name.IndexOf("radeon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 name.IndexOf("rx", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 name.IndexOf("vega", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 name.IndexOf("fury", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 name.IndexOf("r9", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 name.IndexOf("r7", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 name.IndexOf("r5", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 name.IndexOf("wx", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            capabilities.HasAmdGPU = true;
                            capabilities.AmdGpuModel = name;
                            capabilities.AmdGpuMemory = memory;
                            Diagnostics.Debug.Log($"Detected AMD GPU: {name} ({memory / 1024 / 1024}MB)");
                        }
                        // 检查 Intel GPU
                        else if (name.IndexOf("intel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 name.IndexOf("hd", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 name.IndexOf("uhd", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 name.IndexOf("iris", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 name.IndexOf("xe", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            capabilities.HasIntelGPU = true;
                            capabilities.IntelGpuModel = name;
                            capabilities.IntelGpuMemory = memory;
                            Diagnostics.Debug.Log($"Detected Intel GPU: {name} ({memory / 1024 / 1024}MB)");
                        }
                    }
                }

                // 如果没有检测到独立显卡，检查集成显卡
                if (!capabilities.HasNvidiaGPU && !capabilities.HasAmdGPU && !capabilities.HasIntelGPU)
                {
                    Diagnostics.Debug.Log("No dedicated GPU detected, checking for integrated graphics...");
                }
            }
            catch (Exception ex)
            {
                Diagnostics.Debug.Log($"GPU detection failed: {ex.Message}");
            }
        }

        private static void DetectEncodingSupport(HardwareCapabilities capabilities)
        {
            // 检测 NVENC 支持（NVIDIA 编码器）
            if (capabilities.HasNvidiaGPU)
            {
                // 检查 GPU 型号是否支持 NVENC
                // NVIDIA 从 Kepler 架构开始支持 NVENC（2012年后发布的GPU）
                string model = capabilities.NvidiaGpuModel.ToLower();
                
                // 简单的型号检查
                if (model.Contains("gt") || model.Contains("gtx") || 
                    model.Contains("rtx") || model.Contains("quadro") ||
                    model.Contains("tesla"))
                {
                    capabilities.SupportsNvenc = true;
                    Diagnostics.Debug.Log($"Hardware encoding support detected: NVENC (NVIDIA)");
                }
                else
                {
                    Diagnostics.Debug.Log($"Hardware encoding support not detected: NVENC not supported by this NVIDIA GPU ({capabilities.NvidiaGpuModel})");
                }
            }

            // 检测 AMF 支持（AMD 编码器）
            if (capabilities.HasAmdGPU)
            {
                // AMD 从 GCN 架构开始支持 AMF（2012年后的Radeon显卡）
                string model = capabilities.AmdGpuModel.ToLower();
                
                // 检查是否是支持 AMF 的 AMD 显卡
                if (model.Contains("radeon") || 
                    model.Contains("rx") || 
                    model.Contains("vega") || 
                    model.Contains("fury") ||
                    model.Contains("r9") ||
                    model.Contains("r7") ||
                    model.Contains("r5") ||
                    model.Contains("hd") ||
                    model.Contains("wx"))
                {
                    capabilities.SupportsAmf = true;
                    Diagnostics.Debug.Log($"Hardware encoding support detected: AMF (AMD)");
                }
                else
                {
                    Diagnostics.Debug.Log($"Hardware encoding support not detected: AMF not supported by this AMD GPU ({capabilities.AmdGpuModel})");
                }
            }

            // 检测 QSV 支持（Intel Quick Sync Video）
            if (capabilities.HasIntelGPU)
            {
                // Intel 从 Sandy Bridge 开始支持 Quick Sync
                string model = capabilities.IntelGpuModel.ToLower();
                
                if (model.Contains("hd") || model.Contains("iris") || model.Contains("xe"))
                {
                    capabilities.SupportsQsv = true;
                    Diagnostics.Debug.Log($"Hardware encoding support detected: QSV (Intel Quick Sync Video)");
                }
                else
                {
                    Diagnostics.Debug.Log($"Hardware encoding support not detected: QSV not supported by this Intel GPU ({capabilities.IntelGpuModel})");
                }
            }
        }

        private static long GetSystemMemory()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        if (obj["TotalPhysicalMemory"] != null)
                        {
                            return Convert.ToInt64(obj["TotalPhysicalMemory"]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Diagnostics.Debug.Log($"Memory detection failed: {ex.Message}");
            }

            return 0;
        }

        public static bool ShouldUseHardwareAcceleration(HardwareCapabilities caps, string outputType)
        {
            if (!caps.HasHardwareAcceleration)
            {
                return false;
            }

            // 对于视频输出，优先使用硬件加速
            if (outputType == "Mp4" || outputType == "Mkv" || 
                outputType == "Webm" || outputType == "Avi")
            {
                return true;
            }

            return false;
        }

        public static string GetRecommendedHardwareEncoder(HardwareCapabilities caps)
        {
            if (caps.SupportsNvenc)
            {
                return "h264_nvenc";
            }
            else if (caps.SupportsAmf)
            {
                return "h264_amf";
            }
            else if (caps.SupportsQsv)
            {
                return "h264_qsv";
            }

            return "libx264";
        }

        public static Helpers.HardwareAccelerationMode GetRecommendedHardwareAccelerationMode(HardwareCapabilities caps)
        {
            if (caps.SupportsNvenc)
            {
                return Helpers.HardwareAccelerationMode.CUDA;
            }
            else if (caps.SupportsAmf)
            {
                return Helpers.HardwareAccelerationMode.AMF;
            }
            else if (caps.SupportsQsv)
            {
                return Helpers.HardwareAccelerationMode.QSV;
            }

            return Helpers.HardwareAccelerationMode.Off;
        }
    }
}
