# Audio System Usage Guide

## Overview
A modular, singleton-based audio management system for Unity with:
- Background music (BGM) control
- Sound effects (SFX) control
- PlayerPrefs persistence
- Dynamic sprite switching for toggle buttons
- Global control from any UI panel

## Setup Instructions

### 1. Create AudioManager in Scene
1. Create an empty GameObject in your Main Menu scene (or a startup scene)
2. Name it "AudioManager"
3. Add the `AudioManager` component to it
4. Assign your audio clips to the fields:
   - **Main Menu BGM**: Drag your main menu music here
   - **Game BGM**: Drag your gameplay music here
   - **Additional BGM**: Optional array for more tracks

### 2. Add Audio Toggle Buttons
For each toggle button on your 4 pages:

1. Select the button GameObject
2. Add the `AudioToggleButton` component
3. Configure in Inspector:
   - **Audio Type**: Choose BGM, SFX, or Master
   - **On Sprite**: Sprite to show when audio is ON
   - **Off Sprite**: Sprite to show when audio is OFF
   - **Use Custom Target**: Check if you want to target a specific Image component

### 3. Place Audio Clips in Resources
For dynamic loading at runtime:
- Put BGM files in: `Resources/Audio/BGM/`
- Put SFX files in: `Resources/Audio/SFX/`

## Usage Examples

### Playing Background Music
```csharp
using Audio;

// Play main menu music
AudioManager.Instance.PlayMainMenuBGM();

// Play game music
AudioManager.Instance.PlayGameBGM();

// Play custom BGM
AudioManager.Instance.PlayBGM(myAudioClip);

// Play from Resources
SFXPlayer.PlayBGM("MyTrackName");
```

### Playing Sound Effects
```csharp
using Audio;

// Using AudioManager
AudioManager.Instance.PlaySFX(myAudioClip);
AudioManager.Instance.PlaySFX(myAudioClip, 0.5f); // With volume

// Using static helper (loads from Resources/Audio/SFX/)
SFXPlayer.Play("ButtonClick");
SFXPlayer.Play("LevelComplete", 0.8f);
```

### Controlling Audio State
```csharp
using Audio;

// Toggle and get new state
bool isNowOn = AudioManager.Instance.ToggleBGM();
bool sfxNowOn = AudioManager.Instance.ToggleSFX();

// Set state directly
AudioManager.Instance.SetBGMEnabled(true);
AudioManager.Instance.SetSFXEnabled(false);

// Adjust volume
AudioManager.Instance.SetBGMVolume(0.5f);
AudioManager.Instance.SetSFXVolume(0.8f);

// Stop music with fade
AudioManager.Instance.StopBGM(fadeOut: true);
```

### Listening to Audio Events
```csharp
// Subscribe to state changes
AudioManager.Instance.OnBGMStateChanged += (isOn) => {
    Debug.Log($"BGM is now {(isOn ? "ON" : "OFF")}");
};

AudioManager.Instance.OnSFXStateChanged += (isOn) => {
    // Handle SFX state change
};
```

## Features

### Automatic Synchronization
All `AudioToggleButton` components automatically sync with the AudioManager state, regardless of which panel they're on. When one button toggles the audio, ALL buttons update their sprites.

### Persistent Settings
Audio settings are automatically saved to PlayerPrefs and loaded on startup:
- `BGM_Enabled`: Whether BGM is on/off
- `SFX_Enabled`: Whether SFX is on/off
- `BGM_Volume`: BGM volume level (0-1)
- `SFX_Volume`: SFX volume level (0-1)

### Fade Effects
The AudioManager includes smooth fade-in/fade-out for BGM transitions. Adjust `Fade Duration` in the Inspector.

## Audio Type Options for Toggle Button
- **BGM**: Controls only background music
- **SFX**: Controls only sound effects
- **Master**: Controls both BGM and SFX together

## Tips
1. The AudioManager uses DontDestroyOnLoad, so it persists across scenes
2. Place your AudioManager in the first scene that loads (usually Main Menu)
3. All toggle buttons will automatically sync - you don't need to manually update them
4. The static `SFXPlayer` class provides convenient shortcuts for one-shot sounds
