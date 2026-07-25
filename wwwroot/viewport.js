window.blazorViewport = {
  getAvailableMapArea: (mapId, leftMenuId, debugId) => {
    const mapEl = document.getElementById(mapId);
    const leftEl = document.getElementById(leftMenuId);
    const debugEl = document.getElementById(debugId);

    const viewportWidth = window.innerWidth;
    const viewportHeight = window.innerHeight;

    const leftWidth = leftEl ? Math.ceil(leftEl.getBoundingClientRect().width) : 0;
    const debugHeight = debugEl ? Math.ceil(debugEl.getBoundingClientRect().height) : 0;

    // include horizontal margins/gaps from map container itself
    let mapExtraX = 0;
    let mapExtraY = 0;
    if (mapEl) {
      const cs = getComputedStyle(mapEl);
      mapExtraX =
        parseFloat(cs.marginLeft || 0) +
        parseFloat(cs.marginRight || 0) +
        parseFloat(cs.borderLeftWidth || 0) +
        parseFloat(cs.borderRightWidth || 0) +
        parseFloat(cs.paddingLeft || 0) +
        parseFloat(cs.paddingRight || 0);

      mapExtraY =
        parseFloat(cs.marginTop || 0) +
        parseFloat(cs.marginBottom || 0) +
        parseFloat(cs.borderTopWidth || 0) +
        parseFloat(cs.borderBottomWidth || 0) +
        parseFloat(cs.paddingTop || 0) +
        parseFloat(cs.paddingBottom || 0);
    }

    // safety fudge for gaps/scrollbar rounding
    const safetyX = 8;
    const safetyY = 8;

    const availableWidth = Math.max(0, viewportWidth - leftWidth - mapExtraX - safetyX); // 
    const availableHeight = Math.max(0, viewportHeight - debugHeight - mapExtraY - safetyY); // 

    return {
      availableWidth,
      availableHeight
    };
  }
};