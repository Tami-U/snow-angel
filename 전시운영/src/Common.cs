// 스노우 엔젤 전시 운영 공통 코드 (C# 5 / .NET Framework 4 — Windows 10/11 기본 포함)
using System;
using System.Diagnostics;
using System.IO;
using System.Management;

static class Kiosk
{
    public const string AppTitle = "스노우 엔젤";
    public const string OnlineUrl = "https://tami-u.github.io/snow-angel/";
    public const string ProfileName = "SnowAngelKiosk"; // 전시 전용 크롬 프로필(개인 크롬과 분리)

    public static string ProfileDir
    {
        get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ProfileName); }
    }

    public static string PidFile
    {
        get { return Path.Combine(ProfileDir, "launcher.pid"); }
    }

    // 전시용 크롬과 이전에 실행한 '전시 시작'을 모두 끈다. (개인적으로 쓰는 크롬 창은 건드리지 않음)
    public static int StopAll()
    {
        int killed = 0;
        int self = Process.GetCurrentProcess().Id;

        try
        {
            if (File.Exists(PidFile))
            {
                // 형식: "PID\n실행 파일 경로" — PID가 다른 프로그램에 재사용됐으면 건드리지 않음
                string[] lines = File.ReadAllLines(PidFile);
                int pid;
                if (lines.Length >= 2 && int.TryParse(lines[0].Trim(), out pid) && pid != self)
                {
                    try
                    {
                        Process p = Process.GetProcessById(pid);
                        string exe = SafeMainModule(p);
                        if (exe != null && string.Equals(exe, lines[1].Trim(), StringComparison.OrdinalIgnoreCase)) { p.Kill(); p.WaitForExit(3000); killed++; }
                    }
                    catch { }
                }
                try { File.Delete(PidFile); } catch { }
            }
        }
        catch { }

        try
        {
            using (ManagementObjectSearcher s = new ManagementObjectSearcher(
                "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name = 'chrome.exe' OR Name = 'msedge.exe'"))
            {
                foreach (ManagementObject mo in s.Get())
                {
                    object cl = mo["CommandLine"];
                    if (cl == null || cl.ToString().IndexOf(ProfileName, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    try { Process.GetProcessById(Convert.ToInt32(mo["ProcessId"])).Kill(); killed++; } catch { }
                }
            }
        }
        catch { }
        return killed;
    }

    public static bool KioskBrowserRunning()
    {
        try
        {
            using (ManagementObjectSearcher s = new ManagementObjectSearcher(
                "SELECT CommandLine FROM Win32_Process WHERE Name = 'chrome.exe' OR Name = 'msedge.exe'"))
            {
                foreach (ManagementObject mo in s.Get())
                {
                    object cl = mo["CommandLine"];
                    if (cl != null && cl.ToString().IndexOf(ProfileName, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                }
            }
        }
        catch { return true; } // 확인이 안 되면 서버를 유지하는 쪽으로
        return false;
    }

    static string SafeMainModule(Process p)
    {
        try { return p.MainModule.FileName; } catch { return null; }
    }
}
