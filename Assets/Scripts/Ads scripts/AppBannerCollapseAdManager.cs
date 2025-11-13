using UnityEngine;
using UnityEngine.UI;
using GoogleMobileAds.Api;
using System;
using System.Collections.Generic;
using Firebase.Analytics;
using Firebase.Crashlytics;
using System.Linq.Expressions;
using System.Collections;

public class AppBannerCollapseAdManager : MonoSingleton<AppBannerCollapseAdManager>
{
#if UNITY_ANDROID
    private const string AD_BANNER_ID = "ca-app-pub-3940256099942544/6300978111"; // test
    //private const string AD_BANNER_ID = "ca-app-pub-4845920793447822/7496706129"; // id real
#elif UNITY_IOS
    private const string AD_BANNER_ID = "ca-app-pub-9674055550946724/9362464015";
#else
    private const string AD_BANNER_ID = "unexpected_platform";
#endif
    private static AppBannerCollapseAdManager instance;

    private BannerView bannerView;
    private AdSize adSize;

    void Start()
    {
        // Nếu đã Initialize ở script khác thì có thể comment khối này.
        // MobileAds.Initialize((initStatus) =>
        // {
        //     //LoadAd();
        // });

    }

    public void CreateBannerView(/*int idx*/)
    {
        if (bannerView != null)
        {
            DestroyBannerView();
        }

        AdSize adSize = AdSize.GetPortraitAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);

#if UNITY_ANDROID
        // Lấy ID từ RemoteConfig (nếu bạn đang dùng script RemoteConfig riêng)
        bannerView = new BannerView(AD_BANNER_ID, adSize, AdPosition.Bottom);
#elif UNITY_IOS
        bannerView = new BannerView(AD_BANNER_ID, adSize, AdPosition.Bottom);
#else
        bannerView = new BannerView("unexpected_platform", adSize, AdPosition.Bottom);
#endif
    }

    public void LoadAndShowBanner(/*int idx*/)
    {
        try
        {
            // Nếu game bạn chưa có PlayerData/noAds thì comment đoạn này
            // if (PlayerData.current.noAds == true)
            // {
            //     return;
            // }

            Debug.LogError("Load and show Banner Collapse ");
            CreateBannerView();
            ListenToAdEvents();

            AdRequest adRequest = AdRequestBuild();

            // Ở game mẫu cũ họ check GameController.state để quyết định có collapse trong gameplay hay không.
            // Game mới của bạn không có GameController -> bỏ check này, luôn cho collapsible là 'bottom'.
            //
            // if (GameController.instance.currentState == GameController.STATE.PLAYING
            //     || GameController.instance.currentState == GameController.STATE.DRAWING)
            // {
            //     if (PlayerPrefs.GetInt("UnlockLevel") >= RemoteConfig.KBM_level_Show_Collapse)
            //     {
            //         adRequest.Extras.Add("collapsible", "bottom");
            //     }
            // }
            // else
            // {
            //     adRequest.Extras.Add("collapsible", "bottom");
            // }

            adRequest.Extras.Add("collapsible", "bottom");   // đơn giản: luôn là banner collapse

            bannerView.LoadAd(adRequest);
            wait();     // hiện tại vẫn chưa StartCoroutine, nếu muốn delay thực sự thì dùng: StartCoroutine(wait());
        }
        catch (Exception e)
        {
            Crashlytics.LogException(e);
            Crashlytics.Log("Exception occurred in LoadAndShowBanner_Collapse");
        }
    }

    IEnumerator wait()
    {
        yield return new WaitForSeconds(3f);
    }

    public void HideBannerCollapse()
    {
        try
        {
            if (bannerView != null)
            {
                bannerView.Hide();
            }
        }
        catch (Exception e)
        {
            Crashlytics.LogException(e);
            Crashlytics.Log("Exception occurred in HideBannerCollapse");
        }
    }

    public void ShowBannerCollapse()
    {
        try
        {
            // Nếu sau này bạn có biến noAds riêng thì check ở đây
            // if (!PlayerData.current.noAds)
            {
                Debug.LogError("ShowBannerCollapse");

                if (bannerView == null)
                {
                    LoadAndShowBanner();
                }

                bannerView.Show();
            }
        }
        catch (Exception e)
        {
            Crashlytics.LogException(e);
            Crashlytics.Log("Exception occurred in ShowBannerCollapse");
        }
    }

    private void ListenToAdEvents()
    {
        bannerView.OnBannerAdLoaded += () =>
        {
            Debug.Log("Banner view loaded an ad with response : "
                + bannerView.GetResponseInfo());
        };
        // Raised when an ad fails to load into the banner view.
        bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            Debug.LogError("Banner view collapse failed to load an ad with error : "
                + error);
            LoadAndShowBanner();
        };
        // Raised when the ad is estimated to have earned money.
        bannerView.OnAdPaid += (AdValue adValue) =>
        {
            Debug.LogError("Banner view paid {0} {1}." +
                adValue.Value +
                adValue.CurrencyCode);

            if (adValue == null) return;
            double value = adValue.Value * 0.000001f;

            Firebase.Analytics.Parameter[] adParameters = {
                new Firebase.Analytics.Parameter("ad_source", "admob"),
                new Firebase.Analytics.Parameter("ad_format", "collapsible_banner"),
                new Firebase.Analytics.Parameter("currency","USD"),
                new Firebase.Analytics.Parameter("value", value)
            };
            FirebaseAnalytics.LogEvent("ad_impression", adParameters);
        };
        // Raised when an impression is recorded for an ad.
        bannerView.OnAdImpressionRecorded += () =>
        {
            Debug.LogError("Banner view recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        bannerView.OnAdClicked += () =>
        {
            Debug.LogError("Banner view was clicked.");
        };
        // Raised when an ad opened full screen content.
        bannerView.OnAdFullScreenContentOpened += () =>
        {
            Debug.LogError("Banner view full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        bannerView.OnAdFullScreenContentClosed += () =>
        {
            Debug.LogError("Banner view full screen content closed.");
        };
    }

    // ==== ĐÃ ĐỔI CHO PHÙ HỢP v9.5 (KHÔNG DÙNG Builder) ====
    AdRequest AdRequestBuild()
    {
        var request = new AdRequest();

        // Nếu muốn thêm keyword / extras chung thì add ở đây
        // request.Keywords.Add("puzzle");
        // request.Extras.Add("sample", "1");

        return request;
    }

    public void DestroyBannerView()
    {
        if (bannerView != null)
        {
            Debug.Log("Destroying banner view.");
            bannerView.Destroy();
            bannerView = null;
        }
    }

    #region Banner Methods Check isCollapse
    private bool IsCollapsibleBanner(AdRequest adRequest)
    {
        // Kiểm tra xem adRequest có chứa thuộc tính "collapsible" không
        if (adRequest.Extras.ContainsKey("collapsible"))
        {
            return true; // Banner có tính năng collapsible
        }

        return false; // Banner không có tính năng collapsible hoặc không chứa thông tin về tính năng collapsible
    }
    #endregion

}
