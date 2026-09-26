// Мост Unity <-> Yandex Games SDK v2. SDK инициализируется в WebGLTemplates/Yandex/index.html
// и кладётся в window.ysdk / window.yplayer. Ответы идут в Unity через SendMessage("YandexSDK", ...).
mergeInto(LibraryManager.library, {

  // ===== Стики-баннеры (включаются в консоли разработчика: "Sticky-баннеры" + показ через SDK) =====
  YG_ShowBanner: function () {
    try { if (window.ysdk && window.ysdk.adv && window.ysdk.adv.showBannerAdv) window.ysdk.adv.showBannerAdv(); } catch (e) { }
  },

  YG_HideBanner: function () {
    try { if (window.ysdk && window.ysdk.adv && window.ysdk.adv.hideBannerAdv) window.ysdk.adv.hideBannerAdv(); } catch (e) { }
  },

  // ===== Покупки за Яны (ysdk.getPayments) =====
  YG_InitPayments: function () {
    var send = function (m, s) {
      if (window.unityInstance) window.unityInstance.SendMessage("YandexSDK", m, s);
      else setTimeout(function () { send(m, s); }, 200);
    };
    var tryInit = function (attempt) {
      if (!window.ysdk) { if (attempt < 50) setTimeout(function () { tryInit(attempt + 1); }, 200); return; }
      window.ysdk.getPayments({ signed: false }).then(function (p) {
        window.ypayments = p;
        p.getCatalog().then(function (list) {
          var items = list.map(function (x) { return { id: x.id, price: x.price, priceValue: x.priceValue, currency: x.priceCurrencyCode }; });
          send("OnCatalog", JSON.stringify({ items: items }));
        }).catch(function () { });
        p.getPurchases().then(function (ps) {
          var items = ps.map(function (x) { return { id: x.productID, token: x.purchaseToken }; });
          send("OnPurchases", JSON.stringify({ items: items }));
        }).catch(function () { });
      }).catch(function (e) { console.warn("payments unavailable", e); });
    };
    tryInit(0);
  },

  YG_Purchase: function (idPtr) {
    var id = UTF8ToString(idPtr);
    if (!window.ypayments) { window.unityInstance.SendMessage("YandexSDK", "OnPurchaseFailed", id); return; }
    window.ypayments.purchase({ id: id }).then(function (pur) {
      window.unityInstance.SendMessage("YandexSDK", "OnPurchaseSuccess", JSON.stringify({ id: pur.productID, token: pur.purchaseToken }));
    }).catch(function () {
      window.unityInstance.SendMessage("YandexSDK", "OnPurchaseFailed", id);
    });
  },

  YG_Consume: function (tokenPtr) {
    var token = UTF8ToString(tokenPtr);
    if (window.ypayments) window.ypayments.consumePurchase(token).catch(function (e) { console.warn("consume", e); });
  },

  YG_IsReady: function () {
    return window.ysdk ? 1 : 0;
  },

  YG_GameReady: function () {
    try {
      if (window.ysdk && window.ysdk.features && window.ysdk.features.LoadingAPI)
        window.ysdk.features.LoadingAPI.ready();
    } catch (e) { console.warn(e); }
  },

  YG_GameplayStart: function () {
    try {
      if (window.ysdk && window.ysdk.features && window.ysdk.features.GameplayAPI)
        window.ysdk.features.GameplayAPI.start();
    } catch (e) { }
  },

  YG_GameplayStop: function () {
    try {
      if (window.ysdk && window.ysdk.features && window.ysdk.features.GameplayAPI)
        window.ysdk.features.GameplayAPI.stop();
    } catch (e) { }
  },

  YG_GetLang: function () {
    var lang = "ru";
    try { if (window.ysdk) lang = window.ysdk.environment.i18n.lang; } catch (e) { }
    var size = lengthBytesUTF8(lang) + 1;
    var buf = _malloc(size);
    stringToUTF8(lang, buf, size);
    return buf;
  },

  YG_IsMobile: function () {
    try {
      if (window.ysdk && window.ysdk.deviceInfo)
        return (window.ysdk.deviceInfo.isMobile() || window.ysdk.deviceInfo.isTablet()) ? 1 : 0;
    } catch (e) { }
    return /Android|iPhone|iPad|iPod|Mobile/i.test(navigator.userAgent) ? 1 : 0;
  },

  YG_ShowFullscreen: function () {
    if (!window.ysdk) return;
    window.ysdk.adv.showFullscreenAdv({
      callbacks: {
        onOpen: function () { window.unityInstance && window.unityInstance.SendMessage("YandexSDK", "OnAdOpen", ""); },
        onClose: function () { window.unityInstance && window.unityInstance.SendMessage("YandexSDK", "OnAdClose", ""); },
        onError: function () { window.unityInstance && window.unityInstance.SendMessage("YandexSDK", "OnAdClose", ""); }
      }
    });
  },

  YG_ShowRewarded: function (id) {
    if (!window.ysdk) {
      window.unityInstance && window.unityInstance.SendMessage("YandexSDK", "OnRewardedError", "");
      return;
    }
    window.ysdk.adv.showRewardedVideo({
      callbacks: {
        onOpen: function () { window.unityInstance.SendMessage("YandexSDK", "OnAdOpen", ""); },
        onRewarded: function () { window.unityInstance.SendMessage("YandexSDK", "OnRewarded", ""); },
        onClose: function () { window.unityInstance.SendMessage("YandexSDK", "OnRewardedClose", ""); },
        onError: function (e) { window.unityInstance.SendMessage("YandexSDK", "OnRewardedError", ""); }
      }
    });
  },

  YG_SaveData: function (jsonPtr) {
    var json = UTF8ToString(jsonPtr);
    try { localStorage.setItem("dragon_heist_backup", json); } catch (e) { }
    if (window.yplayer) {
      window.yplayer.setData({ save: json }, false).catch(function (e) { console.warn("setData", e); });
    }
  },

  YG_LoadData: function () {
    var send = function (s) {
      // Unity-инстанс может быть ещё не присвоен — ждём его
      if (window.unityInstance) window.unityInstance.SendMessage("YandexSDK", "OnCloudData", s || "");
      else setTimeout(function () { send(s); }, 100);
    };
    var tryLoad = function (attempt) {
      if (window.yplayer) {
        window.yplayer.getData(["save"]).then(function (d) { send(d && d.save ? d.save : ""); })
          .catch(function () { send(""); });
      } else if (attempt < 30 && !window.yplayerFailed) {
        setTimeout(function () { tryLoad(attempt + 1); }, 100);
      } else {
        send("");
      }
    };
    tryLoad(0);
  }
});
