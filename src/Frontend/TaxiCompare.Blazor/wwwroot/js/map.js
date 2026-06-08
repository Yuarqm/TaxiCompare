const K = 'b85ff6fd-79dc-43cb-8c47-df7034be67cf';

window._tcMap  = null;
window._tcObj  = [];
window._tcMode = null;
window._tcDot  = null;
window._tcO    = null;   // null пока не выбрана точка
window._tcD    = null;
window._tcRdy  = false;
window._tcClk  = false;

/* ── рисуем маркеры (без линии — линию рисует маршрут) ──────────── */
function _draw() {
  window._tcObj.forEach(function(o){ try{ o.destroy(); }catch(e){} });
  window._tcObj = [];
  var m = window._tcMap, o = window._tcO, d = window._tcD;
  if (!m || !window._tcRdy) return;

  if (o) {
    window._tcObj.push(new mapgl.CircleMarker(m, {
      coordinates: o, radius: 12, color: '#00D46A',
      strokeWidth: 3, strokeColor: '#fff', zIndex: 5
    }));
  }
  if (d) {
    window._tcObj.push(new mapgl.CircleMarker(m, {
      coordinates: d, radius: 12, color: '#FF4B4B',
      strokeWidth: 3, strokeColor: '#fff', zIndex: 5
    }));
  }

  if (o && d) {
    try {
      var pad = Math.max(0.1, Math.abs(o[1] - d[1]) * 0.3);
      m.fitBounds([
        [Math.min(o[0], d[0]) - pad, Math.min(o[1], d[1]) - pad],
        [Math.max(o[0], d[0]) + pad, Math.max(o[1], d[1]) + pad]
      ], { padding: 80, maxZoom: 12 });
    } catch(e) {}
  }
}

/* ── прямая линия между точками (fallback) ───────────────────────── */
function _drawStraight() {
  window._tcObj.forEach(function(o){ try{ o.destroy(); }catch(e){} });
  window._tcObj = [];
  var m = window._tcMap, o = window._tcO, d = window._tcD;
  if (!m || !window._tcRdy) return;

  if (o) {
    window._tcObj.push(new mapgl.CircleMarker(m, {
      coordinates: o, radius: 12, color: '#00D46A',
      strokeWidth: 3, strokeColor: '#fff', zIndex: 5
    }));
  }
  if (d) {
    window._tcObj.push(new mapgl.CircleMarker(m, {
      coordinates: d, radius: 12, color: '#FF4B4B',
      strokeWidth: 3, strokeColor: '#fff', zIndex: 5
    }));
  }
  if (o && d) {
    window._tcObj.push(new mapgl.Polyline(m, {
      coordinates: [o, d], width: 5, color: '#00D46A', zIndex: 2
    }));
    try {
      var pad = Math.max(0.1, Math.abs(o[1] - d[1]) * 0.3);
      m.fitBounds([
        [Math.min(o[0], d[0]) - pad, Math.min(o[1], d[1]) - pad],
        [Math.max(o[0], d[0]) + pad, Math.max(o[1], d[1]) + pad]
      ], { padding: 80, maxZoom: 12 });
    } catch(e) {}
  }
}

/* ── маршрут через OSRM (бесплатно, без ключа, реальные дороги) ── */
async function _drawRoute(oLng, oLat, dLng, dLat) {
  try {
    const url = `https://router.project-osrm.org/route/v1/driving/${oLng},${oLat};${dLng},${dLat}?overview=full&geometries=geojson`;
    const resp = await fetch(url);

    if (!resp.ok) {
      console.warn('[OSRM] HTTP error:', resp.status);
      return null;
    }

    const data = await resp.json();
    console.log('[OSRM] response:', data);

    if (data.code !== 'Ok' || !data.routes?.length) {
      console.warn('[OSRM] no route:', data.code);
      return null;
    }

    const route = data.routes[0];
    // GeoJSON координаты [lng, lat] — именно то что нужно 2GIS
    const coords = route.geometry.coordinates;

    return {
      coords,
      distanceM: route.distance,  // метры по дорогам
      durationS: route.duration   // секунды
    };
  } catch (e) {
    console.error('[OSRM] error:', e);
    return null;
  }
}

