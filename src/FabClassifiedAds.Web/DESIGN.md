---
name: AiciGăsești
description: Romanian classifieds where every ad carries a visible inspection result.
colors:
  document-green: "#0b6b4f"
  document-green-deep: "#08573f"
  check-cell-green: "#e7f1ec"
  check-line-green: "#a9cdbd"
  navy-ink: "#13233a"
  navy-ink-raised: "#1e3352"
  slate-muted: "#556274"
  on-ink-muted: "#b9c6d6"
  header-mint: "#7fd1ad"
  cool-page: "#f3f5f7"
  sheet-white: "#ffffff"
  sheet-tint: "#f6f8fa"
  hairline: "#d6dce3"
  hairline-strong: "#aeb9c6"
  photo-plate: "#e4e9ee"
  risk-red: "#c62828"
  risk-red-soft: "#fdeceb"
  risk-red-line: "#f0b4b0"
  caution-amber: "#8f5400"
  caution-amber-soft: "#fdf3e1"
  caution-amber-line: "#e9c98f"
  focus-blue: "#1f6feb"
typography:
  display:
    fontFamily: "Public Sans, system-ui, -apple-system, Segoe UI, Roboto, sans-serif"
    fontSize: "clamp(1.9rem, 3.4vw, 2.75rem)"
    fontWeight: 750
    lineHeight: 1.2
    letterSpacing: "-0.025em"
  headline:
    fontFamily: "Public Sans, system-ui, sans-serif"
    fontSize: "clamp(1.6rem, 2.6vw, 2.1rem)"
    fontWeight: 700
    lineHeight: 1.2
    letterSpacing: "-0.01em"
  title:
    fontFamily: "Public Sans, system-ui, sans-serif"
    fontSize: "clamp(1.25rem, 2vw, 1.5rem)"
    fontWeight: 700
    lineHeight: 1.2
    letterSpacing: "-0.01em"
  subtitle:
    fontFamily: "Public Sans, system-ui, sans-serif"
    fontSize: "1.05rem"
    fontWeight: 650
    lineHeight: 1.2
  body:
    fontFamily: "Public Sans, system-ui, sans-serif"
    fontSize: "16px"
    fontWeight: 400
    lineHeight: 1.5
  label:
    fontFamily: "Public Sans, system-ui, sans-serif"
    fontSize: "0.78rem"
    fontWeight: 750
    lineHeight: 1.2
    letterSpacing: "0.08em"
  price:
    fontFamily: "Public Sans, system-ui, sans-serif"
    fontSize: "1.85rem"
    fontWeight: 800
    lineHeight: 1.2
    letterSpacing: "-0.02em"
    fontFeature: "tnum"
rounded:
  sm: "4px"
  md: "6px"
  round: "50%"
spacing:
  xs: "4px"
  sm: "8px"
  md: "12px"
  lg: "16px"
  xl: "24px"
  section: "40px"
components:
  button-primary:
    backgroundColor: "{colors.document-green}"
    textColor: "{colors.sheet-white}"
    rounded: "{rounded.md}"
    padding: "10px 20px"
    height: "44px"
  button-primary-hover:
    backgroundColor: "{colors.document-green-deep}"
  button-ghost:
    backgroundColor: "{colors.sheet-white}"
    textColor: "{colors.navy-ink}"
    rounded: "{rounded.md}"
    padding: "10px 20px"
    height: "44px"
  button-ghost-hover:
    backgroundColor: "{colors.sheet-tint}"
  button-danger:
    backgroundColor: "{colors.sheet-white}"
    textColor: "{colors.risk-red}"
    rounded: "{rounded.md}"
    padding: "10px 20px"
  input:
    backgroundColor: "{colors.sheet-white}"
    textColor: "{colors.navy-ink}"
    rounded: "{rounded.md}"
    padding: "9px 12px"
    height: "42px"
  card:
    backgroundColor: "{colors.sheet-white}"
    rounded: "{rounded.md}"
    padding: "12px 14px 0"
  check-strip:
    backgroundColor: "{colors.check-cell-green}"
    textColor: "{colors.document-green-deep}"
    padding: "7px 14px"
  chip:
    backgroundColor: "{colors.sheet-white}"
    textColor: "{colors.navy-ink}"
    rounded: "{rounded.md}"
    padding: "7px 13px"
    height: "38px"
  trust-chip-good:
    backgroundColor: "{colors.check-cell-green}"
    textColor: "{colors.document-green-deep}"
    rounded: "{rounded.sm}"
    padding: "2px 8px"
  site-header:
    backgroundColor: "{colors.navy-ink}"
    textColor: "{colors.on-ink-muted}"
    height: "64px"
  stamp:
    textColor: "{colors.document-green}"
    rounded: "{rounded.round}"
    size: "104px"
