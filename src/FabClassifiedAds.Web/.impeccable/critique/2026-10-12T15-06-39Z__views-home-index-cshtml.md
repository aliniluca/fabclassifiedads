---
target: Views/Home/Index.cshtml
total_score: 20
max_score: 40
na_heuristics: 
p0_count: 1
p1_count: 3
target_identity: "file:C:\\Users\\alini\\source\\repos\\aliniluca\\fabclassifiedads\\src\\FabClassifiedAds.Web\\Views\\Home\\Index.cshtml"
target_fingerprint: "sha256:9291f1d09e2f2058bc6d2d6fdc8fe7d5b1ffab42cd665d1b1f630f32d9e71b3d"
target_path: "C:\\Users\\alini\\source\\repos\\aliniluca\\fabclassifiedads\\src\\FabClassifiedAds.Web\\Views\\Home\\Index.cshtml"
timestamp: 2026-10-12T15-06-39Z
slug: views-home-index-cshtml
---
Method: dual-agent (A: design review · B: detector + browser evidence). Browser inspection skipped: 3 Chrome browsers connected, none selected; review from rendered HTML (curl) + source; no overlay.

## Design Health Score — 20/40 (Acceptable)
| # | Heuristic | Score | Key Issue |
|---|---|---|---|
| 1 | Visibility of System Status | 2 | "25.000+ anunțuri" vs "46 anunțuri active" in first screen |
| 2 | Match System / Real World | 2 | English categories/cities w/o diacritics in RO; literal copy |
| 3 | User Control and Freedom | 3 | RO/EN keeps URL |
| 4 | Consistency and Standards | 2 | Featured duplicated in Noutăți; two searches; gradient overloaded |
| 5 | Error Prevention | 2 | Scam ad (🛡 5) in Noutăți without warning |
| 6 | Recognition Rather Than Recall | 3 | Categories, brands, chips visible |
| 7 | Flexibility and Efficiency | 2 | No location in hero search |
| 8 | Aesthetic and Minimalist Design | 2 | 7 sections, categories twice, gradient everywhere |
| 9 | Error Recovery | 1 | Risk score unlabeled; tooltip only |
| 10 | Help and Documentation | 1 | Trust score never explained |

## Design Specificity Verdict
Category-interchangeable dark "AI startup" template: --grad (site.css:17) on h1/logo/prices/buttons/badges/CTA, glow-blob hero, stock section sequence, emoji icon system. Trust identity reduced to "🛡 95" chip. Detector: 1 finding, overused-font (_Layout.cshtml:13, Plus Jakarta Sans); Index.cshtml clean (static scan blind to site.css).

## Priority Issues
- [P0] Fabricated/contradicted scale claim: placeholder "25.000+" (_Layout.cshtml:23, Translator.cs:110) vs hero "46 active / 291 categorii" (Index.cshtml:10); footer superlative. Fix: honest placeholder, drop/threshold count, replace superlative. → clarify
- [P1] "Vânzători verificați" (Index.cshtml:112) promises verification that isn't modeled; ".NET 11" card (Index.cshtml:115) is a dev brag. Fix: describe real Verificare AiciGăsești checks. → clarify
- [P1] Trust differentiator invisible; scam proof unexplained (_ListingCard.cshtml:38). Fix: label chip, "Cum te protejăm" strip under search, scam ad as worked example. → bolder/onboard
- [P1] Seller path buried; header "+ Adaugă anunț" → /post not /start (_Layout.cshtml:57). Fix: seller line under hero, import at top of /post, move CTA band up. → layout
- [P2] Hero search lacks location; duplicate search on mobile; outline:none without replacement (site.css:138). → adapt

## Persona Red Flags
Jordan: unlabeled 🛡 scores, unexplained "★ Promovat", "291 categorii" vs 11 tiles, "Vinde tot", "🎬 Feed".
Casey: non-sticky mobile header (site.css:107), top-right hamburger, single-column at 390px, ~30px chips, cookie bar over fold.
Riley: empty states unguarded except Featured (Index.cshtml:57); "acum 1 zile" (Display.cs:42); overlapping badges; hardcoded "În tendințe".
Ioana (OLX seller): no seller cue above fold; header goes to /post; "Mută" implies deleting from OLX; no "free / keep your OLX ad".

## Minor Observations
--primary #6d5dfc on #0e0f17 ≈4.2:1 fails AA; gradient-clipped prices; missing diacritics in cities; Latest not excluding featured (HomeController.cs:22); emoji in select options; ★/🔍 not aria-hidden; "Menu" aria-label unlocalized; HomeTop banner in prime slot.

## Questions to Consider
- Why isn't the 5/100 scam catch the hero proof?
- Buyer-first or seller-first above the fold at 46 listings?
- Does the neon-gradient emoji look read as "crypto app" to a Romanian buyer?
