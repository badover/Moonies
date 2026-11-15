using System;
using System.Reflection;
using UnityEngine;
using VoltstroStudios.UnityWebBrowser.Communication;
using VoltstroStudios.UnityWebBrowser.Core;
using VoltstroStudios.UnityWebBrowser.Core.Engines;
using VoltstroStudios.UnityWebBrowser.Shared;

/// <summary>Streams a YouTube embed into a low-resolution texture at a capped update rate.</summary>
public sealed class YouTubeBackgroundPlayer : MonoBehaviour
{
    private WebBrowserClient browser;
    private float textureUpdateInterval;
    private float textureUpdateElapsed;

    public Texture2D Texture => browser != null ? browser.BrowserTexture : null;
    public bool IsReady { get; private set; }

    public void Initialize(EngineConfiguration engine, string url, int width, int height, int framesPerSecond)
    {
        browser = new WebBrowserClient
        {
            engine = engine,
            communicationLayer = ScriptableObject.CreateInstance<TCPCommunicationLayer>(),
            initialUrl = url,
            windowlessFrameRate = Mathf.Clamp(framesPerSecond, 1, 60),
            backgroundColor = new Color32(0, 0, 0, 255)
        };
        textureUpdateInterval = 1f / Mathf.Clamp(framesPerSecond, 1, 60);

        // UWB 2.2.8 exposes resize after connection; set its serialized startup size before Init creates the texture.
        var resolutionField = typeof(WebBrowserClient).GetField("resolution", BindingFlags.Instance | BindingFlags.NonPublic);
        if (resolutionField == null)
            throw new MissingFieldException(typeof(WebBrowserClient).FullName, "resolution");
        resolutionField.SetValue(browser, new VoltstroStudios.UnityWebBrowser.Shared.Resolution((uint)width, (uint)height));

        browser.OnLoadFinish += OnPageLoaded;
        try
        {
            browser.Init();
        }
        catch (Exception exception)
        {
            Debug.LogError("YouTube background could not start: " + exception.Message);
            DisposeBrowser();
            browser = null;
        }
    }

    private void Update()
    {
        if (browser == null) return;
        browser.UpdateFps();
        if (!browser.IsConnected) return;

        textureUpdateElapsed += Time.unscaledDeltaTime;
        if (textureUpdateElapsed < textureUpdateInterval) return;
        textureUpdateElapsed %= textureUpdateInterval;
        browser.LoadTextureData();
    }

    private void OnPageLoaded(string url)
    {
        if (!string.IsNullOrEmpty(url) && url.Contains("youtube")) IsReady = true;
    }

    private void OnDestroy()
    {
        if (browser == null) return;
        browser.OnLoadFinish -= OnPageLoaded;
        DisposeBrowser();
        browser = null;
    }

    private void DisposeBrowser()
    {
        var communication = browser.communicationLayer;
        browser.Dispose();
        if (communication != null) Destroy(communication);
    }
}
