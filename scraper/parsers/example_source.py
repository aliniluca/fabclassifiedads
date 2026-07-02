"""
Template parser — copy this per source you are AUTHORIZED to collect from.

A parser turns a fetched HTML page into NormalizedListing objects. This example
uses selectolax (CSS selectors); adapt the selectors to the source's markup.

Do NOT import personal contact data (names, phone numbers). Map only the listing
facts you have the right to republish, and set a real `category_slug` that exists
on the server (see the seeded category tree).
"""
from __future__ import annotations

from urllib.parse import urljoin

from selectolax.parser import HTMLParser

from aicigasesti_import import NormalizedListing


def parse_list_page(html: str, base_url: str) -> list[str]:
    """Return absolute detail-page URLs found on a listing/index page."""
    doc = HTMLParser(html)
    urls: list[str] = []
    for a in doc.css("a.listing-link"):            # <-- adjust selector
        href = a.attributes.get("href")
        if href:
            urls.append(urljoin(base_url, href))
    return urls


def parse_detail_page(html: str, url: str) -> NormalizedListing | None:
    """Return one NormalizedListing from a detail page, or None if unparseable."""
    doc = HTMLParser(html)

    def text(sel: str) -> str | None:
        node = doc.css_first(sel)
        return node.text(strip=True) if node else None

    title = text("h1.title")                        # <-- adjust selectors
    if not title:
        return None

    # derive a stable external id from the URL (or a data attribute on the page)
    external_id = url.rstrip("/").rsplit("/", 1)[-1]

    price_raw = text(".price")
    price = None
    if price_raw:
        digits = "".join(ch for ch in price_raw if ch.isdigit())
        price = float(digits) if digits else None

    images = [
        urljoin(url, img.attributes.get("src", ""))
        for img in doc.css("img.photo")             # <-- adjust selector
        if img.attributes.get("src")
    ]

    return NormalizedListing(
        external_id=external_id,
        source_url=url,
        title=title,
        description=text(".description"),
        price=price,
        currency="RON",
        city=text(".location"),
        category_slug="electronics-and-appliances",  # <-- map to a real server slug
        images=images,
        # attributes: only non-personal listing facts, e.g. {"condition": "used"}
    )
