# ⚡ FabAds — the most complete classifieds platform

A full-featured classified ads marketplace built on **.NET 11** (ASP.NET Core MVC + EF Core + SQLite + ASP.NET Identity), designed to out-filter OLX-style platforms.

![.NET 11](https://img.shields.io/badge/.NET-11-6d5dfc) ![EF Core](https://img.shields.io/badge/EF%20Core-10-fc5d8d) ![SQLite](https://img.shields.io/badge/SQLite-embedded-28c76f)

## Run it

```bash
cd src/FabClassifiedAds.Web
dotnet run
```

The database is created and seeded automatically on first start (full category tree, ~75 car brands with models, demo users and ~40 realistic demo listings).

**Demo accounts** (password `Demo123!`): `alex@demo.fab`, `maria@demo.fab`, `dealer@demo.fab` (business), `estate@demo.fab` (business), `dan@demo.fab`.

## What's inside

### Categories
11 root categories, 200+ subcategories: Auto/Moto/Boats, Real Estate, Electronics & Appliances, Home & Garden, Fashion & Beauty, Mom & Kids, Sports/Hobby/Leisure, Animals, Agriculture & Industry, Jobs, Services — each with deep subcategory trees (e.g. tires split into summer/winter/all-season, phones split per brand).

### Car marketplace
- **~75 seeded brands** (Abarth → Zeekr) with full model lists for the popular ones (BMW: Seria 1–8, X1–X7, i3–iX, M models…)
- Dependent brand → model dropdown (JSON API endpoint)
- Filters: brand, model, **year range, mileage range, engine cm³ range, power hp range**, fuel (petrol/diesel/hybrid/PHEV/EV/LPG/CNG/H₂), transmission, body type, drivetrain, color, Euro emission standard, accident-free, service book
- **36 equipment options** (matrix LED, HUD, 360 camera, air suspension, ventilated seats…) stored as bit flags and filterable in combination
- EV-aware specs: battery kWh, range km

### Real estate
- 12 property categories (sale/rent apartments & houses, land, offices, commercial, garages, rooms, vacation rentals, new developments)
- Filters: property type, sale/rent, **rooms range, surface range, floor range, year-built range**, heating type, furnishing level, energy class
- **29 amenities** as filterable flags (pool, sauna, EV charger, smart home, solar panels, sea view, pet-friendly…)

### Platform features
- Hero homepage with global search, category grid, featured band, brand strip, CTA + trust sections
- Full-text search across titles/descriptions with price/location/condition/seller filters and 7 sort orders
- Listing detail with gallery, spec grid, equipment tags, seller card with ratings, safety tips, similar ads
- Post-ad form with **dynamic category-specific fieldsets** (car fields and property fields appear based on category)
- Auth (register/login, private vs business accounts), favorites (AJAX heart toggle), **built-in buyer↔seller chat**, my-ads dashboard
- SVG placeholder image service (deterministic gradients per listing) — zero external image dependencies
- Custom dark design system, no CSS framework, fully responsive

## Architecture

```
src/FabClassifiedAds.Web
├── Controllers/    Home, Listings (search/detail/create), Account, Favorites, Messages, Media
├── Data/           AppDbContext, CategorySeeder, CarDataSeeder, DbSeeder
├── Models/
│   ├── Entities/   Listing, Category, CarDetails (+brands/models), RealEstateDetails, Messaging…
│   ├── SearchFilters.cs
│   └── ViewModels.cs
├── Services/       SearchService (composable typed filtering + category subtree expansion)
└── Views/          Razor views + custom design system in wwwroot/css/site.css
```

Typed detail tables (`CarDetails`, `RealEstateDetails`) keep range filtering indexed and fast, while `ListingAttribute` key/values cover any other category.
