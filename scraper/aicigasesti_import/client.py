"""Client for the AiciGăsești import API (POST /api/import/listings)."""
from __future__ import annotations

import os
import httpx

from .models import NormalizedListing


class ImportClient:
    def __init__(self, base_url: str | None = None, api_key: str | None = None, source: str = "unknown"):
        self.base_url = (base_url or os.environ["IMPORT_API_URL"]).rstrip("/")
        self.api_key = api_key or os.environ["IMPORT_API_KEY"]
        self.source = source
        self._client = httpx.Client(timeout=30)

    def push(self, listings: list[NormalizedListing]) -> dict:
        """Send a batch. Returns the server's {received, inserted, updated, ...} summary."""
        if not listings:
            return {"received": 0, "inserted": 0, "updated": 0, "unchanged": 0, "errors": []}
        payload = {"source": self.source, "listings": [l.to_api() for l in listings]}
        r = self._client.post(
            f"{self.base_url}/api/import/listings",
            json=payload,
            headers={"X-Import-Key": self.api_key},
        )
        r.raise_for_status()
        return r.json()

    def push_in_batches(self, listings: list[NormalizedListing], batch_size: int = 100) -> dict:
        totals = {"received": 0, "inserted": 0, "updated": 0, "unchanged": 0, "errors": []}
        for i in range(0, len(listings), batch_size):
            res = self.push(listings[i : i + batch_size])
            for k in ("received", "inserted", "updated", "unchanged"):
                totals[k] += res.get(k, 0)
            totals["errors"] += res.get("errors", [])
        return totals

    def close(self) -> None:
        self._client.close()

    def __enter__(self):
        return self

    def __exit__(self, *_):
        self.close()
