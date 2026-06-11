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
