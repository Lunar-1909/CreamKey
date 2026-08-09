using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace CreamyKeysDesktop
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (HasArgument(args, "--smoke-test"))
            {
                Environment.ExitCode = SmokeTest.Run();
                return;
            }

            bool created;
            using (Mutex mutex = new Mutex(true, "CreamyKeysDesktop.SingleInstance.4ECED3E4", out created))
            {
                if (!created)
                {
                    MessageBox.Show("CreamyKeys is already running in the tray.", "CreamyKeys",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                try
                {
                    Application.Run(new MainForm());
                }
                catch (Exception ex)
                {
                    WriteCrashLog(ex);
                    MessageBox.Show("CreamyKeys could not start:\r\n" + ex.Message,
                        "CreamyKeys", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static void WriteCrashLog(Exception ex)
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "CreamyKeysDesktop");
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "crash.log"), ex.ToString(), Encoding.UTF8);
            }
            catch
            {
            }
        }

        private static bool HasArgument(string[] args, string name)
        {
            if (args == null)
            {
                return false;
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal static class SmokeTest
    {
        public static int Run()
        {
            string assetsRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
            List<PresetInfo> presets = SoundLibrary.Scan(assetsRoot);
            if (presets.Count == 0)
            {
                return 2;
            }

            using (AudioEngine audio = new AudioEngine())
            {
                if (!audio.AudioReady)
                {
                    return 3;
                }

                AppConfig config = AppConfig.Defaults(presets[0].Id);
                audio.Configure(config);
                audio.LoadPreset(presets[0].DirectoryPath);
                if (audio.SampleCount == 0)
                {
                    return 4;
                }

                if (!audio.PlayRandom())
                {
                    return 5;
                }

                Thread.Sleep(250);
            }

            return 0;
        }
    }

    public sealed class VirtualButtonConfig
    {
        public string Label { get; set; }
        public string IconText { get; set; }
        public string SoundPath { get; set; }
        public int OffsetX { get; set; }
        public int OffsetY { get; set; }

        public bool IsEmpty()
        {
            return string.IsNullOrWhiteSpace(Label) &&
                   string.IsNullOrWhiteSpace(IconText) &&
                   string.IsNullOrWhiteSpace(SoundPath) &&
                   OffsetX == 0 &&
                   OffsetY == 0;
        }
    }

    public sealed class AppConfig
    {
        public int ConfigVersion { get; set; }
        public bool Enabled { get; set; }
        public bool KeyboardSoundsEnabled { get; set; }
        public string Preset { get; set; }
        public string KeyboardLayout { get; set; }
        public string MouseStyle { get; set; }
        public int Volume { get; set; }
        public int KeyGainPercent { get; set; }
        public int RandomVolumePercent { get; set; }
        public int RandomPitchPercent { get; set; }
        public int CooldownMs { get; set; }
        public int MaxVoices { get; set; }
        public bool PlayOnRepeat { get; set; }
        public bool PlayModifiers { get; set; }
        public bool IgnoreInjectedInput { get; set; }
        public bool RunAtStartup { get; set; }
        public bool MouseSoundsEnabled { get; set; }
        public bool ShowMouse { get; set; }
        public bool EditMode { get; set; }
        public bool AutoDetectDevices { get; set; }
        public bool UseAppAllowList { get; set; }
        public List<string> AllowedProcesses { get; set; }
        public List<string> ExcludedProcesses { get; set; }
        public bool VirtualShadowEnabled { get; set; }
        public int VirtualShadowDepth { get; set; }
        public int VirtualShadowOffsetX { get; set; }
        public int VirtualShadowOffsetY { get; set; }
        public Dictionary<string, VirtualButtonConfig> ButtonOverrides { get; set; }

        public AppConfig()
        {
            AllowedProcesses = new List<string>();
            ExcludedProcesses = new List<string>();
            ButtonOverrides = new Dictionary<string, VirtualButtonConfig>(StringComparer.OrdinalIgnoreCase);
        }

        public static AppConfig Defaults(string fallbackPreset)
        {
            AppConfig config = new AppConfig();
            config.ConfigVersion = 5;
            config.Enabled = true;
            config.KeyboardSoundsEnabled = true;
            config.Preset = fallbackPreset;
            config.KeyboardLayout = "full";
            config.MouseStyle = "gaming";
            config.Volume = 70;
            config.KeyGainPercent = 180;
            config.RandomVolumePercent = 12;
            config.RandomPitchPercent = 4;
            config.CooldownMs = 0;
            config.MaxVoices = 24;
            config.PlayOnRepeat = false;
            config.PlayModifiers = false;
            config.IgnoreInjectedInput = true;
            config.RunAtStartup = true;
            config.MouseSoundsEnabled = true;
            config.ShowMouse = true;
            config.EditMode = false;
            config.AutoDetectDevices = true;
            config.UseAppAllowList = false;
            config.VirtualShadowEnabled = true;
            config.VirtualShadowDepth = 18;
            config.VirtualShadowOffsetX = 6;
            config.VirtualShadowOffsetY = 8;
            return config;
        }

        public void Normalize(string fallbackPreset)
        {
            if (ConfigVersion < 2)
            {
                KeyboardSoundsEnabled = true;
                MouseSoundsEnabled = true;
                ShowMouse = true;
            }
            if (ConfigVersion < 3)
            {
                AutoDetectDevices = true;
            }
            if (ConfigVersion < 4)
            {
                MouseSoundsEnabled = true;
                ShowMouse = true;
                RunAtStartup = true;
                CooldownMs = 0;
            }
            if (ConfigVersion < 5)
            {
                UseAppAllowList = false;
                VirtualShadowEnabled = true;
                VirtualShadowDepth = 18;
                VirtualShadowOffsetX = 6;
                VirtualShadowOffsetY = 8;
            }
            ConfigVersion = 5;

            if (string.IsNullOrWhiteSpace(Preset))
            {
                Preset = fallbackPreset;
            }
            if (string.IsNullOrWhiteSpace(KeyboardLayout))
            {
                KeyboardLayout = "full";
            }
            if (string.IsNullOrWhiteSpace(MouseStyle))
            {
                MouseStyle = "gaming";
            }

            Volume = Clamp(Volume, 0, 100);
            if (KeyGainPercent <= 0)
            {
                KeyGainPercent = 180;
            }
            KeyGainPercent = Clamp(KeyGainPercent, 50, 400);
            RandomVolumePercent = Clamp(RandomVolumePercent, 0, 50);
            RandomPitchPercent = Clamp(RandomPitchPercent, 0, 20);
            CooldownMs = Clamp(CooldownMs, 0, 80);
            MaxVoices = Clamp(MaxVoices, 4, 64);

            if (ExcludedProcesses == null)
            {
                ExcludedProcesses = new List<string>();
            }
            if (AllowedProcesses == null)
            {
                AllowedProcesses = new List<string>();
            }
            VirtualShadowDepth = Clamp(VirtualShadowDepth, 0, 48);
            VirtualShadowOffsetX = Clamp(VirtualShadowOffsetX, -32, 32);
            VirtualShadowOffsetY = Clamp(VirtualShadowOffsetY, -32, 32);
            if (ButtonOverrides == null)
            {
                ButtonOverrides = new Dictionary<string, VirtualButtonConfig>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }
            if (value > max)
            {
                return max;
            }
            return value;
        }
    }

    internal sealed class ConfigStore
    {
        private readonly string _path;
        private readonly JavaScriptSerializer _serializer;

        public ConfigStore()
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CreamyKeysDesktop");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "config.json");
            _serializer = new JavaScriptSerializer();
        }

        public string ConfigPath
        {
            get { return _path; }
        }

        public string ConfigDirectory
        {
            get { return Path.GetDirectoryName(_path); }
        }

        public AppConfig Load(string fallbackPreset)
        {
            try
            {
                if (File.Exists(_path))
                {
                    string json = File.ReadAllText(_path, Encoding.UTF8);
                    AppConfig config = _serializer.Deserialize<AppConfig>(json);
                    if (config != null)
                    {
                        config.Normalize(fallbackPreset);
                        return config;
                    }
                }
            }
            catch
            {
            }

            return AppConfig.Defaults(fallbackPreset);
        }

        public void Save(AppConfig config)
        {
            config.Normalize(config.Preset);
            Directory.CreateDirectory(ConfigDirectory);
            string json = _serializer.Serialize(config);
            File.WriteAllText(_path, json, Encoding.UTF8);
        }
    }

    internal sealed class PresetInfo
    {
        public string Id;
        public string DisplayName;
        public string DirectoryPath;
        public int Count;
    }

    internal sealed class SoundLibrary
    {
        public static List<PresetInfo> Scan(string assetsRoot)
        {
            List<PresetInfo> presets = new List<PresetInfo>();
            string keyboardRoot = Path.Combine(assetsRoot, "keyboards");
            if (!Directory.Exists(keyboardRoot))
            {
                return presets;
            }

            string[] dirs = Directory.GetDirectories(keyboardRoot);
            Array.Sort(dirs, StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < dirs.Length; i++)
            {
                string dir = dirs[i];
                string[] files = Directory.GetFiles(dir, "*.wav");
                if (files.Length == 0)
                {
                    continue;
                }

                PresetInfo preset = new PresetInfo();
                preset.Id = Path.GetFileName(dir);
                preset.DisplayName = ToDisplayName(preset.Id);
                preset.DirectoryPath = dir;
                preset.Count = files.Length;
                presets.Add(preset);
            }

            return presets;
        }

        private static string ToDisplayName(string id)
        {
            string[] parts = id.Replace('-', '_').Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> words = new List<string>();
            TextInfo textInfo = CultureInfo.InvariantCulture.TextInfo;

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].ToLowerInvariant();
                if (part == "cherrymx")
                {
                    words.Add("Cherry MX");
                }
                else if (part == "abs" || part == "pbt")
                {
                    words.Add(part.ToUpperInvariant());
                }
                else
                {
                    words.Add(textInfo.ToTitleCase(part));
                }
            }

            return string.Join(" ", words.ToArray());
        }
    }

    internal sealed class DeviceDetectionResult
    {
        public string KeyboardLayout;
        public string MouseStyle;
        public string Summary;
    }

    internal static class DeviceDetector
    {
        public static DeviceDetectionResult Detect()
        {
            List<string> keyboardNames = QueryNames("SELECT Name, Description FROM Win32_Keyboard");
            List<string> mouseNames = QueryNames("SELECT Name, Description FROM Win32_PointingDevice");
            bool portable = IsPortableComputer(keyboardNames, mouseNames);

            DeviceDetectionResult result = new DeviceDetectionResult();
            result.KeyboardLayout = portable ? "laptop" : "full";
            result.MouseStyle = DetectMouseStyle(mouseNames);
            string keyboardSummary = keyboardNames.Count > 0 ? keyboardNames[0] : (portable ? "portable keyboard" : "desktop keyboard");
            string mouseSummary = mouseNames.Count > 0 ? mouseNames[0] : "mouse";
            result.Summary = keyboardSummary + " / " + mouseSummary;
            return result;
        }

        private static List<string> QueryNames(string query)
        {
            List<string> names = new List<string>();
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
                using (ManagementObjectCollection objects = searcher.Get())
                {
                    foreach (ManagementObject item in objects)
                    {
                        AddManagementText(names, item, "Name");
                        AddManagementText(names, item, "Description");
                    }
                }
            }
            catch
            {
            }
            return names;
        }

        private static void AddManagementText(List<string> names, ManagementObject item, string property)
        {
            try
            {
                object value = item[property];
                if (value == null)
                {
                    return;
                }

                string text = value.ToString();
                if (!string.IsNullOrWhiteSpace(text) && !ContainsName(names, text))
                {
                    names.Add(text.Trim());
                }
            }
            catch
            {
            }
        }

        private static bool ContainsName(List<string> names, string text)
        {
            for (int i = 0; i < names.Count; i++)
            {
                if (string.Equals(names[i], text, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsPortableComputer(List<string> keyboardNames, List<string> mouseNames)
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT ChassisTypes FROM Win32_SystemEnclosure"))
                using (ManagementObjectCollection objects = searcher.Get())
                {
                    foreach (ManagementObject item in objects)
                    {
                        ushort[] chassis = item["ChassisTypes"] as ushort[];
                        if (chassis == null)
                        {
                            continue;
                        }

                        for (int i = 0; i < chassis.Length; i++)
                        {
                            if (chassis[i] == 8 || chassis[i] == 9 || chassis[i] == 10 || chassis[i] == 14)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return ContainsAny(keyboardNames, new string[] { "laptop", "notebook" }) ||
                   ContainsAny(mouseNames, new string[] { "touchpad", "trackpad", "precision touchpad" });
        }

        private static string DetectMouseStyle(List<string> mouseNames)
        {
            if (ContainsAny(mouseNames, new string[] { "gaming", "razer", "logitech g", "corsair", "steelseries", "hyperx", "glorious", "redragon", "bloody", "zowie" }))
            {
                return "gaming";
            }
            if (ContainsAny(mouseNames, new string[] { "touchpad", "trackpad", "precision touchpad", "compact" }))
            {
                return "compact";
            }
            return "office";
        }

        private static bool ContainsAny(List<string> values, string[] needles)
        {
            for (int i = 0; i < values.Count; i++)
            {
                string value = values[i];
                if (value == null)
                {
                    continue;
                }

                string lower = value.ToLowerInvariant();
                for (int j = 0; j < needles.Length; j++)
                {
                    if (lower.IndexOf(needles[j], StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }

    internal sealed class WavSample
    {
        public string Name;
        public short[] Data;
        public int SampleRate;
    }

    internal sealed class WavLoader
    {
        public static WavSample Load(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                string riff = ReadFourCC(reader);
                reader.ReadInt32();
                string wave = ReadFourCC(reader);
                if (riff != "RIFF" || wave != "WAVE")
                {
                    throw new InvalidDataException("Not a RIFF/WAVE file: " + path);
                }

                ushort audioFormat = 0;
                ushort channels = 0;
                int sampleRate = 0;
                ushort bitsPerSample = 0;
                byte[] dataBytes = null;

                while (stream.Position + 8 <= stream.Length)
                {
                    string chunkId = ReadFourCC(reader);
                    int chunkSize = reader.ReadInt32();
                    long nextChunk = stream.Position + chunkSize;

                    if (chunkId == "fmt ")
                    {
                        audioFormat = reader.ReadUInt16();
                        channels = reader.ReadUInt16();
                        sampleRate = reader.ReadInt32();
                        reader.ReadInt32();
                        reader.ReadUInt16();
                        bitsPerSample = reader.ReadUInt16();
                    }
                    else if (chunkId == "data")
                    {
                        dataBytes = reader.ReadBytes(chunkSize);
                    }

                    stream.Position = nextChunk;
                    if ((chunkSize & 1) == 1 && stream.Position < stream.Length)
                    {
                        stream.Position++;
                    }
                }

                if (audioFormat != 1 || bitsPerSample != 16 || channels == 0 || dataBytes == null)
                {
                    throw new InvalidDataException("Only PCM 16-bit WAV files are supported: " + path);
                }

                int frameCount = dataBytes.Length / (2 * channels);
                short[] mono = new short[frameCount];
                for (int frame = 0; frame < frameCount; frame++)
                {
                    if (channels == 1)
                    {
                        mono[frame] = BitConverter.ToInt16(dataBytes, frame * 2);
                    }
                    else
                    {
                        int sum = 0;
                        int offset = frame * channels * 2;
                        for (int channel = 0; channel < channels; channel++)
                        {
                            sum += BitConverter.ToInt16(dataBytes, offset + channel * 2);
                        }
                        mono[frame] = (short)(sum / channels);
                    }
                }

                WavSample sample = new WavSample();
                sample.Name = Path.GetFileNameWithoutExtension(path);
                sample.Data = mono;
                sample.SampleRate = sampleRate;
                return sample;
            }
        }

        private static string ReadFourCC(BinaryReader reader)
        {
            char[] chars = reader.ReadChars(4);
            return new string(chars);
        }
    }

    internal sealed class AudioEngine : IDisposable
    {
        private const int OutputSampleRate = 44100;
        private const int OutputChannels = 2;
        private const int BitsPerSample = 16;
        private const int BufferFrames = 512;
        private const int BufferCount = 4;
        private const int WaveMapper = -1;
        private const int CallbackEvent = 0x00050000;
        private const ushort WaveFormatPcm = 1;
        private const uint WhdrDone = 0x00000001;

        private readonly object _sampleLock;
        private readonly object _voiceLock;
        private readonly Dictionary<string, WavSample> _customSamples;
        private readonly Random _random;
        private List<WavSample> _samples;
        private List<Voice> _voices;
        private IntPtr _waveOut;
        private AutoResetEvent _bufferEvent;
        private Thread _audioThread;
        private AudioBuffer[] _buffers;
        private volatile bool _running;
        private volatile bool _audioReady;
        private volatile bool _enabled;
        private float _masterVolume;
        private int _randomVolumePercent;
        private int _randomPitchPercent;
        private int _maxVoices;
        private string _lastError;

        public AudioEngine()
        {
            _sampleLock = new object();
            _voiceLock = new object();
            _customSamples = new Dictionary<string, WavSample>(StringComparer.OrdinalIgnoreCase);
            _random = new Random();
            _samples = new List<WavSample>();
            _voices = new List<Voice>();
            _enabled = true;
            _masterVolume = 0.7f;
            _randomVolumePercent = 12;
            _randomPitchPercent = 4;
            _maxVoices = 24;
            StartAudio();
        }

        public bool AudioReady
        {
            get { return _audioReady; }
        }

        public string LastError
        {
            get { return _lastError; }
        }

        public int SampleCount
        {
            get
            {
                lock (_sampleLock)
                {
                    return _samples.Count;
                }
            }
        }

        public void Configure(AppConfig config)
        {
            _enabled = config.Enabled;
            float outputVolume = config.Volume / 100.0f;
            float keyGain = config.KeyGainPercent / 100.0f;
            _masterVolume = Math.Max(0.0f, Math.Min(4.0f, outputVolume * keyGain));
            _randomVolumePercent = Math.Max(0, Math.Min(50, config.RandomVolumePercent));
            _randomPitchPercent = Math.Max(0, Math.Min(20, config.RandomPitchPercent));
            _maxVoices = Math.Max(4, Math.Min(64, config.MaxVoices));

            if (!_enabled)
            {
                lock (_voiceLock)
                {
                    _voices.Clear();
                }
            }
        }

        public void LoadPreset(string directoryPath)
        {
            List<WavSample> loaded = new List<WavSample>();
            if (Directory.Exists(directoryPath))
            {
                string[] files = Directory.GetFiles(directoryPath, "*.wav");
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < files.Length; i++)
                {
                    loaded.Add(WavLoader.Load(files[i]));
                }
            }

            lock (_sampleLock)
            {
                _samples = loaded;
            }

            lock (_voiceLock)
            {
                _voices.Clear();
            }
        }

        public bool PlayRandom()
        {
            if (!_enabled || !_audioReady)
            {
                return false;
            }

            WavSample sample;
            lock (_sampleLock)
            {
                if (_samples.Count == 0)
                {
                    return false;
                }
                sample = _samples[_random.Next(_samples.Count)];
            }

            return PlaySample(sample, true);
        }

        public bool PlaySoundPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return PlayRandom();
            }

            if (!_enabled || !_audioReady)
            {
                return false;
            }

            WavSample sample;
            lock (_sampleLock)
            {
                if (!_customSamples.TryGetValue(path, out sample))
                {
                    sample = WavLoader.Load(path);
                    _customSamples[path] = sample;
                }
            }

            return PlaySample(sample, true);
        }

        private bool PlaySample(WavSample sample, bool allowJitter)
        {
            if (sample == null)
            {
                return false;
            }

            float volume = _masterVolume;
            if (allowJitter && _randomVolumePercent > 0)
            {
                double spread = _randomVolumePercent / 100.0;
                double scale = 1.0 - spread + (_random.NextDouble() * spread * 2.0);
                volume = (float)Math.Max(0.0, Math.Min(4.0, volume * scale));
            }

            double pitch = 1.0;
            if (allowJitter && _randomPitchPercent > 0)
            {
                double spread = _randomPitchPercent / 100.0;
                pitch = 1.0 - spread + (_random.NextDouble() * spread * 2.0);
            }

            double step = (sample.SampleRate / (double)OutputSampleRate) * pitch;

            lock (_voiceLock)
            {
                while (_voices.Count >= _maxVoices)
                {
                    _voices.RemoveAt(0);
                }

                Voice voice = new Voice();
                voice.Sample = sample;
                voice.Position = 0.0;
                voice.Step = step;
                voice.Volume = volume;
                _voices.Add(voice);
            }

            return true;
        }

        public void Dispose()
        {
            StopAudio();
        }

        private void StartAudio()
        {
            try
            {
                _bufferEvent = new AutoResetEvent(false);
                WaveFormatEx format = new WaveFormatEx();
                format.wFormatTag = WaveFormatPcm;
                format.nChannels = OutputChannels;
                format.nSamplesPerSec = OutputSampleRate;
                format.wBitsPerSample = BitsPerSample;
                format.nBlockAlign = (ushort)(OutputChannels * BitsPerSample / 8);
                format.nAvgBytesPerSec = (uint)(format.nSamplesPerSec * format.nBlockAlign);
                format.cbSize = 0;

                int result = waveOutOpen(out _waveOut, WaveMapper, ref format,
                    _bufferEvent.SafeWaitHandle.DangerousGetHandle(), IntPtr.Zero, CallbackEvent);
                if (result != 0)
                {
                    throw new InvalidOperationException(GetWaveError(result));
                }

                _buffers = new AudioBuffer[BufferCount];
                int bufferBytes = BufferFrames * OutputChannels * (BitsPerSample / 8);
                int headerSize = Marshal.SizeOf(typeof(WaveHeader));

                for (int i = 0; i < BufferCount; i++)
                {
                    AudioBuffer buffer = new AudioBuffer();
                    buffer.Bytes = new byte[bufferBytes];
                    buffer.DataPtr = Marshal.AllocHGlobal(bufferBytes);
                    buffer.HeaderPtr = Marshal.AllocHGlobal(headerSize);

                    WaveHeader header = new WaveHeader();
                    header.lpData = buffer.DataPtr;
                    header.dwBufferLength = (uint)bufferBytes;
                    Marshal.StructureToPtr(header, buffer.HeaderPtr, false);

                    result = waveOutPrepareHeader(_waveOut, buffer.HeaderPtr, (uint)headerSize);
                    if (result != 0)
                    {
                        throw new InvalidOperationException(GetWaveError(result));
                    }

                    _buffers[i] = buffer;
                }

                _running = true;
                _audioReady = true;
                _audioThread = new Thread(AudioThreadMain);
                _audioThread.IsBackground = true;
                _audioThread.Name = "CreamyKeysDesktop.Audio";
                _audioThread.Priority = ThreadPriority.AboveNormal;
                _audioThread.Start();
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                _audioReady = false;
            }
        }

        private void StopAudio()
        {
            _running = false;
            if (_bufferEvent != null)
            {
                _bufferEvent.Set();
            }

            if (_audioThread != null)
            {
                _audioThread.Join(1000);
                _audioThread = null;
            }

            if (_waveOut != IntPtr.Zero)
            {
                waveOutReset(_waveOut);
            }

            if (_buffers != null)
            {
                int headerSize = Marshal.SizeOf(typeof(WaveHeader));
                for (int i = 0; i < _buffers.Length; i++)
                {
                    AudioBuffer buffer = _buffers[i];
                    if (buffer == null)
                    {
                        continue;
                    }

                    if (_waveOut != IntPtr.Zero && buffer.HeaderPtr != IntPtr.Zero)
                    {
                        waveOutUnprepareHeader(_waveOut, buffer.HeaderPtr, (uint)headerSize);
                    }
                    if (buffer.DataPtr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(buffer.DataPtr);
                    }
                    if (buffer.HeaderPtr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(buffer.HeaderPtr);
                    }
                }
                _buffers = null;
            }

            if (_waveOut != IntPtr.Zero)
            {
                waveOutClose(_waveOut);
                _waveOut = IntPtr.Zero;
            }

            if (_bufferEvent != null)
            {
                _bufferEvent.Close();
                _bufferEvent = null;
            }

            _audioReady = false;
        }

        private void AudioThreadMain()
        {
            try
            {
                for (int i = 0; i < _buffers.Length; i++)
                {
                    FillAndWriteBuffer(_buffers[i]);
                }

                while (_running)
                {
                    _bufferEvent.WaitOne(100);
                    if (!_running)
                    {
                        break;
                    }

                    for (int i = 0; i < _buffers.Length; i++)
                    {
                        if (IsBufferDone(_buffers[i]))
                        {
                            FillAndWriteBuffer(_buffers[i]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                _audioReady = false;
            }
        }

        private bool IsBufferDone(AudioBuffer buffer)
        {
            WaveHeader header = (WaveHeader)Marshal.PtrToStructure(buffer.HeaderPtr, typeof(WaveHeader));
            return (header.dwFlags & WhdrDone) != 0;
        }

        private void FillAndWriteBuffer(AudioBuffer buffer)
        {
            Render(buffer.Bytes);
            Marshal.Copy(buffer.Bytes, 0, buffer.DataPtr, buffer.Bytes.Length);
            int result = waveOutWrite(_waveOut, buffer.HeaderPtr, (uint)Marshal.SizeOf(typeof(WaveHeader)));
            if (result != 0 && _running)
            {
                throw new InvalidOperationException(GetWaveError(result));
            }
        }

        private void Render(byte[] output)
        {
            Array.Clear(output, 0, output.Length);
            if (!_enabled)
            {
                lock (_voiceLock)
                {
                    _voices.Clear();
                }
                return;
            }

            int offset = 0;
            lock (_voiceLock)
            {
                for (int frame = 0; frame < BufferFrames; frame++)
                {
                    float mix = 0.0f;

                    for (int i = _voices.Count - 1; i >= 0; i--)
                    {
                        Voice voice = _voices[i];
                        short[] data = voice.Sample.Data;
                        int index = (int)voice.Position;

                        if (index >= data.Length)
                        {
                            _voices.RemoveAt(i);
                            continue;
                        }

                        int next = index + 1;
                        if (next >= data.Length)
                        {
                            next = index;
                        }

                        double fraction = voice.Position - index;
                        double s1 = data[index] / 32768.0;
                        double s2 = data[next] / 32768.0;
                        mix += (float)((s1 + (s2 - s1) * fraction) * voice.Volume);

                        voice.Position += voice.Step;
                        if (voice.Position >= data.Length)
                        {
                            _voices.RemoveAt(i);
                        }
                    }

                    if (mix > 1.0f)
                    {
                        mix = 1.0f;
                    }
                    else if (mix < -1.0f)
                    {
                        mix = -1.0f;
                    }

                    short value = (short)(mix * 32767.0f);
                    output[offset++] = (byte)(value & 0xff);
                    output[offset++] = (byte)((value >> 8) & 0xff);
                    output[offset++] = (byte)(value & 0xff);
                    output[offset++] = (byte)((value >> 8) & 0xff);
                }
            }
        }

        private static string GetWaveError(int result)
        {
            StringBuilder builder = new StringBuilder(256);
            if (waveOutGetErrorText(result, builder, builder.Capacity) == 0)
            {
                return builder.ToString();
            }
            return "winmm error " + result.ToString(CultureInfo.InvariantCulture);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WaveFormatEx
        {
            public ushort wFormatTag;
            public ushort nChannels;
            public int nSamplesPerSec;
            public uint nAvgBytesPerSec;
            public ushort nBlockAlign;
            public ushort wBitsPerSample;
            public ushort cbSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WaveHeader
        {
            public IntPtr lpData;
            public uint dwBufferLength;
            public uint dwBytesRecorded;
            public IntPtr dwUser;
            public uint dwFlags;
            public uint dwLoops;
            public IntPtr lpNext;
            public IntPtr reserved;
        }

        private sealed class AudioBuffer
        {
            public byte[] Bytes;
            public IntPtr DataPtr;
            public IntPtr HeaderPtr;
        }

        private sealed class Voice
        {
            public WavSample Sample;
            public double Position;
            public double Step;
            public float Volume;
        }

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveOutOpen(out IntPtr hWaveOut, int uDeviceID, ref WaveFormatEx lpFormat,
            IntPtr dwCallback, IntPtr dwInstance, int dwFlags);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveOutPrepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, uint uSize);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveOutWrite(IntPtr hWaveOut, IntPtr lpWaveOutHdr, uint uSize);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveOutUnprepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, uint uSize);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveOutReset(IntPtr hWaveOut);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveOutClose(IntPtr hWaveOut);

        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        private static extern int waveOutGetErrorText(int mmrError, StringBuilder pszText, int cchText);
    }

    internal sealed class KeyboardHook : IDisposable
    {
        private const int WhKeyboardLl = 13;
        private const int WmKeyDown = 0x0100;
        private const int WmKeyUp = 0x0101;
        private const int WmSysKeyDown = 0x0104;
        private const int WmSysKeyUp = 0x0105;
        private const int LlkInjected = 0x00000010;

        private readonly HashSet<int> _pressedKeys;
        private LowLevelKeyboardProc _proc;
        private IntPtr _hook;

        public bool PlayOnRepeat;
        public bool PlayModifiers;
        public bool IgnoreInjectedInput;

        public event KeyPressedHandler KeyPressed;

        public KeyboardHook()
        {
            _pressedKeys = new HashSet<int>();
            _proc = HookProc;
            PlayOnRepeat = false;
            PlayModifiers = false;
            IgnoreInjectedInput = true;
        }

        public void Install()
        {
            if (_hook != IntPtr.Zero)
            {
                return;
            }

            using (Process process = Process.GetCurrentProcess())
            {
                ProcessModule module = process.MainModule;
                IntPtr moduleHandle = GetModuleHandle(module.ModuleName);
                _hook = SetWindowsHookEx(WhKeyboardLl, _proc, moduleHandle, 0);
            }

            if (_hook == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        public void Dispose()
        {
            if (_hook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hook);
                _hook = IntPtr.Zero;
            }
        }

        private IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int message = wParam.ToInt32();
                KbdLlHookStruct info = (KbdLlHookStruct)Marshal.PtrToStructure(lParam, typeof(KbdLlHookStruct));
                int vkCode = info.vkCode;

                if (message == WmKeyDown || message == WmSysKeyDown)
                {
                    bool injected = (info.flags & LlkInjected) != 0;
                    bool repeated = _pressedKeys.Contains(vkCode);
                    _pressedKeys.Add(vkCode);

                    if (!((IgnoreInjectedInput && injected) ||
                          (!PlayOnRepeat && repeated) ||
                          (!PlayModifiers && IsModifier(vkCode))))
                    {
                        KeyPressedHandler handler = KeyPressed;
                        if (handler != null)
                        {
                            handler(vkCode);
                        }
                    }
                }
                else if (message == WmKeyUp || message == WmSysKeyUp)
                {
                    _pressedKeys.Remove(vkCode);
                }
            }

            return CallNextHookEx(_hook, nCode, wParam, lParam);
        }

        private static bool IsModifier(int vkCode)
        {
            return vkCode == 0x10 ||
                   vkCode == 0x11 ||
                   vkCode == 0x12 ||
                   vkCode == 0x5B ||
                   vkCode == 0x5C ||
                   (vkCode >= 0xA0 && vkCode <= 0xA5);
        }

        public delegate void KeyPressedHandler(int vkCode);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct KbdLlHookStruct
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod,
            uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }

    internal sealed class MouseHook : IDisposable
    {
        private const int WhMouseLl = 14;
        private const int WmLButtonDown = 0x0201;
        private const int WmRButtonDown = 0x0204;
        private const int WmMButtonDown = 0x0207;
        private const int WmXButtonDown = 0x020B;
        private const int XButton1 = 0x0001;
        private const int XButton2 = 0x0002;

        private LowLevelMouseProc _proc;
        private IntPtr _hook;

        public event MouseButtonPressedHandler MouseButtonPressed;

        public MouseHook()
        {
            _proc = HookProc;
        }

        public void Install()
        {
            if (_hook != IntPtr.Zero)
            {
                return;
            }

            using (Process process = Process.GetCurrentProcess())
            {
                ProcessModule module = process.MainModule;
                IntPtr moduleHandle = GetModuleHandle(module.ModuleName);
                _hook = SetWindowsHookEx(WhMouseLl, _proc, moduleHandle, 0);
            }

            if (_hook == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        public void Dispose()
        {
            if (_hook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hook);
                _hook = IntPtr.Zero;
            }
        }

        private IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int message = wParam.ToInt32();
                string id = null;

                if (message == WmLButtonDown)
                {
                    id = "mouse.left";
                }
                else if (message == WmRButtonDown)
                {
                    id = "mouse.right";
                }
                else if (message == WmMButtonDown)
                {
                    id = "mouse.middle";
                }
                else if (message == WmXButtonDown)
                {
                    MsLlHookStruct info = (MsLlHookStruct)Marshal.PtrToStructure(lParam, typeof(MsLlHookStruct));
                    int button = (info.mouseData >> 16) & 0xffff;
                    if (button == XButton1)
                    {
                        id = "mouse.x1";
                    }
                    else if (button == XButton2)
                    {
                        id = "mouse.x2";
                    }
                }

                if (id != null)
                {
                    MouseButtonPressedHandler handler = MouseButtonPressed;
                    if (handler != null)
                    {
                        handler(id);
                    }
                }
            }

            return CallNextHookEx(_hook, nCode, wParam, lParam);
        }

        public delegate void MouseButtonPressedHandler(string buttonId);

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct PointStruct
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MsLlHookStruct
        {
            public PointStruct pt;
            public int mouseData;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod,
            uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }

    internal sealed class VirtualButtonEventArgs : EventArgs
    {
        public readonly string ButtonId;
        public readonly int VkCode;
        public readonly bool IsMouse;

        public VirtualButtonEventArgs(string buttonId, int vkCode, bool isMouse)
        {
            ButtonId = buttonId;
            VkCode = vkCode;
            IsMouse = isMouse;
        }
    }

    internal sealed class VirtualButtonDefinition
    {
        public string Id;
        public string Label;
        public string Kind;
        public int VkCode;
        public RectangleF UnitBounds;
        public Rectangle Bounds;
    }

    internal sealed class VirtualDeviceControl : Control
    {
        private struct KeyboardMeasure
        {
            public float MaxX;
            public float MaxY;
        }

        private readonly List<VirtualButtonDefinition> _buttons;
        private readonly Dictionary<string, long> _activeUntil;
        private readonly System.Windows.Forms.Timer _flashTimer;
        private Dictionary<string, VirtualButtonConfig> _overrides;
        private string _keyboardLayout;
        private string _mouseStyle;
        private bool _showMouse;
        private bool _editMode;
        private bool _shadowEnabled;
        private int _shadowDepth;
        private int _shadowOffsetX;
        private int _shadowOffsetY;

        public event EventHandler<VirtualButtonEventArgs> ButtonActivated;
        public event EventHandler ButtonCustomizationChanged;

        public VirtualDeviceControl()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);
            BackColor = Color.FromArgb(246, 248, 249);
            ForeColor = Color.FromArgb(31, 42, 47);
            Font = new Font("Segoe UI", 8.5f);
            Cursor = Cursors.Hand;
            _buttons = new List<VirtualButtonDefinition>();
            _activeUntil = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            _overrides = new Dictionary<string, VirtualButtonConfig>(StringComparer.OrdinalIgnoreCase);
            _keyboardLayout = "full";
            _mouseStyle = "gaming";
            _showMouse = true;
            _shadowEnabled = true;
            _shadowDepth = 18;
            _shadowOffsetX = 6;
            _shadowOffsetY = 8;
            _flashTimer = new System.Windows.Forms.Timer();
            _flashTimer.Interval = 35;
            _flashTimer.Tick += delegate { PruneActiveButtons(); };
            _flashTimer.Start();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            _buttons.Clear();
            Invalidate();
        }

        public string KeyboardLayout
        {
            get { return _keyboardLayout; }
            set
            {
                _keyboardLayout = string.IsNullOrWhiteSpace(value) ? "full" : value;
                Invalidate();
            }
        }

        public string MouseStyle
        {
            get { return _mouseStyle; }
            set
            {
                _mouseStyle = string.IsNullOrWhiteSpace(value) ? "gaming" : value;
                Invalidate();
            }
        }

        public bool ShowMouse
        {
            get { return _showMouse; }
            set
            {
                _showMouse = value;
                Invalidate();
            }
        }

        public bool EditMode
        {
            get { return _editMode; }
            set
            {
                _editMode = value;
                Cursor = _editMode ? Cursors.Cross : Cursors.Hand;
                Invalidate();
            }
        }

        public bool ShadowEnabled
        {
            get { return _shadowEnabled; }
            set
            {
                _shadowEnabled = value;
                Invalidate();
            }
        }

        public int ShadowDepth
        {
            get { return _shadowDepth; }
            set
            {
                _shadowDepth = Math.Max(0, Math.Min(48, value));
                Invalidate();
            }
        }

        public int ShadowOffsetX
        {
            get { return _shadowOffsetX; }
            set
            {
                _shadowOffsetX = Math.Max(-32, Math.Min(32, value));
                Invalidate();
            }
        }

        public int ShadowOffsetY
        {
            get { return _shadowOffsetY; }
            set
            {
                _shadowOffsetY = Math.Max(-32, Math.Min(32, value));
                Invalidate();
            }
        }

        public void SetOverrides(Dictionary<string, VirtualButtonConfig> overrides)
        {
            _overrides = overrides ?? new Dictionary<string, VirtualButtonConfig>(StringComparer.OrdinalIgnoreCase);
            Invalidate();
        }

        public void FlashKey(int vkCode)
        {
            EnsureLayout();
            bool found = false;
            for (int i = 0; i < _buttons.Count; i++)
            {
                if (_buttons[i].VkCode == vkCode)
                {
                    FlashButton(_buttons[i].Id);
                    found = true;
                }
            }

            if (!found && vkCode >= 65 && vkCode <= 90)
            {
                FlashButton("key." + ((char)vkCode).ToString());
            }
        }

        public void FlashButton(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            _activeUntil[id] = Environment.TickCount + 160;
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _flashTimer.Stop();
                _flashTimer.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (SolidBrush bg = new SolidBrush(BackColor))
            {
                g.FillRectangle(bg, ClientRectangle);
            }

            EnsureLayout();
            DrawHeader(g);
            for (int i = 0; i < _buttons.Count; i++)
            {
                DrawButton(g, _buttons[i]);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            EnsureLayout();
            VirtualButtonDefinition button = HitTest(e.Location);
            if (button == null)
            {
                return;
            }

            if (e.Button == MouseButtons.Right)
            {
                ShowButtonMenu(button, e.Location);
                return;
            }

            if (_editMode)
            {
                EditButton(button);
                return;
            }

            FlashButton(button.Id);
            EventHandler<VirtualButtonEventArgs> handler = ButtonActivated;
            if (handler != null)
            {
                handler(this, new VirtualButtonEventArgs(button.Id, button.VkCode, button.Kind == "mouse"));
            }
        }

        private void DrawHeader(Graphics g)
        {
            using (Font headerFont = new Font("Segoe UI Semibold", 10.0f))
            using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(45, 58, 64)))
            using (SolidBrush mutedBrush = new SolidBrush(Color.FromArgb(99, 111, 116)))
            {
                string layout = DisplayName(_keyboardLayout);
                string mouse = _showMouse ? "Mouse: " + DisplayName(_mouseStyle) : "Mouse hidden";
                g.DrawString("Virtual devices", headerFont, textBrush, 16, 12);
                g.DrawString(layout + " keyboard  |  " + mouse, Font, mutedBrush, 16, 34);
            }

            if (_editMode)
            {
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(22, 116, 128)))
                {
                    g.DrawString("EDIT MODE", new Font("Segoe UI Semibold", 8.5f), brush, Width - 96, 16);
                }
            }
        }

        private void DrawButton(Graphics g, VirtualButtonDefinition button)
        {
            VirtualButtonConfig config = GetOverride(button.Id);
            string label = !string.IsNullOrWhiteSpace(config.IconText) ? config.IconText :
                (!string.IsNullOrWhiteSpace(config.Label) ? config.Label : button.Label);
            bool active = _activeUntil.ContainsKey(button.Id);
            bool custom = config != null && !config.IsEmpty();

            if (button.Kind == "mouse")
            {
                DrawMouseButton(g, button, label, active, custom);
                return;
            }

            Color fill = active ? Color.FromArgb(22, 132, 147) :
                (button.Kind == "mouse" ? Color.FromArgb(236, 230, 220) : Color.White);
            Color border = active ? Color.FromArgb(13, 93, 105) :
                (custom ? Color.FromArgb(212, 137, 72) : Color.FromArgb(213, 221, 224));
            Color text = active ? Color.White : Color.FromArgb(28, 39, 44);

            Rectangle r = button.Bounds;
            if (_shadowEnabled && _shadowDepth > 0)
            {
                using (SolidBrush shadow = new SolidBrush(Color.FromArgb(Math.Min(90, _shadowDepth * 3), 0, 0, 0)))
                {
                    g.FillRectangle(shadow, r.X + _shadowOffsetX, r.Y + _shadowOffsetY, r.Width, r.Height);
                }
            }
            using (SolidBrush brush = new SolidBrush(fill))
            using (Pen pen = new Pen(border))
            {
                g.FillRectangle(brush, r);
                g.DrawRectangle(pen, r);
            }

            StringFormat format = new StringFormat();
            format.Alignment = StringAlignment.Center;
            format.LineAlignment = StringAlignment.Center;
            format.Trimming = StringTrimming.EllipsisCharacter;
            format.FormatFlags = StringFormatFlags.NoWrap;
            using (SolidBrush textBrush = new SolidBrush(text))
            {
                float labelFactor = label.Length >= 5 ? 0.20f : (label.Length >= 4 ? 0.24f : 0.34f);
                float fontSize = Math.Min(8.5f, Math.Max(5.6f, Math.Min(r.Height * 0.42f, r.Width * labelFactor)));
                Font drawFont = Math.Abs(fontSize - Font.Size) > 0.2f ? new Font("Segoe UI", fontSize) : Font;
                g.DrawString(label, drawFont, textBrush, r, format);
                if (!object.ReferenceEquals(drawFont, Font))
                {
                    drawFont.Dispose();
                }
            }
        }

        private void DrawMouseButton(Graphics g, VirtualButtonDefinition button, string label, bool active, bool custom)
        {
            Rectangle r = button.Bounds;
            bool body = string.Equals(button.Id, "mouse.body", StringComparison.OrdinalIgnoreCase);
            bool wheel = string.Equals(button.Id, "mouse.middle", StringComparison.OrdinalIgnoreCase);
            bool side = string.Equals(button.Id, "mouse.x1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(button.Id, "mouse.x2", StringComparison.OrdinalIgnoreCase);

            Color fill = active ? Color.FromArgb(22, 132, 147) :
                (body ? Color.FromArgb(229, 232, 226) : Color.FromArgb(246, 243, 236));
            Color border = active ? Color.FromArgb(13, 93, 105) :
                (custom ? Color.FromArgb(212, 137, 72) : Color.FromArgb(186, 194, 190));
            Color text = active ? Color.White : Color.FromArgb(28, 39, 44);

            using (SolidBrush brush = new SolidBrush(fill))
            using (Pen pen = new Pen(border, body ? 1.6f : 1.0f))
            {
                Rectangle shadowRect = new Rectangle(r.X + _shadowOffsetX, r.Y + _shadowOffsetY, r.Width, r.Height);
                if (body)
                {
                    using (GraphicsPath path = RoundedRect(r, 12))
                    {
                        if (_shadowEnabled && _shadowDepth > 0)
                        {
                            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(Math.Min(110, _shadowDepth * 3), 0, 0, 0)))
                            using (GraphicsPath shadowPath = RoundedRect(shadowRect, 12))
                            {
                                g.FillPath(shadow, shadowPath);
                            }
                        }
                        g.FillPath(brush, path);
                        g.DrawPath(pen, path);
                    }

                    using (Pen centerPen = new Pen(Color.FromArgb(150, 199, 205, 201)))
                    {
                        g.DrawLine(centerPen, r.X + r.Width / 2, r.Y + 14, r.X + r.Width / 2, r.Y + r.Height - 46);
                    }
                }
                else
                {
                    using (GraphicsPath path = RoundedRect(r, wheel ? 8 : (side ? 4 : 7)))
                    {
                        if (_shadowEnabled && _shadowDepth > 0)
                        {
                            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(Math.Min(95, _shadowDepth * 3), 0, 0, 0)))
                            using (GraphicsPath shadowPath = RoundedRect(shadowRect, wheel ? 8 : (side ? 4 : 7)))
                            {
                                g.FillPath(shadow, shadowPath);
                            }
                        }
                        g.FillPath(brush, path);
                        g.DrawPath(pen, path);
                    }
                }
            }

            Rectangle textRect = body
                ? new Rectangle(r.X + 8, r.Bottom - 38, r.Width - 16, 28)
                : r;
            StringFormat format = new StringFormat();
            format.Alignment = StringAlignment.Center;
            format.LineAlignment = StringAlignment.Center;
            format.Trimming = StringTrimming.EllipsisCharacter;
            format.FormatFlags = StringFormatFlags.NoWrap;
            using (SolidBrush textBrush = new SolidBrush(text))
            using (Font mouseFont = new Font("Segoe UI", body ? 7.8f : (side ? 6.6f : (r.Width < 42 ? 7.0f : 8.0f))))
            {
                g.DrawString(label, mouseFont, textBrush, textRect, format);
            }
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = Math.Max(2, radius * 2);
            if (r.Width <= diameter || r.Height <= diameter)
            {
                path.AddRectangle(r);
                path.CloseFigure();
                return path;
            }

            path.AddArc(r.X, r.Y, diameter, diameter, 180, 90);
            path.AddArc(r.Right - diameter, r.Y, diameter, diameter, 270, 90);
            path.AddArc(r.Right - diameter, r.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(r.X, r.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void EnsureLayout()
        {
            _buttons.Clear();

            int bottomReserve = Math.Max(84, Height / 4);
            Rectangle content = new Rectangle(
                16,
                62,
                Math.Max(120, Width - 32),
                Math.Max(120, Height - 62 - bottomReserve));
            if (_showMouse)
            {
                LayoutKeyboardAndMouse(content);
            }
            else
            {
                BuildKeyboardButtons(content, true);
                CenterKeyboardButtonsInArea(content, false);
            }

            ApplyVirtualDeviceNudge();
        }

        private void ApplyVirtualDeviceNudge()
        {
            if (_buttons.Count == 0)
            {
                return;
            }

            Rectangle bounds = _buttons[0].Bounds;
            for (int i = 1; i < _buttons.Count; i++)
            {
                bounds = Rectangle.Union(bounds, _buttons[i].Bounds);
            }

            int dx = -Math.Max(66, Math.Min(108, Width / 18));
            int dy = -Math.Max(38, Math.Min(104, Height / 8));
            int minimumTop = 58;
            if (bounds.Top + dy < minimumTop)
            {
                dy = minimumTop - bounds.Top;
            }

            if (dx == 0 && dy == 0)
            {
                return;
            }

            for (int i = 0; i < _buttons.Count; i++)
            {
                VirtualButtonDefinition button = _buttons[i];
                button.Bounds = new Rectangle(
                    button.Bounds.X + dx,
                    button.Bounds.Y + dy,
                    button.Bounds.Width,
                    button.Bounds.Height);
            }
        }

        private void CenterKeyboardButtonsInArea(Rectangle area, bool moveMouseWithKeyboard)
        {
            if (_buttons.Count == 0)
            {
                return;
            }

            Rectangle bounds = Rectangle.Empty;
            bool found = false;
            for (int i = 0; i < _buttons.Count; i++)
            {
                if (_buttons[i].Kind == "mouse")
                {
                    continue;
                }

                bounds = found ? Rectangle.Union(bounds, _buttons[i].Bounds) : _buttons[i].Bounds;
                found = true;
            }

            if (!found)
            {
                return;
            }

            int targetX = area.X + Math.Max(0, (area.Width - bounds.Width) / 2);
            int targetY = area.Y + Math.Max(0, (area.Height - bounds.Height) / 2);
            int dx = targetX - bounds.X;
            int dy = targetY - bounds.Y;
            if (dx == 0 && dy == 0)
            {
                return;
            }

            for (int i = 0; i < _buttons.Count; i++)
            {
                if (!moveMouseWithKeyboard && _buttons[i].Kind == "mouse")
                {
                    continue;
                }

                VirtualButtonDefinition button = _buttons[i];
                button.Bounds = new Rectangle(
                    button.Bounds.X + dx,
                    button.Bounds.Y + dy,
                    button.Bounds.Width,
                    button.Bounds.Height);
            }
        }

        private void LayoutKeyboardAndMouse(Rectangle content)
        {
            List<VirtualButtonDefinition> units = KeyboardUnits(_keyboardLayout);
            KeyboardMeasure measure = MeasureKeyboard(units);
            int gap = content.Width >= 1300 ? 78 : 48;
            int mouseWidth = Math.Min(250, Math.Max(170, content.Width / 7));
            int sideKeyboardWidth = Math.Max(260, content.Width - mouseWidth - gap);
            Rectangle sideKeyboardProbe = new Rectangle(content.X, content.Y, sideKeyboardWidth, content.Height);
            float sideScale = ResolveKeyboardScale(sideKeyboardProbe, measure, 44.0f);
            Size sideKeyboardSize = KeyboardSize(measure, sideScale);
            int sideMouseHeight = Math.Min(content.Height, Math.Min(280, Math.Max(150, sideKeyboardSize.Height + 32)));
            int sideGroupWidth = sideKeyboardSize.Width + gap + mouseWidth;
            int centeredKeyboardX = content.X + Math.Max(0, (content.Width - sideKeyboardSize.Width) / 2);
            bool mouseFitsRight = centeredKeyboardX + sideKeyboardSize.Width + gap + mouseWidth <= content.Right;
            bool sideBySide = content.Width >= 1180 && content.Height >= 210 &&
                sideKeyboardSize.Width >= 420 && sideKeyboardSize.Height >= 120 &&
                sideGroupWidth <= content.Width && mouseFitsRight;

            if (sideBySide)
            {
                int groupHeight = Math.Max(sideKeyboardSize.Height, sideMouseHeight);
                int groupY = AlignedGroupY(content, groupHeight);
                Rectangle keyboardArea = new Rectangle(
                    centeredKeyboardX,
                    groupY + Math.Max(0, (groupHeight - sideKeyboardSize.Height) / 2),
                    sideKeyboardSize.Width,
                    sideKeyboardSize.Height);
                Rectangle mouseArea = new Rectangle(
                    keyboardArea.Right + gap,
                    groupY + Math.Max(0, (groupHeight - sideMouseHeight) / 2),
                    mouseWidth,
                    sideMouseHeight);
                BuildKeyboardButtons(units, measure, keyboardArea, sideScale, true);
                BuildMouseButtons(mouseArea);
                CenterKeyboardButtonsInArea(content, true);
            }
            else
            {
                float compactScale = ResolveKeyboardScale(content, measure, 44.0f);
                Size compactKeyboardSize = KeyboardSize(measure, compactScale);
                Rectangle keyboardArea = new Rectangle(
                    content.X + Math.Max(0, (content.Width - compactKeyboardSize.Width) / 2),
                    content.Y + Math.Max(0, (content.Height - compactKeyboardSize.Height) / 2),
                    compactKeyboardSize.Width,
                    compactKeyboardSize.Height);
                BuildKeyboardButtons(units, measure, keyboardArea, compactScale, true);
                CenterKeyboardButtonsInArea(content, false);
            }
        }

        private int AlignedGroupY(Rectangle content, int groupHeight)
        {
            int spare = Math.Max(0, content.Height - groupHeight);
            if (spare == 0)
            {
                return content.Y;
            }

            return content.Y + spare / 2;
        }

        private void BuildKeyboardButtons(Rectangle area, bool centerVertically)
        {
            List<VirtualButtonDefinition> units = KeyboardUnits(_keyboardLayout);
            KeyboardMeasure measure = MeasureKeyboard(units);
            float scale = ResolveKeyboardScale(area, measure, _showMouse ? 44.0f : 52.0f);
            BuildKeyboardButtons(units, measure, area, scale, centerVertically);
        }

        private KeyboardMeasure MeasureKeyboard(List<VirtualButtonDefinition> units)
        {
            KeyboardMeasure measure = new KeyboardMeasure();
            measure.MaxX = 1.0f;
            measure.MaxY = 1.0f;
            for (int i = 0; i < units.Count; i++)
            {
                measure.MaxX = Math.Max(measure.MaxX, units[i].UnitBounds.Right);
                measure.MaxY = Math.Max(measure.MaxY, units[i].UnitBounds.Bottom);
            }
            return measure;
        }

        private float ResolveKeyboardScale(Rectangle area, KeyboardMeasure measure, float maxScale)
        {
            int verticalPadding = 10;
            float usableWidth = Math.Max(40.0f, area.Width - 8.0f);
            float usableHeight = Math.Max(40.0f, area.Height - verticalPadding * 2.0f);
            float scale = Math.Min(usableWidth / measure.MaxX, usableHeight / measure.MaxY);
            return Math.Max(8.0f, Math.Min(maxScale, scale));
        }

        private Size KeyboardSize(KeyboardMeasure measure, float scale)
        {
            return new Size(
                (int)Math.Ceiling(measure.MaxX * scale),
                (int)Math.Ceiling(measure.MaxY * scale));
        }

        private void BuildKeyboardButtons(List<VirtualButtonDefinition> units, KeyboardMeasure measure, Rectangle area, float scale, bool centerVertically)
        {
            int verticalPadding = 10;
            float maxX = 1.0f;
            float maxY = 1.0f;
            maxX = measure.MaxX;
            maxY = measure.MaxY;

            int usedWidth = (int)Math.Ceiling(maxX * scale);
            int usedHeight = (int)Math.Ceiling(maxY * scale);
            int startX = area.X + Math.Max(0, (area.Width - usedWidth) / 2);
            int startY = centerVertically
                ? area.Y + Math.Max(0, (area.Height - usedHeight) / 2)
                : area.Y + Math.Max(0, Math.Min(verticalPadding, area.Height - usedHeight));
            int gap = Math.Max(1, (int)Math.Round(scale * 0.07f));

            for (int i = 0; i < units.Count; i++)
            {
                VirtualButtonDefinition def = units[i];
                RectangleF u = def.UnitBounds;
                def.Bounds = new Rectangle(
                    startX + (int)(u.X * scale) + gap,
                    startY + (int)(u.Y * scale) + gap,
                    Math.Max(4, (int)(u.Width * scale) - gap * 2),
                    Math.Max(4, (int)(u.Height * scale) - gap * 2));
                ApplyButtonOffset(def);
                _buttons.Add(def);
            }
        }

        private void BuildMouseButtons(Rectangle area)
        {
            int w = Math.Max(150, Math.Min(area.Width - 12, 250));
            int h = Math.Max(150, Math.Min(area.Height - 12, 280));
            int x = area.X + Math.Max(0, (area.Width - w) / 2);
            int y = area.Y + Math.Max(0, (area.Height - h) / 2);
            int topButtonHeight = Math.Max(34, Math.Min(52, h / 5));
            int sideButtonWidth = Math.Max(16, Math.Min(22, w / 11));
            int bodyWidth = Math.Max(92, w - sideButtonWidth * 2 - 36);
            int bodyTop = y + topButtonHeight + 16;
            int bodyHeight = Math.Max(86, h - topButtonHeight - 20);
            Rectangle body = new Rectangle(x + (w - bodyWidth) / 2, bodyTop, bodyWidth, bodyHeight);
            int bodyMid = body.X + body.Width / 2;
            int halfButtonWidth = Math.Max(38, (body.Width - 10) / 2);
            int sideButtonHeight = Math.Max(28, Math.Min(42, body.Height / 4));
            int sideButtonGap = Math.Max(5, body.Height / 22);
            int sideBlockHeight = sideButtonHeight * 2 + sideButtonGap;
            int sideButtonY = body.Y + Math.Max(0, (body.Height - sideBlockHeight) / 2);
            int sideButtonX = body.X - sideButtonWidth + 3;
            int wheelWidth = Math.Max(18, Math.Min(26, body.Width / 5));
            int wheelHeight = Math.Max(38, Math.Min(60, body.Height / 3));

            AddMouse("mouse.body", DisplayName(_mouseStyle), body);
            AddMouse("mouse.left", "Left", new Rectangle(body.X, y, halfButtonWidth, topButtonHeight));
            AddMouse("mouse.right", "Right", new Rectangle(body.Right - halfButtonWidth, y, halfButtonWidth, topButtonHeight));
            AddMouse("mouse.middle", "", new Rectangle(bodyMid - wheelWidth / 2, bodyTop + body.Height / 3, wheelWidth, wheelHeight));
            AddMouse("mouse.x1", "X1", new Rectangle(sideButtonX, sideButtonY, sideButtonWidth, sideButtonHeight));
            AddMouse("mouse.x2", "X2", new Rectangle(sideButtonX, sideButtonY + sideButtonHeight + sideButtonGap, sideButtonWidth, sideButtonHeight));
        }

        private void AddMouse(string id, string label, Rectangle bounds)
        {
            VirtualButtonDefinition def = new VirtualButtonDefinition();
            def.Id = id;
            def.Label = label;
            def.Kind = "mouse";
            def.VkCode = 0;
            def.Bounds = bounds;
            ApplyButtonOffset(def);
            _buttons.Add(def);
        }

        private void ApplyButtonOffset(VirtualButtonDefinition def)
        {
            VirtualButtonConfig config = GetOverride(def.Id);
            if (config != null && (config.OffsetX != 0 || config.OffsetY != 0))
            {
                def.Bounds = new Rectangle(
                    def.Bounds.X + config.OffsetX,
                    def.Bounds.Y + config.OffsetY,
                    def.Bounds.Width,
                    def.Bounds.Height);
            }
        }

        private List<VirtualButtonDefinition> KeyboardUnits(string layout)
        {
            bool compact = string.Equals(layout, "60", StringComparison.OrdinalIgnoreCase);
            bool tkl = string.Equals(layout, "tkl", StringComparison.OrdinalIgnoreCase);
            bool laptop = string.Equals(layout, "laptop", StringComparison.OrdinalIgnoreCase);
            bool full = !compact && !tkl && !laptop;
            List<VirtualButtonDefinition> list = new List<VirtualButtonDefinition>();

            AddRow(list, 0, 0, new object[] { "Esc", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=", new KeySpec("Back", 2.0f) });
            AddRow(list, 0, 1.15f, new object[] { new KeySpec("Tab", 1.5f), "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "[", "]", new KeySpec("\\", 1.5f) });
            AddRow(list, 0, 2.3f, new object[] { new KeySpec("Caps", 1.75f), "A", "S", "D", "F", "G", "H", "J", "K", "L", ";", "'", new KeySpec("Enter", 2.25f) });
            if (laptop)
            {
                AddRow(list, 0, 3.45f, new object[] { new KeySpec("Shift", 2.25f), "Z", "X", "C", "V", "B", "N", "M", ",", ".", "/", new KeySpec("Shift", 1.25f), new KeySpec("Up", 1.0f, "key.Up", 0x26) });
                AddRow(list, 0, 4.6f, new object[] { new KeySpec("Ctrl", 1.35f), new KeySpec("Win", 1.2f), new KeySpec("Alt", 1.2f), new KeySpec("Space", 5.8f), new KeySpec("Alt", 1.2f), new KeySpec("Fn", 1.2f), new KeySpec("<", 1.0f, "key.Left", 0x25), new KeySpec("Dn", 1.0f, "key.Down", 0x28), new KeySpec(">", 1.0f, "key.Right", 0x27) });
            }
            else
            {
                AddRow(list, 0, 3.45f, new object[] { new KeySpec("Shift", 2.25f), "Z", "X", "C", "V", "B", "N", "M", ",", ".", "/", new KeySpec("Shift", 2.75f) });
                AddRow(list, 0, 4.6f, new object[] { new KeySpec("Ctrl", 1.35f), new KeySpec("Win", 1.2f), new KeySpec("Alt", 1.2f), new KeySpec("Space", 5.8f), new KeySpec("Alt", 1.2f), new KeySpec("Fn", 1.2f), new KeySpec("Menu", 1.1f), new KeySpec("Ctrl", 1.0f) });
            }

            if (!compact && !laptop)
            {
                AddKey(list, "Prt", 15.5f, 0, 1.0f);
                AddKey(list, "Home", 16.65f, 0, 1.1f);
                AddKey(list, "End", 17.8f, 0, 1.0f);
                AddKey(list, "Ins", 15.5f, 1.15f, 1.0f);
                AddKey(list, "Del", 16.65f, 1.15f, 1.0f);
                AddKey(list, "PgUp", 17.8f, 1.15f, 1.1f);
                AddKey(list, "PgDn", 17.8f, 2.3f, 1.1f);
                AddKey(list, "Up", 16.65f, 3.45f, 1.0f);
                AddKey(list, "<", 15.5f, 4.6f, 1.0f, "key.Left", 0x25);
                AddKey(list, "Dn", 16.65f, 4.6f, 1.0f, "key.Down", 0x28);
                AddKey(list, ">", 17.8f, 4.6f, 1.0f, "key.Right", 0x27);
            }

            if (full)
            {
                AddKey(list, "Num", 19.2f, 1.15f, 1.0f);
                AddKey(list, "/", 20.35f, 1.15f, 1.0f);
                AddKey(list, "*", 21.5f, 1.15f, 1.0f);
                AddKey(list, "-", 22.65f, 1.15f, 1.0f);
                AddKey(list, "7", 19.2f, 2.3f, 1.0f, "key.Num7", 0x67);
                AddKey(list, "8", 20.35f, 2.3f, 1.0f, "key.Num8", 0x68);
                AddKey(list, "9", 21.5f, 2.3f, 1.0f, "key.Num9", 0x69);
                AddKey(list, "+", 22.65f, 2.3f, 1.0f);
                AddKey(list, "4", 19.2f, 3.45f, 1.0f, "key.Num4", 0x64);
                AddKey(list, "5", 20.35f, 3.45f, 1.0f, "key.Num5", 0x65);
                AddKey(list, "6", 21.5f, 3.45f, 1.0f, "key.Num6", 0x66);
                AddKey(list, "1", 19.2f, 4.6f, 1.0f, "key.Num1", 0x61);
                AddKey(list, "2", 20.35f, 4.6f, 1.0f, "key.Num2", 0x62);
                AddKey(list, "3", 21.5f, 4.6f, 1.0f, "key.Num3", 0x63);
                AddKey(list, "Enter", 22.65f, 3.45f, 1.0f);
                AddKey(list, "0", 19.2f, 5.75f, 2.15f, "key.Num0", 0x60);
                AddKey(list, ".", 21.5f, 5.75f, 1.0f, "key.NumDot", 0x6E);
                AddKey(list, "Enter", 22.65f, 4.6f, 1.0f);
            }

            return list;
        }

        private void AddRow(List<VirtualButtonDefinition> list, float x, float y, object[] keys)
        {
            float cursor = x;
            for (int i = 0; i < keys.Length; i++)
            {
                KeySpec spec = keys[i] as KeySpec;
                string label;
                float width;
                if (spec != null)
                {
                    label = spec.Label;
                    width = spec.Width;
                }
                else
                {
                    label = (string)keys[i];
                    width = 1.0f;
                }
                if (spec != null && !string.IsNullOrWhiteSpace(spec.Id))
                {
                    AddKey(list, label, cursor, y, width, spec.Id, spec.VkCode);
                }
                else
                {
                    AddKey(list, label, cursor, y, width);
                }
                cursor += width + 0.15f;
            }
        }

        private void AddKey(List<VirtualButtonDefinition> list, string label, float x, float y, float width)
        {
            AddKey(list, label, x, y, width, null, 0);
        }

        private void AddKey(List<VirtualButtonDefinition> list, string label, float x, float y, float width, string id, int vkCode)
        {
            VirtualButtonDefinition def = new VirtualButtonDefinition();
            def.Label = label;
            def.Kind = "key";
            def.VkCode = vkCode == 0 ? VkForLabel(label) : vkCode;
            def.Id = id ?? ("key." + label.Replace("\\", "Backslash").Replace("/", "Slash").Replace(" ", ""));
            def.UnitBounds = new RectangleF(x, y, width, 1.0f);
            list.Add(def);
        }

        private int VkForLabel(string label)
        {
            if (label.Length == 1)
            {
                char c = char.ToUpperInvariant(label[0]);
                if (c >= 'A' && c <= 'Z')
                {
                    return (int)c;
                }
                if (c >= '0' && c <= '9')
                {
                    return (int)c;
                }
            }

            switch (label)
            {
                case "Esc": return 0x1B;
                case "Back": return 0x08;
                case "Tab": return 0x09;
                case "Caps": return 0x14;
                case "Enter": return 0x0D;
                case "Shift": return 0x10;
                case "Ctrl": return 0x11;
                case "Alt": return 0x12;
                case "Space": return 0x20;
                case "Win": return 0x5B;
                case "Menu": return 0x5D;
                case "Up": return 0x26;
                case "Down": return 0x28;
                case "Left": return 0x25;
                case "Right": return 0x27;
                case "Del": return 0x2E;
                case "Ins": return 0x2D;
                case "Home": return 0x24;
                case "End": return 0x23;
                case "PgUp": return 0x21;
                case "PgDn": return 0x22;
                case "-": return 0xBD;
                case "=": return 0xBB;
                case "[": return 0xDB;
                case "]": return 0xDD;
                case "\\": return 0xDC;
                case ";": return 0xBA;
                case "'": return 0xDE;
                case ",": return 0xBC;
                case ".": return 0xBE;
                case "/": return 0xBF;
                default: return 0;
            }
        }

        private VirtualButtonDefinition HitTest(Point point)
        {
            for (int i = _buttons.Count - 1; i >= 0; i--)
            {
                if (_buttons[i].Bounds.Contains(point))
                {
                    return _buttons[i];
                }
            }
            return null;
        }

        private void ShowButtonMenu(VirtualButtonDefinition button, Point location)
        {
            ContextMenu menu = new ContextMenu();
            menu.MenuItems.Add(new MenuItem("Test sound", delegate { ActivateButton(button); }));
            menu.MenuItems.Add(new MenuItem("Customize...", delegate { EditButton(button); }));
            menu.MenuItems.Add("-");
            menu.MenuItems.Add(new MenuItem("Clear customization", delegate { ClearButton(button); }));
            menu.Show(this, location);
        }

        private void ActivateButton(VirtualButtonDefinition button)
        {
            FlashButton(button.Id);
            EventHandler<VirtualButtonEventArgs> handler = ButtonActivated;
            if (handler != null)
            {
                handler(this, new VirtualButtonEventArgs(button.Id, button.VkCode, button.Kind == "mouse"));
            }
        }

        private void EditButton(VirtualButtonDefinition button)
        {
            VirtualButtonConfig current = CloneConfig(GetOverride(button.Id));
            using (VirtualButtonDialog dialog = new VirtualButtonDialog(button.Label, current))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    if (dialog.ResultConfig == null || dialog.ResultConfig.IsEmpty())
                    {
                        _overrides.Remove(button.Id);
                    }
                    else
                    {
                        _overrides[button.Id] = dialog.ResultConfig;
                    }
                    Invalidate();
                    EventHandler handler = ButtonCustomizationChanged;
                    if (handler != null)
                    {
                        handler(this, EventArgs.Empty);
                    }
                }
            }
        }

        private void ClearButton(VirtualButtonDefinition button)
        {
            _overrides.Remove(button.Id);
            Invalidate();
            EventHandler handler = ButtonCustomizationChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private VirtualButtonConfig GetOverride(string id)
        {
            VirtualButtonConfig config;
            if (_overrides != null && _overrides.TryGetValue(id, out config) && config != null)
            {
                return config;
            }
            return new VirtualButtonConfig();
        }

        private static VirtualButtonConfig CloneConfig(VirtualButtonConfig source)
        {
            VirtualButtonConfig clone = new VirtualButtonConfig();
            if (source != null)
            {
                clone.Label = source.Label;
                clone.IconText = source.IconText;
                clone.SoundPath = source.SoundPath;
                clone.OffsetX = source.OffsetX;
                clone.OffsetY = source.OffsetY;
            }
            return clone;
        }

        private void PruneActiveButtons()
        {
            if (_activeUntil.Count == 0)
            {
                return;
            }

            long now = Environment.TickCount;
            List<string> expired = new List<string>();
            foreach (KeyValuePair<string, long> pair in _activeUntil)
            {
                if (pair.Value <= now)
                {
                    expired.Add(pair.Key);
                }
            }

            for (int i = 0; i < expired.Count; i++)
            {
                _activeUntil.Remove(expired[i]);
            }

            if (expired.Count > 0)
            {
                Invalidate();
            }
        }

        private static string DisplayName(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return "";
            }
            if (string.Equals(id, "tkl", StringComparison.OrdinalIgnoreCase))
            {
                return "TKL";
            }
            if (string.Equals(id, "60", StringComparison.OrdinalIgnoreCase))
            {
                return "60%";
            }
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(id.Replace("_", " "));
        }

        private sealed class KeySpec
        {
            public readonly string Label;
            public readonly float Width;
            public readonly string Id;
            public readonly int VkCode;

            public KeySpec(string label, float width)
            {
                Label = label;
                Width = width;
                Id = null;
                VkCode = 0;
            }

            public KeySpec(string label, float width, string id, int vkCode)
            {
                Label = label;
                Width = width;
                Id = id;
                VkCode = vkCode;
            }
        }
    }

    internal sealed class VirtualButtonDialog : Form
    {
        private readonly TextBox _labelBox;
        private readonly TextBox _iconBox;
        private readonly TextBox _soundBox;
        private readonly NumericUpDown _offsetXBox;
        private readonly NumericUpDown _offsetYBox;

        public VirtualButtonConfig ResultConfig { get; private set; }

        public VirtualButtonDialog(string defaultLabel, VirtualButtonConfig config)
        {
            Text = "Customize " + defaultLabel;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new Size(560, 340);
            Font = new Font("Segoe UI", 9.0f);
            BackColor = Color.White;

            Label labelText = new Label();
            labelText.Text = "Name";
            labelText.SetBounds(24, 26, 120, 26);
            Controls.Add(labelText);

            _labelBox = new TextBox();
            _labelBox.SetBounds(150, 24, 376, 28);
            _labelBox.Text = config != null ? config.Label : "";
            Controls.Add(_labelBox);

            Label iconText = new Label();
            iconText.Text = "Icon";
            iconText.SetBounds(24, 70, 120, 26);
            Controls.Add(iconText);

            _iconBox = new TextBox();
            _iconBox.SetBounds(150, 68, 376, 28);
            _iconBox.Text = config != null ? config.IconText : "";
            Controls.Add(_iconBox);

            Label soundText = new Label();
            soundText.Text = "Sound";
            soundText.SetBounds(24, 114, 120, 26);
            Controls.Add(soundText);

            _soundBox = new TextBox();
            _soundBox.SetBounds(150, 112, 280, 28);
            _soundBox.Text = config != null ? config.SoundPath : "";
            Controls.Add(_soundBox);

            Button browse = new Button();
            browse.Text = "Browse";
            browse.SetBounds(442, 110, 84, 32);
            browse.Click += delegate { BrowseSound(); };
            Controls.Add(browse);

            Button clear = new Button();
            clear.Text = "Clear sound";
            clear.SetBounds(150, 154, 116, 32);
            clear.Click += delegate { _soundBox.Text = ""; };
            Controls.Add(clear);

            Label offsetText = new Label();
            offsetText.Text = "Position";
            offsetText.SetBounds(24, 214, 120, 26);
            Controls.Add(offsetText);

            _offsetXBox = new NumericUpDown();
            _offsetXBox.Minimum = -120;
            _offsetXBox.Maximum = 120;
            _offsetXBox.SetBounds(150, 212, 86, 28);
            _offsetXBox.Value = config != null ? config.OffsetX : 0;
            Controls.Add(_offsetXBox);

            Label xText = new Label();
            xText.Text = "X";
            xText.SetBounds(246, 216, 28, 24);
            Controls.Add(xText);

            _offsetYBox = new NumericUpDown();
            _offsetYBox.Minimum = -120;
            _offsetYBox.Maximum = 120;
            _offsetYBox.SetBounds(284, 212, 86, 28);
            _offsetYBox.Value = config != null ? config.OffsetY : 0;
            Controls.Add(_offsetYBox);

            Label yText = new Label();
            yText.Text = "Y";
            yText.SetBounds(380, 216, 28, 24);
            Controls.Add(yText);

            Button ok = new Button();
            ok.Text = "OK";
            ok.DialogResult = DialogResult.OK;
            ok.SetBounds(346, 286, 84, 34);
            ok.Click += delegate { SaveResult(); };
            Controls.Add(ok);

            Button cancel = new Button();
            cancel.Text = "Cancel";
            cancel.DialogResult = DialogResult.Cancel;
            cancel.SetBounds(442, 286, 84, 34);
            Controls.Add(cancel);

            AcceptButton = ok;
            CancelButton = cancel;
        }

        private void BrowseSound()
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "WAV files (*.wav)|*.wav|All files (*.*)|*.*";
                dialog.Title = "Choose button sound";
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _soundBox.Text = dialog.FileName;
                }
            }
        }

        private void SaveResult()
        {
            VirtualButtonConfig config = new VirtualButtonConfig();
            config.Label = _labelBox.Text.Trim();
            config.IconText = _iconBox.Text.Trim();
            config.SoundPath = _soundBox.Text.Trim();
            config.OffsetX = (int)_offsetXBox.Value;
            config.OffsetY = (int)_offsetYBox.Value;
            ResultConfig = config;
        }
    }

    internal sealed class CleanTrackBar : Control
    {
        private int _minimum;
        private int _maximum;
        private int _value;
        private int _smallChange;
        private int _largeChange;
        private int _tickFrequency;
        private TickStyle _tickStyle;
        private bool _dragging;

        public event EventHandler ValueChanged;

        public CleanTrackBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Height = 34;
            TabStop = false;
            _maximum = 100;
            _smallChange = 1;
            _largeChange = 5;
            _tickFrequency = 10;
            _tickStyle = TickStyle.None;
        }

        public int Minimum
        {
            get { return _minimum; }
            set
            {
                _minimum = value;
                if (_maximum < _minimum)
                {
                    _maximum = _minimum;
                }
                Value = _value;
                Invalidate();
            }
        }

        public int Maximum
        {
            get { return _maximum; }
            set
            {
                _maximum = Math.Max(_minimum, value);
                Value = _value;
                Invalidate();
            }
        }

        public int Value
        {
            get { return _value; }
            set
            {
                int next = Clamp(value);
                if (_value == next)
                {
                    return;
                }
                _value = next;
                Invalidate();
                EventHandler handler = ValueChanged;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            }
        }

        public int SmallChange
        {
            get { return _smallChange; }
            set { _smallChange = Math.Max(1, value); }
        }

        public int LargeChange
        {
            get { return _largeChange; }
            set { _largeChange = Math.Max(1, value); }
        }

        public int TickFrequency
        {
            get { return _tickFrequency; }
            set { _tickFrequency = Math.Max(1, value); }
        }

        public TickStyle TickStyle
        {
            get { return _tickStyle; }
            set { _tickStyle = value; }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            int trackY = Height / 2;
            Rectangle trackRect = new Rectangle(8, trackY - 2, Math.Max(4, Width - 16), 4);
            using (GraphicsPath trackPath = RoundedRect(trackRect, 3))
            using (SolidBrush trackBrush = new SolidBrush(Color.FromArgb(207, 216, 214)))
            {
                e.Graphics.FillPath(trackBrush, trackPath);
            }

            int thumbX = ValueToX();
            Rectangle fillRect = new Rectangle(trackRect.X, trackRect.Y, Math.Max(0, thumbX - trackRect.X), trackRect.Height);
            if (fillRect.Width > 0)
            {
                using (GraphicsPath fillPath = RoundedRect(fillRect, 3))
                using (SolidBrush fillBrush = new SolidBrush(Color.FromArgb(20, 125, 139)))
                {
                    e.Graphics.FillPath(fillBrush, fillPath);
                }
            }

            Rectangle thumb = new Rectangle(thumbX - 7, trackY - 10, 14, 20);
            using (GraphicsPath thumbPath = RoundedRect(thumb, 7))
            using (SolidBrush thumbBrush = new SolidBrush(Color.FromArgb(12, 123, 214)))
            using (Pen thumbBorder = new Pen(Color.FromArgb(238, 246, 248), 1.2f))
            {
                e.Graphics.FillPath(thumbBrush, thumbPath);
                e.Graphics.DrawPath(thumbBorder, thumbPath);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _dragging = true;
                Capture = true;
                SetValueFromMouse(e.X);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragging)
            {
                SetValueFromMouse(e.X);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _dragging = false;
            Capture = false;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            int delta = e.Shift ? LargeChange : SmallChange;
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Down)
            {
                Value = Math.Max(Minimum, Value - delta);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Up)
            {
                Value = Math.Min(Maximum, Value + delta);
                e.Handled = true;
            }
            else
            {
                base.OnKeyDown(e);
            }
        }

        private void SetValueFromMouse(int x)
        {
            int left = 8;
            int right = Math.Max(left + 1, Width - 8);
            double ratio = (x - left) / (double)(right - left);
            ratio = Math.Max(0.0, Math.Min(1.0, ratio));
            int next = Minimum + (int)Math.Round((Maximum - Minimum) * ratio);
            next = Math.Max(Minimum, Math.Min(Maximum, next));
            if (next != Value)
            {
                Value = next;
            }
        }

        private int ValueToX()
        {
            if (Maximum <= Minimum)
            {
                return 8;
            }

            double ratio = (Value - Minimum) / (double)(Maximum - Minimum);
            return 8 + (int)Math.Round((Width - 16) * ratio);
        }

        private int Clamp(int value)
        {
            return Math.Max(_minimum, Math.Min(_maximum, value));
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = Math.Max(2, radius * 2);
            if (r.Width <= d || r.Height <= d)
            {
                path.AddRectangle(r);
                path.CloseFigure();
                return path;
            }

            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class TrayMenuForm : Form
    {
        public TrayMenuForm(
            Icon appIcon,
            bool enabled,
            bool startup,
            bool repeat,
            bool modifiers,
            bool ignoreInjected,
            int volume,
            int keyBoost,
            string preset,
            Action settings,
            Action toggleEnabled,
            Action testSound,
            Action openConfig,
            Action runAdmin,
            Action toggleStartup,
            Action toggleRepeat,
            Action toggleModifiers,
            Action toggleInjected,
            Action exit)
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            Opacity = 0.92;
            BackColor = Color.FromArgb(26, 26, 27);
            ClientSize = new Size(382, 650);
            Font = new Font("Segoe UI", 9.0f);

            ModernPanel shell = new ModernPanel();
            shell.FillColor = Color.FromArgb(36, 37, 38);
            shell.FillColor2 = Color.FromArgb(17, 18, 19);
            shell.BorderColor = Color.FromArgb(96, 99, 101);
            shell.CornerRadius = 18;
            shell.ShadowSize = 0;
            shell.SetBounds(0, 0, ClientSize.Width, ClientSize.Height);
            Controls.Add(shell);

            AddDot(shell, 26, Color.FromArgb(255, 58, 56));
            AddDot(shell, 48, Color.FromArgb(255, 189, 46));
            AddDot(shell, 70, Color.FromArgb(39, 201, 63));

            PictureBox icon = new PictureBox();
            icon.Image = appIcon != null ? appIcon.ToBitmap() : SystemIcons.Application.ToBitmap();
            icon.SizeMode = PictureBoxSizeMode.StretchImage;
            icon.SetBounds(28, 62, 50, 50);
            shell.Controls.Add(icon);

            Label title = new Label();
            title.Text = "CreamyKeys";
            title.AutoSize = false;
            title.Font = new Font("Segoe UI Semibold", 13.2f);
            title.ForeColor = Color.White;
            title.TextAlign = ContentAlignment.MiddleLeft;
            title.SetBounds(92, 55, 250, 42);
            shell.Controls.Add(title);

            Label subtitle = new Label();
            subtitle.Text = enabled ? "Desktop sound is enabled" : "Desktop sound is disabled";
            subtitle.ForeColor = Color.FromArgb(196, 199, 198);
            subtitle.TextAlign = ContentAlignment.MiddleLeft;
            subtitle.SetBounds(94, 92, 270, 30);
            shell.Controls.Add(subtitle);

            Panel line = new Panel();
            line.BackColor = Color.FromArgb(88, 90, 91);
            line.SetBounds(28, 134, 326, 1);
            shell.Controls.Add(line);

            int y = 142;
            y = AddRow(shell, y, "\u2699", "Settings", "", true, settings);
            y = AddRow(shell, y, "\u23fb", enabled ? "Disable" : "Enable", "", false, toggleEnabled);
            y = AddRow(shell, y, "\u266b", "Test sound", "", false, testSound);
            y = AddDivider(shell, y);
            y = AddRow(shell, y, "\u25b8", "Preset", preset, false, settings);
            y = AddRow(shell, y, "\u25b8", "Output volume", volume.ToString(CultureInfo.InvariantCulture) + "%", false, settings);
            y = AddRow(shell, y, "\u25b8", "Key boost", keyBoost.ToString(CultureInfo.InvariantCulture) + "%", false, settings);
            y = AddDivider(shell, y);
            y = AddRow(shell, y, startup ? "\u2713" : "", "Run at startup", "", false, toggleStartup);
            y = AddRow(shell, y, repeat ? "\u2713" : "", "Held key repeat", "", false, toggleRepeat);
            y = AddRow(shell, y, modifiers ? "\u2713" : "", "Shift / Ctrl / Alt", "", false, toggleModifiers);
            y = AddRow(shell, y, ignoreInjected ? "\u2713" : "", "Ignore injected input", "", false, toggleInjected);
            y = AddDivider(shell, y);
            y = AddRow(shell, y, "\u25a3", "Open config folder", "", false, openConfig);
            y = AddRow(shell, y, "\u26a1", "Run as admin", "", false, runAdmin);
            y = AddDivider(shell, y);
            AddRow(shell, y, "\u21b2", "Exit", "", false, exit);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Close();
        }

        private static void AddDot(Control parent, int x, Color color)
        {
            DotControl dot = new DotControl(color);
            dot.SetBounds(x, 27, 12, 12);
            parent.Controls.Add(dot);
        }

        private int AddRow(Control parent, int y, string icon, string text, string detail, bool active, Action action)
        {
            TrayMenuRow row = new TrayMenuRow(icon, text, detail, active);
            row.SetBounds(28, y, 326, 32);
            row.Click += delegate
            {
                Close();
                if (action != null)
                {
                    action();
                }
            };
            parent.Controls.Add(row);
            return y + 34;
        }

        private static int AddDivider(Control parent, int y)
        {
            Panel line = new Panel();
            line.BackColor = Color.FromArgb(76, 78, 79);
            line.SetBounds(28, y + 5, 326, 1);
            parent.Controls.Add(line);
            return y + 12;
        }
    }

    internal sealed class TrayMenuRow : Control
    {
        private readonly string _icon;
        private readonly string _label;
        private readonly string _detail;
        private readonly bool _active;
        private bool _hover;

        public TrayMenuRow(string icon, string label, string detail, bool active)
        {
            _icon = icon ?? "";
            _label = label ?? "";
            _detail = detail ?? "";
            _active = active;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hover = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bg = new Rectangle(0, 0, Width - 1, Height - 1);
            if (_active || _hover)
            {
                Color fill = _active ? Color.FromArgb(202, 18, 26) : Color.FromArgb(64, 65, 66);
                using (GraphicsPath path = RoundedRect(bg, 7))
                using (SolidBrush brush = new SolidBrush(fill))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }

            Color fore = Color.White;
            TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;

            Rectangle iconRect = new Rectangle(18, 0, 30, Height);
            using (Font iconFont = new Font("Segoe UI Symbol", 10.2f))
            {
                TextRenderer.DrawText(e.Graphics, _icon, iconFont, iconRect, fore,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }

            int detailWidth = string.IsNullOrWhiteSpace(_detail) ? 0 :
                (_detail.EndsWith("%", StringComparison.OrdinalIgnoreCase) ? 76 : 118);
            Rectangle textRect = new Rectangle(60, 0, Width - 84 - detailWidth, Height);
            using (Font textFont = new Font("Segoe UI", 9.35f))
            {
                TextRenderer.DrawText(e.Graphics, _label, textFont, textRect, fore, flags);
            }

            if (detailWidth > 0)
            {
                Rectangle detailRect = new Rectangle(Width - detailWidth - 18, 0, detailWidth, Height);
                using (Font detailFont = new Font("Segoe UI", 9.0f))
                {
                    TextRenderer.DrawText(e.Graphics, _detail, detailFont, detailRect,
                        Color.FromArgb(236, 236, 236),
                        TextFormatFlags.Right | TextFormatFlags.VerticalCenter |
                        TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                }
            }
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = Math.Max(2, radius * 2);
            if (r.Width <= d || r.Height <= d)
            {
                path.AddRectangle(r);
                path.CloseFigure();
                return path;
            }

            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class DotControl : Control
    {
        private readonly Color _color;

        public DotControl(Color color)
        {
            _color = color;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush brush = new SolidBrush(_color))
            {
                e.Graphics.FillEllipse(brush, 1, 1, Width - 2, Height - 2);
            }
        }
    }

    internal sealed class RailCreditLabel : Control
    {
        public RailCreditLabel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            ForeColor = Color.FromArgb(238, 241, 240);
            Font = new Font("Segoe UI Semibold", 9.0f);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            string text = Text ?? string.Empty;
            if (text.Length == 0 || Width <= 0 || Height <= 0)
            {
                return;
            }

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

            using (StringFormat format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                format.FormatFlags = StringFormatFlags.NoWrap;
                format.Trimming = StringTrimming.EllipsisCharacter;

                float size = FitFontSize(g, text);
                using (Font drawFont = new Font(Font.FontFamily, size, Font.Style))
                using (SolidBrush brush = new SolidBrush(ForeColor))
                {
                    g.TranslateTransform(Width / 2.0f, Height / 2.0f);
                    g.RotateTransform(-90.0f);
                    RectangleF drawRect = new RectangleF(-Height / 2.0f, -Width / 2.0f, Height, Width);
                    g.DrawString(text, drawFont, brush, drawRect, format);
                    g.ResetTransform();
                }
            }
        }

        private float FitFontSize(Graphics g, string text)
        {
            for (float size = 9.0f; size >= 6.4f; size -= 0.2f)
            {
                using (Font testFont = new Font(Font.FontFamily, size, Font.Style))
                {
                    SizeF measured = g.MeasureString(text, testFont);
                    if (measured.Width <= Height - 8 && measured.Height <= Width - 6)
                    {
                        return size;
                    }
                }
            }
            return 6.4f;
        }
    }

    internal sealed class ModernPanel : Panel
    {
        public Color FillColor = Color.FromArgb(238, 241, 240);
        public Color FillColor2 = Color.Empty;
        public Color BorderColor = Color.FromArgb(220, 225, 224);
        public Color ShadowColor = Color.FromArgb(45, 0, 0, 0);
        public int CornerRadius = 22;
        public int ShadowSize = 0;
        public int ShadowOffsetX = 0;
        public int ShadowOffsetY = 0;

        public ModernPanel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.ResizeRedraw |
                ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.Transparent;
        }

        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            Rectangle oldBounds = Bounds;
            base.SetBoundsCore(x, y, width, height, specified);
            if (Parent != null)
            {
                Rectangle dirty = Rectangle.Union(oldBounds, Bounds);
                int inflate = ShadowSize + Math.Max(Math.Abs(ShadowOffsetX), Math.Abs(ShadowOffsetY)) + 8;
                dirty.Inflate(inflate, inflate);
                Parent.Invalidate(dirty, true);
            }
            Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (Parent != null)
            {
                using (SolidBrush brush = new SolidBrush(Parent.BackColor))
                {
                    e.Graphics.FillRectangle(brush, ClientRectangle);
                }
            }
            else
            {
                base.OnPaintBackground(e);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle main = new Rectangle(
                Math.Max(1, ShadowSize - Math.Min(0, ShadowOffsetX)),
                Math.Max(1, ShadowSize - Math.Min(0, ShadowOffsetY)),
                Math.Max(1, Width - ShadowSize * 2 - Math.Abs(ShadowOffsetX) - 1),
                Math.Max(1, Height - ShadowSize * 2 - Math.Abs(ShadowOffsetY) - 1));

            if (ShadowSize > 0)
            {
                Rectangle shadowRect = new Rectangle(
                    main.X + ShadowOffsetX,
                    main.Y + ShadowOffsetY,
                    main.Width,
                    main.Height);
                using (GraphicsPath shadowPath = RoundedRect(shadowRect, CornerRadius))
                using (SolidBrush shadowBrush = new SolidBrush(ShadowColor))
                {
                    e.Graphics.FillPath(shadowBrush, shadowPath);
                }
            }

            using (GraphicsPath path = RoundedRect(main, CornerRadius))
            using (Pen border = new Pen(BorderColor))
            {
                if (FillColor2 == Color.Empty)
                {
                    using (SolidBrush brush = new SolidBrush(FillColor))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                }
                else
                {
                    using (LinearGradientBrush brush = new LinearGradientBrush(main, FillColor, FillColor2, LinearGradientMode.Vertical))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                }

                e.Graphics.DrawPath(border, path);
            }

            base.OnPaint(e);
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = Math.Max(2, radius * 2);
            if (r.Width <= d || r.Height <= d)
            {
                path.AddRectangle(r);
                path.CloseFigure();
                return path;
            }

            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class HiddenTabControl : TabControl
    {
        private const int TcmAdjustRect = 0x1328;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == TcmAdjustRect && !DesignMode)
            {
                m.Result = (IntPtr)1;
                return;
            }
            base.WndProc(ref m);
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly string _assetsRoot;
        private readonly ConfigStore _configStore;
        private readonly AudioEngine _audio;
        private readonly KeyboardHook _hook;
        private readonly MouseHook _mouseHook;
        private readonly Stopwatch _stopwatch;
        private readonly NotifyIcon _trayIcon;
        private readonly List<PresetInfo> _presets;
        private AppConfig _config;
        private bool _allowClose;
        private bool _loadingControls;
        private bool _hookInstalled;
        private bool _mouseHookInstalled;
        private long _lastPlayTicks;

        private VirtualDeviceControl _deviceView;
        private ComboBox _keyboardLayoutCombo;
        private ComboBox _mouseStyleCombo;
        private CheckBox _autoDetectCheck;
        private Button _detectButton;
        private CheckBox _mouseSoundsCheck;
        private CheckBox _showMouseCheck;
        private CheckBox _editModeCheck;
        private ComboBox _presetCombo;
        private CheckBox _enabledCheck;
        private CheckBox _keyboardSoundsCheck;
        private CheckBox _playRepeatCheck;
        private CheckBox _playModifiersCheck;
        private CheckBox _ignoreInjectedCheck;
        private CheckBox _runStartupCheck;
        private CleanTrackBar _volumeTrack;
        private CleanTrackBar _keyGainTrack;
        private CleanTrackBar _randomVolumeTrack;
        private CleanTrackBar _randomPitchTrack;
        private NumericUpDown _cooldownNumber;
        private NumericUpDown _maxVoicesNumber;
        private TextBox _excludedBox;
        private CheckBox _appAllowListCheck;
        private CheckedListBox _allowedAppsList;
        private TextBox _manualAppBox;
        private CheckBox _shadowEnabledCheck;
        private CleanTrackBar _shadowDepthTrack;
        private CleanTrackBar _shadowXTrack;
        private CleanTrackBar _shadowYTrack;
        private NumericUpDown _volumeLabel;
        private NumericUpDown _keyGainLabel;
        private NumericUpDown _randomVolumeLabel;
        private NumericUpDown _randomPitchLabel;
        private NumericUpDown _shadowDepthLabel;
        private NumericUpDown _shadowXLabel;
        private NumericUpDown _shadowYLabel;
        private Label _statusLabel;
        private Button _adminButton;
        private TrayMenuForm _trayPopup;
        private MenuItem _trayEnabledItem;
        private MenuItem _trayStartupItem;
        private MenuItem _trayRepeatItem;
        private MenuItem _trayModifiersItem;
        private MenuItem _trayInjectedItem;
        private List<MenuItem> _trayPresetItems;
        private List<MenuItem> _trayVolumeItems;
        private List<MenuItem> _trayGainItems;
        private Icon _appIcon;

        public MainForm()
        {
            _assetsRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
            _configStore = new ConfigStore();
            _presets = SoundLibrary.Scan(_assetsRoot);
            string fallbackPreset = _presets.Count > 0 ? _presets[0].Id : "";
            _config = _configStore.Load(fallbackPreset);
            if (FindPreset(_config.Preset) == null && fallbackPreset.Length > 0)
            {
                _config.Preset = fallbackPreset;
            }
            if (_config.AutoDetectDevices)
            {
                ApplyDetectedDevices(DeviceDetector.Detect());
            }

            _audio = new AudioEngine();
            _hook = new KeyboardHook();
            _hook.KeyPressed += OnHookKeyPressed;
            _mouseHook = new MouseHook();
            _mouseHook.MouseButtonPressed += OnMouseButtonPressed;
            _stopwatch = Stopwatch.StartNew();

            BuildUi();
            _trayIcon = BuildTrayIcon();
            LoadControlsFromConfig();
            ApplyRuntimeSettings(true);

            try
            {
                _hook.Install();
                _hookInstalled = true;
            }
            catch (Exception ex)
            {
                _hookInstalled = false;
                MessageBox.Show("Could not install the global keyboard hook:\r\n" + ex.Message,
                    "CreamyKeys", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            try
            {
                _mouseHook.Install();
                _mouseHookInstalled = true;
            }
            catch (Exception ex)
            {
                _mouseHookInstalled = false;
                MessageBox.Show("Could not install the global mouse hook:\r\n" + ex.Message,
                    "CreamyKeys", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            UpdateStatus("Ready.");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                    _trayIcon.Dispose();
                }
                if (_trayPopup != null)
                {
                    _trayPopup.Close();
                    _trayPopup.Dispose();
                }
                if (_hook != null)
                {
                    _hook.Dispose();
                }
                if (_mouseHook != null)
                {
                    _mouseHook.Dispose();
                }
                if (_audio != null)
                {
                    _audio.Dispose();
                }
                if (_appIcon != null)
                {
                    _appIcon.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_allowClose)
            {
                e.Cancel = true;
                Hide();
                return;
            }
            base.OnFormClosing(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate(true);
        }

        private void BuildUi()
        {
            _appIcon = LoadAppIcon();

            Text = "CreamyKeys";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            ClientSize = new Size(1180, 820);
            MinimumSize = new Size(980, 680);
            AutoScaleMode = AutoScaleMode.None;
            Font = new Font("Segoe UI", 9.0f);
            BackColor = Color.FromArgb(188, 190, 187);
            Icon = _appIcon;

            ModernPanel rail = new ModernPanel();
            rail.FillColor = Color.FromArgb(32, 33, 35);
            rail.FillColor2 = Color.Empty;
            rail.BorderColor = Color.FromArgb(32, 33, 35);
            rail.CornerRadius = 24;
            rail.ShadowSize = 0;
            rail.ShadowOffsetX = 0;
            rail.ShadowOffsetY = 0;
            rail.SetBounds(20, 24, 76, 748);
            rail.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;
            Controls.Add(rail);

            Label railLogo = new Label();
            railLogo.Text = "★";
            railLogo.Font = new Font("Segoe UI Symbol", 21.0f);
            railLogo.Text = "\u2605";
            railLogo.ForeColor = Color.White;
            railLogo.TextAlign = ContentAlignment.MiddleCenter;
            railLogo.SetBounds(12, 22, 52, 52);
            rail.Controls.Add(railLogo);

            RailCreditLabel railCredit = new RailCreditLabel();
            railCredit.Text = "Author: NTGH | Idea: CreamyKeys mod - Minecraft";
            railCredit.SetBounds(12, 92, 52, 618);
            railCredit.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;
            rail.Controls.Add(railCredit);

            Button railHome = CreateRailButton("⌂", 112);
            rail.Controls.Add(railHome);
            railHome.Text = "\u2302";
            railHome.Visible = false;
            Button railSound = CreateRailButton("♫", 176);
            rail.Controls.Add(railSound);
            railSound.Text = "\u266b";
            railSound.Visible = false;
            Button railBehavior = CreateRailButton("⚙", 240);
            rail.Controls.Add(railBehavior);
            railBehavior.Text = "\u2699";
            railBehavior.Visible = false;
            Button railApps = CreateRailButton("✓", 304);
            rail.Controls.Add(railApps);
            railApps.Text = "\u2713";
            railApps.Visible = false;
            Button railSettings = CreateRailButton("⚙", 660);
            railSettings.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            rail.Controls.Add(railSettings);
            railSettings.Visible = false;

            ModernPanel sidebar = new ModernPanel();
            sidebar.FillColor = Color.FromArgb(214, 217, 214);
            sidebar.FillColor2 = Color.Empty;
            sidebar.BorderColor = Color.FromArgb(214, 217, 214);
            sidebar.CornerRadius = 24;
            sidebar.ShadowSize = 0;
            sidebar.ShadowOffsetX = 0;
            sidebar.ShadowOffsetY = 0;
            sidebar.SetBounds(112, 24, 300, 748);
            sidebar.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;
            Controls.Add(sidebar);

            PictureBox profileIcon = new PictureBox();
            profileIcon.Image = _appIcon.ToBitmap();
            profileIcon.SizeMode = PictureBoxSizeMode.StretchImage;
            profileIcon.SetBounds(28, 34, 58, 58);
            sidebar.Controls.Add(profileIcon);

            Label profileTitle = new Label();
            profileTitle.Text = "CreamyKeys";
            profileTitle.Font = new Font("Segoe UI Semibold", 10.8f);
            profileTitle.ForeColor = Color.FromArgb(26, 28, 29);
            profileTitle.TextAlign = ContentAlignment.MiddleLeft;
            profileTitle.SetBounds(96, 30, 194, 38);
            sidebar.Controls.Add(profileTitle);

            Label profileSub = new Label();
            profileSub.Text = "Desktop sound studio";
            profileSub.ForeColor = Color.FromArgb(92, 95, 94);
            profileSub.TextAlign = ContentAlignment.MiddleLeft;
            profileSub.SetBounds(97, 66, 190, 32);
            sidebar.Controls.Add(profileSub);

            Label projectsLabel = CreateMutedLabel("Menu", 30, 126, 180, 24);
            sidebar.Controls.Add(projectsLabel);

            ModernPanel content = new ModernPanel();
            content.FillColor = Color.FromArgb(230, 232, 229);
            content.FillColor2 = Color.FromArgb(214, 217, 214);
            content.BorderColor = Color.FromArgb(235, 237, 235);
            content.CornerRadius = 24;
            content.ShadowSize = 9;
            content.ShadowOffsetX = 5;
            content.ShadowOffsetY = 10;
            content.SetBounds(432, 24, 728, 748);
            content.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
            Controls.Add(content);

            Label pageTitle = new Label();
            pageTitle.Text = "Dashboard";
            pageTitle.AutoSize = false;
            pageTitle.Font = new Font("Segoe UI Semibold", 16.8f);
            pageTitle.ForeColor = Color.FromArgb(25, 27, 28);
            pageTitle.TextAlign = ContentAlignment.MiddleLeft;
            pageTitle.SetBounds(28, 14, 320, 62);
            content.Controls.Add(pageTitle);

            Label pageSub = new Label();
            pageSub.Text = "All CreamyKeys controls in one quiet workspace";
            pageSub.ForeColor = Color.FromArgb(92, 95, 94);
            pageSub.TextAlign = ContentAlignment.MiddleLeft;
            pageSub.SetBounds(30, 70, 500, 32);
            content.Controls.Add(pageSub);

            TabControl tabs = new HiddenTabControl();
            tabs.SetBounds(24, 104, 680, 520);
            tabs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabs.Font = new Font("Segoe UI", 9.0f);
            tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabs.SizeMode = TabSizeMode.Fixed;
            tabs.Padding = new Point(0, 0);
            tabs.ItemSize = new Size(1, 1);
            tabs.DrawItem += DrawWideTab;
            content.Controls.Add(tabs);

            TabPage devicePage = CreateTabPage("Devices");
            TabPage soundPage = CreateTabPage("Sound");
            TabPage behaviorPage = CreateTabPage("Behavior");
            TabPage appsPage = CreateTabPage("Apps");
            tabs.TabPages.Add(devicePage);
            tabs.TabPages.Add(soundPage);
            tabs.TabPages.Add(behaviorPage);
            tabs.TabPages.Add(appsPage);

            Button navDevices = AddSidebarButton(sidebar, "Dashboard", 156, true, delegate { tabs.SelectedIndex = 0; });
            Button navSound = AddSidebarButton(sidebar, "Sound library", 204, false, delegate { tabs.SelectedIndex = 1; });
            Button navBehavior = AddSidebarButton(sidebar, "Behavior", 252, false, delegate { tabs.SelectedIndex = 2; });
            Button navApps = AddSidebarButton(sidebar, "Allowed apps", 300, false, delegate { tabs.SelectedIndex = 3; });
            Button[] sidebarNavButtons = new Button[] { navDevices, navSound, navBehavior, navApps };
            tabs.SelectedIndexChanged += delegate
            {
                UpdateSidebarNav(sidebarNavButtons, tabs.SelectedIndex);
                pageTitle.Text = PageTitleForIndex(tabs.SelectedIndex);
            };

            Label statusGroup = CreateMutedLabel("Status", 30, 382, 160, 24);
            sidebar.Controls.Add(statusGroup);
            Label statusOne = CreateLabel("Audio / hook ready", 50, 420, 210, 24);
            sidebar.Controls.Add(statusOne);
            Label statusTwo = CreateLabel("Mouse and keyboard enabled", 50, 456, 236, 24);
            statusTwo.AutoEllipsis = false;
            sidebar.Controls.Add(statusTwo);

            Label documents = CreateMutedLabel("Documents", 30, 560, 160, 24);
            documents.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            sidebar.Controls.Add(documents);
            Button configButtonSide = CreateSidebarButton("Config folder", false);
            configButtonSide.SetBounds(28, 592, 244, 38);
            configButtonSide.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            configButtonSide.Click += delegate { Process.Start(_configStore.ConfigDirectory); };
            sidebar.Controls.Add(configButtonSide);

            ModernPanel deviceCard = new ModernPanel();
            deviceCard.FillColor = Color.FromArgb(42, 43, 45);
            deviceCard.FillColor2 = Color.FromArgb(56, 57, 59);
            deviceCard.BorderColor = Color.FromArgb(108, 111, 112);
            deviceCard.CornerRadius = 18;
            deviceCard.ShadowSize = 3;
            deviceCard.ShadowOffsetX = 2;
            deviceCard.ShadowOffsetY = 5;
            deviceCard.SetBounds(18, 18, 620, 138);
            deviceCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            devicePage.Controls.Add(deviceCard);

            PictureBox deviceIcon = new PictureBox();
            deviceIcon.Image = _appIcon.ToBitmap();
            deviceIcon.SizeMode = PictureBoxSizeMode.StretchImage;
            deviceIcon.SetBounds(20, 30, 58, 58);
            deviceCard.Controls.Add(deviceIcon);
            deviceIcon.Visible = false;

            Label deviceTitle = new Label();
            deviceTitle.Text = "CreamyKeys";
            deviceTitle.Font = new Font("Segoe UI Semibold", 11.4f);
            deviceTitle.ForeColor = Color.White;
            deviceTitle.TextAlign = ContentAlignment.MiddleLeft;
            deviceTitle.SetBounds(96, 20, 236, 40);
            deviceCard.Controls.Add(deviceTitle);
            deviceTitle.Visible = false;

            Label deviceSub = new Label();
            deviceSub.Text = "Virtual devices";
            deviceSub.Font = new Font("Segoe UI", 10.2f);
            deviceSub.ForeColor = Color.FromArgb(210, 214, 213);
            deviceSub.TextAlign = ContentAlignment.MiddleCenter;
            deviceSub.SetBounds(28, 48, 285, 34);
            deviceCard.Controls.Add(deviceSub);

            Label layoutLabel = CreateDarkLabel("Keyboard", 300, 22, 116, 24);
            deviceCard.Controls.Add(layoutLabel);

            _keyboardLayoutCombo = new ComboBox();
            _keyboardLayoutCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _keyboardLayoutCombo.FlatStyle = FlatStyle.Flat;
            _keyboardLayoutCombo.SetBounds(424, 18, 98, 30);
            _keyboardLayoutCombo.Items.Add(new ComboItem("full", "Full size"));
            _keyboardLayoutCombo.Items.Add(new ComboItem("tkl", "TKL"));
            _keyboardLayoutCombo.Items.Add(new ComboItem("60", "60%"));
            _keyboardLayoutCombo.Items.Add(new ComboItem("laptop", "Laptop"));
            _keyboardLayoutCombo.SelectedIndexChanged += delegate { OnSettingChanged(false); };
            deviceCard.Controls.Add(_keyboardLayoutCombo);

            Label mouseLabel = CreateDarkLabel("Mouse", 300, 58, 116, 24);
            deviceCard.Controls.Add(mouseLabel);

            _mouseStyleCombo = new ComboBox();
            _mouseStyleCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _mouseStyleCombo.FlatStyle = FlatStyle.Flat;
            _mouseStyleCombo.SetBounds(424, 54, 98, 30);
            _mouseStyleCombo.Items.Add(new ComboItem("gaming", "Gaming"));
            _mouseStyleCombo.Items.Add(new ComboItem("office", "Office"));
            _mouseStyleCombo.Items.Add(new ComboItem("compact", "Compact"));
            _mouseStyleCombo.SelectedIndexChanged += delegate { OnSettingChanged(false); };
            deviceCard.Controls.Add(_mouseStyleCombo);

            _editModeCheck = CreateDarkCheck("Edit", 536, 22);
            _editModeCheck.CheckedChanged += delegate { OnSettingChanged(false); };
            deviceCard.Controls.Add(_editModeCheck);

            _autoDetectCheck = CreateDarkCheck("Auto", 536, 54);
            _autoDetectCheck.CheckedChanged += delegate { OnAutoDetectChanged(); };
            deviceCard.Controls.Add(_autoDetectCheck);

            _detectButton = CreateButton("Detect", false);
            _detectButton.SetBounds(528, 92, 82, 32);
            _detectButton.Click += delegate { DetectDevicesNow(); };
            deviceCard.Controls.Add(_detectButton);

            EventHandler layoutDeviceCard = delegate
            {
                int cardWidth = deviceCard.ClientSize.Width;
                if (cardWidth < 560)
                {
                    deviceSub.TextAlign = ContentAlignment.MiddleLeft;
                    deviceSub.SetBounds(20, 40, 132, 32);
                    layoutLabel.SetBounds(158, 22, 88, 24);
                    _keyboardLayoutCombo.SetBounds(252, 18, 82, 30);
                    mouseLabel.SetBounds(158, 58, 88, 24);
                    _mouseStyleCombo.SetBounds(252, 54, 82, 30);
                    int checkX = Math.Max(342, cardWidth - 76);
                    _editModeCheck.Location = new Point(checkX, 22);
                    _autoDetectCheck.Location = new Point(checkX, 54);
                    _detectButton.SetBounds(Math.Max(252, cardWidth - 92), 92, 74, 32);
                }
                else
                {
                    deviceSub.TextAlign = ContentAlignment.MiddleCenter;
                    deviceSub.SetBounds(28, 48, 285, 34);
                    layoutLabel.SetBounds(300, 22, 116, 24);
                    _keyboardLayoutCombo.SetBounds(424, 18, 98, 30);
                    mouseLabel.SetBounds(300, 58, 116, 24);
                    _mouseStyleCombo.SetBounds(424, 54, 98, 30);
                    _editModeCheck.Location = new Point(536, 22);
                    _autoDetectCheck.Location = new Point(536, 54);
                    _detectButton.SetBounds(528, 92, 82, 32);
                }
            };
            deviceCard.Resize += layoutDeviceCard;
            layoutDeviceCard(deviceCard, EventArgs.Empty);

            _deviceView = new VirtualDeviceControl();
            _deviceView.SetBounds(18, 168, 620, 306);
            _deviceView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _deviceView.ButtonActivated += OnVirtualButtonActivated;
            _deviceView.ButtonCustomizationChanged += OnDeviceCustomizationChanged;
            devicePage.Controls.Add(_deviceView);

            BuildSoundPage(soundPage);
            BuildBehaviorPage(behaviorPage);
            BuildAppsPage(appsPage);

            Panel actions = new Panel();
            actions.BackColor = Color.FromArgb(214, 217, 214);
            actions.SetBounds(24, 626, 680, 48);
            actions.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            content.Controls.Add(actions);

            Button testButton = CreateButton("Test", false);
            testButton.SetBounds(16, 7, 76, 34);
            testButton.Click += delegate { _audio.PlayRandom(); };
            actions.Controls.Add(testButton);

            Button saveButton = CreateButton("Save", true);
            saveButton.SetBounds(102, 7, 82, 34);
            saveButton.Click += delegate { SaveConfig(); };
            actions.Controls.Add(saveButton);

            Button openConfigButton = CreateButton("Config", false);
            openConfigButton.SetBounds(194, 7, 92, 34);
            openConfigButton.Click += delegate { Process.Start(_configStore.ConfigDirectory); };
            actions.Controls.Add(openConfigButton);

            _adminButton = CreateButton("Admin", false);
            _adminButton.SetBounds(296, 7, 82, 34);
            _adminButton.Click += delegate { RelaunchAsAdmin(); };
            actions.Controls.Add(_adminButton);

            Button hideButton = CreateButton("Hide", false);
            hideButton.SetBounds(500, 7, 72, 34);
            hideButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            hideButton.Click += delegate { Hide(); };
            actions.Controls.Add(hideButton);

            Button exitButton = CreateButton("Exit", false);
            exitButton.SetBounds(584, 7, 72, 34);
            exitButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            exitButton.Click += delegate { ExitApplication(); };
            actions.Controls.Add(exitButton);

            Panel statusPanel = new Panel();
            statusPanel.BackColor = Color.FromArgb(214, 217, 214);
            statusPanel.SetBounds(24, 686, 680, 38);
            statusPanel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            content.Controls.Add(statusPanel);

            _statusLabel = new Label();
            _statusLabel.AutoEllipsis = true;
            _statusLabel.BackColor = statusPanel.BackColor;
            _statusLabel.ForeColor = Color.FromArgb(45, 49, 50);
            _statusLabel.Padding = new Padding(14, 7, 14, 0);
            _statusLabel.SetBounds(0, 0, 680, 38);
            _statusLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
            statusPanel.Controls.Add(_statusLabel);
        }

        private void BuildSoundPage(TabPage soundPage)
        {
            ModernPanel card = CreateInnerCard(18, 20, 620, 468);
            card.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            soundPage.Controls.Add(card);

            Label title = CreateCardTitle("Sound", 24, 18, 220, 30);
            card.Controls.Add(title);

            _enabledCheck = CreateLightCheck("App enabled", 28, 64);
            _enabledCheck.CheckedChanged += delegate { OnSettingChanged(false); };
            card.Controls.Add(_enabledCheck);

            _keyboardSoundsCheck = CreateLightCheck("Keyboard sound", 210, 64);
            _keyboardSoundsCheck.CheckedChanged += delegate { OnSettingChanged(false); };
            card.Controls.Add(_keyboardSoundsCheck);

            _mouseSoundsCheck = CreateLightCheck("Mouse sound", 410, 64);
            _mouseSoundsCheck.CheckedChanged += delegate { OnSettingChanged(false); };
            card.Controls.Add(_mouseSoundsCheck);

            _showMouseCheck = CreateLightCheck("Show virtual mouse", 28, 102);
            _showMouseCheck.CheckedChanged += delegate { OnSettingChanged(false); };
            card.Controls.Add(_showMouseCheck);

            Label presetLabel = CreateLabel("Preset", 28, 152, 140, 26);
            card.Controls.Add(presetLabel);

            _presetCombo = new ComboBox();
            _presetCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _presetCombo.FlatStyle = FlatStyle.Flat;
            _presetCombo.SetBounds(180, 148, 310, 30);
            _presetCombo.SelectedIndexChanged += delegate { OnSettingChanged(true); };
            card.Controls.Add(_presetCombo);

            Button reloadButton = CreateButton("Reload", false);
            reloadButton.SetBounds(506, 146, 90, 34);
            reloadButton.Click += delegate { ReloadPresets(); };
            card.Controls.Add(reloadButton);

            AddSoundSlider(card, "Output volume", 204, out _volumeTrack, out _volumeLabel, 0, 100);
            AddSoundSlider(card, "Key boost", 262, out _keyGainTrack, out _keyGainLabel, 50, 400);
            _keyGainTrack.TickFrequency = 50;
            _keyGainTrack.LargeChange = 25;
            AddSoundSlider(card, "Volume jitter", 320, out _randomVolumeTrack, out _randomVolumeLabel, 0, 50);
            AddSoundSlider(card, "Pitch jitter", 378, out _randomPitchTrack, out _randomPitchLabel, 0, 20);
        }

        private void BuildBehaviorPage(TabPage behaviorPage)
        {
            ModernPanel behavior = CreateInnerCard(18, 20, 620, 210);
            behavior.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            behaviorPage.Controls.Add(behavior);

            Label title = CreateCardTitle("Behavior", 24, 18, 220, 30);
            behavior.Controls.Add(title);

            _playRepeatCheck = CreateLightCheck("Held key repeat", 28, 68);
            _playRepeatCheck.CheckedChanged += delegate { OnSettingChanged(false); };
            behavior.Controls.Add(_playRepeatCheck);

            _playModifiersCheck = CreateLightCheck("Shift / Ctrl / Alt", 322, 68);
            _playModifiersCheck.CheckedChanged += delegate { OnSettingChanged(false); };
            behavior.Controls.Add(_playModifiersCheck);

            _ignoreInjectedCheck = CreateLightCheck("Ignore injected input", 28, 110);
            _ignoreInjectedCheck.CheckedChanged += delegate { OnSettingChanged(false); };
            behavior.Controls.Add(_ignoreInjectedCheck);

            _runStartupCheck = CreateLightCheck("Run at startup", 322, 110);
            _runStartupCheck.CheckedChanged += delegate { OnSettingChanged(false); };
            behavior.Controls.Add(_runStartupCheck);

            Label cooldownLabel = CreateLabel("Cooldown", 28, 158, 120, 26);
            behavior.Controls.Add(cooldownLabel);

            _cooldownNumber = CreateNumberBox(150, 154, 88, 0, 80);
            _cooldownNumber.ValueChanged += delegate { OnSettingChanged(false); };
            behavior.Controls.Add(_cooldownNumber);

            Label msLabel = CreateMutedLabel("ms", 248, 158, 48, 26);
            behavior.Controls.Add(msLabel);

            Label maxVoicesLabel = CreateLabel("Max voices", 322, 158, 116, 26);
            behavior.Controls.Add(maxVoicesLabel);

            _maxVoicesNumber = CreateNumberBox(452, 154, 88, 4, 64);
            _maxVoicesNumber.ValueChanged += delegate { OnSettingChanged(false); };
            behavior.Controls.Add(_maxVoicesNumber);

            ModernPanel shadow = CreateInnerCard(18, 250, 620, 230);
            shadow.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            behaviorPage.Controls.Add(shadow);

            Label shadowTitle = CreateCardTitle("Virtual device shadow", 24, 18, 260, 30);
            shadow.Controls.Add(shadowTitle);

            _shadowEnabledCheck = CreateLightCheck("Shadow enabled", 28, 62);
            _shadowEnabledCheck.CheckedChanged += delegate { OnSettingChanged(false); };
            shadow.Controls.Add(_shadowEnabledCheck);

            AddShadowSlider(shadow, "Depth", 98, out _shadowDepthTrack, out _shadowDepthLabel, 0, 48);
            AddShadowSlider(shadow, "Direction X", 134, out _shadowXTrack, out _shadowXLabel, -32, 32);
            AddShadowSlider(shadow, "Direction Y", 170, out _shadowYTrack, out _shadowYLabel, -32, 32);
        }

        private void BuildAppsPage(TabPage appsPage)
        {
            ModernPanel card = CreateInnerCard(18, 20, 620, 430);
            card.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            appsPage.Controls.Add(card);

            Label title = CreateCardTitle("Allowed apps", 24, 18, 240, 30);
            card.Controls.Add(title);

            Label hint = CreateMutedLabel("Checked apps can play CreamyKeys sounds. Empty boxes stay silent when the allow list is on.", 26, 48, 560, 38);
            card.Controls.Add(hint);

            _appAllowListCheck = CreateLightCheck("Use app allow list", 28, 88);
            _appAllowListCheck.CheckedChanged += delegate
            {
                OnSettingChanged(false);
                UpdateAllowedAppsEnabled();
            };
            card.Controls.Add(_appAllowListCheck);

            Button refreshButton = CreateButton("Refresh", false);
            refreshButton.SetBounds(402, 86, 86, 32);
            refreshButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            refreshButton.Click += delegate { PopulateAllowedAppList(); };
            card.Controls.Add(refreshButton);

            Button currentButton = CreateButton("Add current", false);
            currentButton.SetBounds(498, 86, 98, 32);
            currentButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            currentButton.Click += delegate { AddCurrentForegroundApp(); };
            card.Controls.Add(currentButton);

            _allowedAppsList = new CheckedListBox();
            _allowedAppsList.CheckOnClick = true;
            _allowedAppsList.BorderStyle = BorderStyle.None;
            _allowedAppsList.BackColor = Color.FromArgb(244, 247, 246);
            _allowedAppsList.ForeColor = Color.FromArgb(33, 40, 42);
            _allowedAppsList.Font = new Font("Segoe UI", 9.0f);
            _allowedAppsList.SetBounds(28, 132, 568, 210);
            _allowedAppsList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _allowedAppsList.ItemCheck += delegate
            {
                if (!_loadingControls && IsHandleCreated)
                {
                    BeginInvoke((MethodInvoker)delegate { OnSettingChanged(false); });
                }
            };
            card.Controls.Add(_allowedAppsList);

            _manualAppBox = new TextBox();
            _manualAppBox.BorderStyle = BorderStyle.FixedSingle;
            _manualAppBox.SetBounds(28, 364, 384, 30);
            _manualAppBox.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            card.Controls.Add(_manualAppBox);

            Button addManual = CreateButton("Add .exe", true);
            addManual.SetBounds(426, 362, 96, 34);
            addManual.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            addManual.Click += delegate { AddManualAllowedApp(); };
            card.Controls.Add(addManual);

            Button clearApps = CreateButton("Clear", false);
            clearApps.SetBounds(532, 362, 64, 34);
            clearApps.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            clearApps.Click += delegate
            {
                if (_allowedAppsList != null)
                {
                    for (int i = 0; i < _allowedAppsList.Items.Count; i++)
                    {
                        _allowedAppsList.SetItemChecked(i, false);
                    }
                    OnSettingChanged(false);
                }
            };
            card.Controls.Add(clearApps);

            _excludedBox = new TextBox();
            _excludedBox.Visible = false;
            appsPage.Controls.Add(_excludedBox);
        }

        private static ModernPanel CreateInnerCard(int x, int y, int width, int height)
        {
            ModernPanel card = new ModernPanel();
            card.FillColor = Color.FromArgb(238, 241, 240);
            card.FillColor2 = Color.FromArgb(230, 234, 232);
            card.BorderColor = Color.FromArgb(222, 228, 226);
            card.CornerRadius = 18;
            card.ShadowSize = 3;
            card.ShadowOffsetX = 2;
            card.ShadowOffsetY = 5;
            card.SetBounds(x, y, width, height);
            return card;
        }

        private static Label CreateCardTitle(string text, int x, int y, int width, int height)
        {
            Label label = new Label();
            label.Text = text;
            label.AutoSize = false;
            label.AutoEllipsis = false;
            label.Font = new Font("Segoe UI Semibold", 11.2f);
            label.ForeColor = Color.FromArgb(28, 34, 36);
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Padding = new Padding(0, 2, 0, 0);
            label.SetBounds(x, y, width, Math.Max(40, height + 10));
            return label;
        }

        private static Label CreateDarkLabel(string text, int x, int y, int width, int height)
        {
            Label label = new Label();
            label.Text = text;
            label.AutoSize = false;
            label.AutoEllipsis = false;
            label.Font = new Font("Segoe UI", 9.4f);
            label.ForeColor = Color.FromArgb(226, 230, 229);
            label.BackColor = Color.Transparent;
            label.Padding = new Padding(6, 0, 0, 0);
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.SetBounds(x, y, width, height);
            return label;
        }

        private static CheckBox CreateDarkCheck(string text, int x, int y)
        {
            CheckBox check = new CheckBox();
            check.Text = text;
            check.AutoSize = true;
            check.ForeColor = Color.FromArgb(235, 239, 238);
            check.BackColor = Color.Transparent;
            check.Location = new Point(x, y);
            return check;
        }

        private static CheckBox CreateLightCheck(string text, int x, int y)
        {
            CheckBox check = new CheckBox();
            check.Text = text;
            check.AutoSize = true;
            check.ForeColor = Color.FromArgb(35, 43, 45);
            check.BackColor = Color.Transparent;
            check.Location = new Point(x, y);
            return check;
        }

        private static Button CreateRailButton(string text, int y)
        {
            Button button = new Button();
            button.Text = text;
            button.Font = new Font("Segoe UI Semibold", 10.0f);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = Color.FromArgb(62, 64, 66);
            button.ForeColor = Color.White;
            button.Cursor = Cursors.Hand;
            button.SetBounds(16, y, 44, 44);
            return button;
        }

        private Button AddSidebarButton(Control parent, string text, int y, bool selected, EventHandler click)
        {
            Button button = CreateSidebarButton(text, selected);
            button.SetBounds(28, y, 244, 42);
            button.Click += click;
            parent.Controls.Add(button);
            return button;
        }

        private static Button CreateSidebarButton(string text, bool selected)
        {
            Button button = new Button();
            button.Text = text;
            button.TextAlign = ContentAlignment.MiddleLeft;
            button.Padding = new Padding(18, 0, 0, 0);
            button.FlatStyle = FlatStyle.Flat;
            button.Font = new Font("Segoe UI Semibold", 9.0f);
            button.Cursor = Cursors.Hand;
            SetSidebarButtonState(button, selected);
            return button;
        }

        private static void UpdateSidebarNav(Button[] buttons, int selectedIndex)
        {
            if (buttons == null)
            {
                return;
            }

            for (int i = 0; i < buttons.Length; i++)
            {
                SetSidebarButtonState(buttons[i], i == selectedIndex);
            }
        }

        private static void SetSidebarButtonState(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            button.BackColor = selected ? Color.FromArgb(228, 236, 234) : Color.FromArgb(218, 220, 217);
            button.ForeColor = Color.FromArgb(31, 36, 38);
            button.FlatAppearance.BorderSize = selected ? 1 : 0;
            button.FlatAppearance.BorderColor = selected ? Color.FromArgb(245, 248, 247) : button.BackColor;
        }

        private static string PageTitleForIndex(int index)
        {
            switch (index)
            {
                case 1: return "Sound library";
                case 2: return "Behavior";
                case 3: return "Allowed apps";
                default: return "Dashboard";
            }
        }

        private static NumericUpDown CreateNumberBox(int x, int y, int width, int min, int max)
        {
            NumericUpDown box = new NumericUpDown();
            box.Minimum = min;
            box.Maximum = max;
            box.BorderStyle = BorderStyle.FixedSingle;
            box.SetBounds(x, y, width, 30);
            return box;
        }

        private void AddSoundSlider(Control parent, string text, int y, out CleanTrackBar track, out NumericUpDown valueBox, int min, int max)
        {
            Label label = CreateLabel(text, 28, y + 10, 156, 26);
            parent.Controls.Add(label);

            CleanTrackBar createdTrack = CreateTrackBar(194, y, 270, min, max);
            createdTrack.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            parent.Controls.Add(createdTrack);
            track = createdTrack;

            NumericUpDown createdBox = CreateNumberBox(480, y + 4, 72, min, max);
            createdBox.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            bool syncing = false;
            createdTrack.ValueChanged += delegate
            {
                if (syncing)
                {
                    return;
                }
                syncing = true;
                SetNumberValue(createdBox, createdTrack.Value);
                syncing = false;
                OnSettingChanged(false);
            };
            createdBox.ValueChanged += delegate
            {
                if (syncing)
                {
                    return;
                }
                syncing = true;
                if (createdTrack.Value != (int)createdBox.Value)
                {
                    createdTrack.Value = (int)createdBox.Value;
                }
                syncing = false;
                OnSettingChanged(false);
            };
            parent.Controls.Add(createdBox);
            valueBox = createdBox;

            Label percent = CreateMutedLabel("%", 560, y + 10, 30, 24);
            percent.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            parent.Controls.Add(percent);
        }

        private void AddShadowSlider(Control parent, string text, int y, out CleanTrackBar track, out NumericUpDown valueBox, int min, int max)
        {
            Label label = CreateLabel(text, 28, y + 8, 120, 24);
            parent.Controls.Add(label);

            CleanTrackBar createdTrack = CreateTrackBar(150, y, 314, min, max);
            createdTrack.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            createdTrack.TickFrequency = Math.Max(1, (max - min) / 8);
            parent.Controls.Add(createdTrack);
            track = createdTrack;

            NumericUpDown createdBox = CreateNumberBox(480, y + 2, 72, min, max);
            createdBox.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            bool syncing = false;
            createdTrack.ValueChanged += delegate
            {
                if (syncing)
                {
                    return;
                }
                syncing = true;
                SetNumberValue(createdBox, createdTrack.Value);
                syncing = false;
                OnSettingChanged(false);
            };
            createdBox.ValueChanged += delegate
            {
                if (syncing)
                {
                    return;
                }
                syncing = true;
                if (createdTrack.Value != (int)createdBox.Value)
                {
                    createdTrack.Value = (int)createdBox.Value;
                }
                syncing = false;
                OnSettingChanged(false);
            };
            parent.Controls.Add(createdBox);
            valueBox = createdBox;
        }

        private CleanTrackBar CreateTrackBar(int x, int y, int width, int min, int max)
        {
            CleanTrackBar track = new CleanTrackBar();
            track.Minimum = min;
            track.Maximum = max;
            track.TickFrequency = Math.Max(1, (max - min) / 10);
            track.TickStyle = TickStyle.None;
            track.SmallChange = 1;
            track.LargeChange = 5;
            track.BackColor = Color.Transparent;
            track.TabStop = false;
            track.SetBounds(x, y, width, 34);
            return track;
        }

        private static void UpdateWideTabs(TabControl tabs)
        {
            if (tabs == null || tabs.TabPages.Count == 0)
            {
                return;
            }

            int width = Math.Max(80, (tabs.ClientSize.Width - 4) / tabs.TabPages.Count);
            if (tabs.ItemSize.Width == width && tabs.ItemSize.Height == 34)
            {
                return;
            }

            tabs.ItemSize = new Size(width, 34);
            tabs.Invalidate();
        }

        private static void DrawWideTab(object sender, DrawItemEventArgs e)
        {
            TabControl tabs = sender as TabControl;
            if (tabs == null || e.Index < 0 || e.Index >= tabs.TabPages.Count)
            {
                return;
            }

            Rectangle bounds = e.Bounds;
            bool selected = e.Index == tabs.SelectedIndex;
            Color fill = selected ? Color.White : Color.FromArgb(244, 247, 248);
            Color border = Color.FromArgb(213, 221, 224);
            Color text = Color.FromArgb(22, 31, 35);

            using (SolidBrush brush = new SolidBrush(fill))
            using (Pen pen = new Pen(border))
            using (SolidBrush textBrush = new SolidBrush(text))
            {
                e.Graphics.FillRectangle(brush, bounds);
                e.Graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);

                StringFormat format = new StringFormat();
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                format.Trimming = StringTrimming.EllipsisCharacter;
                e.Graphics.DrawString(tabs.TabPages[e.Index].Text, tabs.Font, textBrush, bounds, format);
            }
        }

        private NotifyIcon BuildTrayIcon()
        {
            _trayEnabledItem = new MenuItem("Disable", delegate { ToggleEnabledFromTray(); });
            _trayStartupItem = new MenuItem("Run at startup", delegate { ToggleStartupFromTray(); });
            _trayRepeatItem = new MenuItem("Held key repeat", delegate { ToggleRepeatFromTray(); });
            _trayModifiersItem = new MenuItem("Shift / Ctrl / Alt", delegate { ToggleModifiersFromTray(); });
            _trayInjectedItem = new MenuItem("Ignore injected input", delegate { ToggleInjectedFromTray(); });
            _trayPresetItems = new List<MenuItem>();
            _trayVolumeItems = new List<MenuItem>();
            _trayGainItems = new List<MenuItem>();

            ContextMenu menu = new ContextMenu();
            menu.MenuItems.Add(new MenuItem("Settings", delegate { ShowSettings(); }));
            menu.MenuItems.Add(_trayEnabledItem);
            menu.MenuItems.Add(new MenuItem("Test sound", delegate { _audio.PlayRandom(); }));
            menu.MenuItems.Add("-");
            menu.MenuItems.Add(CreatePresetMenu());
            menu.MenuItems.Add(CreateVolumeMenu());
            menu.MenuItems.Add(CreateGainMenu());
            menu.MenuItems.Add("-");
            menu.MenuItems.Add(_trayStartupItem);
            menu.MenuItems.Add(_trayRepeatItem);
            menu.MenuItems.Add(_trayModifiersItem);
            menu.MenuItems.Add(_trayInjectedItem);
            menu.MenuItems.Add("-");
            menu.MenuItems.Add(new MenuItem("Open config folder", delegate { Process.Start(_configStore.ConfigDirectory); }));
            menu.MenuItems.Add(new MenuItem("Run as admin", delegate { RelaunchAsAdmin(); }));
            menu.MenuItems.Add("-");
            menu.MenuItems.Add(new MenuItem("Exit", delegate { ExitApplication(); }));

            NotifyIcon icon = new NotifyIcon();
            icon.Icon = _appIcon != null ? _appIcon : SystemIcons.Application;
            icon.Text = "CreamyKeys";
            icon.ContextMenu = null;
            icon.Visible = true;
            icon.MouseUp += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Right)
                {
                    ShowTrayPopup();
                }
                else if (e.Button == MouseButtons.Left)
                {
                    ShowSettings();
                }
            };
            icon.DoubleClick += delegate { ShowSettings(); };
            return icon;
        }

        private void ShowTrayPopup()
        {
            if (_trayPopup != null && !_trayPopup.IsDisposed)
            {
                _trayPopup.Close();
            }

            PresetInfo preset = FindPreset(_config.Preset);
            string presetName = preset != null ? preset.DisplayName : _config.Preset;
            _trayPopup = new TrayMenuForm(
                _appIcon,
                _config.Enabled,
                _config.RunAtStartup,
                _config.PlayOnRepeat,
                _config.PlayModifiers,
                _config.IgnoreInjectedInput,
                _config.Volume,
                _config.KeyGainPercent,
                presetName,
                delegate { ShowSettings(); },
                delegate { ToggleEnabledFromTray(); },
                delegate { _audio.PlayRandom(); },
                delegate { Process.Start(_configStore.ConfigDirectory); },
                delegate { RelaunchAsAdmin(); },
                delegate { ToggleStartupFromTray(); },
                delegate { ToggleRepeatFromTray(); },
                delegate { ToggleModifiersFromTray(); },
                delegate { ToggleInjectedFromTray(); },
                delegate { ExitApplication(); });

            Point cursor = Cursor.Position;
            Rectangle work = Screen.FromPoint(cursor).WorkingArea;
            int x = Math.Min(Math.Max(work.Left + 8, cursor.X - _trayPopup.Width + 18), work.Right - _trayPopup.Width - 8);
            int y = Math.Min(Math.Max(work.Top + 8, cursor.Y - _trayPopup.Height + 18), work.Bottom - _trayPopup.Height - 8);
            _trayPopup.Location = new Point(x, y);
            _trayPopup.Show();
        }

        private MenuItem CreatePresetMenu()
        {
            MenuItem menu = new MenuItem("Preset");
            for (int i = 0; i < _presets.Count; i++)
            {
                PresetInfo preset = _presets[i];
                MenuItem item = new MenuItem(preset.DisplayName, delegate(object sender, EventArgs e)
                {
                    MenuItem clicked = sender as MenuItem;
                    if (clicked != null && clicked.Tag is string)
                    {
                        SetPresetFromTray((string)clicked.Tag);
                    }
                });
                item.Tag = preset.Id;
                _trayPresetItems.Add(item);
                menu.MenuItems.Add(item);
            }
            return menu;
        }

        private MenuItem CreateVolumeMenu()
        {
            MenuItem menu = new MenuItem("Output volume");
            int[] values = new int[] { 30, 50, 70, 85, 100 };
            for (int i = 0; i < values.Length; i++)
            {
                int value = values[i];
                MenuItem item = new MenuItem(value.ToString(CultureInfo.InvariantCulture) + "%",
                    delegate(object sender, EventArgs e)
                    {
                        MenuItem clicked = sender as MenuItem;
                        if (clicked != null && clicked.Tag is int)
                        {
                            SetVolumeFromTray((int)clicked.Tag);
                        }
                    });
                item.Tag = value;
                _trayVolumeItems.Add(item);
                menu.MenuItems.Add(item);
            }
            return menu;
        }

        private MenuItem CreateGainMenu()
        {
            MenuItem menu = new MenuItem("Key boost");
            int[] values = new int[] { 100, 150, 180, 220, 300, 400 };
            for (int i = 0; i < values.Length; i++)
            {
                int value = values[i];
                MenuItem item = new MenuItem(value.ToString(CultureInfo.InvariantCulture) + "%",
                    delegate(object sender, EventArgs e)
                    {
                        MenuItem clicked = sender as MenuItem;
                        if (clicked != null && clicked.Tag is int)
                        {
                            SetGainFromTray((int)clicked.Tag);
                        }
                    });
                item.Tag = value;
                _trayGainItems.Add(item);
                menu.MenuItems.Add(item);
            }
            return menu;
        }

        private static TabPage CreateTabPage(string text)
        {
            TabPage page = new TabPage(text);
            page.BackColor = Color.White;
            page.ForeColor = Color.FromArgb(35, 45, 49);
            return page;
        }

        private static Label CreateLabel(string text, int x, int y, int width, int height)
        {
            Label label = new Label();
            label.Text = text;
            label.AutoEllipsis = false;
            label.ForeColor = Color.FromArgb(42, 54, 59);
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.SetBounds(x, y, width, height);
            return label;
        }

        private static Label CreateMutedLabel(string text, int x, int y, int width, int height)
        {
            Label label = CreateLabel(text, x, y, width, height);
            label.ForeColor = Color.FromArgb(111, 124, 130);
            return label;
        }

        private static Label CreateValueLabel(int x, int y, int width, int height)
        {
            Label label = CreateMutedLabel("", x, y, width, height);
            label.TextAlign = ContentAlignment.MiddleRight;
            return label;
        }

        private static Button CreateButton(string text, bool accent)
        {
            Button button = new Button();
            button.Text = text;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = accent ? Color.FromArgb(22, 116, 128) : Color.FromArgb(235, 241, 242);
            button.ForeColor = accent ? Color.White : Color.FromArgb(38, 52, 58);
            button.Cursor = Cursors.Hand;
            return button;
        }

        private static Icon LoadAppIcon()
        {
            try
            {
                Icon icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (icon != null)
                {
                    return icon;
                }
            }
            catch
            {
            }

            return (Icon)SystemIcons.Application.Clone();
        }

        private static void SelectComboValue(ComboBox combo, string value)
        {
            if (combo == null)
            {
                return;
            }

            for (int i = 0; i < combo.Items.Count; i++)
            {
                ComboItem item = combo.Items[i] as ComboItem;
                if (item != null && string.Equals(item.Value, value, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }

            if (combo.Items.Count > 0 && combo.SelectedIndex < 0)
            {
                combo.SelectedIndex = 0;
            }
        }

        private static string SelectedComboValue(ComboBox combo, string fallback)
        {
            if (combo == null)
            {
                return fallback;
            }

            ComboItem item = combo.SelectedItem as ComboItem;
            if (item != null)
            {
                return item.Value;
            }

            return fallback;
        }

        private void LoadControlsFromConfig()
        {
            _loadingControls = true;
            try
            {
                _presetCombo.Items.Clear();
                for (int i = 0; i < _presets.Count; i++)
                {
                    _presetCombo.Items.Add(new PresetComboItem(_presets[i]));
                    if (string.Equals(_presets[i].Id, _config.Preset, StringComparison.OrdinalIgnoreCase))
                    {
                        _presetCombo.SelectedIndex = i;
                    }
                }

                if (_presetCombo.SelectedIndex < 0 && _presetCombo.Items.Count > 0)
                {
                    _presetCombo.SelectedIndex = 0;
                }

                _enabledCheck.Checked = _config.Enabled;
                _keyboardSoundsCheck.Checked = _config.KeyboardSoundsEnabled;
                SelectComboValue(_keyboardLayoutCombo, _config.KeyboardLayout);
                SelectComboValue(_mouseStyleCombo, _config.MouseStyle);
                _autoDetectCheck.Checked = _config.AutoDetectDevices;
                _mouseSoundsCheck.Checked = _config.MouseSoundsEnabled;
                _showMouseCheck.Checked = _config.ShowMouse;
                _editModeCheck.Checked = _config.EditMode;
                _playRepeatCheck.Checked = _config.PlayOnRepeat;
                _playModifiersCheck.Checked = _config.PlayModifiers;
                _ignoreInjectedCheck.Checked = _config.IgnoreInjectedInput;
                _runStartupCheck.Checked = _config.RunAtStartup;
                _volumeTrack.Value = _config.Volume;
                _keyGainTrack.Value = _config.KeyGainPercent;
                _randomVolumeTrack.Value = _config.RandomVolumePercent;
                _randomPitchTrack.Value = _config.RandomPitchPercent;
                _cooldownNumber.Value = _config.CooldownMs;
                _maxVoicesNumber.Value = _config.MaxVoices;
                _shadowEnabledCheck.Checked = _config.VirtualShadowEnabled;
                _shadowDepthTrack.Value = _config.VirtualShadowDepth;
                _shadowXTrack.Value = _config.VirtualShadowOffsetX;
                _shadowYTrack.Value = _config.VirtualShadowOffsetY;
                _appAllowListCheck.Checked = _config.UseAppAllowList;
                if (_excludedBox != null)
                {
                    _excludedBox.Lines = _config.ExcludedProcesses.ToArray();
                }
                PopulateAllowedAppList();
                UpdateAllowedAppsEnabled();
                UpdateValueLabels();
            }
            finally
            {
                _loadingControls = false;
            }
        }

        private void ApplyControlsToConfig()
        {
            PresetComboItem selected = _presetCombo.SelectedItem as PresetComboItem;
            if (selected != null)
            {
                _config.Preset = selected.Preset.Id;
            }

            _config.Enabled = _enabledCheck.Checked;
            _config.KeyboardSoundsEnabled = _keyboardSoundsCheck.Checked;
            _config.KeyboardLayout = SelectedComboValue(_keyboardLayoutCombo, _config.KeyboardLayout);
            _config.MouseStyle = SelectedComboValue(_mouseStyleCombo, _config.MouseStyle);
            _config.AutoDetectDevices = _autoDetectCheck.Checked;
            _config.MouseSoundsEnabled = _mouseSoundsCheck.Checked;
            _config.ShowMouse = _showMouseCheck.Checked;
            _config.EditMode = _editModeCheck.Checked;
            _config.PlayOnRepeat = _playRepeatCheck.Checked;
            _config.PlayModifiers = _playModifiersCheck.Checked;
            _config.IgnoreInjectedInput = _ignoreInjectedCheck.Checked;
            _config.RunAtStartup = _runStartupCheck.Checked;
            _config.Volume = _volumeTrack.Value;
            _config.KeyGainPercent = _keyGainTrack.Value;
            _config.RandomVolumePercent = _randomVolumeTrack.Value;
            _config.RandomPitchPercent = _randomPitchTrack.Value;
            _config.CooldownMs = (int)_cooldownNumber.Value;
            _config.MaxVoices = (int)_maxVoicesNumber.Value;
            _config.VirtualShadowEnabled = _shadowEnabledCheck.Checked;
            _config.VirtualShadowDepth = _shadowDepthTrack.Value;
            _config.VirtualShadowOffsetX = _shadowXTrack.Value;
            _config.VirtualShadowOffsetY = _shadowYTrack.Value;
            _config.UseAppAllowList = _appAllowListCheck.Checked;
            _config.AllowedProcesses = ReadAllowedProcesses();
            _config.ExcludedProcesses = ReadExcludedProcesses();
            _config.Normalize(_config.Preset);
            UpdateValueLabels();
        }

        private List<string> ReadExcludedProcesses()
        {
            List<string> result = new List<string>();
            if (_excludedBox == null)
            {
                return result;
            }

            string[] lines = _excludedBox.Lines;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length > 0 && !line.StartsWith("#", StringComparison.Ordinal))
                {
                    result.Add(line);
                }
            }
            return result;
        }

        private List<string> ReadAllowedProcesses()
        {
            List<string> result = new List<string>();
            if (_allowedAppsList == null)
            {
                return result;
            }

            for (int i = 0; i < _allowedAppsList.Items.Count; i++)
            {
                if (!_allowedAppsList.GetItemChecked(i))
                {
                    continue;
                }

                string item = _allowedAppsList.Items[i] as string;
                string normalized = NormalizeProcessName(item);
                if (normalized.Length > 0 && !ContainsProcessName(result, normalized))
                {
                    result.Add(normalized);
                }
            }
            return result;
        }

        private void PopulateAllowedAppList()
        {
            if (_allowedAppsList == null)
            {
                return;
            }

            List<string> checkedNames = _loadingControls ? _config.AllowedProcesses : ReadAllowedProcesses();
            SortedSet<string> names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            AddProcessNames(names, checkedNames);
            AddProcessNames(names, _config.AllowedProcesses);
            AddProcessNames(names, _config.ExcludedProcesses);

            foreach (string processName in GetCandidateProcessNames())
            {
                string normalized = NormalizeProcessName(processName);
                if (normalized.Length > 0)
                {
                    names.Add(normalized);
                }
            }

            if (names.Count == 0)
            {
                names.Add("explorer.exe");
            }

            _allowedAppsList.BeginUpdate();
            try
            {
                _allowedAppsList.Items.Clear();
                foreach (string name in names)
                {
                    _allowedAppsList.Items.Add(name, ContainsProcessName(checkedNames, name));
                }
            }
            finally
            {
                _allowedAppsList.EndUpdate();
            }
        }

        private static void AddProcessNames(SortedSet<string> target, List<string> names)
        {
            if (target == null || names == null)
            {
                return;
            }

            for (int i = 0; i < names.Count; i++)
            {
                string normalized = NormalizeProcessName(names[i]);
                if (normalized.Length > 0)
                {
                    target.Add(normalized);
                }
            }
        }

        private static List<string> GetCandidateProcessNames()
        {
            SortedSet<string> names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                Process[] processes = Process.GetProcesses();
                for (int i = 0; i < processes.Length; i++)
                {
                    using (Process process = processes[i])
                    {
                        try
                        {
                            if (process.MainWindowHandle != IntPtr.Zero)
                            {
                                string normalized = NormalizeProcessName(process.ProcessName);
                                if (normalized.Length > 0)
                                {
                                    names.Add(normalized);
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch
            {
            }

            return new List<string>(names);
        }

        private void AddCurrentForegroundApp()
        {
            string foreground = NormalizeProcessName(GetForegroundProcessName());
            if (foreground.Length == 0)
            {
                return;
            }

            AddAllowedAppItem(foreground, true);
            OnSettingChanged(false);
        }

        private void AddManualAllowedApp()
        {
            if (_manualAppBox == null)
            {
                return;
            }

            string normalized = NormalizeProcessName(_manualAppBox.Text);
            if (normalized.Length == 0)
            {
                return;
            }

            AddAllowedAppItem(normalized, true);
            _manualAppBox.Text = "";
            OnSettingChanged(false);
        }

        private void AddAllowedAppItem(string processName, bool isChecked)
        {
            if (_allowedAppsList == null)
            {
                return;
            }

            string normalized = NormalizeProcessName(processName);
            if (normalized.Length == 0)
            {
                return;
            }

            for (int i = 0; i < _allowedAppsList.Items.Count; i++)
            {
                string existing = _allowedAppsList.Items[i] as string;
                if (string.Equals(NormalizeProcessName(existing), normalized, StringComparison.OrdinalIgnoreCase))
                {
                    _allowedAppsList.SetItemChecked(i, isChecked);
                    return;
                }
            }

            _allowedAppsList.Items.Add(normalized, isChecked);
        }

        private void UpdateAllowedAppsEnabled()
        {
            bool enabled = _appAllowListCheck != null && _appAllowListCheck.Checked;
            if (_allowedAppsList != null)
            {
                _allowedAppsList.Enabled = enabled;
            }
            if (_manualAppBox != null)
            {
                _manualAppBox.Enabled = enabled;
            }
        }

        private static string NormalizeProcessName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "";
            }

            string trimmed = name.Trim().Trim('"');
            try
            {
                trimmed = Path.GetFileName(trimmed);
            }
            catch
            {
            }

            if (trimmed.Length == 0)
            {
                return "";
            }

            if (!trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                trimmed += ".exe";
            }

            return trimmed.ToLowerInvariant();
        }

        private static bool ContainsProcessName(List<string> names, string processName)
        {
            if (names == null)
            {
                return false;
            }

            string target = NormalizeProcessName(processName);
            for (int i = 0; i < names.Count; i++)
            {
                if (string.Equals(NormalizeProcessName(names[i]), target, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private void OnSettingChanged(bool reloadPreset)
        {
            if (_loadingControls)
            {
                return;
            }

            ApplyControlsToConfig();
            ApplyRuntimeSettings(reloadPreset);
            UpdateStatus("Unsaved changes are active.");
        }

        private void OnAutoDetectChanged()
        {
            if (_loadingControls)
            {
                return;
            }

            ApplyControlsToConfig();
            if (_config.AutoDetectDevices)
            {
                DetectDevicesNow();
            }
            else
            {
                ApplyRuntimeSettings(false);
                UpdateStatus("Device auto detect disabled.");
            }
        }

        private void DetectDevicesNow()
        {
            DeviceDetectionResult detected = DeviceDetector.Detect();
            ApplyDetectedDevices(detected);

            _loadingControls = true;
            try
            {
                SelectComboValue(_keyboardLayoutCombo, _config.KeyboardLayout);
                SelectComboValue(_mouseStyleCombo, _config.MouseStyle);
                _autoDetectCheck.Checked = _config.AutoDetectDevices;
            }
            finally
            {
                _loadingControls = false;
            }

            ApplyRuntimeSettings(false);
            UpdateStatus("Detected devices: " + detected.Summary);
        }

        private void ApplyDetectedDevices(DeviceDetectionResult detected)
        {
            if (detected == null)
            {
                return;
            }

            if (IsKeyboardLayout(detected.KeyboardLayout))
            {
                _config.KeyboardLayout = detected.KeyboardLayout;
            }
            if (IsMouseStyle(detected.MouseStyle))
            {
                _config.MouseStyle = detected.MouseStyle;
            }
        }

        private static bool IsKeyboardLayout(string layout)
        {
            return string.Equals(layout, "full", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(layout, "tkl", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(layout, "60", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(layout, "laptop", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsMouseStyle(string style)
        {
            return string.Equals(style, "gaming", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(style, "office", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(style, "compact", StringComparison.OrdinalIgnoreCase);
        }

        private void ApplyRuntimeSettings(bool reloadPreset)
        {
            _hook.PlayOnRepeat = _config.PlayOnRepeat;
            _hook.PlayModifiers = _config.PlayModifiers;
            _hook.IgnoreInjectedInput = _config.IgnoreInjectedInput;
            _audio.Configure(_config);
            if (_deviceView != null)
            {
                _deviceView.KeyboardLayout = _config.KeyboardLayout;
                _deviceView.MouseStyle = _config.MouseStyle;
                _deviceView.ShowMouse = _config.ShowMouse || _config.MouseSoundsEnabled;
                _deviceView.EditMode = _config.EditMode;
                _deviceView.ShadowEnabled = _config.VirtualShadowEnabled;
                _deviceView.ShadowDepth = _config.VirtualShadowDepth;
                _deviceView.ShadowOffsetX = _config.VirtualShadowOffsetX;
                _deviceView.ShadowOffsetY = _config.VirtualShadowOffsetY;
                _deviceView.SetOverrides(_config.ButtonOverrides);
            }

            if (reloadPreset)
            {
                PresetInfo preset = FindPreset(_config.Preset);
                if (preset != null)
                {
                    _audio.LoadPreset(preset.DirectoryPath);
                }
            }

            UpdateTrayMenu();
            UpdateAdminButton();
        }

        private void SaveConfig()
        {
            ApplyControlsToConfig();
            ApplyRuntimeSettings(false);
            SetStartup(_config.RunAtStartup);
            _configStore.Save(_config);
            UpdateStatus("Saved config.");
        }

        private void ReloadPresets()
        {
            _presets.Clear();
            List<PresetInfo> fresh = SoundLibrary.Scan(_assetsRoot);
            for (int i = 0; i < fresh.Count; i++)
            {
                _presets.Add(fresh[i]);
            }
            LoadControlsFromConfig();
            ApplyRuntimeSettings(true);
            UpdateStatus("Reloaded sound presets.");
        }

        private void OnHookKeyPressed(int vkCode)
        {
            FlashVirtualKey(vkCode);
            if (!_config.Enabled || !_config.KeyboardSoundsEnabled)
            {
                return;
            }

            if (IsForegroundProcessExcluded())
            {
                return;
            }

            if (_config.CooldownMs > 0)
            {
                long now = _stopwatch.ElapsedTicks;
                long cooldownTicks = (long)(_config.CooldownMs * (Stopwatch.Frequency / 1000.0));
                if (now - _lastPlayTicks < cooldownTicks)
                {
                    return;
                }
                _lastPlayTicks = now;
            }

            PlayButtonSound(PhysicalButtonIdForVk(vkCode));
        }

        private void OnMouseButtonPressed(string buttonId)
        {
            FlashVirtualButton(buttonId);
            if (!_config.Enabled || !_config.MouseSoundsEnabled)
            {
                return;
            }

            if (IsForegroundProcessExcluded())
            {
                return;
            }

            PlayButtonSound(buttonId);
        }

        private void OnVirtualButtonActivated(object sender, VirtualButtonEventArgs e)
        {
            if (!_config.Enabled)
            {
                return;
            }
            if (e.IsMouse)
            {
                if (!_config.MouseSoundsEnabled)
                {
                    return;
                }
            }
            else if (!_config.KeyboardSoundsEnabled)
            {
                return;
            }
            PlayButtonSound(e.ButtonId);
        }

        private void OnDeviceCustomizationChanged(object sender, EventArgs e)
        {
            _configStore.Save(_config);
            UpdateStatus("Device customization saved.");
        }

        private void PlayButtonSound(string buttonId)
        {
            VirtualButtonConfig custom;
            if (_config.ButtonOverrides != null &&
                _config.ButtonOverrides.TryGetValue(buttonId, out custom) &&
                custom != null &&
                !string.IsNullOrWhiteSpace(custom.SoundPath))
            {
                try
                {
                    if (_audio.PlaySoundPath(custom.SoundPath))
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus("Custom sound failed: " + ex.Message);
                }
            }

            string mouseSound = DefaultMouseSoundPath(buttonId);
            if (!string.IsNullOrEmpty(mouseSound) && _audio.PlaySoundPath(mouseSound))
            {
                return;
            }

            _audio.PlayRandom();
        }

        private string DefaultMouseSoundPath(string buttonId)
        {
            if (string.IsNullOrWhiteSpace(buttonId) ||
                !buttonId.StartsWith("mouse.", StringComparison.OrdinalIgnoreCase))
            {
                return "";
            }

            string fileName;
            if (string.Equals(buttonId, "mouse.middle", StringComparison.OrdinalIgnoreCase))
            {
                fileName = "mouse_middle.wav";
            }
            else if (string.Equals(buttonId, "mouse.x1", StringComparison.OrdinalIgnoreCase))
            {
                fileName = "mouse_x1.wav";
            }
            else if (string.Equals(buttonId, "mouse.x2", StringComparison.OrdinalIgnoreCase))
            {
                fileName = "mouse_x2.wav";
            }
            else if (string.Equals(buttonId, "mouse.right", StringComparison.OrdinalIgnoreCase))
            {
                fileName = "mouse_right.wav";
            }
            else
            {
                fileName = "mouse_left.wav";
            }

            string path = Path.Combine(_assetsRoot, "mouse", fileName);
            return File.Exists(path) ? path : "";
        }

        private static string PhysicalButtonIdForVk(int vkCode)
        {
            if (vkCode >= 65 && vkCode <= 90)
            {
                return "key." + ((char)vkCode).ToString();
            }
            if (vkCode >= 48 && vkCode <= 57)
            {
                return "key." + ((char)vkCode).ToString();
            }

            switch (vkCode)
            {
                case 0x1B: return "key.Esc";
                case 0x08: return "key.Back";
                case 0x09: return "key.Tab";
                case 0x14: return "key.Caps";
                case 0x0D: return "key.Enter";
                case 0x10:
                case 0xA0:
                case 0xA1:
                    return "key.Shift";
                case 0x11:
                case 0xA2:
                case 0xA3:
                    return "key.Ctrl";
                case 0x12:
                case 0xA4:
                case 0xA5:
                    return "key.Alt";
                case 0x20: return "key.Space";
                case 0x5B:
                case 0x5C:
                    return "key.Win";
                case 0x5D: return "key.Menu";
                case 0x26: return "key.Up";
                case 0x28: return "key.Down";
                case 0x25: return "key.Left";
                case 0x27: return "key.Right";
                case 0x2E: return "key.Del";
                case 0x2D: return "key.Ins";
                case 0x24: return "key.Home";
                case 0x23: return "key.End";
                case 0x21: return "key.PgUp";
                case 0x22: return "key.PgDn";
                case 0x60: return "key.Num0";
                case 0x61: return "key.Num1";
                case 0x62: return "key.Num2";
                case 0x63: return "key.Num3";
                case 0x64: return "key.Num4";
                case 0x65: return "key.Num5";
                case 0x66: return "key.Num6";
                case 0x67: return "key.Num7";
                case 0x68: return "key.Num8";
                case 0x69: return "key.Num9";
                case 0x6E: return "key.NumDot";
                case 0xBD: return "key.-";
                case 0xBB: return "key.=";
                case 0xDB: return "key.[";
                case 0xDD: return "key.]";
                case 0xDC: return "key.Backslash";
                case 0xBA: return "key.;";
                case 0xDE: return "key.'";
                case 0xBC: return "key.,";
                case 0xBE: return "key..";
                case 0xBF: return "key.Slash";
                default: return "";
            }
        }

        private void FlashVirtualKey(int vkCode)
        {
            if (_deviceView == null || _deviceView.IsDisposed)
            {
                return;
            }

            try
            {
                if (_deviceView.InvokeRequired)
                {
                    _deviceView.BeginInvoke((MethodInvoker)delegate { _deviceView.FlashKey(vkCode); });
                }
                else
                {
                    _deviceView.FlashKey(vkCode);
                }
            }
            catch
            {
            }
        }

        private void FlashVirtualButton(string buttonId)
        {
            if (_deviceView == null || _deviceView.IsDisposed)
            {
                return;
            }

            try
            {
                if (_deviceView.InvokeRequired)
                {
                    _deviceView.BeginInvoke((MethodInvoker)delegate { _deviceView.FlashButton(buttonId); });
                }
                else
                {
                    _deviceView.FlashButton(buttonId);
                }
            }
            catch
            {
            }
        }

        private bool IsForegroundProcessExcluded()
        {
            string foreground = GetForegroundProcessName();
            if (foreground.Length == 0)
            {
                return false;
            }

            string foregroundWithExe = NormalizeProcessName(foreground);
            string foregroundBare = Path.GetFileNameWithoutExtension(foregroundWithExe).ToLowerInvariant();

            if (_config.UseAppAllowList)
            {
                if (_config.AllowedProcesses == null || _config.AllowedProcesses.Count == 0)
                {
                    return true;
                }

                if (!ContainsProcessName(_config.AllowedProcesses, foregroundWithExe))
                {
                    return true;
                }
            }

            if (_config.ExcludedProcesses == null || _config.ExcludedProcesses.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < _config.ExcludedProcesses.Count; i++)
            {
                string item = _config.ExcludedProcesses[i];
                if (item == null)
                {
                    continue;
                }

                string normalized = item.Trim().ToLowerInvariant();
                if (normalized.Length == 0)
                {
                    continue;
                }

                string normalizedBare = Path.GetFileNameWithoutExtension(normalized);
                if (normalized == foregroundWithExe || normalizedBare == foregroundBare)
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetForegroundProcessName()
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero)
                {
                    return "";
                }

                int pid;
                GetWindowThreadProcessId(hwnd, out pid);
                if (pid <= 0)
                {
                    return "";
                }

                using (Process process = Process.GetProcessById(pid))
                {
                    return process.ProcessName + ".exe";
                }
            }
            catch
            {
                return "";
            }
        }

        private void UpdateValueLabels()
        {
            if (_volumeLabel != null)
            {
                SetNumberValue(_volumeLabel, _volumeTrack.Value);
            }
            if (_keyGainLabel != null)
            {
                SetNumberValue(_keyGainLabel, _keyGainTrack.Value);
            }
            if (_randomVolumeLabel != null)
            {
                SetNumberValue(_randomVolumeLabel, _randomVolumeTrack.Value);
            }
            if (_randomPitchLabel != null)
            {
                SetNumberValue(_randomPitchLabel, _randomPitchTrack.Value);
            }
            if (_shadowDepthLabel != null)
            {
                SetNumberValue(_shadowDepthLabel, _shadowDepthTrack.Value);
            }
            if (_shadowXLabel != null)
            {
                SetNumberValue(_shadowXLabel, _shadowXTrack.Value);
            }
            if (_shadowYLabel != null)
            {
                SetNumberValue(_shadowYLabel, _shadowYTrack.Value);
            }
        }

        private static void SetNumberValue(NumericUpDown box, int value)
        {
            if (box == null)
            {
                return;
            }

            int clamped = Math.Max((int)box.Minimum, Math.Min((int)box.Maximum, value));
            if ((int)box.Value != clamped)
            {
                box.Value = clamped;
            }
        }

        private void UpdateStatus(string note)
        {
            string hook = _hookInstalled ? "OK" : "failed";
            string mouseHook = _mouseHookInstalled ? "OK" : "failed";
            string admin = IsAdministrator() ? "yes" : "no";
            string audio = _audio.AudioReady ? "OK" : "failed";
            if (!_audio.AudioReady && !string.IsNullOrEmpty(_audio.LastError))
            {
                audio = "failed: " + _audio.LastError;
            }

            PresetInfo preset = FindPreset(_config.Preset);
            string presetText = preset != null
                ? preset.DisplayName + " (" + _audio.SampleCount.ToString(CultureInfo.InvariantCulture) + ")"
                : "none";

            _statusLabel.Text = note + "  Audio: " + audio + " | Keys: " + hook +
                " | Mouse: " + mouseHook + " | Admin: " + admin + " | Preset: " + presetText;
        }

        private void ToggleEnabledFromTray()
        {
            _config.Enabled = !_config.Enabled;
            ApplyTrayChange(false, _config.Enabled ? "Enabled." : "Disabled.");
        }

        private void ToggleStartupFromTray()
        {
            _config.RunAtStartup = !_config.RunAtStartup;
            ApplyTrayChange(false, _config.RunAtStartup ? "Startup enabled." : "Startup disabled.");
        }

        private void ToggleRepeatFromTray()
        {
            _config.PlayOnRepeat = !_config.PlayOnRepeat;
            ApplyTrayChange(false, _config.PlayOnRepeat ? "Held key repeat enabled." : "Held key repeat disabled.");
        }

        private void ToggleModifiersFromTray()
        {
            _config.PlayModifiers = !_config.PlayModifiers;
            ApplyTrayChange(false, _config.PlayModifiers ? "Modifier keys enabled." : "Modifier keys disabled.");
        }

        private void ToggleInjectedFromTray()
        {
            _config.IgnoreInjectedInput = !_config.IgnoreInjectedInput;
            ApplyTrayChange(false, _config.IgnoreInjectedInput ? "Injected input ignored." : "Injected input allowed.");
        }

        private void SetPresetFromTray(string presetId)
        {
            if (FindPreset(presetId) == null)
            {
                return;
            }

            _config.Preset = presetId;
            ApplyTrayChange(true, "Preset changed.");
        }

        private void SetVolumeFromTray(int volume)
        {
            _config.Volume = volume;
            ApplyTrayChange(false, "Output volume changed.");
        }

        private void SetGainFromTray(int gain)
        {
            _config.KeyGainPercent = gain;
            ApplyTrayChange(false, "Key boost changed.");
        }

        private void ApplyTrayChange(bool reloadPreset, string note)
        {
            _config.Normalize(_config.Preset);
            LoadControlsFromConfig();
            ApplyRuntimeSettings(reloadPreset);
            SetStartup(_config.RunAtStartup);
            _configStore.Save(_config);
            UpdateStatus(note);
        }

        private void UpdateTrayMenu()
        {
            if (_trayEnabledItem != null)
            {
                _trayEnabledItem.Text = _config.Enabled ? "Disable" : "Enable";
                _trayEnabledItem.Checked = _config.Enabled;
            }

            if (_trayStartupItem != null)
            {
                _trayStartupItem.Checked = _config.RunAtStartup;
            }

            if (_trayRepeatItem != null)
            {
                _trayRepeatItem.Checked = _config.PlayOnRepeat;
            }

            if (_trayModifiersItem != null)
            {
                _trayModifiersItem.Checked = _config.PlayModifiers;
            }

            if (_trayInjectedItem != null)
            {
                _trayInjectedItem.Checked = _config.IgnoreInjectedInput;
            }

            UpdateCheckedMenuItems(_trayPresetItems, _config.Preset);
            UpdateCheckedMenuItems(_trayVolumeItems, _config.Volume);
            UpdateCheckedMenuItems(_trayGainItems, _config.KeyGainPercent);

            if (_trayIcon != null)
            {
                _trayIcon.Text = _config.Enabled ? "CreamyKeys: enabled" : "CreamyKeys: disabled";
            }
        }

        private static void UpdateCheckedMenuItems(List<MenuItem> items, string selected)
        {
            if (items == null)
            {
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                string value = items[i].Tag as string;
                items[i].Checked = string.Equals(value, selected, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static void UpdateCheckedMenuItems(List<MenuItem> items, int selected)
        {
            if (items == null)
            {
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Tag is int)
                {
                    items[i].Checked = (int)items[i].Tag == selected;
                }
            }
        }

        private void UpdateAdminButton()
        {
            if (_adminButton != null)
            {
                _adminButton.Enabled = !IsAdministrator();
            }
        }

        private void ShowSettings()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void ExitApplication()
        {
            _allowClose = true;
            Close();
        }

        private void RelaunchAsAdmin()
        {
            try
            {
                SaveConfig();
                ProcessStartInfo startInfo = new ProcessStartInfo(Application.ExecutablePath);
                startInfo.UseShellExecute = true;
                startInfo.Verb = "runas";
                Process.Start(startInfo);
                ExitApplication();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not relaunch as administrator:\r\n" + ex.Message,
                    "CreamyKeys", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private PresetInfo FindPreset(string id)
        {
            for (int i = 0; i < _presets.Count; i++)
            {
                if (string.Equals(_presets[i].Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    return _presets[i];
                }
            }
            return null;
        }

        private static bool IsAdministrator()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private static void SetStartup(bool enabled)
        {
            const string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
            const string valueName = "CreamyKeys";
            const string legacyValueName = "CreamyKeysDesktop";
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath, true))
            {
                if (key == null)
                {
                    return;
                }

                if (enabled)
                {
                    key.SetValue(valueName, "\"" + Application.ExecutablePath + "\"");
                    key.DeleteValue(legacyValueName, false);
                }
                else
                {
                    key.DeleteValue(valueName, false);
                    key.DeleteValue(legacyValueName, false);
                }
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        private sealed class PresetComboItem
        {
            public readonly PresetInfo Preset;

            public PresetComboItem(PresetInfo preset)
            {
                Preset = preset;
            }

            public override string ToString()
            {
                return Preset.DisplayName + " (" + Preset.Count.ToString(CultureInfo.InvariantCulture) + ")";
            }
        }

        private sealed class ComboItem
        {
            public readonly string Value;
            private readonly string _label;

            public ComboItem(string value, string label)
            {
                Value = value;
                _label = label;
            }

            public override string ToString()
            {
                return _label;
            }
        }
    }
}
