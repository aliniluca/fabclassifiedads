# AiciGăsești — API

Token-protected HTTP API for adding listings programmatically, plus the staging
import API. JSON in, JSON out. Enum fields accept their **string names** (e.g.
`"Diesel"`).

Base URL: `https://aicigasesti.ro`

---

## Authentication

Every `/api/*` endpoint requires an API key in a header:

```
X-Api-Key: <your-key>
```

There are two kinds of key:

- **Per-account keys** — each signed-in user generates their own from **My ads → 🔑 API key**
  (`/account/api`). Listings created with an account key are **owned by that account**
  (appear under their profile, business/private per the account). Only a hash is stored;
  the raw key is shown once. Regenerating or revoking invalidates the old key immediately.
- **Master key** — the server-wide admin key, from the `IMPORT_API_KEY` environment
  variable (or `Api:Key` in config). It creates listings under a system "API" account and
  is the **only** key allowed on the staging import API. Set it at deploy time:

  ```bash
  sudo IMPORT_API_KEY="a-long-random-secret" ./scripts/deploy.sh
  ```

If no master key is configured and the caller presents no valid account key, the API stays
**locked** (every call returns `401`).

Responses: `401 Unauthorized` (missing/wrong key), `400`/`422` (validation),
`201 Created` (success).

| Endpoint | Master key | Account key |
|----------|:---------:|:-----------:|
| `POST /api/listings`, `GET /api/categories` | ✅ (owns → system user) | ✅ (owns → that account) |
| `POST /api/import/*` (staging) | ✅ | ❌ 401 |

---

## POST /api/listings — create a listing

Creates a listing owned by the key's account (or the system account for the master
key). Returns `201` with the new id and public URL.

### Photos

Photos are a **JSON array of absolute image URLs** (`"images": ["https://…/1.jpg"]`),
**not** a multipart file upload. The server downloads each URL itself through an
SSRF-guarded fetcher (no internal/metadata hosts), validates type/size, and attaches
up to 10. An image that can't be fetched is skipped and never blocks creation.

### Body

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `title` | string | ✅ | 8–120 chars |
| `description` | string | ✅ | 10–8000 chars |
| `price` | number | — | omit for "ask for price"; ignored if `isFree` |
| `currency` | string | — | `EUR` (default), `USD`, `RON` |
| `isNegotiable` | bool | — | default `true` |
| `isFree` | bool | — | default `false` |
| **`categorySlug`** | string | one of slug/id/auto | e.g. `"iphone"`. **Preferred** — stable across deploys |
| **`categoryId`** | int | one of slug/id/auto | numeric alternative, from `GET /api/categories` |
| `city` | string | ✅ | |
| `region` | string | — | |
| `condition` | enum | — | `New`, `Used` (default), `Refurbished`, `ForParts` |
| `active` | bool | — | `true` (default) = live; `false` = hidden Draft |
| `images` | string[] | — | absolute URLs; downloaded server-side (SSRF-guarded), max 10 |
| `car` | object | **only** for car categories | see below |
| `realEstate` | object | **only** for real-estate categories | see below |

### Choosing a category (important)

The field is **`categorySlug`** (a string) — **not** a numeric `categoryId` in your
config unless you use the `categoryId` field explicitly. Resolution order:

1. `categoryId` if you send it, else
2. `categorySlug` if you send it, else
3. **auto-detected** from the title/description (car brands + keywords). If nothing
   matches you get `422` — pass a slug/id.

**Which categories need an extra object?** Only two kinds:

| Category kind | Extra object required | Examples (slug) |
|---------------|----------------------|-----------------|
| Cars (`kind: "Cars"`) | **`car`** (year, mileage, …) | `cars` |
| Real estate (`kind: "RealEstate"`) | **`realEstate`** (surfaceM2, propertyType, …) | `apartments-for-sale`, `houses-for-rent`, `land-and-plots`, `offices`, … |
| **Everything else** | **none** | `iphone`, `samsung`, `laptops`, `televisions`, `playstation`, `sofas-and-armchairs`, … |

`GET /api/categories` returns a `requires` field per category (`"car"`,
`"realEstate"`, or `null`) so your mapper knows exactly what each one needs. A full
snapshot is committed at [`docs/categories.json`](docs/categories.json).

