# Group_06_NMB_Haematology_Lab
Nelson Mandela Bay Haematology Diagnostic Laboratory MVC Project

### South African home address search

Patient registration and profile use Google Places Autocomplete (New), restricted to South Africa and address types. Manual entry remains available; Google cannot guarantee coverage of every home.

Configure `GoogleMaps__ApiKey` in the hosting environment or `GoogleMaps:ApiKey` through .NET user secrets. Enable Maps JavaScript API and Places API (New), with billing enabled. Restrict the browser key to your application's HTTP referrers and these APIs. Do not commit a real key.

When Google is not configured or fails, search falls back to Photon/OpenStreetMap suggestions restricted to South Africa. Coverage varies by provider. Manual entry remains available if both providers are unavailable.
