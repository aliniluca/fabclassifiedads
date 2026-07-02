#!/usr/bin/env python3
"""
Entrypoint: crawl an authorized source, normalize, and push to the import API.

    python run.py --source my-feed --seed https://example.com/authorized/listings

Wire your own parser module (see parsers/example_source.py). Only point this at
sources you are permitted to collect from — see README.md.
"""
from __future__ import annotations

import argparse
import asyncio
import logging

from aicigasesti_import import ImportClient, PoliteCrawler
from parsers import example_source as parser   # swap for your authorized-source parser

logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(name)s: %(message)s")
log = logging.getLogger("run")


async def collect(seed: str, *, max_items: int, min_delay: float) -> list:
    crawler = PoliteCrawler(min_delay=min_delay, concurrency=2)
    listings = []
    try:
        index_html = await crawler.fetch(seed)
        if not index_html:
            log.error("Could not fetch seed page (blocked, disallowed, or error). Stopping.")
            return []

        detail_urls = parser.parse_list_page(index_html, seed)[:max_items]
        log.info("Found %d detail URLs", len(detail_urls))

        for url in detail_urls:
            html = await crawler.fetch(url)
            if not html:
                continue
            item = parser.parse_detail_page(html, url)
            if item:
                listings.append(item)
    finally:
        await crawler.close()
    return listings


def main() -> None:
    ap = argparse.ArgumentParser(description="Collect listings and push to the AiciGăsești import API.")
    ap.add_argument("--source", required=True, help="source tag stored on each row, e.g. 'my-feed'")
    ap.add_argument("--seed", required=True, help="index/listing page URL to start from")
    ap.add_argument("--max-items", type=int, default=50)
    ap.add_argument("--min-delay", type=float, default=2.0, help="minimum seconds between requests")
    ap.add_argument("--dry-run", action="store_true", help="parse only; do not push")
    args = ap.parse_args()

    listings = asyncio.run(collect(args.seed, max_items=args.max_items, min_delay=args.min_delay))
    log.info("Normalized %d listings", len(listings))

    if args.dry_run:
        for l in listings[:5]:
            log.info("  %s | %s | %s", l.external_id, l.title, l.price)
        return

    with ImportClient(source=args.source) as client:
        summary = client.push_in_batches(listings)
    log.info("Import summary: %s", summary)


if __name__ == "__main__":
    main()
