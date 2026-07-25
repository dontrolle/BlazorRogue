window.blazorViewport = (() => {
  let resizeHandler = null;

  function getAvailableMapArea(mapId, leftMenuId, debugId) {
    const mapEl = document.getElementById(mapId);
    const leftEl = document.getElementById(leftMenuId);
    const debugEl = document.getElementById(debugId);

    const viewportWidth = window.innerWidth;
    const viewportHeight = window.innerHeight;

    const leftWidth = leftEl ? Math.ceil(leftEl.getBoundingClientRect().width) : 0;
    const debugWidth = debugEl ? Math.ceil(debugEl.getBoundingClientRect().width) : 0;

    let mapExtraX = 0, mapExtraY = 0;
    if (mapEl) {
      const cs = getComputedStyle(mapEl);
      mapExtraX =
        parseFloat(cs.marginLeft || 0) + parseFloat(cs.marginRight || 0) +
        parseFloat(cs.borderLeftWidth || 0) + parseFloat(cs.borderRightWidth || 0) +
        parseFloat(cs.paddingLeft || 0) + parseFloat(cs.paddingRight || 0);

      mapExtraY =
        parseFloat(cs.marginTop || 0) + parseFloat(cs.marginBottom || 0) +
        parseFloat(cs.borderTopWidth || 0) + parseFloat(cs.borderBottomWidth || 0) +
        parseFloat(cs.paddingTop || 0) + parseFloat(cs.paddingBottom || 0);
    }

    const safetyX = 8, safetyY = 8;

    return {
      availableWidth: Math.max(0, viewportWidth - Math.max(leftWidth,debugWidth) - mapExtraX - safetyX),
      availableHeight: Math.max(0, viewportHeight - mapExtraY - safetyY)
    };
  }

  function registerResize(dotNetRef) {
    if (resizeHandler) {
      window.removeEventListener("resize", resizeHandler);
    }

    let timeoutId = null;
    resizeHandler = () => {
      clearTimeout(timeoutId);
      timeoutId = setTimeout(() => {
        dotNetRef.invokeMethodAsync("OnBrowserResized");
      }, 120); // debounce
    };

    window.addEventListener("resize", resizeHandler);
  }

  function unregisterResize() {
    if (resizeHandler) {
      window.removeEventListener("resize", resizeHandler);
      resizeHandler = null;
    }
  }

  return {
    getAvailableMapArea,
    registerResize,
    unregisterResize
  };
})();