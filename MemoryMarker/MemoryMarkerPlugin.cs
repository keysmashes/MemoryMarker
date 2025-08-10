using System.Linq;
using Dalamud.Plugin;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.Interop;
using KamiLib.Extensions;
using KamiLib.Window;
using Lumina.Excel.Sheets;

namespace MemoryMarker;

public sealed class MemoryMarkerPlugin : IDalamudPlugin {

	public MemoryMarkerPlugin(IDalamudPluginInterface pluginInterface) {
		pluginInterface.Create<Service>();

		System.Configuration = Service.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
		EnsureSafeConfigVersion();

		System.ContextMenu = new AddonFieldMarkerContextMenu();
		System.FieldMarkerController = new AddonFieldMarkerController();
		System.WindowManager = new WindowManager(Service.PluginInterface);

		if (Service.Condition.IsBoundByDuty()) OnZoneChange(Service.ClientState.TerritoryType);

		Service.ClientState.TerritoryChanged += OnZoneChange;
	}

	public void Dispose() {
		Service.ClientState.TerritoryChanged -= OnZoneChange;

		System.ContextMenu.Dispose();
		System.FieldMarkerController.Dispose();
		System.WindowManager.Dispose();
	}
    
	private void OnZoneChange(ushort territoryType) {
		if (Service.ClientState.IsPvP) return;

		if (Service.PluginInterface.InstalledPlugins.Any(pluginInfo => pluginInfo is { InternalName: "WaymarkPresetPlugin", IsLoaded: true })) {
			Service.Log.Information("WaymarkPreset plugin detected, skipping writing waymarks to memory");
		}

		// If we are bound by duty after changing zones, we need to either generate new markers data, or load existing.
		else if (Service.Condition.IsBoundByDuty()) {
			TryImportMarkers();

			if (System.Configuration.FieldMarkerData.TryAdd(territoryType, new ZoneMarkerData())) {
				Service.Log.Debug($"No markers for {territoryType}, creating");
				System.Configuration.Save(false);
			}

			var markersForTerritory = System.Configuration.FieldMarkerData[territoryType];

			Service.Log.Info($"[Territory: {territoryType,4}] Loading Waymarks, Count: {markersForTerritory.Count}");
			SetZoneMarkerData(markersForTerritory);
		}
	}

	private unsafe void TryImportMarkers() {
		var markersChanged = false;

		foreach (var index in Enumerable.Range(0, FieldMarkerModule.Instance()->Presets.Length)) {
			var marker = FieldMarkerModule.Instance()->Presets.GetPointer(index);
			if (marker->ContentFinderConditionId is 0) continue;

			// Markers store ContentFinderConditions, we store TerritoryTypes, need to convert.
			var territoryType = Service.DataManager.GetExcelSheet<ContentFinderCondition>().GetRow(marker->ContentFinderConditionId).TerritoryType.RowId;
			if (territoryType is 0) continue;

			// Add a ZoneMarkerData entry for this territory if we don't have one already, do nothing if we do have one already.
			System.Configuration.FieldMarkerData.TryAdd(territoryType, new ZoneMarkerData());

			// If our copy of this slot is null, we need to read the marker data and save it.
			if (System.Configuration.FieldMarkerData[territoryType].MarkerData[index] is null) {
				Service.Log.Debug($"[Territory: {territoryType,4}] New Waymark Found, Index {index}");
				System.Configuration.FieldMarkerData[territoryType].MarkerData[index] = new NamedMarker {
					Marker = *(FieldMarkerPreset*) marker,
					Name = string.Empty,
				};

				markersChanged = true;
			}
		}

		if (markersChanged) {
			System.Configuration.Save();
		}
	}

	private static unsafe void SetZoneMarkerData(ZoneMarkerData data) {
		foreach (var index in Enumerable.Range(0, data.MarkerData.Length)) {
			var namedMarker = data.MarkerData[index];
			var targetAddress = (FieldMarkerPreset*) FieldMarkerModule.Instance()->Presets.GetPointer(index);

			if (namedMarker is not null) {
				Service.Log.Debug($"[Territory: {Service.ClientState.TerritoryType,4}] [{index,2}] Loaded '{(namedMarker.Name.IsNullOrEmpty() ? "Unnamed" : namedMarker.Name)}'");
				*targetAddress = namedMarker.Marker;
			}
			else {
				*targetAddress = default;
			}
		}
	}
	
	
	private static void EnsureSafeConfigVersion() {
		// If version is outdated, wipe and reset, it contains invalid data.
		if (System.Configuration.Version <= 2) {
			System.Configuration = new Configuration();
			System.Configuration.Save();
		}
	}
}
