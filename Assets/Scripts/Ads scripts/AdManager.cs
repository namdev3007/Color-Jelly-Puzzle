using UnityEngine;
using GoogleMobileAds.Api;
using System;
using System.Collections;

/// <summary>
/// Manager chính để khởi tạo MobileAds và quản lý trạng thái ads
/// Đặt script này vào scene đầu tiên và để nó DontDestroyOnLoad
/// </summary>
public class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }
    
    public static bool IsInitialized { get; private set; }
    public static bool NoAdsEnabled { get; set; } // Set từ IAP khi user mua remove ads
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Đợi RemoteConfig trước khi init ads
        StartCoroutine(InitializeAdsWithRemoteConfig());
    }
    
    private IEnumerator InitializeAdsWithRemoteConfig()
    {
        LogDebug("Waiting for RemoteConfig...");
        
        // Đợi RemoteConfig với timeout 5 giây
        float timeout = 5f;
        float elapsed = 0f;
        
        while (!RemoteConfig.IsReady && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
        if (!RemoteConfig.IsReady)
        {
            LogDebug("RemoteConfig timeout - using default values");
        }
        else
        {
            LogDebug("RemoteConfig ready!");
        }
        
        // Khởi tạo MobileAds MỘT LẦN duy nhất
        InitializeMobileAds();
    }
    
    private void InitializeMobileAds()
    {
        if (IsInitialized)
        {
            LogDebug("MobileAds already initialized");
            return;
        }
        
        try
        {
            LogDebug("Initializing MobileAds...");
            
            // Cấu hình test devices nếu cần
            var requestConfiguration = new RequestConfiguration
            {
                TestDeviceIds = new System.Collections.Generic.List<string>
                {
                    AdRequest.TestDeviceSimulator,
                    // Thêm device ID của bạn ở đây nếu test
                }
            };
            MobileAds.SetRequestConfiguration(requestConfiguration);
            
            // Khởi tạo
            MobileAds.Initialize(initStatus =>
            {
                IsInitialized = true;
                LogDebug("MobileAds initialized successfully!");
                
                // Sau khi init xong, load các ads cần thiết
                LoadInitialAds();
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"[AdManager] Failed to initialize: {e.Message}");
        }
    }
    
    private void LoadInitialAds()
    {
        // Kiểm tra xem ads có được bật không
        if (RemoteConfig.ads_display == 0)
        {
            LogDebug("Ads disabled via RemoteConfig");
            return;
        }
        
        if (NoAdsEnabled)
        {
            LogDebug("User purchased No Ads");
            return;
        }
        
        // Load App Open Ad nếu được bật
        if (RemoteConfig.OpenAdsEnabled && AppOpenAdManager.Instance != null)
        {
            LogDebug("Loading App Open Ad...");
            AppOpenAdManager.Instance.LoadSafe();
        }

        
        // Load Interstitial Ad
        if (AppInterstitialAdManager_Admob_For_Play.Instance != null)
        {
            LogDebug("Loading Interstitial Ad...");
            AppInterstitialAdManager_Admob_For_Play.Instance.LoadAd();
        }
        
        // Banner sẽ được load khi cần show
    }
    
    /// <summary>
    /// Kiểm tra xem có thể show ads không (dựa vào RemoteConfig và IAP)
    /// </summary>
    public static bool CanShowAds()
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("[AdManager] MobileAds not initialized yet");
            return false;
        }
        
        if (NoAdsEnabled)
        {
            return false;
        }
        
        if (RemoteConfig.ads_display == 0)
        {
            return false;
        }
        
        return true;
    }
    
    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[AdManager] {message}");
        }
    }
    
    // void OnApplicationQuit()
    // {
    //     // Cleanup khi thoát app
    //     if (AppOpenAdManager.Instance != null)
    //     {
    //         Destroy(AppOpenAdManager.Instance.gameObject);
    //     }
    //     if (AppInterstitialAdManager_Admob_For_Play.Instance != null)
    //     {
    //         Destroy(AppInterstitialAdManager_Admob_For_Play.Instance.gameObject);
    //     }
    // }
}