---

# Design System: AiciGăsești

## Overview

**Creative North Star: "Fișa de inspecție"**

Every ad on AiciGăsești has been inspected, and the site reads like the paperwork that proves it: an official inspection sheet in the spirit of the Romanian RAR/ITP forms. White sheets lie on a cool grey-blue page. Navy ink carries the header bar, the headings and the body text. Document green means two things only: the primary action, and "verified". Red and amber appear only when something is actually wrong.

The density is that of a working marketplace, not a brochure. Grids are tight, information is ruled into cells with 1px hairlines, and figures (prices, km, years, scores) sit in tabular numerals so columns line up the way they would on a printed form. The one recurring artifact is the verification itself: a ruled check strip at the foot of every listing card, and a full inspection sheet with a round double-ring stamp on the homepage and the listing detail.

The system rejects the dark neon-gradient template and the bare OLX look where trust is invisible. There are no gradients, no glows, no emoji. The one sanctioned exception is the vertical video feed (`/feed`), which stays dark and immersive (see Do's and Don'ts).

**Key Characteristics:**
- White sheets on a cool page (#f3f5f7), ruled with 1px hairlines.
- Navy ink for structure and text; document green for action and "verified" only.
- Risk colors (red, amber) reserved for real warnings, always as soft fill + line + text triplets.
- Public Sans, self-hosted, one family, tabular figures for every number.
- 6px corners everywhere; 4px for small inner pieces; full circle only for the stamp, avatars and round icon buttons.
- Drawn 1.5px line icons on a 24px grid, `currentColor`, never emoji or glyphs.

## Colors

A document palette: paper whites, navy ink, one official green, and two warning inks held in reserve.

### Primary
- **Document Green** (`document-green`): primary buttons, the "Adaugă anunț" header action, links inside sheets, focus border on fields, checkbox accent, the VERIFICAT stamp. Also the word "verified".
- **Document Green Deep** (`document-green-deep`): hover for green actions; text on pale-green check cells.
- **Check Cell Green** (`check-cell-green`): fill of check cells (sheet ticks, check strip, good trust chips, flash messages, active conversation, import box, category tile hover).
- **Check Line Green** (`check-line-green`): hairline around green check cells and chips.

### Secondary
- **Navy Ink** (`navy-ink`): the full-width header bar, the footer, the CTA band, all headings and body text, the inspection sheet's frame and inner rules, active pagination/tabs, "Promovat" badges, own chat bubbles, code blocks.
- **Navy Ink Raised** (`navy-ink-raised`): hover and active fills on the navy bar (nav links, language switch), avatars.
- **Header Mint** (`header-mint`): the logo mark and the header search focus ring; green lifted for legibility on navy. Lives on the navy bar only.

### Tertiary (risk inks, reserved)
- **Risk Red** (`risk-red`) with **soft** fill and **line**: risky ads (score below 40), the RISC stamp, danger buttons, chat scam warnings, admin flags, validation errors, active favorite heart.
- **Caution Amber** (`caution-amber`) with **soft** fill and **line**: middling scores (40 to 74), "in review" badges, price warnings.

### Neutral
- **Cool Page** (`cool-page`): page background behind all sheets.
- **Sheet White** (`sheet-white`): cards, panels, sheets, inputs, the intro band.
- **Sheet Tint** (`sheet-tint`): sheet head strips, tags, ghost hover, seller section of the trust panel.
- **Hairline** (`hairline`): every ruled divider, card border, cell rule.
- **Hairline Strong** (`hairline-strong`): field borders, ghost button borders, card hover border, dashed empty states.
- **Slate Muted** (`slate-muted`): secondary text, meta lines, filter group labels.
- **On-Ink Muted** (`on-ink-muted`): resting nav and footer text on navy.
- **Photo Plate** (`photo-plate`): media wells behind photos and thumbnails.
- **Focus Blue** (`focus-blue`): the global 2px `:focus-visible` outline, deliberately distinct from document green.

### Named Rules
**The Green Means Checked Rule.** Document green marks either the primary action or a passed check. It never decorates.

**The Risk Ink Rule.** Red and amber appear only when the inspection found something. Each warning uses its triplet (soft fill, line, ink); never a bare red fill with white text.

**The No Gradient Rule.** Flat fills only. Emphasis comes from weight and ink, never gradients or glow.

## Typography

**Display Font:** Public Sans (variable, self-hosted woff2, latin + latin-ext for Romanian diacritics), with system-ui fallback
**Body Font:** Public Sans
**Label/Mono Font:** none distinct; labels are Public Sans uppercase

**Character:** A single civic sans, the kind used on government forms, carried across every role by weight (400 to 800) rather than by a second family. Tight negative tracking on large sizes, wide positive tracking on small uppercase sheet labels.

### Hierarchy
- **Display** (750, clamp(1.9rem, 3.4vw, 2.75rem), 1.2, -0.025em): the homepage and `/start` intro H1 only.
- **Headline** (700, clamp(1.6rem, 2.6vw, 2.1rem), 1.2): page H1 (search results, listing title, account pages). Listing titles cap at 30ch.
- **Title** (700, clamp(1.25rem, 2vw, 1.5rem), 1.2): section heads ("Categorii", "Anunțuri noi").
- **Subtitle** (650, 1.05rem): H3 inside panels.
- **Body** (400, 16px, 1.5): running text; descriptions and legal copy cap at 72ch, intro copy at 52ch.
- **Label** (750, 0.78rem, 0.08em, uppercase): the heading of a sheet or ruled group ("Verificare AiciGăsești", "Fișă de verificare", filter groups, spec cell terms, footer column heads). It names the block it heads; it is never a tagline above another heading.
- **Price** (800, 1.85rem on detail; 750, 1.14rem on cards; tabular): prices are the loudest figure on a card or detail page.

### Named Rules
**The Tabular Figures Rule.** Every price, km, year, cm³, hp, count and score uses `font-variant-numeric: tabular-nums`.

**The One Family Rule.** Public Sans carries every role. Hierarchy is weight and size, never a second face.

## Layout

A centered 1240px container with 20px gutters (860px for narrow forms and legal). Section rhythm is 40px block padding; inner spacing runs on 4 / 8 / 12 / 16 / 24px, with 18px used inside sheets and filter panels. The navy header is sticky at 64px; on screens up to 900px it wraps, the search drops to its own full-width row and the nav collapses behind a 44px toggle into a navy drop panel.

- **Homepage intro band:** white, two columns (1.25fr / 1fr, 48px gap): H1 + search + OLX import line left, inspection sheet right. Stacks at 960px.
- **Category grid:** a ruled table of tiles (shared hairlines, no gaps), 6 columns from 1100px, auto-fill 150px below, 2 columns under 560px with the tile turning horizontal.
- **Listing grid:** auto-fill 232px minimum, 16px gap; 4 fixed columns from 1100px on the homepage; 2 columns with 10px gap under 560px.
- **Search:** 290px sticky filter panel + results; single column at 920px.
- **Detail:** main column + 360px side column (inspection sheet, seller, share, safety). Under 960px the order becomes photos, then the inspection sheet, then everything else, so the check result comes before the seller's pitch.

Touch targets are at least 44px for buttons and nav, 36 to 42px for chips and pagination.

## Elevation & Depth

Mostly flat, ruled paper. Depth comes from the page/sheet contrast and 1px hairlines. A soft navy-tinted shadow is reserved for two jobs: lifting the inspection sheet (the one artifact that should feel like a document laid on the page) and responding to card hover. The stronger shadow belongs only to floating layers (mobile nav drop panel, cookie bar).

### Shadow Vocabulary
- **Sheet lift** (`box-shadow: 0 1px 2px rgba(19,35,58,.06), 0 4px 14px rgba(19,35,58,.07)`): inspection sheets at rest; listing cards on hover only.
- **Floating layer** (`box-shadow: 0 2px 4px rgba(19,35,58,.06), 0 12px 32px rgba(19,35,58,.12)`): mobile nav panel, cookie consent bar.
- **Pressed** (`box-shadow: inset 0 2px 4px rgba(0,0,0,.18)`): primary button active.

### Named Rules
**The Flat Card Rule.** Listing cards, panels and chips are flat at rest with a hairline border; shadow appears on cards only on hover. Only the inspection sheet is lifted at rest.

## Shapes

One corner: 6px on every button, field, card, panel, chip, sheet and map. 4px on small inner pieces (badges, tags, trust chips, sheet ticks, thumbnails, the search button inside the header field). Circles are reserved for the stamp, avatars, the favorite heart button and the feed rail buttons. Chat bubbles use 12px with a 3px tail corner, the one rounder shape, for conversational contrast.

Rules are 1px hairlines. The inspection sheet is the only container framed in navy ink (1px), with navy rules under its head and above its foot; everything else is framed in Hairline. Grids of cells (categories, specs, admin stats) share borders like a printed table rather than floating as separate tiles. Empty states and drop zones use a 1px dashed Hairline Strong.

## Components

### Buttons
Plain, firm and legible; a form's buttons, not a landing page's.
- **Shape:** 6px corners, 44px minimum height (50px large, 36px small), weight 650, 8px icon gap.
- **Primary:** Document Green fill, white text; hover Document Green Deep; active inset press.
- **Ghost:** white fill, Hairline Strong border, navy text; hover border goes navy and fill Sheet Tint.
- **Light:** white on navy bands (CTA band), white border.
- **Danger:** white fill, Risk Red text and line; hover fills Risk Red Soft.
- **Focus:** global 2px Focus Blue outline, 2px offset. Transitions 150ms on `cubic-bezier(.16,1,.3,1)`.

### Chips
- **Style:** white, Hairline border, 6px, weight 550, navy text (brand chips, subcategory chips, popular searches on Sheet Tint); hover border goes navy.
- **Trust chip:** 4px, tabular score, icon + "60/100" in the matching risk triplet (green / amber / red).
- **Badges on photos:** 4px, 0.7rem bold; white with hairline by default, navy for "Promovat", translucent navy for "Video", amber/red triplets for review states.

### Cards / Containers
- **Corner Style:** 6px.
- **Background:** Sheet White on Cool Page; photo wells in Photo Plate at 4:3.
- **Shadow Strategy:** flat at rest, sheet lift + Hairline Strong border on hover.
- **Border:** 1px Hairline.
- **Internal Padding:** 12px 14px (10px under 560px).
- **Listing card order:** category meta, two-line title, summary specs, price + heart, city + age, then the check strip.
- **No-photo plate:** ads without pictures show a generated flat plate (#e9edf1) with the double-ring check mark and the AiciGăsești wordmark in muted slate; never a stock image or a generic icon.

### Inputs / Fields
- **Style:** white, 1px Hairline Strong, 6px, 42px min height, 9px 12px padding.
- **Focus:** border turns Document Green with a 3px ring at 18% green. Composite search bars (homepage, `/start`) frame several fields in one 6px border with hairline separators and a flush green button; the whole bar takes the focus ring.
- **Error:** Risk Red border and text.
- **Header search:** white field on navy, Header Mint focus ring, green square search button inset 3px.

### Navigation
- **Header:** full-width Navy Ink bar, 64px. Wordmark (mint check mark, "Aici" 750 + "Găsești" 400), search, RO|EN switch, icon + label nav links in On-Ink Muted (labels from 1100px), hover white on Navy Ink Raised, green "Adaugă anunț" at the end.
- **Footer:** navy, four columns with uppercase label heads, links in On-Ink Muted turning white and underlined on hover.
- **Pagination / tabs:** white 6px cells with hairline; active is navy fill, white text.

### Inspection Sheet (signature)
The site's one recurring artifact. A white sheet framed in 1px navy with sheet lift; a Sheet Tint head strip carrying the uppercase label and a muted reference on the right; ruled rows each led by a 26px pale-green tick cell; a navy-ruled foot. On the detail page it becomes the trust panel: score in large tabular figures colored by result, a price-vs-market box in its risk triplet, ruled check lines with line icons (check / x / alert / info), and a Sheet Tint seller section. A risky result reframes the panel in Risk Red.

### Stamp (signature)
A round double-ring rubber stamp, rotated -9°: 3px outer ring and a 1px inner ring inset 5px, translucent white fill, three stacked lines ("VERIFICAT" or "RISC", the score, "AICIGĂSEȘTI"). Green for good, amber for caution, red for risk. 104px on the homepage sheet (overhanging its bottom-right corner), 88px in the detail trust head and on mobile.

### Check Strip (signature)
The ruled foot of every listing card: a full-bleed row above a hairline, icon + uppercase result word + tabular "95/100", in the result's soft fill and ink (green VERIFICAT, amber ATENȚIE, red RISC RIDICAT). Every card has one; it is how the inspection becomes visible in browsing.

## Do's and Don'ts

### Do:
- **Do** put the inspection result where the decision is made: a check strip on every card, the inspection sheet before the seller's pitch on detail.
- **Do** frame grouped facts as ruled cells sharing 1px hairlines, like a printed form.
- **Do** use tabular figures for every price, km, year and score.
- **Do** use the Icons helper (24px grid, 1.5px stroke, `currentColor`) for every icon; 20px default, 16px small, 28 to 30px for category tiles.
- **Do** use the risk triplet (soft fill, line, ink) for every warning state.
- **Do** keep the vertical video feed (`/feed`) dark and immersive: black ground, full-height snap items, a bottom shade over the video for caption legibility, 10px-rounded media on desktop, round translucent-navy rail buttons that turn green on hover. This is the one sanctioned departure from the white-sheet world.

### Don't:
- **Don't** use gradients, glows or neon anywhere outside the feed's caption shade.
- **Don't** use green for decoration, or red and amber for anything that is not a real warning.
- **Don't** use emoji or text glyphs as icons.
- **Don't** add a second typeface.
- **Don't** put an uppercase tagline above a heading; uppercase labels only head the sheet or ruled group they belong to.
- **Don't** lift cards or panels with shadow at rest; only the inspection sheet is lifted.
- **Don't** invent scale or social proof (user counts, testimonials, ratings) in any sheet, band or badge.
