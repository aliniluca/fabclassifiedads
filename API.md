# AiciGăsești — API

Token-protected HTTP API for adding listings programmatically, plus the staging
import API. JSON in, JSON out. Enum fields accept their **string names** (e.g.
`"Diesel"`).

Base URL: `https://aicigasesti.ro`

---

## Authentication

Every `/api/*` endpoint requires a shared secret in a header:

```
X-Api-Key: <your-key>
```

The key is read from the `IMPORT_API_KEY` environment variable (or `Api:Key` in
config). If no key is configured the API stays **locked** (every call returns `401`).
Set it at deploy time — it flows into the systemd service automatically:

```bash
sudo IMPORT_API_KEY="a-long-random-secret" ./scripts/deploy.sh
```

Responses: `401 Unauthorized` (missing/wrong key), `400`/`422` (validation),
`201 Created` (success).

---

## POST /api/listings — create a listing

Creates a live listing (owned by a system "API" account). Returns `201` with the
new id and public URL.

### Body

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `title` | string | ✅ | 8–120 chars |
| `description` | string | ✅ | 10–8000 chars |
| `price` | number | — | omit for "ask for price"; ignored if `isFree` |
| `currency` | string | — | `EUR` (default), `USD`, `RON` |
| `isNegotiable` | bool | — | default `true` |
| `isFree` | bool | — | default `false` |
| `categorySlug` | string | — | **omit to auto-detect** from title+description (see GET /api/categories) |
| `city` | string | ✅ | |
| `region` | string | — | |
| `condition` | enum | — | `New`, `Used` (default), `Refurbished`, `ForParts` |
| `active` | bool | — | `true` (default) = live; `false` = hidden Draft |
| `images` | string[] | — | absolute URLs; downloaded server-side (SSRF-guarded), max 10 |
| `car` | object | for car categories | see below |
| `realEstate` | object | for real-estate categories | see below |

**Auto-detection**: if `categorySlug` is omitted, the category is guessed from the
text (car brands + keywords). For cars it also fills brand/model when recognized.
If nothing matches, the call returns `422` asking you to pass `categorySlug`.

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

## GET /api/categories — list category slugs

Returns every category with its slug, name, kind and parent slug — use it to pick a
valid `categorySlug`.

```bash
curl https://aicigasesti.ro/api/categories -H "X-Api-Key: $KEY"
```
```json
[ { "slug": "cars", "name": "Cars", "kind": "Cars", "parent": "auto-moto-and-boats" }, … ]
```

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
