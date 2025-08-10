using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Utility;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.Interop;
using KamiLib.Window;

namespace MemoryMarker;

public unsafe class RenameWindow : Window {
    private AgentFieldMarker* AgentFieldMarker => (AgentFieldMarker*) AgentModule.Instance()->GetAgentByInternalId(AgentId.FieldMarker);
    
    private int SelectedSlot => AgentFieldMarker->PageIndexOffset;
    
    private string SelectedSlotString => MemoryHelper.ReadSeString(AgentFieldMarker->PresetLabels.GetPointer(SelectedSlot)).ToString()[3..];

    public RenameWindow() : base("Rename Waymark", new Vector2(200.0f, 125.0f), true) {
        Flags |= ImGuiWindowFlags.NoResize;
        Flags |= ImGuiWindowFlags.NoCollapse;
    }

    protected override void DrawContents() {
        if (System.Configuration.FieldMarkerData[Service.ClientState.TerritoryType].MarkerData[SelectedSlot] is not { } setting) return;

        if (setting is { Name: "" }) {
            setting.Name = SelectedSlotString;
        }

        ImGuiHelpers.ScaledDummy(10.0f);

        if (ImGui.IsWindowAppearing()) ImGui.SetKeyboardFocusHere();

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if (ImGui.InputText("###RenameTextInput", ref setting.Name, 35, ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EnterReturnsTrue)) {
            Close();
        }

        var buttonSize = ImGuiHelpers.ScaledVector2(100.0f, 23.0f);
        ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - buttonSize.X);
        if (ImGui.Button("Save & Close", buttonSize)) {
            Close();
        }
    }

    public override void OnClose() {
        if (System.Configuration.FieldMarkerData[Service.ClientState.TerritoryType].MarkerData[SelectedSlot] is not { } setting) return;

        if (setting is { Name: "" }) setting.Name = SelectedSlotString;

        Service.NotificationManager.AddNotification(new Notification {
            Type = NotificationType.Success,
            Content = $"""Preset "{setting.Name}" saved""",
        });

        System.WindowManager.RemoveWindow(this);
        System.Configuration.Save();
    }
}
