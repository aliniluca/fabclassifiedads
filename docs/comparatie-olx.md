# Comparație de produs: OLX.ro vs. AiciGăsești.ro

*Analiză realizată pe 2026-10-12, pe baza navigării pe cele două site-uri live (WebFetch / descărcare HTML) și a unor surse publice despre OLX. Unde o funcție nu a putut fi văzută direct, este marcată „neverificat”.*

---

## 1. Scurt rezumat

1. **Lipsa de conținut e problema nr. 1.** AiciGăsești are live **un singur anunț** (Jaguar XF, Constanța). Toate categoriile verificate (Imobiliare, Electronice, Locuri de muncă etc.) afișează „0 anunțuri”, așa că funcțiile bune nu se văd. OLX afișează „peste 1.000 de rezultate” pe orice categorie.
2. **Nu există tranzacție cu livrare și plată protejată.** OLX are „Livrare prin OLX” (FAN Courier, de la 14,99 lei), plată online cu cardul sau la livrare prin PayU și „Siguranța Achiziției” (retur în 24–48 h). AiciGăsești nu are nimic din toate acestea.
3. **Nu există ratinguri verificate.** OLX permite evaluarea vânzătorului după o comandă cu livrare. AiciGăsești afișează doar „Fără recenzii încă”, iar recenziile nu sunt legate de nicio tranzacție.
4. **Nu există monetizare.** OLX vinde reactualizări, promovare în listă și pe prima pagină (pachete Start/Standard/Pro), pachete pentru firme și pagini de business. Pe AiciGăsești „Featured” se setează doar din admin.
5. **Pagina anunțului e mai săracă:**
   - telefonul nu apare pe anunțul live, deși la înscriere e obligatoriu;
   - nu există buton „Raportează”;
   - nu poți salva la favorite fără cont;
   - nu există hartă cu locația și nici JSON-LD.
6. **Lipsesc conținutul de suport și aplicația:** nu există pagini de Ajutor, Contact, Despre noi sau Cum funcționează, nu există aplicație mobilă/PWA, iar 404-ul e o pagină goală.
7. **Căutarea are goluri față de OLX:**
   - prețul se filtrează doar în €, fără comutator lei/€;
   - lipsesc filtrele „Doar cu poze” și „Numai cu livrare”;
   - lipsesc numărul de anunțuri pe județ/subcategorie și „Căutări similare”;
   - pe Locuri de muncă apar filtre nepotrivite (stare „Utilizat”/„Pentru piese”).

---

## 2. Ce am verificat

Data verificării: **2026-10-12**.

### aicigasesti.ro
| URL | Ce am văzut |
|---|---|
| https://aicigasesti.ro/ | Hero cu căutare (ce + oraș/județ), secțiunea „Verificare AiciGăsești”, 11 categorii, „Anunțuri noi” (1 anunț), mărci auto, banner pentru import OLX/Autovit |
| https://aicigasesti.ro/search?Sort=Newest și /search | „1 anunțuri”, filtre: preț €, locație, vânzător, stare, negociabil; sortări; link „Hartă” |
| https://aicigasesti.ro/search?Q=iphone | 0 anunțuri |
| https://aicigasesti.ro/c/cars | Filtre auto foarte detaliate (marcă/model, an, km, combustibil, cutie, caroserie, tracțiune, cm³, CP, Euro, fără accidente, carte service, ~37 dotări); 6 sortări; 1 anunț |
| https://aicigasesti.ro/c/real-estate | 12 subcategorii, filtre imobiliare detaliate (tip, camere, m², etaj, an, încălzire, mobilare, 26+ facilități); 0 anunțuri |
| https://aicigasesti.ro/c/electronics-and-appliances | 9 subcategorii, doar filtrele generice; 0 anunțuri |
| https://aicigasesti.ro/c/jobs | 15 subcategorii, filtre generice de produs (preț €, stare Nou/Utilizat/Pentru piese); 0 anunțuri |
| https://aicigasesti.ro/c/services, /c/animals | HTTP 200 (fără anunțuri) |
| https://aicigasesti.ro/anunt/autoturisme/jaguar-xf-2014-2-2-diesel-160-cai-488 | **Singurul anunț live.** Galerie cu 6 poze, preț „negociabil”, specificații, dotări, descriere, fișă de verificare 100/100, vânzător 70/100, „Conectează-te pentru a contacta”, share link + card 9:16, sfaturi de siguranță |
| https://aicigasesti.ro/l/488 | Redirect 301 către URL-ul SEO (corect) |
| https://aicigasesti.ro/u/6d71a3f9-…/alin | Profil public: nume, „1 anunțuri active”, oraș, „Membru din October 2026”, scor 70, sortări, listă de anunțuri |
| https://aicigasesti.ro/start | Import din link OLX/Autovit + formular rapid de publicare cu creare de cont (titlu, preț, monedă EUR/USD/RON, oraș, categorie, nume, telefon, email, parolă) |
| https://aicigasesti.ro/register | Nume, email, telefon, oraș, parolă, „Sunt firmă / dealer”, acord termeni |
| https://aicigasesti.ro/login, /forgot-password | Email + parolă, „Ține-mă conectat”, „Ai uitat parola?”; fără login social |
| https://aicigasesti.ro/feed | Feed vertical cu singurul anunț („Glisează sau derulează”) |
| https://aicigasesti.ro/map | Hartă Leaflet („Toate anunțurile pe hartă”) |
| /favorites, /messages | Redirect la login (corect) |
| /set-language?culture=en | Versiunea EN a paginii principale |
| /sitemap.xml, /robots.txt | Sitemap cu 293 URL-uri; robots.txt conține doar comentarii Cloudflare, fără nicio directivă |
| /help, /ajutor, /contact, /despre, /about | 404 |
| /favicon.ico, /manifest.json, /site.webmanifest | 404 |
| /anunt/x/nu-exista-99999 | 404 cu corp gol |

