using System;
using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;

public class AppOpenAdManager : MonoBehaviour
{
    public static AppOpenAdManager Instance { get; private set; }

#if UNITY_ANDROID
    private const string AD_UNIT_ID = "ca-app-pub-3940256099942544/9257395921";
#elif UNITY_IOS
    private const string AD_UNIT_ID = "your-ios-id";
#else
    private const string AD_UNIT_ID = "unused";
#endif

    private AppOpenAd appOpenAd;
    private DateTime loadTimeUtc;

    private bool isShowing;
    private bool isLoading;
    private bool isLoaded;
    private bool firstShown;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        AppStateEventNotifier.AppStateChanged += OnAppStateChanged;
    }

    private bool IsAdFresh =>
        isLoaded && appOpenAd != null &&
        (DateTime.UtcNow - loadTimeUtc).TotalHours < 4;

    // ---------------------- LOAD SAFE ----------------------
    public void LoadSafe()
    {
        if (!AdManager.IsInitialized) return;
        if (isLoading) return;
        if (appOpenAd != null) appOpenAd.Destroy();

        isLoading = true;
        isLoaded = false;

        Debug.Log("[AOA] Loading...");

        var request = new AdRequest();
        AppOpenAd.Load(AD_UNIT_ID, request, (ad, error) =>
        {
            isLoading = false;

            if (error != null || ad == null)
            {
                Debug.LogWarning($"[AOA] Load failed: {error}");
                return;
            }

            Debug.Log("[AOA] Loaded OK");

            appOpenAd = ad;
            loadTimeUtc = DateTime.UtcNow;
            isLoaded = true;

            RegisterCallbacks(ad);

            // CHỈ show lần đầu với delay lớn hơn (tránh crash)
            if (!firstShown)
                Invoke(nameof(ShowFirstTimeSafe), 1.0f);
        });
    }

    private void ShowFirstTimeSafe()
    {
        if (!firstShown)
        {
            firstShown = true;
            Show("first_open");
        }
    }

    // ---------------------- SHOW SAFE ----------------------
    public void Show(string reason)
    {
        if (!AdManager.CanShowAds()) return;
        if (!IsAdFresh) { LoadAfterDelay(); return; }
        if (isShowing) return;

        if (!appOpenAd.CanShowAd())
        {
            LoadAfterDelay();
            return;
        }

        try
        {
            Debug.Log($"[AOA] Showing ({reason})");
            isShowing = true;
            appOpenAd.Show();
        }
        catch (Exception e)
        {
            Debug.LogError($"[AOA] Show ERROR: {e.Message}");
            isShowing = false;
            LoadAfterDelay();
        }
    }

    // ---------------------- EVENT HANDLERS ----------------------
    private void RegisterCallbacks(AppOpenAd ad)
    {
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("[AOA] OPENED");
            isShowing = true;
        };

        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("[AOA] CLOSED");
            isShowing = false;
            isLoaded = false;

            // IMPORTANT: delay reload để tránh crash activity
            LoadAfterDelay();
        };

        ad.OnAdFullScreenContentFailed += error =>
        {
            Debug.LogWarning($"[AOA] FULLSCREEN FAIL: {error}");
            isShowing = false;
            isLoaded = false;
            LoadAfterDelay();
        };
    }

    private void OnAppStateChanged(AppState state)
    {
        if (state == AppState.Foreground && firstShown)
            Invoke(nameof(ShowForegroundSafe), 0.6f); // delay lớn → tránh crash
    }

    private void ShowForegroundSafe()
    {
        Show("foreground");
    }

    private void LoadAfterDelay()
    {
        Invoke(nameof(LoadSafe), 1.0f);
    }

    private void OnDestroy()
    {
        AppStateEventNotifier.AppStateChanged -= OnAppStateChanged;
        appOpenAd?.Destroy();
    }
}
