# Avatar Customization System

A modular, ScriptableObject-based avatar customization system for Unity.

## Architecture Overview

The system uses a **ScriptableObject-based architecture** which provides:
- **Editor-friendly** configuration
- **Runtime-efficient** data access
- **Easy to extend** with new categories
- **Persistent** storage via PlayerPrefs

## System Components

### Core ScriptableObjects

#### 1. `AvatarPartItem` (ScriptableObject)
Represents a single customizable part (e.g., a specific hairstyle, dress, etc.)

**Properties:**
- `itemId` - Unique identifier (auto-generated)
- `displayName` - Human-readable name
- `category` - Which category this part belongs to
- `iconSprite` - Small sprite for UI selection buttons
- `avatarSprite` - Full sprite for avatar display
- `sortOrder` - Display order in UI
- `isDefault` - Is this the default selection?
- `isLocked` - Unlock requirements
- `requiredLevel` - Level requirement for unlock
- `requiredCoins` - Coin cost for unlock

**Create:** Right-click in Project > Create > Avatar > Avatar Part Item

#### 2. `AvatarPartDatabase` (ScriptableObject)
Central repository containing all avatar parts.

**Features:**
- Automatic categorization
- Quick lookups by ID or category
- Default item management
- Runtime caching

**Create:** Right-click in Project > Create > Avatar > Avatar Part Database

### Runtime Managers

#### 3. `AvatarCustomizationManager` (MonoBehaviour)
Main runtime state manager for avatar customization.

**Responsibilities:**
- Manages current selections
- Coordinates save/load to PlayerPrefs
- Broadcasts selection events
- Provides API for UI controllers

**Events:**
- `OnPartSelected` - Fired when a part is selected
- `OnCategoryChanged` - Fired when a category changes
- `OnAvatarLoaded` - Fired after loading from PlayerPrefs
- `OnAvatarReset` - Fired after resetting to defaults

#### 4. `AvatarUIController` (MonoBehaviour)
Controls the UI side of avatar customization.

**Features:**
- Category button management
- Dynamic item button spawning
- Selection state visualization
- Category switching

**Setup Requirements:**
- Reference to `AvatarCustomizationManager`
- Category buttons container (already created in prefab)
- Item container (spawn location)
- Item button prefab

#### 5. `AvatarDisplayController` (MonoBehaviour)
Renders the avatar preview with proper layering.

**Modes:**
- **Static Mode:** Assign Image components for each category
- **Dynamic Mode:** Spawns renderers automatically

**Features:**
- Proper layering/sorting
- Automatic sprite updates
- Body color tinting support

### UI Components

#### 6. `AvatarItemButton` (MonoBehaviour)
Component for individual item selection buttons.

**Features:**
- Icon and name display
- Lock status visualization
- Selection animation
- Requirements display (level/coins)

## Setup Instructions

### Step 1: Create Avatar Part Assets

1. Create your avatar part items:
   ```
   Right-click in Project > Create > Avatar > Avatar Part Item
   ```

2. Configure each part:
   - Set `displayName` (e.g., "Ponytail Hair")
   - Select `category`
   - Assign `iconSprite` (for UI buttons)
   - Assign `avatarSprite` (for character display)
   - Set `isDefault = true` for default items

3. Repeat for all items across all categories.

### Step 2: Create Database

1. Create the database asset:
   ```
   Right-click in Project > Create > Avatar > Avatar Part Database
   ```

2. Add all your created parts to the `allAvatarParts` list.

### Step 3: Setup Scene

1. Add `AvatarCustomizationManager` to your scene:
   - Assign the database asset
   - Configure save key prefix if needed

2. Setup UI Controller:
   - Add `AvatarUIController` to your panel
   - Assign references to category buttons container
   - Assign item container
   - Create item button prefab (see below)

3. Setup Display Controller:
   - Add `AvatarDisplayController` to your avatar preview area
   - Either:
     - **Static Mode:** Assign Image components for each category
     - **Dynamic Mode:** Provide renderer prefab and container

### Step 4: Create Item Button Prefab

Create a prefab for the item selection buttons with:
- `Button` component (required)
- `AvatarItemButton` script
- Child `Image` for icon display
- Optional `Image` for background/selection outline
- Optional `TextMeshProUGUI` for name/lock text

