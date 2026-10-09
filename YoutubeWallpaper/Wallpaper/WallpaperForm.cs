using Microsoft.Web.WebView2.WinForms;

namespace YoutubeWallpaper.Wallpaper;

internal sealed class WallpaperForm : Form
{
    public WebView2 View { get; }
    public nint SurfaceHandle { get; }

    public WallpaperForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.Black;
        Text = "YouTube Wallpaper — superficie";
        View = new WebView2
        {
            Dock = DockStyle.Fill, DefaultBackgroundColor = Color.Black,
            AllowExternalDrop = false, TabStop = false
        };
        Controls.Add(View);
        SurfaceHandle = Handle;
    }

    protected override bool ShowWithoutActivation => true;
    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams parameters = base.CreateParams;
            parameters.ExStyle |= 0x08000000 | 0x80; // NOACTIVATE | TOOLWINDOW
            return parameters;
        }
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == 0x84) { message.Result = (nint)(-1); return; } // HTTRANSPARENT
        if (message.Msg == 0x21) { message.Result = (nint)3; return; } // MA_NOACTIVATE
        base.WndProc(ref message);
    }
}