**Phones/electronics** are plain `Generic` categories — send only the common fields,
no nested object. This is the fix for the "needs a car object" error: that only happens
when the resolved category is a **car** one (either you passed a car slug, or
auto-detect matched a car brand). Pass an explicit phone slug/id and the car branch
never runs.

For your uploader config, use:

```yaml
upload:
  extra_fields:
    categorySlug: "iphone"     # or samsung, xiaomi, other-phones, mobile-phones, laptops, …
    # categoryId: 149          # alternatively the numeric id from /api/categories
```

### `car` object (required when the category is a vehicle)

| Field | Type | Notes |
|-------|------|-------|
| `year` | int | required |
| `mileage` | int | km, required |
| `fuel` | enum | `Petrol`, `Diesel`, `Hybrid`, `PlugInHybrid`, `Electric`, `Lpg`, `Cng`, `HydrogenFuelCell` |
| `transmission` | enum | `Manual`, `Automatic`, `SemiAutomatic`, `Cvt` |
| `body` | enum | `Sedan`, `Hatchback`, `Estate`, `Suv`, `Coupe`, `Convertible`, `Pickup`, `Minivan`, `Van`, `OffRoad` |
| `drive` | enum | `FrontWheel`, `RearWheel`, `AllWheel` |
| `brandId` / `modelId` | int | optional; auto-filled when detected |
| `variant` | string | e.g. `"320d xDrive M Sport"` |
| `engineCc` | int | cm³ |
| `powerHp` | int | |
| `color` | string | |
| `options` | enum[] | any of: `Abs, Esp, AirConditioning, ClimateControl, CruiseControl, AdaptiveCruiseControl, HeatedSeats, VentilatedSeats, LeatherInterior, Sunroof, PanoramicRoof, Navigation, AppleCarPlayAndroidAuto, ParkingSensors, RearCamera, Camera360, KeylessEntry, XenonLed, MatrixHeadlights, AlloyWheels, TowBar, LaneAssist, BlindSpotMonitor, HeadUpDisplay, ElectricSeats, MemorySeats, HeatedSteeringWheel, AmbientLighting, PremiumSound, WirelessCharging, AutoParking, NightVision, AirSuspension, SportPackage, WinterTiresIncluded, IsofixMounts` |

### `realEstate` object (required when the category is property)

| Field | Type | Notes |
|-------|------|-------|
| `propertyType` | enum | `Apartment, House, Studio, Penthouse, Duplex, Villa, Land, Commercial, Office, Garage, Room, Warehouse, Building` |
| `transaction` | enum | `Sale`, `Rent`, `RentShortTerm` |
| `surfaceM2` | number | required |
| `rooms`, `floor`, `totalFloors`, `yearBuilt` | int | optional |
| `heating` | enum | `None, Central, OwnGasBoiler, Electric, HeatPump, Wood, District, UnderfloorHeating` |
| `furnished` | enum | `Unfurnished, PartiallyFurnished, Furnished, LuxuryFurnished` |
| `amenities` | enum[] | any of: `Balcony, Terrace, Garden, ParkingSpot, Garage, Elevator, AirConditioning, Basement, StorageRoom, SwimmingPool, Sauna, Gym, Security24h, VideoIntercom, Alarm, SmartHome, SolarPanels, EvCharger, Fireplace, PetsAllowed, WheelchairAccess, SeaView, MountainView, Dishwasher, WashingMachine, Internet, CableTv, NewBuilding, Renovated` |

### Example — car (category auto-detected)

```bash
curl -X POST https://aicigasesti.ro/api/listings \
  -H "X-Api-Key: $KEY" -H "Content-Type: application/json" -d '{
    "title": "BMW 320d 2019 xDrive M Sport, full options",
    "description": "Primul proprietar, 90.000 km reali, carte service, fără accidente.",
    "price": 18500, "currency": "EUR", "city": "Cluj-Napoca",
    "images": ["https://example.com/1.jpg", "https://example.com/2.jpg"],
    "car": { "year": 2019, "mileage": 90000, "fuel": "Diesel",
             "transmission": "Automatic", "body": "Sedan", "drive": "AllWheel",
             "powerHp": 190, "options": ["Navigation", "LeatherInterior", "HeatedSeats"] }
  }'
```

Response `201 Created`:
```json
{ "id": 128, "url": "https://aicigasesti.ro/l/128", "status": "Active",
  "categorySlug": "cars", "trustScore": 82 }
```

