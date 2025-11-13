using UnityEngine;
using UnityEngine.UI;
using GoogleMobileAds.Api;
using System;
using System.Collections.Generic;
//using Popup;
using Firebase.Analytics;
using Firebase.Crashlytics;
using System.Collections;

public class AppBannerRectangleAdManager : MonoSingleton<AppBannerRectangleAdManager>
{
#if UNITY_ANDROID
    private const string AD_BANNER_ID = "ca-app-pub-3940256099942544/6300978111"; // test
    //private const string AD_BANNER_ID = "ca-app-pub-4845920793447822/5221170220"; // id real
#elif UNITY_IOS
    private const string AD_BANNER_ID = "ca-app-pub-9674055550946724/6674868723";
#else
    private const string AD_BANNER_ID = "unexpected_platform";
#endif

    private static AppBannerRectangleAdManager instance;

    private BannerView bannerView;
    private AdSize adSize;

    void Start()
    {
        // Nếu bạn đã Initialize ở chỗ khác rồi thì có thể comment khối này lại
        // MobileAds.Initialize((initStatus) =>
        // {
        //     LoadAndShowBanner();
        // });
    }

    public void CreateBannerView(/*int idx*/)
    {
        if (bannerView != null)
        {
            DestroyBannerView();
        }

        AdSize adSize = AdSize.MediumRectangle;
        bannerView = new BannerView(AD_BANNER_ID, adSize, AdPosition.Bottom);
    }

    public void LoadAndShowBanner(/*int idx*/)
    {
        try
        {
            // Nếu game bạn không có hệ thống mua noAds thì bỏ đoạn check này luôn
            // if (PlayerData.current.noAds == true)
            // {
            //     return;
            // }

            Debug.LogError("Load and show Banner Rectangle ");
            CreateBannerView(/*idx*/);
            ListenToAdEvents();

            AdRequest adRequest = AdRequestBuild();

            // Rectangle banner bình thường KHÔNG cần collapsible
            // Nếu bạn muốn nó cũng collapsible thì có thể giữ lại:
            // adRequest.Extras.Add("collapsible", "bottom");

            bannerView.LoadAd(adRequest);
            bannerView.Hide();   // load xong nhưng ẩn đi, chỉ show ở chỗ bạn gọi ShowBannerRectangle
            wait();              // NOTE: hàm này hiện tại KHÔNG chờ gì cả (không dùng StartCoroutine)
        }
        catch (Exception e)
        {
            Crashlytics.LogException(e);
            Crashlytics.Log("Exception occurred in LoadAndShowBanner_Rectangle");
        }

    }

    IEnumerator wait()
    {
        yield return new WaitForSeconds(3f);
    }

    public void HideBannerRectangle()
    {
        try
        {
            if (bannerView != null)
            {
                bannerView.Hide();
            }

            // Khi ẩn Rectangle thì show Collapse (nếu bạn muốn vậy)
            if (AppBannerCollapseAdManager.Instance != null)
            {
                AppBannerCollapseAdManager.Instance.ShowBannerCollapse();
            }
        }
        catch (Exception e)
        {
            Crashlytics.LogException(e);
            Crashlytics.Log("Exception occurred in HideBannerRectangle");
        }

    }

    public void ShowBannerRectangle()
    {
        try
        {
            Debug.LogError("ShowBannerRectangle");

            // Nếu sau này bạn có biến noAds riêng thì check ở đây
            // if (!PlayerData.current.noAds)
            {
                if (bannerView == null)
                {
                    LoadAndShowBanner();
                }

                bannerView.Show();

                if (AppBannerCollapseAdManager.Instance != null)
                {
                    AppBannerCollapseAdManager.Instance.HideBannerCollapse(); // show Rectangle thì ẩn Collapse
                }
            }
        }
        catch (Exception e)
        {
            Crashlytics.LogException(e);
            Crashlytics.Log("Exception occurred in ShowBannerRectangle");
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
            Debug.LogError("Banner view rectangle failed to load an ad with error : "
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
                new Firebase.Analytics.Parameter("ad_format", "rectangle_banner"),
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
        // Với plugin 9.5 có thể tạo request trực tiếp như này
        var request = new AdRequest();
        // Nếu cần thêm keyword / extras thì add vào đây, ví dụ:
        // request.Keywords.Add("game");
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
