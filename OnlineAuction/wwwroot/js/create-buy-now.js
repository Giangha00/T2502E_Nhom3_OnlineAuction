(function () {
  'use strict';

  var DRAFT_KEY = 'auctionHouse_createBuyNow_draft';
  var MAX_IMAGE_SIZE = 5 * 1024 * 1024;
  var IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp'];

  var state = {
    images: [],
    editor: null
  };

  var form = document.getElementById('createBuyNowForm');
  if (!form) return;

  var i18n = (window.sellBuyNowPageConfig && window.sellBuyNowPageConfig.i18n) || {};

  function t(key, fallback) {
    return i18n[key] || fallback || '';
  }

  function tf(template) {
    var args = Array.prototype.slice.call(arguments, 1);
    return String(template).replace(/\{(\d+)\}/g, function (_, index) {
      return args[Number(index)] !== undefined ? args[Number(index)] : '';
    });
  }

  function $(id) { return document.getElementById(id); }

  function $all(id) {
    return Array.from(document.querySelectorAll('[id="' + id + '"]'));
  }

  function setPreviewText(id, value) {
    $all(id).forEach(function (el) {
      el.textContent = value;
    });
  }

  function getSelectedRadio(name) {
    var checked = form.querySelector('input[name="' + name + '"]:checked');
    return checked ? checked.value : '';
  }

  function getFormData() {
    return {
      productName: $('productName')?.value.trim() || '',
      category: $('category')?.value || '',
      productDescription: state.editor ? state.editor.getData() : ($('productDescription')?.value || ''),
      condition: getSelectedRadio('Condition'),
      productOrigin: $('productOrigin')?.value.trim() || '',
      price: $('price')?.value || ''
    };
  }

  function formatCardMoney(value) {
    if (value === '' || value === null || isNaN(value)) return '$ —';
    return '$' + Number(value).toLocaleString('en-US', { maximumFractionDigits: 0 });
  }

  function localizeCondition(value) {
    switch (value) {
      case 'New': return t('conditionNew', value);
      case 'Like New': return t('conditionLikeNew', value);
      case 'Used': return t('conditionUsed', value);
      default: return value;
    }
  }

  function buildPreviewSubtitle(data) {
    var parts = [];
    if (data.category) parts.push(data.category);
    if (data.productOrigin) parts.push(data.productOrigin);
    if (data.condition) parts.push(localizeCondition(data.condition));
    return parts.length ? parts.join(' · ') : '—';
  }

  function setPreviewGradeBadge(condition) {
    $all('previewGrade').forEach(function (badge) {
      var showGrade = condition && /^(PSA|BGS|CGC)\s/i.test(condition);
      if (showGrade) {
        badge.textContent = condition;
        badge.classList.remove('hidden');
      } else {
        badge.textContent = '';
        badge.classList.add('hidden');
      }
    });
  }

  function setPreviewMainImage(url) {
    $all('previewImage').forEach(function (previewImg) {
      if (url) {
        previewImg.src = url;
        previewImg.classList.remove('hidden');
      } else {
        previewImg.src = '';
        previewImg.classList.add('hidden');
      }
    });

    $all('previewImagePlaceholder').forEach(function (placeholder) {
      placeholder.classList.toggle('hidden', Boolean(url));
    });
  }

  function updatePreview() {
    var data = getFormData();
    setPreviewText('previewName', data.productName || t('productNameDefault', 'Product Name'));
    setPreviewText('previewSubtitle', buildPreviewSubtitle(data));
    setPreviewText('previewPrice', formatCardMoney(data.price));
    setPreviewGradeBadge(data.condition);
    setPreviewMainImage(state.images.length > 0 ? state.images[0].url : '');
  }

  function showError(field, message) {
    var el = document.querySelector('.field-error[data-for="' + field + '"]');
    if (!el) return;
    if (message) {
      el.textContent = message;
      el.classList.remove('hidden');
    } else {
      el.textContent = '';
      el.classList.add('hidden');
    }
  }

  function clearErrors() {
    document.querySelectorAll('.field-error').forEach(function (el) {
      el.textContent = '';
      el.classList.add('hidden');
    });
    document.querySelectorAll('.field-input.border-red-400').forEach(function (el) {
      el.classList.remove('border-red-400', 'ring-2', 'ring-red-100');
    });
  }

  function markInvalid(input) {
    if (input) {
      input.classList.add('border-red-400', 'ring-2', 'ring-red-100');
    }
  }

  function renderImagePreviews() {
    var grid = $('imagePreviewGrid');
    var list = $('imagePreviewList');
    var count = $('imageCount');
    if (!list) return;

    list.innerHTML = '';
    if (state.images.length === 0) {
      grid.classList.add('hidden');
      updatePreview();
      return;
    }

    grid.classList.remove('hidden');
    count.textContent = '(' + state.images.length + ')';

    state.images.forEach(function (img) {
      var card = document.createElement('div');
      card.className = 'group relative aspect-square overflow-hidden rounded-lg border border-slate-200 bg-slate-100';
      card.innerHTML =
        '<img src="' + img.url + '" alt="Preview" class="h-full w-full object-cover"/>' +
        '<button type="button" data-remove-image="' + img.id + '" class="absolute right-1.5 top-1.5 cursor-pointer rounded-lg bg-red-600/90 px-2 py-1 text-[10px] font-bold uppercase text-white opacity-0 transition group-hover:opacity-100">' + t('remove', 'Remove') + '</button>';
      list.appendChild(card);
    });

    list.querySelectorAll('[data-remove-image]').forEach(function (btn) {
      btn.addEventListener('click', function () {
        var id = btn.getAttribute('data-remove-image');
        removeImage(id);
      });
    });

    updatePreview();
  }

  function addImages(files) {
    var errors = [];
    var file = Array.from(files)[0];

    state.images.forEach(function (img) { URL.revokeObjectURL(img.url); });
    state.images = [];

    if (!file) {
      renderImagePreviews();
      return;
    }

    if (!IMAGE_TYPES.includes(file.type)) {
      errors.push(tf(t('errorImageInvalidFormat', '{0}: invalid format (JPG, PNG, WEBP only)'), file.name));
    } else if (file.size > MAX_IMAGE_SIZE) {
      errors.push(tf(t('errorImageSizeLimit', '{0}: exceeds 5MB limit'), file.name));
    } else {
      state.images.push({
        id: 'img_' + Date.now() + '_' + Math.random().toString(36).slice(2),
        file: file,
        url: URL.createObjectURL(file)
      });
    }

    if (errors.length) {
      showError('images', errors[0]);
    } else {
      showError('images', '');
    }

    renderImagePreviews();
  }

  function removeImage(id) {
    var idx = state.images.findIndex(function (i) { return i.id === id; });
    if (idx >= 0) {
      URL.revokeObjectURL(state.images[idx].url);
      state.images.splice(idx, 1);
    }
    var input = $('imageInput');
    if (input && state.images.length === 0) input.value = '';
    renderImagePreviews();
  }

  function clearAllImages() {
    state.images.forEach(function (img) { URL.revokeObjectURL(img.url); });
    state.images = [];
    var input = $('imageInput');
    if (input) input.value = '';
    renderImagePreviews();
    showError('images', '');
  }

  function setupDropZone(zoneId, inputId, onFiles) {
    var zone = $(zoneId);
    var input = $(inputId);
    if (!zone || !input) return;

    zone.addEventListener('click', function () { input.click(); });
    zone.addEventListener('dragover', function (e) {
      e.preventDefault();
      zone.classList.add('border-blue-500', 'bg-blue-50/60');
    });
    zone.addEventListener('dragleave', function () {
      zone.classList.remove('border-blue-500', 'bg-blue-50/60');
    });
    zone.addEventListener('drop', function (e) {
      e.preventDefault();
      zone.classList.remove('border-blue-500', 'bg-blue-50/60');
      if (e.dataTransfer.files.length) onFiles(e.dataTransfer.files);
    });
    input.addEventListener('change', function () {
      if (input.files.length) onFiles(input.files);
    });
  }

  function validateForm() {
    clearErrors();
    var valid = true;
    var data = getFormData();

    if (!data.productName) {
      showError('productName', t('errorProductNameRequired', 'Product name is required'));
      markInvalid($('productName'));
      valid = false;
    }

    if (!data.category) {
      showError('category', t('errorCategoryRequired', 'Please select a category'));
      markInvalid($('category'));
      valid = false;
    }

    if (!data.condition) {
      showError('Condition', t('errorConditionRequired', 'Please select a condition'));
      valid = false;
    }

    var price = Number(data.price);
    if (!data.price || isNaN(price) || price <= 0) {
      showError('price', t('errorPriceRequired', 'Price must be greater than 0'));
      markInvalid($('price'));
      valid = false;
    }

    return valid;
  }

  function saveDraft() {
    var data = getFormData();
    try {
      localStorage.setItem(DRAFT_KEY, JSON.stringify(data));
      var banner = $('draftBanner');
      if (banner) {
        banner.classList.remove('hidden');
        setTimeout(function () { banner.classList.add('hidden'); }, 3000);
      }
    } catch (e) {
      console.warn('Could not save draft', e);
    }
  }

  function loadDraft() {
    try {
      var raw = localStorage.getItem(DRAFT_KEY);
      if (!raw) return;
      var data = JSON.parse(raw);

      if ($('productName') && data.productName) $('productName').value = data.productName;
      if ($('category') && data.category) $('category').value = data.category;
      if ($('productOrigin') && data.productOrigin) $('productOrigin').value = data.productOrigin;
      if ($('price') && data.price) $('price').value = data.price;

      if (data.condition) {
        var radio = form.querySelector('input[name="Condition"][value="' + data.condition + '"]');
        if (radio) radio.checked = true;
      }

      if (state.editor && data.productDescription) {
        state.editor.setData(data.productDescription);
      } else if ($('productDescription') && data.productDescription) {
        $('productDescription').value = data.productDescription;
      }

      updatePreview();
    } catch (e) {
      console.warn('Could not load draft', e);
    }
  }

  function showSuccess(name) {
    showTopToast('success', tf(t('successCreated', 'Your listing "{0}" has been created successfully!'), name));
    try { localStorage.removeItem(DRAFT_KEY); } catch (e) { /* ignore */ }
  }

  function showTopToast(type, message) {
    var banner = $('successBanner');
    var text = $('successMessageText');
    if (text) text.textContent = message;
    if (banner) {
      banner.className = 'fixed left-1/2 top-24 z-9999 w-[min(92vw,520px)] -translate-x-1/2 rounded-xl border px-5 py-4 text-sm font-semibold shadow-lg';
      if (type === 'success') {
        banner.classList.add('border-emerald-200', 'bg-emerald-50', 'text-emerald-800');
      } else {
        banner.classList.add('border-red-200', 'bg-red-50', 'text-red-700');
      }
      banner.classList.remove('hidden');
      window.setTimeout(function () {
        banner.classList.add('hidden');
      }, 5000);
    }
  }

  function initEditor() {
    var el = $('descriptionEditor');
    if (!el || typeof ClassicEditor === 'undefined') return;

    ClassicEditor.create(el, {
      placeholder: t('editorPlaceholder', "Describe your product's unique features, grade, and history..."),
      toolbar: ['bold', 'italic', 'link', 'bulletedList', 'numberedList', '|', 'undo', 'redo']
    }).then(function (editor) {
      state.editor = editor;
      editor.model.document.on('change:data', updatePreview);
      loadDraft();
      updatePreview();
    }).catch(function (err) {
      console.warn('CKEditor failed to load', err);
      loadDraft();
      updatePreview();
    });
  }

  function bindEvents() {
    ['productName', 'category', 'productOrigin', 'price'].forEach(function (id) {
      var el = $(id);
      if (!el) return;
      el.addEventListener('input', updatePreview);
      el.addEventListener('change', updatePreview);
    });

    form.querySelectorAll('input[name="Condition"]').forEach(function (el) {
      el.addEventListener('change', updatePreview);
    });

    setupDropZone('imageDropZone', 'imageInput', addImages);

    var clearBtn = $('clearAllImages');
    if (clearBtn) clearBtn.addEventListener('click', clearAllImages);

    var draftBtn = $('saveDraftBtn');
    if (draftBtn) {
      draftBtn.addEventListener('click', function () {
        if (state.editor) {
          $('productDescription').value = state.editor.getData();
        }
        saveDraft();
      });
    }

    form.addEventListener('submit', function (e) {
      e.preventDefault();
      if (state.editor) {
        $('productDescription').value = state.editor.getData();
      }
      if (!validateForm()) {
        var firstError = document.querySelector('.field-error:not(.hidden)');
        if (firstError) firstError.closest('div')?.scrollIntoView({ behavior: 'smooth', block: 'center' });
        return;
      }

      var data = getFormData();
      var formData = new FormData(form);

      formData.delete('PrimaryImageFile');
      if (state.images.length > 0) {
        formData.append('PrimaryImageFile', state.images[0].file);
      }

      fetch(form.action, {
        method: 'POST',
        body: formData,
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
      })
        .then(function (response) {
          return response.json().then(function (payload) {
            if (!response.ok || payload.success === false) {
              throw new Error(payload.message || t('errorCreateFailed', 'Could not create listing.'));
            }
            return payload;
          });
        })
        .then(function (payload) {
          showSuccess(data.productName);
          if (payload.redirectUrl) {
            window.location.href = payload.redirectUrl;
          }
        })
        .catch(function (error) {
          showError('images', error.message);
          showTopToast('error', error.message);
        });
    });
  }

  document.addEventListener('DOMContentLoaded', function () {
    bindEvents();
    initEditor();
    if (!state.editor) {
      loadDraft();
      updatePreview();
    }
  });
})();
