# Consolation

It's difficult to retrieve logs and warnings from Unity outside the editor.  To
make it easier, this console displays output from `Debug` in the game itself.
This is especially useful on mobile devices.

![Console in Unity game](https://matthewminer.com/images/consolation.png)


## Installing

### 1: Download the repo and drop it into folder `Assets`
### 2: Add the line below to `Packages/manifest.json`

for version `1.4.2`
```csharp
"com.matthewminer.consolation":"https://github.com/wolf-package/consolation#1.4.2",
```


## Using

Attach the `Console` component to a game object. When playing your game, open
the console window with the back quote key <kbd>`</kbd>. This shortcut is
configurable in the inspector.

Alternatively, enable shake-to-open in the inspector to open the console on
mobile devices. The component provides an option to prevent accidental shakes by
requiring 3 or more fingers on the screen.

Several other settings like font size and the maximum log count are also
configurable in the inspector.

![Screenshot 2024-08-06 165840](https://github.com/user-attachments/assets/03907710-0a67-4ea1-b7a2-a415ec4b416d)



## Compatibility

Supports Unity 2017.x and above. It hasn't been tested on all the platforms that
Unity supports but it probably works on most.
