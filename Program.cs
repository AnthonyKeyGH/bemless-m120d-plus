using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using LibreHardwareMonitor.Hardware;
using Microsoft.Win32.SafeHandles;
using System.Security.Principal;

namespace BEMlessM120DPlus;

internal static class Program
{
    private const ushort VendorId = 0x5131;
    private const ushort ProductId = 0x2007;
    private const int ReportLength = 65;
    private const int TemperatureOffset = 2;
    private const int UpdateIntervalMs = 1000;
    private const int StartupDelayMs = 5000;
    private const float MinimumValidTemperature = 5.0f;
    private const float MaximumValidTemperature = 110.0f;

    private static readonly bool Spanish =
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
            .Equals("es", StringComparison.OrdinalIgnoreCase);

    private static readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BEMless");

    private static readonly string LogPath =
        Path.Combine(DataDirectory, "BEMless.log");

    private static Computer? _computer;
    private static SafeFileHandle? _hidHandle;
    private static FileStream? _hidStream;

    private const uint DigcfPresent = 0x00000002;
    private const uint DigcfDeviceInterface = 0x00000010;
    private const uint GenericWrite = 0x40000000;
    private const uint FileShareRead = 0x00000001;
    private const uint FileShareWrite = 0x00000002;
    private const uint OpenExisting = 3;
    private const int ErrorNoMoreItems = 259;

    [StructLayout(LayoutKind.Sequential)]
    private struct SpDeviceInterfaceData
    {
        public int cbSize;
        public Guid InterfaceClassGuid;
        public int Flags;
        private UIntPtr Reserved;
    }

    [DllImport("hid.dll")]
    private static extern void HidD_GetHidGuid(out Guid hidGuid);

