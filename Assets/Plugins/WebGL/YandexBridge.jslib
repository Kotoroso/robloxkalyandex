// Мост Unity <-> Yandex Games SDK v2. SDK инициализируется в WebGLTemplates/Yandex/index.html
// и кладётся в window.ysdk / window.yplayer. Ответы идут в Unity через SendMessage("YandexSDK", ...).
mergeInto(LibraryManager.library, {

  // ===== Стики-баннеры =====
  // Работают, только если в консоли разработчика включены Sticky-баннеры и опция "Использовать API для показа sticky-баннера".
  // Unity лишь задаёт желаемое состояние; вызовы идемпотентны, реальный show/hide с проверкой getBannerAdvStatus()
  // и повторами делает window.ygApplyBanner (index.html) — и только после LoadingAPI.ready().
  YG_ShowBanner: function () {
    window.ygBannerWanted = true;
    if (window.ygApplyBanner) window.ygApplyBanner();
  },

  YG_HideBanner: function () {
    window.ygBannerWanted = false;
    if (window.ygApplyBanner) window.ygApplyBanner();
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
    if (!window.ypayments) { window.unityInstance && window.unityInstance.SendMessage("YandexSDK", "OnPurchaseFailed", id); return; }
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

  // 1 — SDK готов ИЛИ точно недоступен (нет sdk.js / init упал), чтобы Unity не ждала зря
  YG_IsReady: function () {
    return (window.ysdk || window.ysdkFailed) ? 1 : 0;
  },

  // LoadingAPI.ready(). Если SDK ещё инициализируется — index.html вызовет ready() сразу после YaGames.init()
  YG_GameReady: function () {
    window.ygReadyPending = true;
    if (window.ygFlushPending) window.ygFlushPending();
  },

  // GameplayAPI.start()/stop(): запоминаем желаемое состояние; если SDK ещё нет — применится после init
  YG_GameplayStart: function () {
    window.ygGameplayPending = "start";
    if (window.ygFlushPending) window.ygFlushPending();
  },

  YG_GameplayStop: function () {
    window.ygGameplayPending = "stop";
    if (window.ygFlushPending) window.ygFlushPending();
  },

  YG_GetLang: function () {
    // язык интерфейса портала (требование 2.14: язык определяется автоматически); "" — SDK нет, Unity возьмёт язык системы
    var lang = "";
    try { if (window.ysdk) lang = window.ysdk.environment.i18n.lang || ""; } catch (e) { }
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
        onError: function () { window.unityInstance && window.unityInstance.SendMessage("YandexSDK", "OnAdClose", ""); },
        onOffline: function () { window.unityInstance && window.unityInstance.SendMessage("YandexSDK", "OnAdClose", ""); }
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
    // пишем в облако только после успешного чтения облака — иначе можно затереть прогресс игрока
    if (window.yplayer && window.ygCloudRead) {
      window.yplayer.setData({ save: json }, false).catch(function (e) { console.warn("setData", e); });
    }
  },

  YG_LoadData: function () {
    var send = function (m, s) {
      // Unity-инстанс может быть ещё не присвоен — ждём его
      if (window.unityInstance) window.unityInstance.SendMessage("YandexSDK", m, s || "");
      else setTimeout(function () { send(m, s); }, 100);
    };
    var tryLoad = function (attempt) {
      if (window.yplayer) {
        window.yplayer.getData(["save"]).then(function (d) {
          window.ygCloudRead = true;
          send("OnCloudData", d && d.save ? d.save : "");
        }).catch(function (e) { console.warn("getData", e); send("OnCloudFailed", ""); });
      } else if (attempt < 100 && !window.yplayerFailed) {
        setTimeout(function () { tryLoad(attempt + 1); }, 100);
      } else {
        // игрок недоступен — только локальное сохранение, облако не трогаем
        send("OnCloudFailed", "");
      }
    };
    tryLoad(0);
  }
});
