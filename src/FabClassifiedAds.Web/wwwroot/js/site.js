// Dependent brand -> model dropdowns (search sidebar + create form)
function wireModelDropdown(brandSelectId, modelSelectId) {
    const brandSelect = document.getElementById(brandSelectId);
    const modelSelect = document.getElementById(modelSelectId);
    if (!brandSelect || !modelSelect) return;

    brandSelect.addEventListener('change', async () => {
        modelSelect.innerHTML = '<option value="">Any model</option>';
        if (!brandSelect.value) return;
        try {
            const res = await fetch(brandSelect.dataset.modelsUrl + brandSelect.value);
            const models = await res.json();
            for (const m of models) {
                const opt = document.createElement('option');
                opt.value = m.id;
                opt.textContent = m.name;
                modelSelect.appendChild(opt);
            }
        } catch { /* leave "Any model" */ }
    });
}
wireModelDropdown('brandSelect', 'modelSelect');
wireModelDropdown('createBrandSelect', 'createModelSelect');

// Show/hide car & real-estate fieldsets on the create form based on category kind
const createForm = document.getElementById('createForm');
if (createForm) {
    const carIds = new Set((createForm.dataset.carCategories || '').split(',').filter(Boolean));
    const reIds = new Set((createForm.dataset.reCategories || '').split(',').filter(Boolean));
    const categorySelect = document.getElementById('categorySelect');
    const carFields = document.getElementById('carFields');
    const reFields = document.getElementById('reFields');

    const sync = () => {
        carFields.hidden = !carIds.has(categorySelect.value);
        reFields.hidden = !reIds.has(categorySelect.value);
    };
    categorySelect.addEventListener('change', sync);
    sync();
}

// Photo upload previews on the create form
const photoInput = document.getElementById('photoInput');
if (photoInput) {
    photoInput.addEventListener('change', () => {
        const previews = document.getElementById('photoPreviews');
        previews.innerHTML = '';
        for (const file of photoInput.files) {
            const img = document.createElement('img');
            img.src = URL.createObjectURL(file);
            img.onload = () => URL.revokeObjectURL(img.src);
            previews.appendChild(img);
        }
        const hint = document.querySelector('.photo-drop-hint');
        if (hint) hint.innerHTML = `📸 ${photoInput.files.length} photo(s) selected — click to change`;
    });
}

// Mobile header menu toggle
const navToggle = document.getElementById('navToggle');
const headerNav = document.getElementById('headerNav');
if (navToggle && headerNav) {
    navToggle.addEventListener('click', () => {
        const open = headerNav.classList.toggle('open');
        navToggle.setAttribute('aria-expanded', open ? 'true' : 'false');
    });
    // close when tapping outside
    document.addEventListener('click', (e) => {
        if (!headerNav.contains(e.target) && e.target !== navToggle && headerNav.classList.contains('open')) {
            headerNav.classList.remove('open');
            navToggle.setAttribute('aria-expanded', 'false');
        }
    });
}

// Cookie consent
const cookieAccept = document.getElementById('cookieAccept');
if (cookieAccept) {
    cookieAccept.addEventListener('click', () => {
        document.cookie = 'cookie_consent=1; path=/; max-age=' + (60 * 60 * 24 * 365) + '; samesite=lax';
        document.getElementById('cookieConsent')?.remove();
    });
}