### olx.ro
| URL | Ce am văzut |
|---|---|
| https://www.olx.ro/ | Header (Chat, Anunțuri salvate, Notificări, Contul tău, Adaugă anunț nou), 16 categorii, „Anunțuri promovate”, banner Business, link-uri spre aplicații, footer bogat (ajutor, livrare, promovare, ANPC, sitemap pe județe și mini-site-uri) |
| https://www.olx.ro/auto-masini-moto-ambarcatiuni/autoturisme/ | Filtre (caroserie, preț, an, rulaj, combustibil, cutie, cm³, putere, culoare, volan, stare, „Doar cu poze”), comutator lei/€, sortări, „Salvează căutarea”, paginare, text SEO, anunțuri Autovit amestecate |
| https://www.olx.ro/imobiliare/apartamente-garsoniere-de-inchiriat/ | Filtre (compartimentare, preț, suprafață, an, etaj, Firmă/Privat, camere 1–4+ cu număr de anunțuri), număr de anunțuri pe județ, „Salvează căutarea”, anunțuri Storia |
| https://www.olx.ro/electronice-si-electrocasnice/telefoane-mobile/ | „Numai cu livrare”, „Doar cu poze”, stare, preț, garanție, lei/€, „Vânzător de X ani” pe card |
| https://www.olx.ro/electronice-si-electrocasnice/telefoane/ | 404 (URL greșit, nu bug OLX) |
| https://www.olx.ro/oferte/q-bicicleta-electrica/ | Căutare text cu sugestii de categorii (cu număr de anunțuri), număr de anunțuri pe județ, „Căutări similare”, „Numai cu livrare” |
| https://www.olx.ro/d/oferta/dacia-logan-2019-0-9-benzina-euro-6-IDkZD6s.html | Anunț auto: 8 poze, breadcrumb până la sector, parametri, „Trimite mesaj”, telefon mascat + „Arată”, „Pe OLX din”, „Activ pe…”, ID, „Raportează” |
| https://www.olx.ro/d/oferta/iphone-13-128gb-baterie-100-impecabil-ca-nou-IDkZofU.html | Anunț electronice (persoană fizică): „Activ azi la 13:49”, „Raportează”, telefon mascat |
| https://www.olx.ro/d/oferta/magazin-google-pixel-9a-obsidian-…-IDkU2Tt.html | Anunț electronice (firmă): parametri „Firma”, „Garantie”, „Livrare cu verificare”, „Cumpărături sigure, retururi simple”, retur în 14 zile de la firme |
| https://www.olx.ro/d/oferta/inchiriez-apartament-3camere-…-IDl0Qz7.html | Anunț imobiliare: „Vezi locația pe hartă”, „Pe OLX din mai 2013”, „Activ ieri la…” |
| https://www.olx.ro/d/oferta/vind-bicicleta-electrica-IDl0Seu.html | Anunț sport: breadcrumb pe categorie + județ + oraș + sector |
| https://www.olx.ro/d/oferta/audi-s8-carbon-ceramic-IDkFkcW.html | 404 (anunț expirat) |
| https://www.olx.ro/oferte/user/m0S0a/ | Profil vânzător randat în JS; se văd doar tab-urile „Toate anunțurile”, „Ratinguri”, „Contact” (detalii neverificate) |
| https://www.olx.ro/adding/ | Randat în JS, conținutul formularului nu a putut fi citit (**neverificat**) |
| https://livrare.olx.ro | Funcționarea Livrare prin OLX, plăți, Siguranța Achiziției, costuri |
| https://www.olx.ro/payment/whypromote/ | Reactualizare, Promovare în listă, Promovare pe prima pagină, pachete Start/Standard/Pro |
| https://www.olx.ro/howitworks | Pașii pentru cumpărător și vânzător |
| https://www.olx.ro/mobileapps | Funcțiile aplicației (notificări push, poze din aplicație) |
| https://business.olx.ro/ | Pachete pentru firme, „Pagina afacerii” cu subdomeniu, statistici, integrare cu site-ul firmei |

