using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using Newtonsoft.Json.Linq;
using StonehearthEditor.Effects;

namespace StonehearthEditor
{
    public class EffectsChromeBrowser
    {
        private static readonly string myPath = Application.StartupPath;
        private static readonly string myPages = Path.Combine(myPath, "pages");
        private static EffectsChromeBrowser sInstance = null;
        private string mEffectKind;
        private string mJson;

        private ChromiumWebBrowser mChromeBrowser;
        private EffectsJsObject mEffectsJsObject;

        public static EffectsChromeBrowser GetInstance()
        {
            if (sInstance == null)
            {
                sInstance = new EffectsChromeBrowser();
            }

            return sInstance;
        }

        private EffectsChromeBrowser()
        {
        }

        public void InitBrowser(Panel panel)
        {
            CefSettings cSettings = new CefSettings();
            cSettings.RemoteDebuggingPort = 8088;
            Cef.Initialize(cSettings);

            // Open main page
            mChromeBrowser = new ChromiumWebBrowser(GetPagePath("main.html"));
            mEffectsJsObject = new EffectsJsObject();
            mChromeBrowser.RegisterJsObject("EffectsJsObject", mEffectsJsObject);
            mChromeBrowser.LoadError += MChromeBrowser_LoadError;
            panel.Controls.Add(mChromeBrowser);
            mChromeBrowser.Dock = DockStyle.Fill;
        }

        private void MChromeBrowser_LoadError(object sender, LoadErrorEventArgs e)
        {
            MessageBox.Show("file type not supported yet");
        }

        public void LoadFromJson(string effectKind, string json, Action<string> saveAction)
        {
            mEffectKind = effectKind;
            mJson = json;
            this.Refresh();
            mEffectsJsObject.saveAction = saveAction;
        }

        public void RunScript(string script)
        {
            mChromeBrowser.EvaluateScriptAsync(script).Wait();
        }

        public void ShowDevTools()
        {
            mChromeBrowser.ShowDevTools();
        }

        private string GetPagePath(string pageName)
        {
            return Path.Combine(myPages, pageName);
        }

        private void SwitchPage(string pageName)
        {
            mChromeBrowser.Load(GetPagePath(pageName));
        }

        public class RenderProcessMessageHandler : IRenderProcessMessageHandler
        {
            // Wait for the underlying `Javascript Context` to be created, this is only called for the main frame.
            // If the page has no javascript, no context will be created.
            void IRenderProcessMessageHandler.OnContextCreated(IWebBrowser browserControl, IBrowser browser, IFrame frame)
            {
                frame.ExecuteJavaScriptAsync(
                        string.Format(
                            @"
                       document.addEventListener('DOMContentLoaded', function() {{
                            CsApi.effectKind = ""{0}"";
                            CsApi.json = {1};
                        }});",
                            effectKind,
                            json));
            }

            public void OnFocusedNodeChanged(IWebBrowser browserControl, IBrowser browser, IFrame frame, IDomNode node)
            {
            }

            private string effectKind;
            private string json;

            public RenderProcessMessageHandler(string effectKind, string json)
            {
                this.effectKind = effectKind;
                this.json = json;
            }
        }

        public void Refresh()
        {
            string sourceDir = @"..\..\..\pages";
            string destinationDir = @"pages";

            CopyDirectory(sourceDir, destinationDir, true);

            mChromeBrowser.RenderProcessMessageHandler = new RenderProcessMessageHandler(mEffectKind, mJson);
            mChromeBrowser.Reload(true);
        }

        private static void CopyDirectory(string sourceDirectory, string targetDirectory, bool recursive)
        {
            // Get information about the source directory
            var sourceDir = new DirectoryInfo(sourceDirectory);

            // Get information about the target directory
            var targetDir = new DirectoryInfo(targetDirectory);

            // Check if the source directory exists
            if (!sourceDir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {sourceDir.FullName}");

            // delete directory if exixtes
            if (targetDir.Exists)
                Directory.Delete(targetDirectory, true);

            // Cache directories before we start copying
            DirectoryInfo[] dirs = sourceDir.GetDirectories();

            // Create the destination directory
            Directory.CreateDirectory(targetDirectory);

            // Get the files in the source directory and copy to the destination directory
            foreach (FileInfo file in sourceDir.GetFiles())
            {
                string targetFilePath = Path.Combine(targetDirectory, file.Name);
                file.CopyTo(targetFilePath);
            }

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(targetDirectory, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }
    }
}