/* ── клик по карте ───────────────────────────────────────────────── */
function _onClick(e) {
  var mode = window._tcMode;
  if (!mode) return;
  var lng = e.lngLat[0], lat = e.lngLat[1];
  if (mode === 'origin') window._tcO = [lng, lat];
  else                   window._tcD = [lng, lat];
  window._tcMode = null;
  try { window._tcMap.getCanvas().style.cursor = ''; } catch(e) {}
  _drawStraight();
  if (window._tcDot) {
    window._tcDot.invokeMethodAsync('OnMapClick', mode, lat, lng).catch(function(){});
  }
}

function _addClick() {
  if (!window._tcMap || window._tcClk) return;
  window._tcClk = true;
  window._tcMap.on('click', _onClick);
}

/* ── инициализация карты ─────────────────────────────────────────── */
function _start(id) {
  if (window._tcMap) { try { window._tcMap.destroy(); } catch(e) {} }
  window._tcMap = null; window._tcRdy = false; window._tcClk = false;

  var defCenter = (window._tcO && window._tcD)
    ? [(window._tcO[0] + window._tcD[0]) / 2, (window._tcO[1] + window._tcD[1]) / 2]
    : window._tcO || window._tcD || [69.4695, 56.1122];
  window._tcMap = new mapgl.Map(id, {
    key: K,
    center: defCenter,
    zoom: 7
  });
  window._tcMap.on('load', async function() {
    window._tcRdy = true;
    if (window._tcO && window._tcD) {
      const result = await window.drawRealRoute(window._tcO[0], window._tcO[1], window._tcD[0], window._tcD[1]);
      if (!result) _drawStraight();
    }
    _addClick();
  });
  setTimeout(_addClick, 3000);
}

/* ── публичные функции ───────────────────────────────────────────── */
window.init2GisMap = function(id, oLat, oLng, dLat, dLng, dot) {
  window._tcDot = dot;
  // Только если координаты реальные (не дефолтные нули)
  window._tcO = (oLat !== 0 && oLng !== 0) ? [oLng, oLat] : null;
  window._tcD = (dLat !== 0 && dLng !== 0) ? [dLng, dLat] : null;

  function go() {
    setTimeout(function() {
      if (document.getElementById(id)) _start(id);
    }, 1000);
  }

  if (window.mapgl) { go(); }
  else {
    var s = document.createElement('script');
    s.src = 'https://mapgl.2gis.com/api/js/v1';
    s.onload = go;
    document.head.appendChild(s);
  }
};

window.updateMapRoute = async function(oLat, oLng, dLat, dLng) {
  window._tcO = [oLng, oLat];
  window._tcD = [dLng, dLat];

  async function waitForMap() {
    let retries = 0;
    while ((!window._tcMap || !window._tcRdy) && retries < 150) {
      await new Promise(r => setTimeout(r, 400));
      retries++;
    }
    return window._tcMap && window._tcRdy;
  }

  const ready = await waitForMap();
  if (!ready) {
    console.error('[Route] map failed to initialize');
    return;
  }

  try {
    window._tcO = (oLng !== 0 && oLat !== 0) ? [oLng, oLat] : window._tcO;
    window._tcD = (dLng !== 0 && dLat !== 0) ? [dLng, dLat] : window._tcD;
    if (!window._tcO || !window._tcD) { _draw(); return; }
    const result = await window.drawRealRoute(window._tcO[0], window._tcO[1], window._tcD[0], window._tcD[1]);
    if (!result) {
      console.warn('[Route] fallback to straight line');
      _drawStraight();
    }
  } catch (e) {
    console.error('[Route] draw error', e);
    _drawStraight();
  }
};

window.enableMapClickMode = function(mode) {
  window._tcMode = mode;
  if (window._tcMap) {
    try { window._tcMap.getCanvas().style.cursor = 'crosshair'; } catch(e) {}
    _addClick();
  }
};

