**New repository: <https://codeberg.org/keysmashes/MemoryMarker>**

---

# MemoryMarker
[![Download count](https://img.shields.io/endpoint?url=https://qzysathwfhebdai6xgauhz4q7m0mzmrf.lambda-url.us-east-1.on.aws/MemoryMarker)](https://github.com/keysmashes/MemoryMarker)

Ever forget which waymark slot holds your pre-fight positions? Which waymarks are for pugs and not your static?

Worry not! MemoryMarker has your back!

MemoryMarker automatically Loads and Saves waymarks when changing zones. This lets you have 30 waymarks **per zone**.

MemoryMarker adds a context menu option to the waymarks menu to rename the preset.

No longer will you wonder which preset is which set of markers!

![image](https://user-images.githubusercontent.com/9083275/215379965-92ca1091-0182-407a-9750-5c3e07a67336.png)

## Notes

- If you have the WaymarkPreset plugin installed, MemoryMarker will not replace the waymark slots with the saved waymarks

  - Unexpected behavior may result from having both MemoryMarker and WaymarkPreset installed
  
  - It is not recommended to use both WaymarkPreset and MemoryMarker at the same time

- Waymarks are saved to file whenever you change zones, close waymark menu, place, save, delete, or overwrite a waymark

- Waymarks are loaded whenever you change zones

  - If you change to a zone that doesn't have any saved waymarks, all your waymarks will be cleared
