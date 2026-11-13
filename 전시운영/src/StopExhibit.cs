// '전시 종료.exe' — 전시용 크롬과 로컬 서버를 끈다. (개인 크롬 창은 그대로)
using System;

static class StopExhibit
{
    [STAThread]
    static int Main()
    {
        Kiosk.StopAll();
        return 0;
    }
}
