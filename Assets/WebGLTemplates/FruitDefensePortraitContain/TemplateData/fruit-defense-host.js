(function installFruitDefenseWebGLHost(global) {
  "use strict";

  const HOST_ID = "fruit-defense-portrait-contain-v1";
  const LOGICAL_WIDTH = 402;
  const LOGICAL_HEIGHT = 874;
  const MOBILE_USER_AGENT = /iPhone|iPad|iPod|Android/i;
  let activeState = null;

  function viewportSize() {
    return {
      width: Math.max(1, global.innerWidth),
      height: Math.max(1, global.innerHeight)
    };
  }

  function updateLayout(state) {
    const viewport = viewportSize();
    const isDesktop = !state.isMobile;
    const scale = isDesktop
      ? Math.min(viewport.width / LOGICAL_WIDTH, viewport.height / LOGICAL_HEIGHT)
      : 1;
    const cssWidth = isDesktop ? LOGICAL_WIDTH * scale : viewport.width;
    const cssHeight = isDesktop ? LOGICAL_HEIGHT * scale : viewport.height;

    state.container.style.width = `${cssWidth}px`;
    state.container.style.height = `${cssHeight}px`;
    state.container.dataset.hostLayout = isDesktop ? "desktop-contain" : "mobile-fill";
    state.container.dataset.hostScale = String(scale);
    state.container.dataset.logicalWidth = String(LOGICAL_WIDTH);
    state.container.dataset.logicalHeight = String(LOGICAL_HEIGHT);
    state.lastViewportWidth = viewport.width;
    state.lastViewportHeight = viewport.height;
    state.lastDevicePixelRatio = global.devicePixelRatio || 1;
    state.scale = scale;
  }

  function armDevicePixelRatioListener(state) {
    if (!global.matchMedia) return;
    if (state.resolutionQuery) {
      state.resolutionQuery.removeEventListener("change", state.onResolutionChange);
    }
    state.resolutionQuery = global.matchMedia(`(resolution: ${global.devicePixelRatio || 1}dppx)`);
    state.resolutionQuery.addEventListener("change", state.onResolutionChange, { once: true });
  }

  function scheduleLayout(state) {
    if (state.frameRequest) return;
    state.frameRequest = global.requestAnimationFrame(() => {
      state.frameRequest = 0;
      updateLayout(state);
    });
  }

  function snapshot(state) {
    const rect = state.canvas.getBoundingClientRect();
    return {
      hostId: HOST_ID,
      layout: state.container.dataset.hostLayout,
      logicalWidth: LOGICAL_WIDTH,
      logicalHeight: LOGICAL_HEIGHT,
      scale: state.scale,
      viewportWidth: global.innerWidth,
      viewportHeight: global.innerHeight,
      devicePixelRatio: global.devicePixelRatio || 1,
      canvas: {
        left: rect.left,
        top: rect.top,
        right: rect.right,
        bottom: rect.bottom,
        width: rect.width,
        height: rect.height,
        backingWidth: state.canvas.width,
        backingHeight: state.canvas.height
      },
      scroll: {
        x: global.scrollX,
        y: global.scrollY,
        documentWidth: document.documentElement.scrollWidth,
        documentHeight: document.documentElement.scrollHeight
      }
    };
  }

  function mount(options) {
    if (!options || !options.host || !options.container || !options.canvas) {
      throw new Error(`${HOST_ID}: host, container, and canvas are required.`);
    }
    if (options.logicalWidth !== LOGICAL_WIDTH || options.logicalHeight !== LOGICAL_HEIGHT) {
      throw new Error(
        `${HOST_ID}: expected ${LOGICAL_WIDTH}x${LOGICAL_HEIGHT}, got ` +
        `${options.logicalWidth}x${options.logicalHeight}.`);
    }
    if (activeState) throw new Error(`${HOST_ID}: the host is already mounted.`);

    const state = {
      host: options.host,
      container: options.container,
      canvas: options.canvas,
      isMobile: MOBILE_USER_AGENT.test(global.navigator.userAgent),
      frameRequest: 0,
      resolutionQuery: null,
      scale: 1
    };
    state.onWindowChange = () => scheduleLayout(state);
    state.onResolutionChange = () => {
      updateLayout(state);
      armDevicePixelRatioListener(state);
    };
    global.addEventListener("resize", state.onWindowChange, { passive: true });
    global.addEventListener("orientationchange", state.onWindowChange, { passive: true });
    if (global.visualViewport) {
      global.visualViewport.addEventListener("resize", state.onWindowChange, { passive: true });
    }
    updateLayout(state);
    armDevicePixelRatioListener(state);
    activeState = state;

    return Object.freeze({
      hostId: HOST_ID,
      layoutMode: state.isMobile ? "mobile-fill" : "desktop-contain",
      usesFixedRenderTarget: !state.isMobile,
      updateLayout: () => updateLayout(state),
      snapshot: () => snapshot(state)
    });
  }

  global.fruitDefenseWebGLHost = Object.freeze({
    hostId: HOST_ID,
    logicalWidth: LOGICAL_WIDTH,
    logicalHeight: LOGICAL_HEIGHT,
    mount: mount,
    updateLayout: () => {
      if (!activeState) throw new Error(`${HOST_ID}: the host is not mounted.`);
      updateLayout(activeState);
    },
    snapshot: () => activeState ? snapshot(activeState) : null
  });
})(window);