window.geocodeAddress = async function(addr) {
  try {
    var r = await fetch(
      'https://catalog.api.2gis.com/3.0/items/geocode?q=' +
      encodeURIComponent(addr) + '&fields=items.point&key=' + K
    );
    var d = await r.json();
    var it = d.result && d.result.items;
    if (it && it.length > 0 && it[0].point)
      return { found: true, lat: it[0].point.lat, lng: it[0].point.lon };
  } catch(e) {}
  return { found: false, lat: 0, lng: 0 };
};

/* ── обратный геокодинг через 2GIS — возвращает full_name с типом нас.пункта ── */
window.reverseGeocode = async function(lat, lng) {
  try {
    var r = await fetch(
      'https://catalog.api.2gis.com/3.0/items/geocode?q=' + lat.toFixed(6) + ',' + lng.toFixed(6) +
      '&fields=items.full_name,items.point&key=' + K + '&locale=ru_RU&page_size=5'
    );
    var d = await r.json();
    var items = d.result && d.result.items;
    if (!items || items.length === 0) return lat.toFixed(4) + ', ' + lng.toFixed(4);

    // Сортируем по расстоянию до точки клика
    function distSq(it) {
      var p = it.point; if (!p) return Infinity;
      return (p.lat - lat) * (p.lat - lat) + (p.lon - lng) * (p.lon - lng);
    }
    var sorted = items.slice().sort(function(a, b) { return distSq(a) - distSq(b); });
    var name = sorted[0].full_name || sorted[0].name;
    return name || (lat.toFixed(4) + ', ' + lng.toFixed(4));
  } catch(e) {
    console.warn('[reverseGeocode] error:', e);
  }
  return lat.toFixed(4) + ', ' + lng.toFixed(4);
};

window.reverseGeocodeExact = window.reverseGeocode;

/* ── главная функция: маршрут OSRM → рисуем на 2GIS карте ───────── */
window.drawRealRoute = async function(oLng, oLat, dLng, dLat) {
  if (!oLng || !oLat || !dLng || !dLat) { _draw(); return null; }
  try {
    const result = await _drawRoute(oLng, oLat, dLng, dLat);

    if (result && result.coords && result.coords.length) {
      // Удаляем старый маршрут
      if (window._tcRoute) {
        try { window._tcRoute.destroy(); } catch(e) {}
        window._tcRoute = null;
      }

      // Рисуем маршрут по дорогам синей линией
      window._tcRoute = new mapgl.Polyline(window._tcMap, {
        coordinates: result.coords,
        width: 6,
        color: '#1976d2',
        zIndex: 3
      });

      // Рисуем маркеры поверх маршрута
      _draw();

      return { DistanceM: result.distanceM, DurationS: result.durationS };
    }

    // Fallback: прямая линия + Haversine × 1.35
    console.warn('[Route] OSRM недоступен — Haversine fallback');
    _drawStraight();

    const R = 6371000;
    const lat1 = oLat * Math.PI / 180;
    const lat2 = dLat * Math.PI / 180;
    const dLat2 = (dLat - oLat) * Math.PI / 180;
    const dLon2 = (dLng - oLng) * Math.PI / 180;
    const a = Math.sin(dLat2/2) * Math.sin(dLat2/2) +
              Math.cos(lat1) * Math.cos(lat2) *
              Math.sin(dLon2/2) * Math.sin(dLon2/2);
    const straightM = R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));
    const distanceM = straightM * 1.35;
    const durationS = (distanceM / 1000) / 60 * 3600;

    return { DistanceM: distanceM, DurationS: durationS };

  } catch (e) {
    console.error('[Route] render failed', e);
    _drawStraight();
    return null;
  }
};

/* ── автодополнение через 2GIS Suggest API ───────────────────────── */
window.suggestAddress = async function(query) {
  try {
    const r = await fetch(
      'https://catalog.api.2gis.com/3.0/suggests?q=' +
      encodeURIComponent(query) +
      '&key=' + K +
      '&fields=items.full_name,items.name&type=address,street,building&locale=ru_RU&page_size=6'
    );
    const d = await r.json();
    const items = d.result?.items ?? [];
    return items
      .map(i => i.full_name || i.name)
      .filter(Boolean)
      .slice(0, 6);
  } catch(e) {
    console.warn('[Suggest] error:', e);
    return [];
  }
};
