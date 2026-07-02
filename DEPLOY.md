# 🚀 Deploy AiciGăsești pe Oracle Cloud (Ubuntu 20.04)

Ghid complet de instalare pentru un server **Canonical Ubuntu 20.04** pe Oracle Cloud
Infrastructure (OCI), cu domeniul **aicigasesti.ro**. Aplicația rulează pe .NET 11
(Kestrel) în spatele Nginx, cu TLS gratuit de la Let's Encrypt și pornire automată
prin systemd. Baza de date este SQLite — nu ai nevoie de un server de DB separat.

> Estimare timp: ~30–40 minute. Rulează comenzile în ordine, ca utilizatorul `ubuntu`.

---

## ⚡ Varianta rapidă (cu scripturi)

Dacă vrei să sari peste pașii manuali, repo-ul conține două scripturi. Fă întâi
**pasul 0.1** din consola OCI (deschiderea porturilor în Security List — nu poate fi
automatizat), apoi:

```bash
# adu scripturile pe server
git clone -b claude/keen-lovelace-pexrbj https://github.com/aliniluca/fabclassifiedads.git /tmp/ag
cd /tmp/ag

# 1) provisioning unic: .NET 11, Nginx, firewall OS, reverse proxy, TLS
sudo DOMAIN=aicigasesti.ro EMAIL=adresa@ta.ro ./scripts/setup-server.sh

# 2) build + pornire (rulează același script și la fiecare update ulterior)
sudo ./scripts/deploy.sh
```

Atât — site-ul e live pe **https://aicigasesti.ro**. La orice actualizare viitoare,
rulezi doar `sudo ./scripts/deploy.sh` (face pull, publish, restart; baza de date și
pozele/video-urile utilizatorilor rămân intacte).

Restul ghidului explică pas cu pas ce fac scripturile, pentru cazul în care preferi
controlul manual sau vrei să depanezi.

---

## 0. Înainte de toate: deschide porturile în Oracle Cloud (PASUL CEL MAI UITAT)

Instanțele OCI blochează tot traficul din **două** locuri. Trebuie deschise amândouă,
altfel site-ul „nu merge" deși serverul e pornit.

### 0.1 Security List (din consola web OCI)
1. **Networking → Virtual Cloud Networks → (VCN-ul tău) → Subnets → (subnet-ul tău) → Security Lists → Default Security List**
2. **Add Ingress Rules** — adaugă două reguli:

| Source CIDR | IP Protocol | Destination Port Range |
|-------------|-------------|------------------------|
| `0.0.0.0/0` | TCP         | `80`                   |
| `0.0.0.0/0` | TCP         | `443`                  |

(Portul 22 pentru SSH este deja deschis implicit.)

### 0.2 Firewall-ul local (iptables) — specific imaginilor Ubuntu de la Oracle
Imaginile Ubuntu de la OCI vin cu o regulă `REJECT` care blochează 80/443 la nivel de OS.
Trebuie inserate reguli ACCEPT **înainte** de acel REJECT:

```bash
sudo iptables -I INPUT 6 -m state --state NEW -p tcp --dport 80 -j ACCEPT
sudo iptables -I INPUT 6 -m state --state NEW -p tcp --dport 443 -j ACCEPT
sudo netfilter-persistent save
```

Verifică ordinea (regulile ACCEPT pentru 80/443 trebuie să apară **deasupra** liniei REJECT):

```bash
sudo iptables -L INPUT --line-numbers -n
```

Dacă din vreun motiv poziția `6` nu e corectă, șterge și re-inserează cu poziția
afișată deasupra regulii `REJECT ... icmp-host-prohibited`.

---

## 1. Conectează-te și actualizează serverul

```bash
ssh ubuntu@IP_PUBLIC_AL_INSTANTEI
sudo apt update && sudo apt -y upgrade
sudo apt -y install git nginx ffmpeg
```

