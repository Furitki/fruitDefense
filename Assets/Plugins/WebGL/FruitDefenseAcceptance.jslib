mergeInto(LibraryManager.library, {
  FruitDefenseAcceptanceReady: function (route) {
    window.fruitDefenseAcceptanceRouteReady = true;
    window.fruitDefenseAppRoute = route;
    if (window.fruitDefensePendingUnityInstance) {
      window.fruitDefenseUnityInstance = window.fruitDefensePendingUnityInstance;
    }
  }
});
