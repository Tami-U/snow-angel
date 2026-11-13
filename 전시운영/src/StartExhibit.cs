// '전시 시작.exe' — 로컬 서버를 띄우고 전시용 크롬을 전체화면(키오스크)으로 연다.
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

static class StartExhibit
{
    [STAThread]
    static int Main(string[] args)
    {
        bool noBrowser = Array.IndexOf(args, "--no-browser") >= 0; // 점검용

        Kiosk.StopAll(); // 이전 실행 정리
        Directory.CreateDirectory(Kiosk.ProfileDir);
        File.WriteAllText(Kiosk.PidFile, Process.GetCurrentProcess().Id + "\n" + Process.GetCurrentProcess().MainModule.FileName);

        string url = Kiosk.OnlineUrl;
        HttpListener listener = null;
        string root = FindAppRoot();
        if (root != null)
        {
            listener = StartServer(root, out url);
            if (listener == null) url = Kiosk.OnlineUrl; // 포트를 못 열면 온라인 주소로
        }

        if (noBrowser)
        {
            Console.WriteLine(url);
            Thread.Sleep(Timeout.Infinite);
            return 0;
        }

        string browser = FindBrowser();
        if (browser == null)
        {
            MessageBox.Show("Chrome을 찾을 수 없어요.\nGoogle Chrome을 설치한 뒤 다시 실행해 주세요.", Kiosk.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }

        string[] flags = {
            "--user-data-dir=\"" + Kiosk.ProfileDir + "\"",
            "--kiosk",
            "--autoplay-policy=no-user-gesture-required", // 클릭 없이 소리 재생
            "--use-fake-ui-for-media-stream",             // 카메라 권한 팝업 자동 허용
            "--no-first-run", "--no-default-browser-check",
            "--disable-session-crashed-bubble", "--noerrdialogs", "--disable-infobars",
            "--disable-features=Translate,TranslateUI",
            "--disable-pinch", "--overscroll-history-navigation=0",
            "--check-for-update-interval=31536000",
            url
        };
        try { Process.Start(new ProcessStartInfo(browser, string.Join(" ", flags)) { UseShellExecute = false }); }
        catch (Exception e)
        {
            MessageBox.Show("Chrome을 실행하지 못했어요.\n" + e.Message, Kiosk.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }

        // 로컬 서버를 쓰는 동안에는 전시용 크롬이 꺼질 때까지 대기 → 꺼지면 같이 종료
        if (listener != null)
        {
            Thread.Sleep(8000);
            int missing = 0;
            while (missing < 2)
            {
                missing = Kiosk.KioskBrowserRunning() ? 0 : missing + 1;
                Thread.Sleep(2000);
            }
            try { listener.Stop(); } catch { }
        }
        try { File.Delete(Kiosk.PidFile); } catch { }
        return 0;
    }

    // exe 옆(또는 상위 폴더)에 있는 전시 파일(index.html)을 찾는다.
    static string FindAppRoot()
    {
        string dir = AppDomain.CurrentDomain.BaseDirectory;
        string[] candidates = { dir, Path.Combine(dir, ".."), Path.Combine(dir, "snow-angel"), Path.Combine(dir, Path.Combine("..", "snow-angel")) };
        foreach (string c in candidates)
        {
            string full = Path.GetFullPath(c);
            if (File.Exists(Path.Combine(full, "index.html")) && Directory.Exists(Path.Combine(full, "vendor")))
                return full.TrimEnd('\\') + "\\";
        }
        return null;
    }

    static HttpListener StartServer(string root, out string url)
    {
        for (int port = 8765; port < 8775; port++)
        {
            HttpListener l = new HttpListener();
            l.Prefixes.Add("http://localhost:" + port + "/");
            try { l.Start(); }
            catch { try { l.Close(); } catch { } continue; }
            url = "http://localhost:" + port + "/";
            Thread t = new Thread(delegate () { Serve(l, root); });
            t.IsBackground = true;
            t.Start();
            return l;
        }
        url = null;
        return null;
    }

    static void Serve(HttpListener l, string root)
    {
        while (l.IsListening)
        {
            HttpListenerContext ctx;
            try { ctx = l.GetContext(); } catch { break; }
            ThreadPool.QueueUserWorkItem(delegate (object _) { Handle(ctx, root); });
        }
    }

    static void Handle(HttpListenerContext ctx, string root)
    {
        HttpListenerResponse res = ctx.Response;
        try
        {
            string rel = Uri.UnescapeDataString(ctx.Request.Url.AbsolutePath).TrimStart('/').Replace('/', '\\');
            if (rel == "") rel = "index.html";
            string file = Path.GetFullPath(Path.Combine(root, rel));
            if (!file.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(file)) { res.StatusCode = 404; return; }

            byte[] data = File.ReadAllBytes(file);
            res.ContentType = Mime(Path.GetExtension(file));
            res.AddHeader("Accept-Ranges", "bytes");
            res.AddHeader("Cache-Control", "no-cache");

            long start = 0, end = data.Length - 1;
            string range = ctx.Request.Headers["Range"];
            if (range != null && range.StartsWith("bytes=") && data.Length > 0)
            {
                string[] p = range.Substring(6).Split('-');
                long a, b;
                if (p.Length == 2)
                {
                    if (p[0] == "" && long.TryParse(p[1], out b)) start = Math.Max(0, data.Length - b);
                    else if (long.TryParse(p[0], out a))
                    {
                        start = a;
                        if (p[1] != "" && long.TryParse(p[1], out b)) end = Math.Min(b, data.Length - 1);
                    }
                    if (start <= end)
                    {
                        res.StatusCode = 206;
                        res.AddHeader("Content-Range", "bytes " + start + "-" + end + "/" + data.Length);
                    }
                    else { start = 0; end = data.Length - 1; }
                }
            }
            long len = end - start + 1;
            res.ContentLength64 = Math.Max(0, len);
            if (ctx.Request.HttpMethod != "HEAD" && len > 0) res.OutputStream.Write(data, (int)start, (int)len);
        }
        catch { try { res.StatusCode = 500; } catch { } }
        finally { try { res.Close(); } catch { } }
    }

    static string Mime(string ext)
    {
        switch (ext.ToLowerInvariant())
        {
            case ".html": return "text/html; charset=utf-8";
            case ".js": case ".mjs": return "text/javascript; charset=utf-8";
            case ".css": return "text/css; charset=utf-8";
            case ".json": return "application/json";
            case ".wasm": return "application/wasm";
            case ".png": return "image/png";
            case ".jpg": case ".jpeg": return "image/jpeg";
            case ".svg": return "image/svg+xml";
            case ".ogg": return "audio/ogg";
            case ".mp3": return "audio/mpeg";
            case ".md": case ".txt": return "text/plain; charset=utf-8";
            default: return "application/octet-stream";
        }
    }

    static string FindBrowser()
    {
        foreach (string exe in new[] { "chrome.exe", "msedge.exe" })
        {
            foreach (RegistryKey hive in new[] { Registry.CurrentUser, Registry.LocalMachine })
            {
                try
                {
                    using (RegistryKey k = hive.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\" + exe))
                    {
                        string p = k == null ? null : k.GetValue("") as string;
                        if (!string.IsNullOrEmpty(p) && File.Exists(p.Trim('"'))) return p.Trim('"');
                    }
                }
                catch { }
            }
        }
        string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string[] paths = {
            Path.Combine(pf, @"Google\Chrome\Application\chrome.exe"),
            Path.Combine(pf86, @"Google\Chrome\Application\chrome.exe"),
            Path.Combine(local, @"Google\Chrome\Application\chrome.exe"),
            Path.Combine(pf86, @"Microsoft\Edge\Application\msedge.exe"),
            Path.Combine(pf, @"Microsoft\Edge\Application\msedge.exe"),
        };
        foreach (string p in paths) if (File.Exists(p)) return p;
        return null;
    }
}
