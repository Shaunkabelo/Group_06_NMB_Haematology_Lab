# Shared collapsible navigation

The Lab Manager layout now uses shared navigation. Other subsystem layouts
can opt in while keeping their own routes, active-link logic, role checks and styling.
This does not change authorization.

## Files to share on GitHub

Paths below are relative to the web project:

- wwwroot/css/shared-navigation.css
- wwwroot/js/shared-navigation.js
- Views/Shared/_LabManager_Layout.cshtml (working integration example)
- wwwroot/css/manager-navigation.css (Lab Manager table alignment only)

Also share this guide. Other layouts do not need manager-navigation.css.
Stage only these files and this guide for a separate navigation commit.
Review the staged diff; leave unrelated controllers, SQL and settings out.
No database migration is required.

## Integrate in each role layout

1. Include shared-navigation.css AFTER the layout stylesheet and page Styles section:

    <link rel="stylesheet" href="~/css/shared-navigation.css" asp-append-version="true" />

2. Give the sidebar id="shared-sidebar" and class="sidebar".
Keep links inside it as a.nav-item, with an icon and label:

    <aside class="sidebar" id="shared-sidebar">
        <div class="sidebar-brand">
            <span class="brand-icon" aria-hidden="true">🧬</span>
            <span class="nav-brand-label">NMB-HLabSys</span>
        </div>
        <nav class="nav-menu">
            <a class="nav-item" asp-controller="YourController" asp-action="Index">
                <span aria-hidden="true">📊</span>
                <span class="nav-label">Dashboard</span>
            </a>
        </nav>
    </aside>

Use your actual routes and existing icons. Labels may also be plain text after
the icon, as in the Lab Manager layout. Keep badge text separate from nav-label.
Section headings use sidebar-section; footer uses sidebar-footer; optional
signed-in user details use user-info. These are hidden when collapsed.

3. Put the toggle beside the heading (one toggle/sidebar pair per layout):

    <div class="shared-heading">
        <button type="button" id="shared-nav-toggle" class="btn btn-outline-secondary"
                aria-controls="shared-sidebar" aria-expanded="true"
                aria-label="Collapse navigation" title="Collapse navigation" hidden>
            <span aria-hidden="true">&#9776;</span>
        </button>
        <h2>@ViewData["Title"]</h2>
    </div>

4. Include the script near the end of body:

    <script src="~/js/shared-navigation.js" asp-append-version="true"></script>

## Layout compatibility

This enhances an existing sidebar layout; it is not a complete theme.
Your base CSS must use --sidebar for desktop sidebar width and main-content
left margin, for example:

    :root { --sidebar: 210px; }
    .sidebar { position: fixed; top: 0; bottom: 0; left: 0; width: var(--sidebar); }
    .main-content { margin-left: var(--sidebar); min-width: 0; }
    @media (max-width: 860px) {
        .sidebar { position: static; width: 100%; }
        .main-content { margin-left: 0; }
    }

Keep existing colors, link styling and mobile layout. Remove conflicting
fixed widths/margins that override the variable. Collapse applies above 860px
and uses a 68px sidebar; mobile retains the existing navigation.
Tooltips appear on hover or keyboard focus. The preference is stored as
nmb-shared-nav-collapsed in localStorage and shared across layouts on the same
website origin. The renamed key initially defaults to expanded.

## Verify before merging

- Expand/collapse on each role page; content should reclaim the freed space.
- Hover and Tab through icons, including My Profile.
- Navigate to another page and check the preference persists.
- Check a narrow/mobile viewport and a long scrolling menu.
- Check active links, logout, profile and role authorization.
- Check the browser console for errors.

## Reverse the enhancement

Remove the shared CSS/script references, toggle and shared-heading wrapper.
Keep the role's original sidebar styling and links. The sidebar id and label
spans are harmless. Keep manager-navigation.css to retain Lab Manager table
action alignment.