
namespace AwesomiumWindowsExternal {
  using System;
  using System.Diagnostics;
  using System.IO;
  using Awesomium.Core;
  using System.Windows.Forms;
  using Awesomium.Core.Data;

  public partial class FrmBrowser : Form {

    private WebSession _session;
    private string _cacheDir;
    private bool _needsResize;
    private WebView _webView;

    public FrmBrowser() {
      if (!WebCore.IsRunning)
        WebCore.Initialize(WebConfig.Default);

      ConfigureCache();
      CreateSession();

      InitializeComponent();
    }

    private void CreateSession() {
      _session = WebCore.CreateWebSession(
          _cacheDir,
          new WebPreferences {
            SmoothScrolling = true,
            WebGL = true,
            EnableGPUAcceleration = true,
          });

      _session.AddDataSource("demo",
          new DirectoryDataSource(
              Path.GetDirectoryName(@"html\greeter.html")));
    }

    private void ConfigureCache() {
      _cacheDir = String.Format(
          "{0}{1}Cache{2}",
          Path.GetDirectoryName(Application.ExecutablePath),
          Path.DirectorySeparatorChar,
          Guid.NewGuid() /*to allow multiple instances of this*/);
    }

    protected override void OnResize(EventArgs e) {
      base.OnResize(e);

      if ((_webView == null) || !_webView.IsLive)
        return;

      if (ClientSize.Width > 0 && ClientSize.Height > 0)
        _needsResize = true;

      // Request resize, if needed.
      ResizeView();
    }

    protected override void OnHandleCreated(EventArgs e) {
      base.OnHandleCreated(e);

      if (_webView == null) {
        _webView = WebCore.CreateWebView(ClientSize.Width, ClientSize.Height, _session, WebViewType.Window);
      }

      InitializeView();
    }

    private void InitializeView() {

      _webView.ParentWindow = Handle;
      _webView.DocumentReady += OnDocumentReady;
      _webView.Crashed += OnCrashed;

      try {

        //This works too, but will fails for large html files.
        //_webView.LoadHTML(Path.GetFullPath(@"html\greeter.html"));

        _webView.Source = new Uri(
          string.Format("asset://demo/{0}", 
          Path.GetFullPath(@"html\greeter.html")));
        //
      }

      catch (Exception ex) {
        MessageBox.Show(ex.Message);
      }

      _webView.FocusView();
    }

    private void OnDocumentReady(
      object sender, UrlEventArgs e) {

      if ((_webView == null) || !_webView.IsLive)
        return;

      _webView.DocumentReady -= OnDocumentReady;
      //workaround
      //http://forums.awesomium.com/viewtopic.php?f=4&t=2482
      var timer = new Timer { Interval = 1000, 
        Enabled = true };

      timer.Tick += (send, args) => {
        if (_webView.IsLoading) 
          return;

        using (JSObject external = _webView
          .CreateGlobalJavascriptObject("_external")) {

          external.Bind("html_click", false, (s, jsArgs) =>
            BeginInvoke((Action<JavascriptMethodEventArgs>)
            html_click, jsArgs));
        }
        timer.Enabled = false;
      };
    }

    private void html_click(JavascriptMethodEventArgs jsCall) {
      
      if ((_webView == null) || !_webView.IsLive)
        return;

      MessageBox.Show(string.Format(
        "Hi {0} from CShrap!", jsCall.Arguments[0]));
    }

    private void OnCrashed(object sender, CrashedEventArgs e) {
      Debug.Print(e.Status.ToString());

      MessageBox.Show(this,
          "The WebView crashed! Status: " + e.Status,
          "Error",
          MessageBoxButtons.OK,
          MessageBoxIcon.Error);
    }

    private void ResizeView() {
      if (!IsHandleCreated)
        return;

      if ((_webView == null) || !_webView.IsLive)
        return;

      if (!_needsResize)
        return;

      //-10 to see scrollbars            
      _webView.Resize(ClientSize.Width - 10, ClientSize.Height - 25);
      _needsResize = false;
    }

  }
}