    [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetupDiGetClassDevs(
        ref Guid classGuid,
        IntPtr enumerator,
        IntPtr hwndParent,
        uint flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiEnumDeviceInterfaces(
        IntPtr deviceInfoSet,
        IntPtr deviceInfoData,
        ref Guid interfaceClassGuid,
        uint memberIndex,
        ref SpDeviceInterfaceData deviceInterfaceData);

    [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool SetupDiGetDeviceInterfaceDetail(
        IntPtr deviceInfoSet,
        ref SpDeviceInterfaceData deviceInterfaceData,
        IntPtr deviceInterfaceDetailData,
        uint deviceInterfaceDetailDataSize,
        out uint requiredSize,
        IntPtr deviceInfoData);

    [DllImport("setupapi.dll")]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(
        string fileName,
        uint desiredAccess,
        uint shareMode,
        IntPtr securityAttributes,
        uint creationDisposition,
        uint flagsAndAttributes,
        IntPtr templateFile);

    [STAThread]
    private static int Main(string[] args)
    {
        Directory.CreateDirectory(DataDirectory);

        using var mutex = new Mutex(
            true,
            "Local\\BEMless_M120D_Plus_5131_2007",
            out bool firstInstance);

        if (!firstInstance)
            return 0;

        try
        {
            Thread.Sleep(StartupDelayMs);
            InitializeTemperatureReader();

            if (args.Length == 2 &&
                args[0].Equals("--set", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(args[1], out int manualValue))
            {
                manualValue = Math.Clamp(manualValue, 0, 99);

                if (!EnsureHidOpen())
                    return 3;

                return SendTemperature(manualValue) ? 0 : 4;
            }

            RunBackgroundLoop();
            return 0;
        }
        catch (Exception ex)
        {
            Log(T("Fatal error: ", "Error fatal: ") + ex);
            return 1;
        }
        finally
        {
            CloseHid();
            try { _computer?.Close(); } catch { }
        }
    }

    private static string T(string english, string spanish) =>
        Spanish ? spanish : english;

    private static void InitializeTemperatureReader()
    {
        _computer = new Computer { IsCpuEnabled = true };
        _computer.Open();

        Log(
            T("Started", "Iniciado") +
            $". HID={VendorId:X4}:{ProductId:X4}. " +
            T("Elevated", "Elevado") +
            $"={IsElevated()}.");
    }

    private static void RunBackgroundLoop()
    {
        int? lastSent = null;
        DateTime lastNoTemperatureLog = DateTime.MinValue;
        DateTime lastNoHidLog = DateTime.MinValue;

        while (true)
        {
            try
            {
                float? rawTemperature = ReadCpuTemperature();

                if (!rawTemperature.HasValue)
                {
                    if ((DateTime.Now - lastNoTemperatureLog).TotalMinutes >= 5)
                    {
                        Log(T(
                            "No valid CPU temperature. Nothing was sent to the display.",
                            "No hay una temperatura de CPU válida. No se envió nada a la pantalla."));

                        LogAvailableCpuTemperatureSensors();
                        lastNoTemperatureLog = DateTime.Now;
                    }

                    Thread.Sleep(UpdateIntervalMs);
                    continue;
                }

                int temperature = NormalizeTemperature(rawTemperature.Value);

                if (!EnsureHidOpen())
                {
                    if ((DateTime.Now - lastNoHidLog).TotalMinutes >= 1)
                    {
                        Log(T(
                            "Could not open HID 5131:2007. Will retry.",
                            "No se pudo abrir el HID 5131:2007. Se volverá a intentar."));

                        lastNoHidLog = DateTime.Now;
                    }

                    Thread.Sleep(3000);
                    continue;
                }

                if (SendTemperature(temperature))
                {
                    if (lastSent != temperature)
                    {
                        Log(
                            $"CPU {rawTemperature.Value:F1} °C -> " +
                            T("display", "pantalla") +
                            $" {temperature}");

                        lastSent = temperature;
                    }
                }
                else
                {
                    CloseHid();
                }
            }
            catch (Exception ex)
            {
                Log(T("Loop error: ", "Error en el ciclo: ") + ex.Message);
                CloseHid();
            }

            Thread.Sleep(UpdateIntervalMs);
        }
    }

    private static float? ReadCpuTemperature()
    {
        if (_computer is null)
            return null;

        var candidates = new List<(float Value, int Score, string Name)>();

        foreach (IHardware hardware in _computer.Hardware)
        {
            if (hardware.HardwareType != HardwareType.Cpu)
                continue;

            hardware.Update();
            CollectTemperatureSensors(hardware, candidates);
        }

        if (candidates.Count == 0)
            return null;

        return candidates
            .OrderByDescending(candidate => candidate.Score)
            .ThenByDescending(candidate => candidate.Value)
            .First()
            .Value;
    }

    private static void CollectTemperatureSensors(
        IHardware hardware,
        List<(float Value, int Score, string Name)> candidates)
    {
        foreach (ISensor sensor in hardware.Sensors)
        {
            if (sensor.SensorType != SensorType.Temperature ||
                !sensor.Value.HasValue)
                continue;

            float value = sensor.Value.Value;

            if (value < MinimumValidTemperature ||
                value > MaximumValidTemperature)
                continue;

            string name = sensor.Name ?? string.Empty;

            if (name.Contains("Distance", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("TjMax", StringComparison.OrdinalIgnoreCase))
                continue;

            candidates.Add((value, ScoreTemperatureSensor(name), name));
        }

        foreach (IHardware subHardware in hardware.SubHardware)
        {
            subHardware.Update();
            CollectTemperatureSensors(subHardware, candidates);
        }
    }

    private static int ScoreTemperatureSensor(string name)
    {
        if (name.Contains("Tctl/Tdie", StringComparison.OrdinalIgnoreCase)) return 110;
        if (name.Equals("CPU Package", StringComparison.OrdinalIgnoreCase)) return 105;
        if (name.Contains("Package", StringComparison.OrdinalIgnoreCase)) return 100;
        if (name.Contains("Tdie", StringComparison.OrdinalIgnoreCase)) return 98;
        if (name.Contains("CPU Die", StringComparison.OrdinalIgnoreCase)) return 95;
        if (name.Contains("Core Max", StringComparison.OrdinalIgnoreCase)) return 90;
        if (name.Contains("Core Average", StringComparison.OrdinalIgnoreCase)) return 85;
        if (name.Contains("CPU Core", StringComparison.OrdinalIgnoreCase)) return 80;
        if (name.Contains("Core", StringComparison.OrdinalIgnoreCase)) return 60;
        return 10;
    }

    private static int NormalizeTemperature(float value)
    {
        int rounded = (int)Math.Round(value, MidpointRounding.AwayFromZero);
        return Math.Clamp(rounded, 0, 99);
    }

    private static bool EnsureHidOpen()
    {
        if (_hidStream is not null &&
            _hidHandle is not null &&
            !_hidHandle.IsInvalid &&
            !_hidHandle.IsClosed)
            return true;

        CloseHid();

        string? path;

        try
        {
            path = FindHidPath(VendorId, ProductId);
        }
        catch (Exception ex)
        {
            Log(T("HID discovery error: ", "Error al buscar el HID: ") + ex.Message);
            return false;
        }

        if (path is null)
            return false;

        SafeFileHandle handle = CreateFile(
            path,
            GenericWrite,
            FileShareRead | FileShareWrite,
            IntPtr.Zero,
            OpenExisting,
            0,
            IntPtr.Zero);

        if (handle.IsInvalid)
        {
            Log(
                T("CreateFile(HID) failed. Win32=", "CreateFile(HID) falló. Win32=") +
                Marshal.GetLastWin32Error());

            handle.Dispose();
            return false;
        }

        try
        {
            _hidStream = new FileStream(
                handle,
                FileAccess.Write,
                ReportLength,
                isAsync: false);

            _hidHandle = handle;
            Log(T("HID opened.", "HID abierto."));
            return true;
        }
        catch (Exception ex)
        {
            Log(T(
                "Could not create HID stream: ",
                "No se pudo crear el flujo HID: ") + ex.Message);

            handle.Dispose();
            return false;
        }
    }

    private static bool SendTemperature(int temperature)
    {
        if (_hidStream is null)
            return false;

        var report = new byte[ReportLength];
        report[0] = 0x00;
        report[1] = 0x40;
        report[TemperatureOffset] = (byte)temperature;

        try
        {
            _hidStream.Write(report, 0, report.Length);
            _hidStream.Flush();
            return true;
        }
        catch (Exception ex)
        {
            Log(T("HID write failed: ", "Falló la escritura HID: ") + ex.Message);
            return false;
        }
    }

    private static void CloseHid()
    {
        try { _hidStream?.Dispose(); } catch { }
        _hidStream = null;

        try { _hidHandle?.Dispose(); } catch { }
        _hidHandle = null;
    }

    private static string? FindHidPath(ushort vendorId, ushort productId)
    {
        HidD_GetHidGuid(out Guid hidGuid);

        IntPtr deviceInfoSet = SetupDiGetClassDevs(
            ref hidGuid,
            IntPtr.Zero,
            IntPtr.Zero,
            DigcfPresent | DigcfDeviceInterface);

        if (deviceInfoSet == new IntPtr(-1))
            throw new Win32Exception(Marshal.GetLastWin32Error());

        try
        {
            for (uint index = 0; ; index++)
            {
                var interfaceData = new SpDeviceInterfaceData
                {
                    cbSize = Marshal.SizeOf<SpDeviceInterfaceData>()
                };

                if (!SetupDiEnumDeviceInterfaces(
                    deviceInfoSet,
                    IntPtr.Zero,
                    ref hidGuid,
                    index,
                    ref interfaceData))
                {
                    int error = Marshal.GetLastWin32Error();

                    if (error == ErrorNoMoreItems)
                        break;

                    throw new Win32Exception(error);
                }

                SetupDiGetDeviceInterfaceDetail(
                    deviceInfoSet,
                    ref interfaceData,
                    IntPtr.Zero,
                    0,
                    out uint requiredSize,
                    IntPtr.Zero);

                if (requiredSize == 0)
                    continue;

                IntPtr detail = Marshal.AllocHGlobal((int)requiredSize);

                try
                {
                    Marshal.WriteInt32(detail, IntPtr.Size == 8 ? 8 : 6);

                    if (!SetupDiGetDeviceInterfaceDetail(
                        deviceInfoSet,
                        ref interfaceData,
                        detail,
                        requiredSize,
                        out _,
                        IntPtr.Zero))
                        continue;

                    string? path = Marshal.PtrToStringAuto(detail + 4);

                    if (path is null)
                        continue;

                    if (path.Contains(
                            $"vid_{vendorId:x4}",
                            StringComparison.OrdinalIgnoreCase) &&
                        path.Contains(
                            $"pid_{productId:x4}",
                            StringComparison.OrdinalIgnoreCase))
                        return path;
                }
                finally
                {
                    Marshal.FreeHGlobal(detail);
                }
            }
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(deviceInfoSet);
        }

        return null;
    }

    private static void LogAvailableCpuTemperatureSensors()
    {
        if (_computer is null)
            return;

        try
        {
            foreach (IHardware hardware in _computer.Hardware)
            {
                if (hardware.HardwareType != HardwareType.Cpu)
                    continue;

                hardware.Update();

                foreach (ISensor sensor in hardware.Sensors)
                {
                    if (sensor.SensorType == SensorType.Temperature)
                    {
                        Log(
                            $"Sensor: {hardware.Name} / {sensor.Name} = " +
                            $"{sensor.Value?.ToString() ?? "null"}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log(T(
                "Sensor enumeration error: ",
                "Error al enumerar sensores: ") + ex.Message);
        }
    }

    private static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(DataDirectory);

            File.AppendAllText(
                LogPath,
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {message}" +
                Environment.NewLine);

            var info = new FileInfo(LogPath);

            if (info.Exists && info.Length > 512 * 1024)
            {
                string oldLog = LogPath + ".old";
                try { File.Delete(oldLog); } catch { }
                try { File.Move(LogPath, oldLog); } catch { }
            }
        }
        catch
        {
        }
    }

    private static bool IsElevated()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);

            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
}