### Example — phone / electronics (no nested object)

```bash
curl -X POST https://aicigasesti.ro/api/listings \
  -H "X-Api-Key: $KEY" -H "Content-Type: application/json" -d '{
    "title": "iPhone 13 Pro 256GB impecabil",
    "description": "Full box, bateria 92%, fara zgarieturi, folosit cu grija.",
    "price": 2500, "currency": "RON", "city": "Bucuresti",
    "categorySlug": "iphone",
    "images": ["https://example.com/iphone-1.jpg", "https://example.com/iphone-2.jpg"]
  }'
```

Response `201 Created`:
```json
{ "id": 131, "url": "https://aicigasesti.ro/l/131", "status": "Active",
  "categorySlug": "iphone", "trustScore": 85 }
```

### Example — apartment (explicit category)

```bash
curl -X POST https://aicigasesti.ro/api/listings \
  -H "X-Api-Key: $KEY" -H "Content-Type: application/json" -d '{
    "title": "Apartament 3 camere de vânzare Mărăști",
    "description": "Renovat recent, mobilat modern, etaj 4, bloc nou.",
    "price": 132000, "city": "Cluj-Napoca", "categorySlug": "apartments-for-sale",
    "realEstate": { "propertyType": "Apartment", "transaction": "Sale", "rooms": 3,
                    "surfaceM2": 72, "floor": 4, "heating": "Central",
                    "furnished": "Furnished", "amenities": ["Balcony","Elevator","AirConditioning"] }
  }'
```

---

## GET /api/categories — the category map

Returns every category with its `id`, `slug`, `name`, `kind`, `parent` slug and the
nested object it **`requires`** (`"car"`, `"realEstate"`, or `null`). This is what you
map external (e.g. OLX) categories onto. A committed snapshot lives at
[`docs/categories.json`](docs/categories.json).

```bash
curl https://aicigasesti.ro/api/categories -H "X-Api-Key: $KEY"
```
```json
[
  { "id": 12,  "slug": "cars",                "name": "Cars",         "kind": "Cars",        "parent": "auto-moto-and-boats",       "requires": "car" },
  { "id": 21,  "slug": "apartments-for-sale", "name": "…",            "kind": "RealEstate",  "parent": "real-estate",               "requires": "realEstate" },
  { "id": 149, "slug": "iphone",              "name": "iPhone",       "kind": "Generic",     "parent": "mobile-phones",             "requires": null },
  { "id": 150, "slug": "samsung",             "name": "Samsung",      "kind": "Generic",     "parent": "mobile-phones",             "requires": null }
]
```

A simple mapper: for each source listing, resolve to a `slug`; if that category's
`requires` is `"car"`/`"realEstate"`, also emit that object; otherwise send just the
common fields.

## Moderation

Every created listing is screened. Clean ads go **live** (`status: "Active"`); ads with
flagged text (profanity, or dangerous/illegal signals) are returned as
`status: "PendingReview"` and held for admin approval — the `201` still succeeds, the
ad just isn't public yet.

---

## Staging import API (bulk, review-before-publish)

For feeding many externally-sourced rows that a human reviews before they go live.
Details and the collector client are in `scraper/README.md`.

| Endpoint | Purpose |
|----------|---------|
| `POST /api/import/listings` | upsert a batch into staging (deduped by `source`+`externalId`) |
| `GET /api/import/pending?take=&source=` | list rows awaiting review |
| `GET /api/import/stats` | counts by status and source |
| `POST /api/import/{id}/publish?activate=true` | promote a staged row to a listing |
| `POST /api/import/{id}/reject` | discard a staged row |

```bash
curl -X POST https://aicigasesti.ro/api/import/listings \
  -H "X-Api-Key: $KEY" -H "Content-Type: application/json" -d '{
    "source": "my-feed",
    "listings": [ { "externalId": "A1", "title": "…", "price": 100,
                    "categorySlug": "iphone", "images": ["https://…"] } ]
  }'
```

---

## Notes

- **Images** you pass as URLs are downloaded on the server through an SSRF-guarded
  fetcher (no loopback / private / cloud-metadata targets; http/https, size-capped).
  An image that can't be fetched is skipped; the listing still publishes.
- Enum values are **case-sensitive** string names as listed above.
- Only add listings you have the right to publish.
