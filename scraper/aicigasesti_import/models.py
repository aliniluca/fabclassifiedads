"""The normalized listing shape the AiciGăsești import API accepts."""
from __future__ import annotations

from dataclasses import dataclass, field, asdict


@dataclass
class NormalizedListing:
    external_id: str                       # stable id in the source system
    title: str
    source_url: str | None = None
    description: str | None = None
    price: float | None = None
    currency: str | None = "EUR"
    city: str | None = None
    region: str | None = None
    category_slug: str | None = None       # must match a category slug on the server
    images: list[str] = field(default_factory=list)
    attributes: dict[str, str] = field(default_factory=dict)

    def to_api(self) -> dict:
        """Map snake_case fields to the API's camelCase DTO, dropping empties."""
        d = {
            "externalId": self.external_id,
            "sourceUrl": self.source_url,
            "title": self.title,
            "description": self.description,
            "price": self.price,
            "currency": self.currency,
            "city": self.city,
            "region": self.region,
            "categorySlug": self.category_slug,
            "images": self.images or None,
            "attributes": self.attributes or None,
        }
        return {k: v for k, v in d.items() if v is not None}
