"""
PoliteCrawler — a deliberately well-behaved fetcher.

It identifies itself honestly, obeys robots.txt (including Crawl-delay), keeps
concurrency low and rate-limits requests, and backs off on transient errors.
It does NOT try to defeat bot protection: a 403/429 is treated as "you are not
welcome here", logged, and respected — not worked around.
"""
from __future__ import annotations

import asyncio
import logging
import time
from urllib.parse import urlparse
from urllib.robotparser import RobotFileParser

import httpx

log = logging.getLogger("crawler")

# Honest, identifiable agent. Put a real contact address here so a site owner can
# reach you (and allow/deny you) instead of silently blocking.
USER_AGENT = "AiciGasestiBot/1.0 (+https://aicigasesti.ro/bot; contact=abuse@aicigasesti.ro)"


class PoliteCrawler:
    def __init__(self, *, min_delay: float = 2.0, concurrency: int = 2, respect_robots: bool = True):
        self.min_delay = min_delay
        self.respect_robots = respect_robots
        self._sem = asyncio.Semaphore(concurrency)
        self._robots: dict[str, RobotFileParser] = {}
        self._last_hit: dict[str, float] = {}
        self._client = httpx.AsyncClient(
            headers={"User-Agent": USER_AGENT},
            timeout=30,
            follow_redirects=True,
        )

    async def _robots_for(self, url: str) -> RobotFileParser | None:
        if not self.respect_robots:
            return None
        origin = "{0.scheme}://{0.netloc}".format(urlparse(url))
        if origin not in self._robots:
            rp = RobotFileParser()
            try:
                r = await self._client.get(f"{origin}/robots.txt")
                rp.parse(r.text.splitlines() if r.status_code == 200 else [])
            except httpx.HTTPError:
                rp.parse([])  # no robots reachable -> treat as allowed but stay slow
            self._robots[origin] = rp
        return self._robots[origin]

    async def allowed(self, url: str) -> bool:
        rp = await self._robots_for(url)
        return True if rp is None else rp.can_fetch(USER_AGENT, url)

    async def _throttle(self, url: str) -> None:
        host = urlparse(url).netloc
        rp = await self._robots_for(url)
        delay = self.min_delay
        if rp is not None:
            cd = rp.crawl_delay(USER_AGENT)
            if cd:
                delay = max(delay, float(cd))
        elapsed = time.monotonic() - self._last_hit.get(host, 0.0)
        if elapsed < delay:
            await asyncio.sleep(delay - elapsed)
        self._last_hit[host] = time.monotonic()

    async def fetch(self, url: str, *, retries: int = 3) -> str | None:
        """Fetch a page politely. Returns HTML, or None if disallowed/blocked/failed."""
        if not await self.allowed(url):
            log.warning("robots.txt disallows %s — skipping", url)
            return None

        async with self._sem:
            for attempt in range(1, retries + 1):
                await self._throttle(url)
                try:
                    r = await self._client.get(url)
                except httpx.HTTPError as e:
                    log.warning("network error on %s (%s/%s): %s", url, attempt, retries, e)
                    await asyncio.sleep(2 ** attempt)
                    continue

                if r.status_code == 200:
                    return r.text
                if r.status_code in (403, 401):
                    log.warning("%s returned %s — access denied, not circumventing. Skipping.",
                                url, r.status_code)
                    return None
                if r.status_code == 429:
                    wait = float(r.headers.get("Retry-After", 30))
                    log.warning("rate-limited on %s — backing off %.0fs", url, wait)
                    await asyncio.sleep(wait)
                    continue
                if 500 <= r.status_code < 600:
                    await asyncio.sleep(2 ** attempt)
                    continue
                log.warning("%s returned %s — skipping", url, r.status_code)
                return None
        return None

    async def close(self) -> None:
        await self._client.aclose()