### Step 5: Wire Category Buttons

For each category button in your UI (Hair, Dress, etc.):
1. Ensure they have a `Button` component
2. The `AvatarUIController` will automatically find them by name matching
3. Or manually call `ShowCategoryItems(category)` from button clicks

## API Usage Examples

### Selecting a Part Programmatically

```csharp
// Get the customization manager
AvatarCustomizationManager manager = GetComponent<AvatarCustomizationManager>();

// Get a part from the database
AvatarPartItem hairPart = manager.Database.GetPartById("hair_ponytail");

// Select it
manager.SelectPart(AvatarPartCategory.Hair, hairPart);
```

### Getting Current Selections

```csharp
// Get selected part for a category
AvatarPartItem currentHair = manager.GetSelectedPart(AvatarPartCategory.Hair);

// Get all selections
Dictionary<AvatarPartCategory, AvatarPartItem> allSelections = manager.GetAllSelections();
```

### Resetting to Defaults

```csharp
manager.ResetToDefaults();
```

### Manual Save/Load

```csharp
// Save current state
manager.SaveAvatar();

// Load saved state
manager.LoadAvatar();

// Clear saved data
manager.ClearSavedData();
```

## Categories

The system supports these categories by default:

| Category | Purpose | ID |
|----------|---------|-----|
| `BodyColor` | Skin tones and body variations | 1 |
| `Hair` | Hairstyles | 2 |
| `Dress` | Clothing/outfits | 3 |
| `Shoes` | Footwear | 4 |
| `Accessories` | Additional items | 5 |
| `Cosmetics` | Makeup, etc. | 6 |

### Adding New Categories

1. Add to `AvatarPartCategory` enum in `AvatarPartCategory.cs`
2. Update `GetDisplayName()` extension method
3. Create items with the new category
4. Setup corresponding renderer in `AvatarDisplayController`

## Data Persistence

Avatar data is saved to PlayerPrefs with the following key format:
```
{SaveKeyPrefix}_{CategoryName}
```

Example: `AvatarCustomization_Avatar.Hair`

The saved value is the `itemId` of the selected part.

### Saving Player Progress

To integrate with your game's progression system:

```csharp
// When player earns coins or levels up
void OnPlayerProgressed(int newLevel, int newCoins)
{
    // Refresh UI to show newly unlocked items
    // This would require extending AvatarUIController
    // to check unlock status dynamically
}
```

## Extending the System

### Adding Unlock Logic

Override or extend `AvatarPartItem.CheckUnlocked()` to integrate with your game:

```csharp
public bool CheckUnlocked(PlayerProgress playerProgress)
{
    if (!isLocked) return true;
    return playerProgress.level >= requiredLevel &&
           playerProgress.coins >= requiredCoins;
}
```

### Adding Rarity/Tier System

Add properties to `AvatarPartItem`:
```csharp
[SerializeField] private ItemRarity rarity;
public enum ItemRarity { Common, Rare, Epic, Legendary }
```

### Adding Part Effects

Create a base class for avatar parts with effects:
```csharp
public abstract class AvatarPartEffect : ScriptableObject
{
    public abstract void Apply(GameObject avatar);
}

// Then add to AvatarPartItem:
[SerializeField] private AvatarPartEffect[] effects;
```

## Performance Considerations

- ScriptableObjects are efficient - data is loaded once and cached
- All sprite references are direct (no runtime loading)
- UI recycling can be added for large item lists
- Consider object pooling for item buttons

## Troubleshooting

**Items not showing:**
- Check that `iconSprite` and `avatarSprite` are assigned
- Verify category matches between button and item
- Ensure database is built (call `BuildCache()`)

**Selection not persisting:**
- Verify `autoSaveOnSelection` is enabled
- Check PlayerPrefs write permissions
- Verify save key prefix is consistent

**Layering issues:**
- Adjust `baseSortingOrder` in AvatarDisplayController
- Set custom `sortingOrderOffset` on individual items
- Use custom layer names if needed

## Editor Tools

The system includes a custom editor for `AvatarPartDatabase` with:
- Category filtering
- Search functionality
- Validation tools
- Export to report
- Statistics dashboard

Access via Inspector when selecting the Database asset.