`ffmpeg` este opțional dar recomandat (folosit pentru procesarea/miniaturile video mai târziu).

---

## 2. Instalează .NET 11 SDK

.NET 11 este preview și nu se află încă în feed-ul apt pentru 20.04, deci folosim
scriptul oficial Microsoft (funcționează pe orice distribuție):

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh
sudo /tmp/dotnet-install.sh --channel 11.0 --install-dir /usr/share/dotnet
sudo ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet
dotnet --version   # ar trebui să afișeze 11.0.x
```

> Ubuntu 20.04 are nevoie de câteva biblioteci native pentru .NET. De obicei sunt
> deja instalate; dacă `dotnet` dă eroare de librării lipsă, rulează:
> `sudo apt -y install libicu66 libssl1.1 zlib1g libgcc-s1`

---

## 3. Adu codul și publică aplicația

```bash
sudo mkdir -p /var/www/aicigasesti
sudo chown -R ubuntu:ubuntu /var/www/aicigasesti

# clonează repo-ul pe branch-ul de dezvoltare
git clone -b claude/keen-lovelace-pexrbj https://github.com/aliniluca/fabclassifiedads.git /tmp/aicigasesti-src
cd /tmp/aicigasesti-src

# Dacă repo-ul e privat, folosește un Personal Access Token:
#   git clone -b claude/keen-lovelace-pexrbj https://UTILIZATOR:TOKEN@github.com/aliniluca/fabclassifiedads.git /tmp/aicigasesti-src

dotnet publish src/FabClassifiedAds.Web/FabClassifiedAds.Web.csproj \
  -c Release -o /var/www/aicigasesti
```

Aplicația își creează și populează singură baza de date SQLite la prima pornire.

---

## 4. Creează un utilizator de serviciu și dă drepturi de scriere

Aplicația scrie în baza de date, în folderul de upload-uri și în „outbox"-ul de email.

```bash
sudo useradd -r -s /usr/sbin/nologin aicigasesti || true
sudo mkdir -p /var/www/aicigasesti/wwwroot/uploads /var/www/aicigasesti/App_Data
sudo chown -R aicigasesti:aicigasesti /var/www/aicigasesti
```

---

## 5. Serviciul systemd (pornire automată + repornire la crash)

```bash
sudo tee /etc/systemd/system/aicigasesti.service >/dev/null <<'EOF'
[Unit]
Description=AiciGasesti (.NET 11) classifieds platform
After=network.target

[Service]
WorkingDirectory=/var/www/aicigasesti
ExecStart=/usr/bin/dotnet /var/www/aicigasesti/FabClassifiedAds.Web.dll
Restart=always
RestartSec=5
KillSignal=SIGINT
SyslogIdentifier=aicigasesti
User=aicigasesti
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000
Environment=DOTNET_CLI_TELEMETRY_OPTOUT=1

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl enable --now aicigasesti
sudo systemctl status aicigasesti --no-pager
```

Aplicația ascultă acum doar pe `127.0.0.1:5000` (intern). Nginx o va publica spre exterior.

Loguri live: `sudo journalctl -u aicigasesti -f`

---

## 6. DNS: leagă domeniul de server

La registrarul unde ai cumpărat **aicigasesti.ro**, creează:

| Tip | Nume  | Valoare                    |
|-----|-------|----------------------------|
| A   | `@`   | IP_PUBLIC_AL_INSTANTEI     |
| A   | `www` | IP_PUBLIC_AL_INSTANTEI     |

Așteaptă propagarea (`dig +short aicigasesti.ro` trebuie să întoarcă IP-ul tău).

---

## 7. Nginx ca reverse proxy

```bash
sudo tee /etc/nginx/sites-available/aicigasesti >/dev/null <<'EOF'
server {
    listen 80;
    server_name aicigasesti.ro www.aicigasesti.ro;

    # necesar pentru upload-urile de video (max 60 MB în aplicație)
    client_max_body_size 120M;

    location / {
        proxy_pass         http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }
}
EOF

