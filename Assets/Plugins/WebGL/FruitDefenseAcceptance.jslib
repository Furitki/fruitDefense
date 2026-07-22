mergeInto(LibraryManager.library, {
  FruitDefenseAcceptanceReady: function (
    route,
    sessionIdPointer,
    seed,
    levelIdPointer,
    mapIdPointer,
    waveSetIdPointer,
    ruleSetIdPointer,
    themeIdPointer) {
    var routeNames = ['lobby', 'battle', 'settlement'];
    var identity = {
      route: route,
      routeName: routeNames[route] || 'unknown',
      sessionId: UTF8ToString(sessionIdPointer),
      seed: seed,
      levelId: UTF8ToString(levelIdPointer),
      mapId: UTF8ToString(mapIdPointer),
      waveSetId: UTF8ToString(waveSetIdPointer),
      ruleSetId: UTF8ToString(ruleSetIdPointer),
      themeId: UTF8ToString(themeIdPointer)
    };
    window.fruitDefenseAcceptanceRouteReady = true;
    window.fruitDefenseAppRoute = route;
    window.fruitDefenseAcceptanceIdentity = identity;
    window.fruitDefenseAcceptanceIdentityHistory =
      window.fruitDefenseAcceptanceIdentityHistory || [];
    window.fruitDefenseAcceptanceIdentityHistory.push(identity);
    if (window.fruitDefensePendingUnityInstance) {
      window.fruitDefenseUnityInstance = window.fruitDefensePendingUnityInstance;
    }
  }
});