// Cross-post bridge: paste a link to your own ad → pre-fill the post form
const importBtn = document.getElementById('importBtn');
if (importBtn) {
    const urlInput = document.getElementById('importUrl');
    const msg = document.getElementById('importMsg');
    const form = document.getElementById('createForm');

    const setMsg = (text, kind) => {
        msg.hidden = false;
        msg.textContent = text;
        msg.className = 'import-box-msg ' + (kind || '');
    };
    const setField = (id, value) => {
        const el = document.getElementById(id);
        if (el && value != null && value !== '') el.value = value;
    };

    const runImport = async () => {
        const url = (urlInput.value || '').trim();
        if (!url) { setMsg('Paste a link first.', 'err'); return; }
        importBtn.disabled = true;
        const original = importBtn.textContent;
        importBtn.textContent = '…';
        setMsg('Reading the page…', '');
        try {
            const body = new FormData();
            body.append('url', url);
            body.append('__RequestVerificationToken', form.querySelector('input[name=__RequestVerificationToken]').value);
            const res = await fetch('/post/import-url', { method: 'POST', body });
            const json = await res.json();
            if (!json.ok) { setMsg(json.error || 'Could not import that link.', 'err'); return; }

            const d = json.data;
            setField('Title', d.title);
            setField('Description', d.description);
            setField('Price', d.price);
            setField('Currency', d.currency);
            setField('City', d.city);
            setField('Region', d.region);

            // auto-detected category: pre-select it and reveal its tailored fields
            let detected = false;
            if (d.categoryId) {
                const catSel = document.getElementById('categorySelect');
                if (catSel && catSel.querySelector(`option[value="${d.categoryId}"]`)) {
                    catSel.value = String(d.categoryId);
                    catSel.dispatchEvent(new Event('change'));
                    detected = true;
                }
            }
            if (d.brandId) {
                const brandSel = document.getElementById('createBrandSelect');
                if (brandSel && brandSel.querySelector(`option[value="${d.brandId}"]`)) {
                    brandSel.value = String(d.brandId);
                    brandSel.dispatchEvent(new Event('change'));
                }
            }

            // carry the remote images as hidden inputs; they download on publish
            const holder = document.getElementById('importedImages');
            holder.innerHTML = '';
            const imgs = d.images || [];
            for (const src of imgs) {
                const input = document.createElement('input');
                input.type = 'hidden';
                input.name = 'ImportedImageUrls';
                input.value = src;
                holder.appendChild(input);
            }
            if (imgs.length) {
                const strip = document.createElement('div');
                strip.className = 'photo-previews';
                imgs.slice(0, 10).forEach(src => {
                    const img = document.createElement('img');
                    img.src = src; img.loading = 'lazy';
                    strip.appendChild(img);
                });
                holder.appendChild(strip);
            }

            const catMsg = detected ? 'category detected' : 'pick a category';
            setMsg(`Done — filled the form${imgs.length ? ` with ${imgs.length} photo(s)` : ''} (${catMsg}). Review and publish. ✅`, 'ok');
            document.getElementById('categorySelect')?.scrollIntoView({ behavior: 'smooth', block: 'center' });
        } catch {
            setMsg('Could not reach that link. Fill the form in manually.', 'err');
        } finally {
            importBtn.disabled = false;
            importBtn.textContent = original;
        }
    };

    importBtn.addEventListener('click', runImport);
    urlInput.addEventListener('keydown', e => { if (e.key === 'Enter') { e.preventDefault(); runImport(); } });
}

// Video picker hint on the create form
const videoInput = document.getElementById('videoInput');
if (videoInput) {
    videoInput.addEventListener('change', () => {
        const hint = document.getElementById('videoHint');
        if (hint && videoInput.files.length > 0) hint.textContent = `🎬 ${videoInput.files[0].name}`;
    });
}

// Favorite hearts: toggle via fetch without a page reload
document.addEventListener('submit', async (e) => {
    const form = e.target.closest('.fav-form');
    if (!form) return;
    e.preventDefault();
    const btn = form.querySelector('.fav-btn');
    try {
        const res = await fetch(form.action, {
            method: 'POST',
            headers: { 'X-Requested-With': 'fetch' },
            body: new FormData(form),
        });
        if (res.redirected) { window.location = res.url; return; }
        const data = await res.json();
        btn.classList.toggle('is-fav', data.isFavorite);
        btn.textContent = data.isFavorite ? '♥' : '♡';
    } catch {
        form.submit();
    }
});
