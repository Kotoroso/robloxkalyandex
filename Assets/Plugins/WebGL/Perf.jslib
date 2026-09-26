// Динамическое разрешение рендера для губернатора FPS (Scripts/Core/Perf.cs).
// Unity WebGL каждый кадр подгоняет размер канваса как CSS-размер * Module.devicePixelRatio
// (стартовое значение — config.devicePixelRatio в WebGL-шаблоне), поэтому его можно менять на лету.
mergeInto(LibraryManager.library, {
  PerfGetDpr: function () {
    try {
      if (typeof Module !== "undefined" && Module.devicePixelRatio) return Module.devicePixelRatio;
    } catch (e) { }
    return window.devicePixelRatio || 1;
  },
  PerfGetNativeDpr: function () {
    try { return window.devicePixelRatio || 1; } catch (e) { return 1; }
  },
  PerfSetDpr: function (v) {
    try {
      if (typeof Module !== "undefined" && v > 0) Module.devicePixelRatio = v;
    } catch (e) { }
  }
});
