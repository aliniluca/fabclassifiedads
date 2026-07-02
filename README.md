# ⚡ AiciGăsești — the most complete classifieds platform

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

### Photos, alerts & maps
- **Real photo uploads** — up to 10 photos per ad (JPEG/PNG/WebP/GIF, 5 MB each) with live previews; stored under `wwwroot/uploads`, falling back to generated SVG covers when no photo is supplied
- **Saved searches with email alerts** — one click on any filtered search saves it; a background service re-runs saved searches every 2 minutes and emails new matches (dev sender writes to `App_Data/outbox`, swap `IEmailSender` for SMTP in production). Manage, pause or delete alerts at `/saved-searches`
- **Map view** — every search has a 🗺 Map view (Leaflet + OpenStreetMap, vendored locally) with filtered markers and listing popups; listings are geocoded offline from a built-in city table

### Feed & sharing (the TikTok play)
- **Vertical swipe feed** at `/feed` — full-screen scroll-snap feed of listings; video ads autoplay (muted, looped, IntersectionObserver-driven) and rank first, image-only ads get a Ken Burns motion effect. Action rail with favorite, message, share and share-card buttons; arrow-key navigation on desktop
- **Video uploads** — sellers can attach a vertical MP4/WebM/MOV (max 60 MB) to any ad; video ads get a 🎬 badge on cards and play on the detail page
- **Share cards** — every listing renders a 1080×1920 (9:16) branded card at `/l/{id}/share-card.svg` sized for TikTok / Instagram stories, with the cover photo embedded, specs summary, price and deep link; plus a native Web Share button on detail pages

### Trust engine ("Verificare AiciGăsești")
- **Listing trust score (0–100)** computed at publish time and shown on every card and detail page: risky-language detection (advance payment, Western Union, WhatsApp-only, "plecat din țară"…), links/phones in text, ALL-CAPS titles, too-short descriptions, copy/paste duplicate detection across accounts, real-photos/video signals
- **Automatic price verification** against the live market median — same car brand ±2 years, price/m² for the same property type and transaction, or category median ("Preț cu 67% sub media pieței — posibil risc")
- **Seller trust score** from account age, email confirmation, ratings, risky-message rate in chat, and measured response speed — shown as a 🛡 badge with full check breakdown
- **Real-time anti-scam chat**: every message is scanned on send; recipients see inline warnings for advance-payment requests, suspicious links/phishing, off-platform moves (WhatsApp/Telegram) and untraceable payments (crypto/gift cards)
- Transparent rule-based + statistical heuristics — each signal can be swapped for an ML model later without UI changes. A seeded scam demo listing (5/100) shows the red flow end to end

### Languages
- **Romanian is the primary language**, English secondary — RO|EN switcher in the header (culture cookie). ~350 translated strings including all filter labels, enum values, category names and relative dates, via a simple dictionary `Translator` with English fallback

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
