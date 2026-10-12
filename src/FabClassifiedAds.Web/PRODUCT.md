# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users
Romanians buying and selling secondhand and new goods, the same audience as OLX. Two sides:

- **Sellers.** Mostly private people who already list on OLX/Autovit. Business accounts (car dealers, real-estate agencies) get a public inventory page. The fastest way in is to paste a link to an ad they already have elsewhere and have it imported.
- **Buyers.** People searching across categories, especially cars and real estate, where they want precise filters and protection from scams.

The product deliberately serves both sides without ranking one above the other. Supply-side onboarding (the import/cross-post path) is the mechanism that gets content in.

## Product Purpose
AiciGăsești ("you find it here") is a Romanian general classifieds marketplace inspired by OLX. It exists to be a safer and more precise place to buy and sell, and to make listing here cost a seller nothing beyond what they've already done on OLX. Success means real sellers moving their listings over and buyers finding what they want without getting scammed.

## Positioning
Four differentiators, all confirmed by the owner as reasons to choose it over OLX/Autovit:

1. **Trust / anti-scam.** Every listing gets a 0–100 trust score (risky language, duplicates, photo signals). Prices are checked against the live market median. Sellers get a trust score. Chat messages are scanned in real time for advance-payment, phishing and off-platform requests.
2. **Deeper filters.** Car and real-estate search goes past OLX: year/mileage/cm³/hp ranges, 36 car equipment flags, rooms/surface/floor ranges and 29 property amenities.
3. **Zero-effort cross-post.** Paste a link to your own OLX/Autovit ad and title, description, price, location and photos are pre-filled. The public `/start` funnel goes from link to signup to published ad.
4. **Video feed and social distribution.** A vertical `/feed` with autoplay video ads, plus branded 9:16 share cards for TikTok/Instagram stories.

## Operating Context
- Live at **aicigasesti.ro**, deployed on Oracle Cloud (Ubuntu, Nginx, Kestrel, SQLite).
- Romanian is the primary language and English is secondary (RO|EN switcher). Copy, filters, enums and categories are bilingual through `Services/Translator.cs`.
- 11 root categories with 200+ subcategories. Cars and real estate have their own typed detail models.
- Sellers come in through `/start` (anonymous preview, then lightweight signup with phone) or through the full post form.
- Moderation runs through `/admin`: content filter, pending review, user bans, banners, and settings.

## Capabilities and Constraints
- Existing stack: ASP.NET Core MVC (.NET 11), Razor views, EF Core + SQLite, ASP.NET Identity. Custom design system in `wwwroot/css/site.css`, no CSS framework. Leaflet/OpenStreetMap is vendored locally.
- No external image dependencies by design: listings without photos fall back to generated SVG covers.
- Everything is free for users. Monetization is limited to admin-managed ad banners. Paid features are undecided.
- GDPR (Romania/EU) obligations are in place: essential-cookie consent, legal pages, data export and account deletion. Future work must keep them.
- The cross-post importer must only import the user's **own** ads, and it is SSRF-hardened.
- Demo accounts and seeded listings exist for development (`Demo123!`).

## Brand Commitments
- Name: **AiciGăsești**, written with Romanian diacritics. The domain is aicigasesti.ro.
- Trust feature name: "Verificare AiciGăsești".
- Romanian-first voice. English is a fallback, not the lead.

## Evidence on Hand
- Production content is mostly seeded demo data plus listings posted by the owner's friends.
- **There are no real testimonials, user counts, reviews, press or traction metrics.** Future work must not invent any (no "10,000 users", no fake quotes, no ratings presented as real).
- Real, demonstrable features can be shown as proof: the seeded scam listing (trust score 5/100) shows the full anti-scam flow, share cards render at `/l/{id}/share-card.svg`, and the `/start` import works end to end.

## Product Principles
1. **Safety you can see.** Trust signals belong where people make decisions (cards, detail pages, chat), not hidden in settings.
2. **Moving from OLX costs nothing.** Any seller-facing flow should reuse what the seller already has and ask for as little as possible.
3. **Precision over volume.** Filters and specs should let a buyer narrow down exactly. Category-specific depth beats generic breadth.
4. **Romanian first, honest always.** Write for Romanians in their language, and never overstate scale or fake social proof on a young marketplace.