**Surse externe folosite pentru OLX** (accesate 2026-10-12):
- [Mobilissimo – OLX schimbă regulile, sistem nou de ratinguri (aug. 2024)](https://www.mobilissimo.ro/stiri-diverse/olx-schimba-regulile-de-listare-a-anunturilor-are-sistem-nou-de-ratinguri): ratinguri doar după comenzi prin Livrare prin OLX.
- [Mobilissimo – plăți prin PayU și verificarea identității vânzătorilor (sept. 2025)](https://www.mobilissimo.ro/stiri-diverse/olx-trece-la-un-nou-sistem-de-plati-pentru-livrari-vanzatorii-trebuie-sa-isi-confirme-identitatea-pentru-a-continua-sa-primeasca-comenzi)
- [StartupCafe – Siguranța Achiziției](https://startupcafe.ro/olx-romania-introduce-functia-sigurantei-achizitiei-89171)
- [Mobilissimo – limitarea anunțurilor gratuite](https://www.mobilissimo.ro/articole-diverse/olx-limiteaza-drastic-anunturile-gratuite-doar-doua-postari-pentru-telefoane-si-gadgeturi-in-180-de-zile) și [Economica.net](https://www.economica.net/olx-anunturi-gratuite_774211.html)

> Notă: o parte din interfața OLX se randează în JavaScript. Butoanele „Cumpără cu livrare” și inima de favorite nu au apărut în textul extras din paginile de anunț. Existența lor reiese din livrare.olx.ro și din header („Anunțuri salvate”), dar poziția exactă pe pagină este **neverificată**.

---

## 3. Tabel comparativ pe zone

Legendă: ✅ există · ⚠️ parțial · ❌ lipsește · ❓ neverificat

### Pagina principală
| Funcționalitate | OLX | AiciGăsești | Observații |
|---|---|---|---|
| Căutare din homepage (text + locație) | ✅ | ✅ | AG: „Ce cauți?” + „Oraș sau județ” |
| Listă categorii | ✅ 16 | ✅ 11 | AG nu are „Piese și accesorii”, „Cazare – Turism”, „Închiriere bunuri”, „Echipamente profesionale” ca rădăcini |
| Anunțuri promovate pe homepage | ✅ (plătite) | ⚠️ | AG are „Featured ads” în cod, setat doar din admin; nu e vizibil live |
| Volum de anunțuri | ✅ milioane | ❌ 1 anunț | Problema critică |
| Banner aplicație mobilă | ✅ | ❌ | |
| Căutări populare / „Căutări frecvente” | ✅ (/popular/) | ⚠️ | AG are 4 căutări populare fixe |
| Mesaj de valoare / anti-țeapă | ⚠️ (link phishing) | ✅ | AG își explică clar verificarea 0–100 |

### Căutare & filtre
| Funcționalitate | OLX | AiciGăsești | Observații |
|---|---|---|---|
| Filtre auto detaliate | ✅ | ✅ | AG are chiar mai multe (dotări, Euro, carte service) |
| Filtre imobiliare detaliate | ✅ | ✅ | AG are mai multe facilități |
| Filtre specifice pentru Electronice (garanție, model) | ✅ | ❌ | AG: doar filtre generice |
| Filtre specifice pentru Locuri de muncă (salariu, tip contract) | ❓ | ❌ | AG afișează filtre de produs (stare „Utilizat”) pe joburi |
| Comutator monedă lei/€ | ✅ | ❌ | AG filtrează doar „Preț (€)” |
| „Doar cu poze” | ✅ | ❌ | |
| „Numai cu livrare” | ✅ | ❌ | Depinde de livrare |
| Firmă/Privat | ✅ | ✅ | |
| Număr de anunțuri pe subcategorie/județ | ✅ | ❌ | AG arată doar numărul de subcategorii |
| Sugestii de categorii la căutarea text | ✅ | ❌ | |
| „Căutări similare” | ✅ | ❌ | |
| Sortări | ✅ 4 (inclusiv „Oferte pentru tine”) | ✅ 4–6 | AG are sortări specifice (km, suprafață); îi lipsește sortarea personalizată |
| Salvează căutarea + alerte | ✅ | ✅ | AG are salvarea în cod, dar butonul nu e vizibil pentru vizitatorul nelogat (verificat pe /search) |
| Vizualizare pe hartă | ❌ (nevăzută) | ✅ | Avantaj AG |
| Căutare după rază (km) | ❌ (nevăzută) | ❓ | |
| Paginare | ✅ | ❓ | Nu se poate testa cu 1 anunț |

### Pagina anunțului
| Funcționalitate | OLX | AiciGăsești | Observații |
|---|---|---|---|
| Galerie foto | ✅ (8 poze/anunț în exemple) | ✅ | AG: miniaturi fără zoom/fullscreen (❓ la OLX) |
| Breadcrumb categorie + județ + oraș + sector | ✅ | ⚠️ | AG: doar categoria; OLX folosește locația pentru link-uri SEO |
| Preț + „negociabil” | ✅ | ✅ | |
| Telefon mascat + „Arată” | ✅ | ⚠️ | AG afișează telefonul doar dacă `ContactPhone` e completat; pe anunțul live nu apare |
| Contact fără cont | ❓ | ❌ | AG: „Conectează-te pentru a contacta”, fără telefon vizibil, deci vizitatorul nu are nicio cale de contact |
| Salvare la favorite | ✅ | ⚠️ | AG: butonul apare doar pentru utilizatori logați |
| „Raportează” anunțul | ✅ | ❌ | Lipsește complet din UI |
| „Cumpără cu livrare” | ✅ | ❌ | |
| ID anunț | ✅ | ✅ | AG: „Anunț nr. 488” |
| Număr de vizualizări | ❌ (nevăzut) | ✅ | AG: „15 vizualizări” |
| Data actualizării („Reactualizat azi la…”) | ✅ | ✅ | |
| Locație pe hartă | ✅ („Vezi locația pe hartă”) | ❌ | AG are hartă globală, dar nu pe anunț |
| Vânzător: „Pe OLX din”, „Activ azi la…” | ✅ | ⚠️ | AG are „Pe AiciGăsești din”, dar nu are „ultima activitate” |
| Toate anunțurile vânzătorului | ✅ | ✅ | |
| Anunțuri similare | ❓ (nevăzute) | ⚠️ | AG are secțiunea în cod; cu 1 anunț nu apare |
| Scor de încredere anunț + vânzător | ❌ | ✅ | Avantaj AG |
| Sfaturi de siguranță pe pagină | ⚠️ (link phishing în footer) | ✅ | |
| Share + card social | ❓ | ✅ | AG: card 9:16 pentru TikTok/Stories |
| Parametri structurați per categorie | ✅ | ✅ | AG are mai mulți la auto |
| Retur 14 zile / garanție (firme) | ✅ | ❌ | |

### Publicare anunț
| Funcționalitate | OLX | AiciGăsești | Observații |
|---|---|---|---|
| Publicare gratuită | ⚠️ limitată (ex.: 2 anunțuri/180 zile la telefoane) | ✅ nelimitat | Argument de marketing pentru AG |
| Import din link OLX/Autovit | ❌ | ✅ | Avantaj AG |
| Publicare + creare cont într-un singur pas | ❓ | ✅ (/start) | |
| Poze direct din cameră (app) | ✅ | ⚠️ | AG: upload web, fără app |
| Video în anunț | ❓ | ✅ | |
| Activarea livrării la publicare | ✅ | ❌ | |
| Formular de publicare (detalii) | ❓ (JS) | ✅ | /adding nu a putut fi citit |

### Cont & profil vânzător
| Funcționalitate | OLX | AiciGăsești | Observații |
|---|---|---|---|
| Profil public | ✅ (tab-uri „Ratinguri”, „Contact”) | ✅ | AG: fără tab de recenzii și fără descriere |
| Ratinguri verificate (după tranzacție) | ✅ | ❌ | AG: „Fără recenzii încă”, fără mecanism verificat |
| Pagină de business (subdomeniu, branding) | ✅ | ❌ | AG are doar bifa „Sunt firmă / dealer” |
| Statistici per anunț (vizualizări, favorite, contactări) | ✅ (business) | ⚠️ | AG are doar vizualizări |
| Pachete și integrare/feed pentru firme | ✅ | ❌ | |
| Login social (Google/Facebook/Apple) | ❓ | ❌ | AG: doar email + parolă |
| Verificarea identității vânzătorului | ✅ (pentru livrare, prin PayU) | ⚠️ | AG: doar email confirmat; telefonul nu e verificat prin SMS (❓) |
| Resetare parolă, schimbare email, GDPR | ✅ | ✅ | |

### Comunicare
| Funcționalitate | OLX | AiciGăsești | Observații |
|---|---|---|---|
| Chat intern | ✅ | ✅ | |
| Avertismente anti-țeapă în chat | ❓ | ✅ | Avantaj AG |
| Notificări (clopoțel în header) | ✅ | ❌ | AG: fără centru de notificări vizibil |
| Notificări push (app) | ✅ | ❌ | |
| Apel telefonic | ✅ | ⚠️ | Vezi problema cu telefonul |

### Plată & livrare
| Funcționalitate | OLX | AiciGăsești | Observații |
|---|---|---|---|
| Livrare prin curier integrată (FAN Courier, de la 14,99 lei) | ✅ | ❌ | |
| Plată online cu cardul (escrow PayU) | ✅ | ❌ | |
| Plată la livrare (ramburs) | ✅ | ❌ | |
| Verificarea coletului la livrare | ✅ | ❌ | |
| Protecția cumpărătorului (retur în 24–48 h) | ✅ | ❌ | |
| AWB și urmărire comandă | ✅ | ❌ | |
| Recompense pentru vânzători | ✅ | ❌ | |

### Promovare/monetizare
| Funcționalitate | OLX | AiciGăsești | Observații |
|---|---|---|---|
| Reactualizare (manuală sau plătită) | ✅ | ⚠️ | AG are „republish” gratuit după expirare |
| Promovare în topul listei („PROMOVAT”) | ✅ | ❌ | |
| Promovare pe prima pagină | ✅ | ⚠️ | AG: „Featured” doar din admin |
| Pachete Start/Standard/Pro | ✅ | ❌ | |
| Publicitate display | ✅ (publicitate.olx.ro) | ❌ | |
| Credit / RCA / CASCO (parteneri) | ✅ (link-uri pe homepage) | ❌ | |

### Încredere & siguranță
| Funcționalitate | OLX | AiciGăsești | Observații |
|---|---|---|---|
| Scor automat de risc pe anunț | ❌ | ✅ | Avantaj AG |
| Raportarea anunțului de către utilizatori | ✅ | ❌ | |
| Ratinguri verificate | ✅ | ❌ | |
| Pagină anti-phishing / verificare link (siguranta.olx.ro) | ✅ | ❌ | |
| Centru de ajutor + contact | ✅ | ❌ | AG: /help, /contact dau 404 |
| Informații ANPC / Ordin 225/2023 / declarație IA | ✅ | ❌ | AG are doar Termeni, Confidențialitate, Cookie-uri |

### Mobil/app
| Funcționalitate | OLX | AiciGăsești | Observații |
|---|---|---|---|
| Aplicații iOS/Android | ✅ | ❌ | |
| PWA (manifest, instalabil) | n/a | ❌ | /manifest.json 404 |
| Design responsive | ✅ | ✅ | AG are meniu mobil („Meniu”) |
| Feed video vertical | ❌ | ✅ | Avantaj AG |

### SEO & social
| Funcționalitate | OLX | AiciGăsești | Observații |
|---|---|---|---|
| URL-uri prietenoase | ✅ | ✅ | AG: slug-uri de categorie în engleză (/c/electronics-and-appliances) |
| Pagini categorie × oraș | ✅ | ❌ | |
| Hartă site HTML (județe, mini-site-uri) | ✅ | ❌ | AG are doar sitemap.xml |
| Text SEO pe categorii | ✅ | ❌ | |
| Meta description / OG pe homepage și categorii | ❓ | ❌ | AG are meta/OG doar pe anunț și pe profil |
| JSON-LD (Product/Offer, BreadcrumbList) | ❓ | ❌ | AG are doar meta `og:`/`product:price` |
| robots.txt cu Sitemap | ❓ | ❌ | AG: robots.txt fără nicio directivă |
| Favicon | ✅ | ❌ | AG: /favicon.ico 404 live, deși fișierul e în repo |
| Card de share 9:16 | ❌ | ✅ | Avantaj AG |
| Rețele sociale în footer | ✅ | ❌ | |

---

## 4. Lista de lipsuri prioritizată

### P1 — esențial pentru utilizatori
1. **Inventar de anunțuri (cold start).** Cu un singur anunț, nicio funcție nu contează. Trebuie:
   - o campanie de import în masă prin /start, cu vânzători invitați direct (dealeri auto, agenții imobiliare);
   - feed-uri XML/CSV pentru firme.

   Argumentul de vânzare: „gratuit și nelimitat”, față de limitele OLX (de exemplu 2 anunțuri gratuite la 180 de zile la telefoane).
2. **Cale de contact pentru orice vizitator.** Telefonul introdus la înscriere trebuie afișat pe anunț (mascat, cu „Arată numărul”, eventual doar pentru utilizatori logați, ca protecție anti-scraping). Pe anunțul live, singura opțiune de contact cere cont, așa că mulți cumpărători pleacă.
3. **Buton „Raportează anunțul”** cu motive (țeapă, duplicat, categorie greșită, vândut), legat de moderarea din admin. E standard pe OLX și completează scorul de încredere.
4. **Pagini de ajutor/contact/despre** (/ajutor, /contact, „Cum funcționează”) și o **pagină 404 personalizată**. Acum dau 404 sau corp gol, ceea ce afectează încrederea și conformarea (ANPC, Ordin 225/2023).
5. **Favorite și „Salvează căutarea” vizibile și pentru vizitatori nelogați.** Butonul poate duce la login cu `returnUrl`, ca utilizatorul să nu piardă intenția.
6. **Filtre corecte pe categorii.** Pe Locuri de muncă trebuie scoase starea și prețul în €, iar în locul lor adăugate salariu, tip contract, normă și remote. La Electronice trebuie adăugate marca/modelul, memoria și garanția.

### P2 — important
7. **Livrare + plată protejată** (integrare cu un curier, de exemplu FAN, Sameday sau Cargus, și escrow prin Stripe Connect sau Netopia, plus ramburs). E cel mai mare avantaj OLX la electronice și modă. Variantă intermediară: bifa „Accept livrare prin curier” pe anunț și filtrul „Numai cu livrare”.
8. **Ratinguri verificate.** Recenzia ar trebui permisă doar după o conversație reală în chat sau după o tranzacție, cu un tab „Recenzii” pe profilul vânzătorului.
9. **Comutator de monedă lei/€ în filtre și pe carduri.** Anunțurile acceptă EUR/USD/RON, dar filtrul e doar în €.
10. **Hartă cu locația pe pagina anunțului** (aproximativă) și **breadcrumb cu județ și oraș**, cu link-uri către „categorie + oraș”.
11. **„Activ azi la…” / ultima activitate** a vânzătorului și eventual timpul mediu de răspuns.
12. **Centru de notificări în header** (mesaje noi, alerte de căutare, expirare anunț) și notificări push web.
13. **SEO tehnic:**
    - meta description și OG pe homepage, categorii și căutare;
    - JSON-LD `Product`/`Offer` + `BreadcrumbList` pe anunț;
    - `Sitemap:` în robots.txt;
    - favicon funcțional;
    - text SEO și pagini „categorie × oraș”.
14. **Filtre rapide:** „Doar cu poze”, număr de anunțuri pe subcategorie/județ, sugestii de categorie la căutarea text.

### P3 — nice-to-have
15. **Monetizare:** reactualizare plătită, „Promovat” în listă, homepage, pachete pentru firme. Aici se poate diferenția prin „Promovat doar pentru anunțuri cu scor ≥ 80”.
16. **Pagini de business:** logo, descriere, CUI, program, site și statistici (vizualizări, favorite, contactări).
17. **PWA/aplicație:** manifest, iconițe, instalare, poze direct din cameră, push.
18. **Login social** (Google/Apple/Facebook) pentru mai puține abandonuri la înregistrare.
19. **Verificarea telefonului prin SMS** și badge „Telefon verificat”, care se pot integra în scorul de încredere.
20. **„Căutări similare” / „Căutări frecvente”** și o hartă a site-ului în HTML.
21. **Parteneriate** pentru credit auto, RCA sau istoric VIN pe anunțurile auto.
22. **Link-uri către rețele sociale** în footer.

---

## 5. Unde AiciGăsești e deja mai bun decât OLX

- **Scor de încredere 0–100** pe anunț și pe vânzător, cu fișă de verificare (preț vs. piață, limbaj riscant, poze reale). OLX nu are nimic echivalent pe pagina anunțului.
- **Avertismente anti-țeapă în chat** (plată în avans, mutarea discuției pe WhatsApp).
- **Publicare gratuită și nelimitată**, plus **import dintr-un link OLX/Autovit** în 30 de secunde. OLX limitează anunțurile gratuite pe categorii.
- **Filtre auto și imobiliare mai bogate** (dotări, normă Euro, carte de service, 26+ facilități, încălzire, mobilare) și sortări specifice (kilometraj minim, suprafață maximă).
- **Hartă cu anunțuri**, care nu e vizibilă pe OLX.
- **Feed video vertical și card de share 9:16** pentru TikTok/Instagram.
- **Număr de vizualizări** afișat public pe anunț.
- **Doar cookie-uri esențiale**, export și ștergere GDPR self-service.

---

## 6. Probleme/bug-uri observate pe aicigasesti.ro

1. **Practic niciun anunț:** 1 anunț live; toate celelalte categorii verificate au 0.
2. **Telefonul nu apare pe anunț** (Jaguar #488), deși formularul /start spune „Telefon (afișat în anunț)”. Vizitatorul nelogat nu are nicio cale de contact.
3. **Lună în engleză pe profilul public:** „Membru din **October** 2026”. Pe pagina anunțului apare corect „Oct 2026”, dar data nu este localizată pentru RO.
4. **Acord gramatical:** „1 anunțuri” (pe /search și /c/cars) și „1 anunțuri active” (profil). Corect: „1 anunț”.
5. **Text în engleză în interfața RO:** „Feed video” în header (în română ar fi „Video-uri”/„Feed”), iar titlul paginii /feed este „Feed”.
6. **Inconsecvență în EN:** pe homepage apar și „CHECKED 0–100”, și „Verified 100/100”.
7. **Filtre nepotrivite pe Locuri de muncă (și Servicii):** „Preț (€)” și stările „Nou/Utilizat/Recondiționat/Pentru piese”.
8. **Pagina 404 are corp gol** (nici layout, nici mesaj). /help, /ajutor, /contact, /despre, /about nu există.
9. **/favicon.ico dă 404 live**, deși `wwwroot/favicon.ico` există în repo (fișierul nu e servit sau nu e inclus în publicare). Nici layout-ul nu are `<link rel="icon">`.
10. **robots.txt nu are nicio directivă** (doar comentariile „content signals” de la Cloudflare): lipsesc `User-agent` și `Sitemap:`.
11. **SEO incomplet:**
    - homepage și categoriile nu au `meta description` și nici tag-uri OG;
    - nicio pagină nu are JSON-LD;
    - meta description pe profil folosește „Constanta” fără diacritice (la fel titlul anunțului: „· Constanta”).
12. **Inconsecvență de date în anunț:** titlul spune „160 cai”, specificațiile spun „163 CP”, iar meta title folosește „Jaguar x250” în loc de „Jaguar XF”. Ține de datele importate, dar arată că importul ia „Versiune” în locul modelului.
13. **Traducere greșită la filtrul de mobilare** pe Imobiliare: opțiunea `Furnished` apare ca „Mobilare” în loc de „Mobilat”. Opțiunile sunt: Nemobilat / Parțial mobilat / **Mobilare** / Mobilat lux.
14. **Profil vânzător:** apare un „70” izolat lângă nume, fără etichetă (scorul de încredere fără context „/100” sau text explicativ).
15. **Slug-uri de categorie în engleză** (/c/electronics-and-appliances, /c/real-estate) pe un site cu public român. Nu e un bug, dar slug-uri în română ar ajuta SEO.
