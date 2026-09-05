# Admin Navigation System - Setup Guide

## Overview
This navigation system provides a modular, animated navigation for the Admin Dashboard with:
- 4 navigation buttons (Home, Stories, Users, Profile)
- Page switching with fade animations
- Color changes on selection (#9B5DE5)
- Hover and click animations

## Scene Structure

### Navigation Bar: "Common Nav Bar"
- **Home Button** → Admin Dashboard (Home Page)
- **Stories Button** → Admin Dashboard (Stories)
- **User Button** → Admin Dashboard (All User)
- **Profile Vutton** → Admin Dashboard (Settings)

## Setup Instructions

### Step 1: Create Navigation Manager Object
1. In your "Admin Dashbaord" scene, create an empty GameObject named "AdminNavigationManager"
2. Add the `AdminNavigationController` component to it

### Step 2: Configure Navigation Buttons
In the Inspector for AdminNavigationController:

**Navigation Buttons Array (Size: 4):**

**Element 0 - Home:**
- Button: Drag the "Home Button" GameObject
- Button Text: Drag the TextMeshProUGUI component from the button
- Button Icon: Drag the Image component from the button's icon
- Page Type: Home

**Element 1 - Stories:**
- Button: Drag the "Stories Button" GameObject
- Button Text: Drag the TextMeshProUGUI component
- Button Icon: Drag the Image component
- Page Type: Stories

**Element 2 - Users:**
- Button: Drag the "User Button" GameObject
- Button Text: Drag the TextMeshProUGUI component
- Button Icon: Drag the Image component
- Page Type: Users

**Element 3 - Profile:**
- Button: Drag the "Profile Vutton" GameObject
- Button Text: Drag the TextMeshProUGUI component
- Button Icon: Drag the Image component
- Page Type: Profile

### Step 3: Configure Page Panels
**Page Panels Array (Size: 4):**

**Element 0 - Home:**
- Page Type: Home
- Page Object: Drag "Admin Dashboard (Home Page)"
- Canvas Group: Add CanvasGroup component to the page or leave null (auto-added)

**Element 1 - Stories:**
- Page Type: Stories
- Page Object: Drag "Admin Dashboard (Stories)"
- Canvas Group: (auto-added if null)

**Element 2 - Users:**
- Page Type: Users
- Page Object: Drag "Admin Dashboard (All User)"
- Canvas Group: (auto-added if null)

**Element 3 - Profile:**
- Page Type: Profile
- Page Object: Drag "Admin Dashboard (Settings)"
- Canvas Group: (auto-added if null)

### Step 4: Configure Settings
- Selected Color: #9B5DE5 (RGB: 155, 93, 229)
- Default Color: Gray for inactive buttons
- Fade Duration: 0.3 seconds for page transitions
- Scale Duration: 0.2 seconds for button scale animation
- Selected Scale: 1.1 (scale of selected button)

### Step 5: Add Button Animators (Optional)
For enhanced hover/click effects:
1. Select each navigation button GameObject
2. Add the `NavigationButtonAnimator` component
3. Configure the colors and scale values
4. The animator will work alongside the main navigation controller

## Notes
- The Home page is set as default and will show on scene start
- Only the Home page should be active in the scene initially; others should be inactive
- The "Profile Vutton" has a typo in the original scene name - this is preserved for compatibility
- All pages are automatically deactivated except the active one

## Color Values
- Selected Color: #9B5DE5 (RGB: 155, 93, 229)
- Hover Color: #B580E6 (RGB: 181, 128, 230) - lighter purple
- Default Color: #757582 (RGB: 117, 117, 130) - gray

## Testing
1. Enter Play Mode
2. Click on different navigation buttons
3. Verify pages transition with fade animation
4. Verify selected button turns purple and scales up
5. Verify hover effects on buttons
