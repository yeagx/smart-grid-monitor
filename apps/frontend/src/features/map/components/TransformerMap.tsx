import { useEffect, useRef } from 'react';
import { MapContainer, TileLayer, Marker, Popup, useMap } from 'react-leaflet';
import L from 'leaflet';
import type { MapMarker, ThreatType } from '../types';
import { EVENT_TYPES, HEALTHY_COLOR } from '@/lib/event-types';

interface TransformerMapProps {
  markers: MapMarker[];
  selectedMarker: MapMarker | null;
  activeTypeFilter?: ThreatType | '';
  onSelectMarker: (marker: MapMarker) => void;
}

// Center of Cairo / Giza distribution network
const DEFAULT_CENTER: [number, number] = [30.0444, 31.2357];
const DEFAULT_ZOOM = 12;

/**
 * Determines marker badge style based on alert profile and active threat filter.
 */
function getMarkerStyle(marker: MapMarker, activeTypeFilter?: ThreatType | '') {
  // If a specific threat filter is active and the transformer has alerts of that type, emphasize it
  if (activeTypeFilter === 'theft' && marker.theftAlertCount > 0) {
    return { bg: EVENT_TYPES.theft.color, ring: EVENT_TYPES.theft.ring, label: marker.theftAlertCount };
  }
  if (activeTypeFilter === 'overload' && marker.overloadAlertCount > 0) {
    return { bg: EVENT_TYPES.overload.color, ring: EVENT_TYPES.overload.ring, label: marker.overloadAlertCount };
  }
  if (activeTypeFilter === 'line_drop' && marker.lineDropAlertCount > 0) {
    return { bg: EVENT_TYPES.line_drop.color, ring: EVENT_TYPES.line_drop.ring, label: marker.lineDropAlertCount };
  }

  // Dominant threat priority: Theft > Overload > Voltage Drop > Healthy
  if (marker.theftAlertCount > 0) {
    return { bg: EVENT_TYPES.theft.color, ring: EVENT_TYPES.theft.ring, label: marker.totalAlertCount };
  }
  if (marker.overloadAlertCount > 0) {
    return { bg: EVENT_TYPES.overload.color, ring: EVENT_TYPES.overload.ring, label: marker.totalAlertCount };
  }
  if (marker.lineDropAlertCount > 0) {
    return { bg: EVENT_TYPES.line_drop.color, ring: EVENT_TYPES.line_drop.ring, label: marker.totalAlertCount };
  }

  // No alerts detected
  return { bg: HEALTHY_COLOR.color, ring: HEALTHY_COLOR.ring, label: 0 };
}

/**
 * Creates custom Leaflet DivIcon for a transformer marker.
 */
const MIN_MARKER_SIZE = 18;
const MAX_MARKER_SIZE = 34;
const SELECTED_MARKER_SIZE_OFFSET = 8;

export function getMarkerSizeForZoom(zoom: number): number {
  return Math.min(MAX_MARKER_SIZE, Math.max(MIN_MARKER_SIZE, 18 + (zoom - 8) * 2));
}

function createCustomMarkerIcon(
  marker: MapMarker,
  isSelected: boolean,
  activeTypeFilter: ThreatType | '' | undefined,
  zoom: number,
) {
  const { bg, ring, label } = getMarkerStyle(marker, activeTypeFilter);
  const baseSize = getMarkerSizeForZoom(zoom);
  const size = isSelected
    ? Math.min(MAX_MARKER_SIZE + SELECTED_MARKER_SIZE_OFFSET, baseSize + SELECTED_MARKER_SIZE_OFFSET)
    : baseSize;
  const pulseClass = isSelected ? 'ring-4 ring-white ring-offset-2 ring-offset-grid-bg animate-pulse' : '';

  const html = `
    <div style="display:flex;align-items:center;justify-content:center;width:${size}px;height:${size}px;border-radius:50%;background-color:${bg};border:2px solid ${ring};box-shadow:0 2px 8px rgba(0,0,0,0.5);cursor:pointer;color:white;font-weight:700;font-size:${isSelected ? '12px' : '10px'};font-family:ui-monospace,monospace;" class="${pulseClass}">
      ${label > 0 ? label : ''}
    </div>
  `;

  return L.divIcon({
    className: 'custom-grid-marker',
    html,
    iconSize: [size, size],
    iconAnchor: [size / 2, size / 2],
    popupAnchor: [0, -size / 2],
  });
}

