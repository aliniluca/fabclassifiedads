# AiciGăsești — collector (ingestion client)

A small, **polite** crawler + client that normalizes listings from a source you are
**authorized to access** and pushes them into the AiciGăsești import API
(`POST /api/import/listings`). Imported rows land in a staging table and stay
**invisible** until a human reviews and publishes them.

## ⚠️ Read before pointing this at any website

This tool does **not** bypass bot protection, rotate proxies, spoof fingerprints or
solve CAPTCHAs — on purpose. Before you crawl a source, make sure you are allowed to:

- **Terms of Service** — most marketplaces (OLX included) prohibit automated collection.
  Crawling them anyway can get you blocked and is a contract breach.
- **Copyright / database rights** — listing text and photos belong to the people who
  posted them (and/or the platform). Re-publishing them on another site is infringement
  unless you have a licence.
- **GDPR** — seller names, phone numbers and photos are *personal data*. Republishing
  them without a lawful basis and without informing the data subjects is a serious
  violation, with real fines. Do **not** import personal contact data.

Legitimate sources this pipeline is built for: your **own** inventory, a **partner
feed / official API**, an open dataset, or a site whose ToS and robots.txt permit
automated access. The crawler honors `robots.txt` and rate limits by default.

## Install

```bash
cd scraper
python3 -m venv .venv && . .venv/bin/activate
pip install -r requirements.txt
```

## Configure

```bash
export IMPORT_API_URL="https://aicigasesti.ro"     # or http://127.0.0.1:5099 in dev
export IMPORT_API_KEY="the-same-key-as-the-server" # server: IMPORT_API_KEY env
```

## Run the example

`run.py` wires a source parser to the crawler and the API client. Implement your own
parser in `parsers/` for a source you're authorized to use, then:

```bash
python run.py --source my-feed --seed https://example.com/authorized/listings
```

## Layout

```
scraper/
├── run.py                       # entrypoint: crawl → normalize → push
├── requirements.txt
├── aicigasesti_import/
│   ├── client.py                # ImportClient — batches POSTs to the .NET API
│   ├── crawler.py               # PoliteCrawler — robots.txt + rate limit + retries
│   └── models.py                # NormalizedListing dataclass (the import DTO)
└── parsers/
    └── example_source.py        # template parser — fill in per authorized source
```
