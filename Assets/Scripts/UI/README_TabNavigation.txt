# Tab Navigation System - Setup Guide

## Overview
Modular tab navigation system for switching between content panels with animated button appearance changes.

## Features
- Tab button appearance changes (stroke width, icon color, text color)
- Smooth content fade transitions
- Support for Figma custom stroke images
- Color animation on selection
- Modular and reusable

## Components

### 1. TabNavigationController.cs
Main controller that manages tab switching and coordinates button appearance with content visibility.

### 2. TabButton.cs
Optional component for individual tab buttons that handles their appearance independently.

## Quick Setup for Store Page

### Step 1: Create Tab Navigation Manager
1. On your Store Page, create empty GameObject "TabNavigationManager"
2. Add `TabNavigationController` component

### Step 2: Configure Tab Buttons (3 buttons)
In Inspector, expand "Tab Buttons" array and set size to 3.

**Element 0 - Stories:**
- Tab Name: "Stories"
- Button: Drag the Stories button GameObject
- Button Image: Drag the Image component
- Button Text: Drag the TextMeshProUGUI component
- Button Icon: Drag the icon Image component
- Stroke Image: Drag your Figma stroke Image
- Content Type: Stories

**Element 1 - Avatar Items:**
- Same setup, Content Type: AvatarItems

**Element 2 - Coins:**
- Same setup, Content Type: Coins

### Step 3: Configure Tab Content Panels
Expand "Tab Contents" array and set size to 3.

**Element 0 - Stories Content:**
- Content Type: Stories
- Content Object: Drag "Stories item List Scroll view"
- Canvas Group: Leave empty (auto-added)

**Element 1 - Avatar Items Content:**
- Content Type: AvatarItems
- Content Object: Drag "Avatar Item List Scroll view"

**Element 2 - Coins Content:**
- Content Type: Coins
- Content Object: Drag "Coin List Scroll view"

### Step 4: Configure Appearance Settings
- Selected Color: Your configured color
- Default Color: Default gray color
- Selected Stroke Width: 4
- Default Stroke Width: 0
- Fade Duration: 0.25s (content transition)
- Color Change Duration: 0.15s (button animation)

### Step 5: Set Initial Scene State
- Only Stories item List Scroll view should be active
- Avatar Item List and Coin List should be inactive

## Stroke Setup

### For Image-based Strokes (Figma exports)
1. Your stroke should be a separate Image GameObject
2. Assign it to "Stroke Image" field
3. The script adjusts its size for stroke width effect

### For Outline Component
1. Add `Outline` component to your button Image
2. The script will adjust `effectDistance` for stroke width

## Usage from Code

```csharp
using UI;

// Reference the controller
public class StoreManager : MonoBehaviour
{
    [SerializeField] private TabNavigationController tabController;

    void SomeMethod()
    {
        // Switch to a specific tab
        tabController.ShowTab(TabNavigationController.ContentType.Coins);

        // Get current tab
        TabNavigationController.ContentType current = tabController.GetCurrentTab();
    }
}
```

## Individual Tab Buttons (Optional)

For more control, you can add `TabButton` component to each tab button:
1. Select the tab button GameObject
2. Add `TabButton` component
3. Assign the visual elements (icon, text, stroke)
4. Call `SetSelected(true/false)` programmatically

## Troubleshooting

**Stroke not appearing:**
- Verify the stroke Image is assigned
- Check that the stroke GameObject is properly positioned behind the button
- For Outline component, ensure it's on the button Image

**Colors not changing:**
- Confirm TextMeshProUGUI and Image components are assigned
- Check if custom shader is blocking color changes

**Content not switching:**
- Ensure content GameObjects are assigned correctly
- Verify Content Type matches between buttons and content panels
- Check that only the default content is active in the scene
