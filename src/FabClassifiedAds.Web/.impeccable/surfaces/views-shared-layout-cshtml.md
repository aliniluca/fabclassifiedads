---
version: 1
slug: "views-shared-layout-cshtml"
primary_target: "Views/Shared/_Layout.cshtml"
related_targets: ["Views/Home/Index.cshtml","Views/Shared/_ListingCard.cshtml","Views/Listings/Details.cshtml","wwwroot/css/site.css"]
---

# Surface brief: whole site (shell, homepage, listing card, listing detail)

Scope: site-wide visual world replacement. Mode: Operate (marketplace app: search, browse, post, chat); the homepage is an Operate entry with light persuasion. Audience: Romanian buyers and OLX/Autovit sellers, mostly on phones in daylight. Confirmed by the user: benchmark is the big marketplaces (OLX, Autovit, Storia, mobile.de); nothing visual is sacred; fix the homepage content issues from the 2026-10-12 critique in the same pass (no fake scale, no unbacked "verified sellers", OLX import visible early, trust score explained).

## Direction contract

THESIS: Every ad is inspected, and the result reads like an official inspection sheet (fișa de inspecție RAR/ITP). "Verificare AiciGăsești" is the one recurring artifact. Refuses the dark neon-gradient AI template and the bare OLX look where trust is invisible.

OWN-WORLD: White sheets on a cool grey-blue page (#f3f5f7); navy ink #13233a carries the full-width header bar, headings and body text; document green #0b6b4f is the primary action and "verified"; pale green #e7f1ec fills check cells; risk red #c62828 and amber #9a5b00 only for warnings. 1px ruled hairlines #d6dce3, radius 6px, no gradients, no glow. Public Sans, self-hosted, tabular figures for prices, km, years, scores. Drawn 1.5px line icons, never emoji. Signature: the ruled check strip at the foot of every card and the full inspection sheet with a round double-ring VERIFICAT stamp on the detail page.

STORY: A buyer sees real ads with their check result visible and understands in one line what is checked; a seller from OLX sees "paste your link" before scrolling and posts in a minute.

FIRST VIEWPORT: Navy header bar: wordmark left, search + location + Caută centre, Favorite/Mesaje/Cont + green "Adaugă anunț" right, RO|EN. Under it a white band: left an H1 "Anunțuri verificate, din toată țara" with search and city, plus the OLX import line; right a "Fișă de verificare" sheet explaining the four checks with the stamp. Below: 11 category tiles with line icons, then the latest ads grid with check strips.

FORM: Fișa de inspecție, position 1 on the ordered list (my pick, chosen by the user over the rolled direction). seed: none (unavailable)

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
