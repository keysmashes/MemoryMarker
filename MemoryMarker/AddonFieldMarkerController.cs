using System;
using System.Linq;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.Interop;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace MemoryMarker;

public unsafe class AddonFieldMarkerController : IDisposable {
    public AddonFieldMarkerController() {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "FieldMarker", OnPostUpdate);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreDraw, "FieldMarker", OnPreDraw);
    }

    public void Dispose() {
        Service.AddonLifecycle.UnregisterListener(OnPreDraw, OnPostUpdate);
    }

    private void OnPostUpdate(AddonEvent eventType, AddonArgs args) {
        var addon = (AddonFieldMarker*) args.Addon.Address;
        if (!System.Configuration.FieldMarkerData.TryGetValue(Service.ClientState.TerritoryType, out var value)) return;

        var configChanged = false;

        foreach (var index in Enumerable.Range(0, value.MarkerData.Length)) {
            var atkValueIndex = index * 2 + 34;
            ref var flagValue = ref addon->AtkValues[atkValueIndex];
            ref var markerData = ref value.MarkerData[index];
            var fieldMarker = (FieldMarkerPreset*) FieldMarkerModule.Instance()->Presets.GetPointer(index);

            // There is a valid entry in this slot
            if (flagValue is { Type: ValueType.UInt, Byte: not 0 }) {
                // Newly added
                if (markerData is null) {
                    Service.Log.Debug($"[{index + 1,2}] Adding preset");

                    markerData = new NamedMarker {
                        Marker = *(FieldMarkerPreset*) FieldMarkerModule.Instance()->Presets.GetPointer(index),
                        Name = string.Empty,
                    };

                    configChanged = true;
                }

                // Preset has been modified
                if (fieldMarker->Timestamp != markerData.Marker.Timestamp) {
                    markerData.Marker = *fieldMarker;

                    configChanged = true;
                }
            }

            // There is no valid entry in this slot
            else {
                // Recently removed
                if (markerData is not null) {
                    Service.Log.Debug($"[{index + 1,2}] Removing preset");

                    markerData = null;
                    configChanged = true;
                }
            }
        }

        if (configChanged) {
            System.Configuration.Save();
        }
    }

    private void OnPreDraw(AddonEvent eventType, AddonArgs args) {
        var addon = (AddonFieldMarker*) args.Addon.Address;
        if (!System.Configuration.FieldMarkerData.TryGetValue(Service.ClientState.TerritoryType, out var value)) return;

        foreach (var index in Enumerable.Range(0, 5)) {
            var entryIndex = index + addon->SelectedPage * 5;
            if (value.MarkerData[entryIndex] is not { Name: { Length: not 0 } name }) continue;

            var nodeId = (uint) (21 + index * 2);

            var buttonNode = addon->GetComponentButtonById(nodeId);
            if (buttonNode is null) continue;

            var desiredLabel = $"{entryIndex + 1}. {name}";

            if (buttonNode->ButtonTextNode->NodeText.ToString() != desiredLabel) {
                buttonNode->ButtonTextNode->NodeText.SetString(desiredLabel);
            }
        }
    }
}