sudo ln -sf /etc/nginx/sites-available/aicigasesti /etc/nginx/sites-enabled/
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t && sudo systemctl reload nginx
```

Acum `http://aicigasesti.ro` ar trebui să afișeze site-ul.

---

## 8. HTTPS gratuit cu Let's Encrypt

```bash
sudo snap install core && sudo snap refresh core
sudo snap install --classic certbot
sudo ln -sf /snap/bin/certbot /usr/bin/certbot

sudo certbot --nginx -d aicigasesti.ro -d www.aicigasesti.ro \
  --agree-tos -m adresa@ta.ro --redirect
```

Certbot rescrie automat configul Nginx pentru 443 + redirect de la 80, și își reînnoiește
singur certificatul. Test reînnoire: `sudo certbot renew --dry-run`.

Gata — **https://aicigasesti.ro** este live. 🎉

---

## 9. Actualizări viitoare (deploy nou)

Cel mai simplu — scriptul face pull + publish + restart, păstrând DB și upload-urile:

```bash
cd /tmp/ag && git pull && sudo ./scripts/deploy.sh
```

Sau manual:

```bash
cd /tmp/aicigasesti-src && git pull
dotnet publish src/FabClassifiedAds.Web/FabClassifiedAds.Web.csproj -c Release -o /var/www/aicigasesti
sudo chown -R aicigasesti:aicigasesti /var/www/aicigasesti
sudo systemctl restart aicigasesti
```

---

## 10. Backup (baza de date + upload-uri)

Totul stă în două locuri — fă-le backup periodic (ex. cron zilnic):

```bash
# baza de date SQLite
cp /var/www/aicigasesti/aicigasesti.db ~/backup-$(date +%F).db
# fișierele încărcate de utilizatori (poze + video)
tar czf ~/uploads-$(date +%F).tgz -C /var/www/aicigasesti/wwwroot uploads
```

---

## Depanare rapidă

| Simptom | Cauză probabilă |
|---------|-----------------|
| Site-ul nu se încarcă deloc din exterior | Porturi neînchise în **Security List** (pasul 0.1) sau iptables (0.2) |
| `502 Bad Gateway` | Serviciul .NET nu rulează → `sudo systemctl status aicigasesti` + `journalctl -u aicigasesti -e` |
| Upload video eșuează cu `413` | Lipsește `client_max_body_size` în Nginx (pasul 7) |
| Certbot: „challenge failed" | DNS-ul încă nu s-a propagat sau portul 80 e blocat (pasul 0) |
| `dotnet: command not found` la systemd | Verifică `ExecStart` — folosește calea completă `/usr/bin/dotnet` sau `/usr/local/bin/dotnet` |

---

## API de import (opțional)

Aplicația expune `POST /api/import/listings` pentru a alimenta anunțuri dintr-o sursă
externă (feed partener, API oficial etc.). Endpoint-ul este **blocat implicit** — se
deblochează doar dacă setezi o cheie secretă:

```bash
# la deploy, cheia intră automat în serviciul systemd:
sudo IMPORT_API_KEY="o-cheie-lunga-si-secreta" ./scripts/deploy.sh
```

Anunțurile importate ajung într-un tabel de staging și **rămân invizibile** până când
sunt promovate manual (`POST /api/import/{id}/publish`). Vezi `scraper/README.md` pentru
clientul de colectare și **notele legale** (ToS, drepturi de autor, GDPR) înainte de a
colecta din orice sursă.

## Notă despre email-uri (alertele de căutare salvată)

În prezent expeditorul de email scrie fișiere HTML în `App_Data/outbox` (mod demo).
Pentru email-uri reale către utilizatori, înlocuiește `IEmailSender` cu un serviciu SMTP
(ex. cont SMTP de la provider, sau un serviciu tranzacțional). Spune-mi când ești gata
și îl conectez.