function ZoomResponsiveMarker({
  marker,
  isSelected,
  activeTypeFilter,
  onSelectMarker,
}: {
  marker: MapMarker;
  isSelected: boolean;
  activeTypeFilter?: ThreatType | '';
  onSelectMarker: (marker: MapMarker) => void;
}) {
  const map = useMap();
  const markerRef = useRef<L.Marker | null>(null);

  useEffect(() => {
    const updateMarkerSize = () => {
      markerRef.current?.setIcon(
        createCustomMarkerIcon(marker, isSelected, activeTypeFilter, map.getZoom()),
      );
    };

    map.on('zoomend', updateMarkerSize);
    return () => {
      map.off('zoomend', updateMarkerSize);
    };
  }, [activeTypeFilter, isSelected, map, marker]);

  return (
    <Marker
      ref={markerRef}
      position={[marker.latitude, marker.longitude]}
      icon={createCustomMarkerIcon(marker, isSelected, activeTypeFilter, map.getZoom())}
      eventHandlers={{
        click: () => onSelectMarker(marker),
      }}
    >
      <Popup className="custom-leaflet-popup">
        <div className="p-1 text-xs">
          <div className="font-mono font-semibold text-gray-900">
            {marker.transformerId}
          </div>
          <div className="text-gray-600 text-[11px]">
            {marker.district} • {marker.capacityKw} kW
          </div>
          <div className="mt-1 font-medium text-gray-800">
            {marker.totalAlertCount > 0 ? (
              <span className="text-red-600 font-semibold">
                {marker.totalAlertCount} alert window{marker.totalAlertCount !== 1 ? 's' : ''}
              </span>
            ) : (
              <span className="text-emerald-600">No alerts</span>
            )}
          </div>
        </div>
      </Popup>
    </Marker>
  );
}

/**
 * Map helper component to auto-pan when a marker is selected
 * or adjust bounds when markers change.
 */
function MapViewUpdater({
  selectedMarker,
  markers,
}: {
  selectedMarker: MapMarker | null;
  markers: MapMarker[];
}) {
  const map = useMap();

  useEffect(() => {
    if (selectedMarker) {
      map.panTo([selectedMarker.latitude, selectedMarker.longitude], {
        animate: true,
        duration: 0.5,
      });
    }
  }, [selectedMarker, map]);

  useEffect(() => {
    if (!selectedMarker && markers.length > 0) {
      const bounds = L.latLngBounds(markers.map((m) => [m.latitude, m.longitude]));
      if (bounds.isValid()) {
        map.fitBounds(bounds, { padding: [40, 40], maxZoom: 14 });
      }
    }
  }, [markers, map, selectedMarker]);

  return null;
}

export function TransformerMap({
  markers,
  selectedMarker,
  activeTypeFilter,
  onSelectMarker,
}: TransformerMapProps) {
  return (
    <div className="relative h-[480px] sm:h-[560px] w-full rounded-lg overflow-hidden border border-grid-border shadow-inner">
      <MapContainer
        center={DEFAULT_CENTER}
        zoom={DEFAULT_ZOOM}
        className="h-full w-full bg-grid-bg"
        scrollWheelZoom={true}
      >
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener noreferrer">OpenStreetMap</a> contributors'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          className="map-tiles"
        />

        <MapViewUpdater selectedMarker={selectedMarker} markers={markers} />

        {markers.map((marker) => (
          <ZoomResponsiveMarker
            key={marker.transformerId}
            marker={marker}
            isSelected={selectedMarker?.transformerId === marker.transformerId}
            activeTypeFilter={activeTypeFilter}
            onSelectMarker={onSelectMarker}
          />
        ))}
      </MapContainer>
    </div>
  );
}
