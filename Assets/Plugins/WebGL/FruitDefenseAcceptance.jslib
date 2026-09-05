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
    if (route !== 2) {
      window.fruitDefenseSettlementOutcomeRevealState = null;
    }
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
    if (route === 2 && window.fruitDefenseSettlementOutcomeRevealState) {
      identity.settlementOutcomeRevealState =
        window.fruitDefenseSettlementOutcomeRevealState;
    }
    window.fruitDefenseAcceptanceRouteReady = true;
    window.fruitDefenseAppRoute = route;
    window.fruitDefenseAcceptanceIdentity = identity;
    window.fruitDefenseAcceptanceIdentityHistory =
      window.fruitDefenseAcceptanceIdentityHistory || [];
    window.fruitDefenseAcceptanceIdentityHistory.push(identity);
    if (window.fruitDefensePendingUnityInstance) {
      window.fruitDefenseUnityInstance = window.fruitDefensePendingUnityInstance;
    }
  },

  FruitDefensePublishSettlementOutcomeReveal: function (state) {
    var stateNames = ['hidden', 'settled-hidden', 'appearing', 'stable'];
    if (state < 0 || state >= stateNames.length) {
      throw new Error('Unknown settlement outcome reveal state: ' + state);
    }
    var identity = window.fruitDefenseAcceptanceIdentity;
    if (window.fruitDefenseAppRoute !== 2 || !identity ||
        identity.route !== 2 || identity.routeName !== 'settlement' ||
        typeof identity.sessionId !== 'string' || identity.sessionId.length === 0) {
      return;
    }
    var stateName = stateNames[state];
    window.fruitDefenseSettlementOutcomeRevealState = stateName;
    identity.settlementOutcomeRevealState = stateName;
    window.fruitDefenseSettlementOutcomeRevealHistory =
      window.fruitDefenseSettlementOutcomeRevealHistory || [];
    window.fruitDefenseSettlementOutcomeRevealHistory.push({
      state: stateName,
      stateCode: state,
      route: 2,
      sessionId: identity.sessionId,
      sequence: window.fruitDefenseSettlementOutcomeRevealHistory.length + 1
    });
    if (window.fruitDefenseSettlementOutcomeRevealHistory.length > 32) {
      window.fruitDefenseSettlementOutcomeRevealHistory.splice(
        0, window.fruitDefenseSettlementOutcomeRevealHistory.length - 32);
    }
  },

  FruitDefensePublishCombatFeedbackTelemetry: function (jsonPointer) {
    var json = UTF8ToString(jsonPointer);
    window.fruitDefenseCombatFeedbackTelemetry = JSON.parse(json);
    window.fruitDefenseCombatFeedbackTelemetryHistory =
      window.fruitDefenseCombatFeedbackTelemetryHistory || [];
    window.fruitDefenseCombatFeedbackTelemetryHistory.push(
      window.fruitDefenseCombatFeedbackTelemetry);
    if (window.fruitDefenseCombatFeedbackTelemetryHistory.length > 32) {
      window.fruitDefenseCombatFeedbackTelemetryHistory.splice(
        0, window.fruitDefenseCombatFeedbackTelemetryHistory.length - 32);
    }
  },

  FruitDefensePublishHubTelemetry: function (jsonPointer) {
    var json = UTF8ToString(jsonPointer);
    var telemetry = JSON.parse(json);
    if (!telemetry || telemetry.schemaVersion !== 1 ||
        typeof telemetry.stateId !== 'string' ||
        typeof telemetry.profileRevision !== 'number' ||
        typeof telemetry.fixtureActive !== 'boolean' ||
        typeof telemetry.fixtureId !== 'string' ||
        typeof telemetry.resolvedState !== 'string' ||
        (telemetry.fixtureActive && telemetry.fixtureId.length === 0)) {
      throw new Error('Invalid hub acceptance telemetry payload.');
    }
    window.fruitDefenseHubTelemetry = telemetry;
    window.fruitDefenseHubTelemetryHistory =
      window.fruitDefenseHubTelemetryHistory || [];
    window.fruitDefenseHubTelemetryHistory.push(telemetry);
    if (window.fruitDefenseHubTelemetryHistory.length > 64) {
      window.fruitDefenseHubTelemetryHistory.splice(
        0, window.fruitDefenseHubTelemetryHistory.length - 64);
    }
  }
});
