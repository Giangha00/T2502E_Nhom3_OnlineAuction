(function () {
  'use strict';

  var DRAFT_KEY = 'auctionHouse_createAuction_draft';
  var MAX_IMAGE_SIZE = 5 * 1024 * 1024;
  var MAX_DOC_SIZE = 10 * 1024 * 1024;
  var IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp'];
  var DOC_TYPES = ['application/pdf', 'image/jpeg', 'image/png'];

  var state = {
    images: [],
    documents: [],
    editor: null
  };

  var form = document.getElementById('createAuctionForm');
  if (!form) return;

  function $(id) { return document.getElementById(id); }

  function formatMoney(value) {
    if (value === '' || value === null || isNaN(value)) return '$—';
    return '$' + Number(value).toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 2 });
  }

  function formatEndDate(value) {
    if (!value) return 'Ends —';
    var d = new Date(value);
    if (isNaN(d.getTime())) return 'Ends —';
    return 'Ends ' + d.toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit'
    });
  }

  function formatCountdown(startValue, endValue) {
    if (!endValue) return '—';
    var end = new Date(endValue);
    if (isNaN(end.getTime())) return '—';
    var now = startValue ? new Date(startValue) : new Date();
    if (isNaN(now.getTime())) now = new Date();
    var diff = Math.max(0, end.getTime() - now.getTime());
    var days = Math.floor(diff / 86400000);
    var hours = Math.floor((diff % 86400000) / 3600000);
    return 'in ' + days + 'd ' + hours + 'h';
  }

  function buildSpecs(data) {
    var parts = [];
    if (data.grade) parts.push(data.grade);
    if (data.setName) parts.push(data.setName);
    if (data.condition) parts.push(data.condition);
    if (data.certificateNumber) parts.push('Cert. ' + data.certificateNumber);
    return parts.length ? parts.join(' · ') : '—';
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
      grade: $('grade')?.value || '',
      subtitle: $('subtitle')?.value.trim() || '',
      year: $('year')?.value || '',
      setName: $('setName')?.value.trim() || '',
      language: $('language')?.value || '',
      cardNumber: $('cardNumber')?.value.trim() || '',
      certificateNumber: $('certificateNumber')?.value.trim() || '',
      gradingCentering: $('gradingCentering')?.value.trim() || '',
      gradingCorners: $('gradingCorners')?.value.trim() || '',
      gradingEdges: $('gradingEdges')?.value.trim() || '',
      gradingSurface: $('gradingSurface')?.value.trim() || '',
      startingPrice: $('startingPrice')?.value || '',
      bidStep: $('bidStep')?.value || '',
      estimatedValue: $('estimatedValue')?.value || '',
      auctionEventName: $('auctionEventName')?.value.trim() || '',
      startDate: $('startDate')?.value || '',
      endDate: $('endDate')?.value || '',
      imageCount: state.images.length,
      documentCount: state.documents.length
    };
  }

  function updatePreview() {
    var data = getFormData();

    $('previewCategory').textContent = data.category || 'Category';
    $('previewName').innerHTML = data.productName
      ? '<em>' + data.productName + '</em>' + (data.year ? ', ' + data.year : '')
      : '<em>Card Name</em>';
    $('previewSpecs').textContent = buildSpecs(data);
    $('previewEstimatedValue').textContent = formatMoney(data.estimatedValue);
    $('previewStartingPrice').textContent = formatMoney(data.startingPrice);
    $('previewBidStep').textContent = formatMoney(data.bidStep);
    $('previewCountdown').textContent = formatCountdown(data.startDate, data.endDate);
    $('previewEndDate').textContent = formatEndDate(data.endDate);
    $('previewEventName').textContent = data.auctionEventName || '—';
    $('previewYear').textContent = data.year || '—';
    $('previewSetName').textContent = data.setName || '—';
    $('previewLanguage').textContent = data.language || '—';
    $('previewCardNumber').textContent = data.cardNumber || '—';

    var previewImg = $('previewImage');
    var placeholder = $('previewImagePlaceholder');
    if (state.images.length > 0) {
      previewImg.src = state.images[0].url;
      previewImg.classList.remove('hidden');
      placeholder.classList.add('hidden');
    } else {
      previewImg.src = '';
      previewImg.classList.add('hidden');
      placeholder.classList.remove('hidden');
    }

    var thumbs = $('previewThumbnails');
    thumbs.innerHTML = '';
    if (state.images.length > 1) {
      thumbs.classList.remove('hidden');
      state.images.slice(0, 5).forEach(function (img, i) {
        var el = document.createElement('img');
        el.src = img.url;
        el.alt = 'Thumbnail ' + (i + 1);
        el.className = 'h-14 w-14 rounded border border-slate-200 object-cover';
        thumbs.appendChild(el);
      });
    } else {
      thumbs.classList.add('hidden');
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
      card.className = 'group relative aspect-square overflow-hidden rounded-xl border border-stone-200 bg-stone-100';
      card.innerHTML =
        '<img src="' + img.url + '" alt="Preview" class="h-full w-full object-cover"/>' +
        '<button type="button" data-remove-image="' + img.id + '" class="absolute right-1.5 top-1.5 rounded-lg bg-red-600/90 px-2 py-1 text-[10px] font-bold uppercase text-white opacity-0 transition group-hover:opacity-100">Remove</button>';
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
    Array.from(files).forEach(function (file) {
      if (!IMAGE_TYPES.includes(file.type)) {
        errors.push(file.name + ': invalid format (JPG, PNG, WEBP only)');
        return;
      }
      if (file.size > MAX_IMAGE_SIZE) {
        errors.push(file.name + ': exceeds 5MB limit');
        return;
      }
      state.images.push({
        id: 'img_' + Date.now() + '_' + Math.random().toString(36).slice(2),
        file: file,
        url: URL.createObjectURL(file)
      });
    });

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
    renderImagePreviews();
  }

  function clearAllImages() {
    state.images.forEach(function (img) { URL.revokeObjectURL(img.url); });
    state.images = [];
    renderImagePreviews();
    showError('images', '');
  }

  function renderDocuments() {
    var list = $('documentList');
    if (!list) return;
    list.innerHTML = '';

    state.documents.forEach(function (doc) {
      var li = document.createElement('li');
      li.className = 'flex items-center justify-between rounded-xl border border-stone-200 bg-stone-50 px-4 py-3';
      li.innerHTML =
        '<div class="flex min-w-0 items-center gap-3">' +
        '<span class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-blue-100 text-xs font-bold text-blue-800">' +
        doc.name.split('.').pop().toUpperCase().slice(0, 3) + '</span>' +
        '<div class="min-w-0"><p class="truncate text-sm font-medium text-stone-800">' + doc.name + '</p>' +
        '<p class="text-xs text-stone-400">' + (doc.size / 1024).toFixed(1) + ' KB</p></div></div>' +
        '<button type="button" data-remove-doc="' + doc.id + '" class="shrink-0 text-xs font-semibold text-red-600 hover:text-red-700">Remove</button>';
      list.appendChild(li);
    });

    list.querySelectorAll('[data-remove-doc]').forEach(function (btn) {
      btn.addEventListener('click', function () {
        var id = btn.getAttribute('data-remove-doc');
        state.documents = state.documents.filter(function (d) { return d.id !== id; });
        renderDocuments();
      });
    });
  }

  function addDocuments(files) {
    var errors = [];
    Array.from(files).forEach(function (file) {
      if (!DOC_TYPES.includes(file.type)) {
        errors.push(file.name + ': invalid format (PDF, JPG, PNG only)');
        return;
      }
      if (file.size > MAX_DOC_SIZE) {
        errors.push(file.name + ': exceeds 10MB limit');
        return;
      }
      state.documents.push({
        id: 'doc_' + Date.now() + '_' + Math.random().toString(36).slice(2),
        name: file.name,
        size: file.size,
        file: file
      });
    });

    if (errors.length) {
      showError('documents', errors[0]);
    } else {
      showError('documents', '');
    }

    renderDocuments();
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
      input.value = '';
    });
  }

  function validateForm() {
    clearErrors();
    var valid = true;
    var data = getFormData();
    var now = new Date();

    if (!data.productName) {
      showError('productName', 'Card name is required');
      markInvalid($('productName'));
      valid = false;
    }

    if (!data.category) {
      showError('category', 'Please select a category');
      markInvalid($('category'));
      valid = false;
    }

    if (!data.setName) {
      showError('setName', 'Set name is required');
      markInvalid($('setName'));
      valid = false;
    }

    if (!data.grade) {
      showError('grade', 'Please select a grade');
      markInvalid($('grade'));
      valid = false;
    }

    var price = Number(data.startingPrice);
    if (!data.startingPrice || isNaN(price) || price <= 0) {
      showError('startingPrice', 'Starting price must be greater than 0');
      markInvalid($('startingPrice'));
      valid = false;
    }

    var step = Number(data.bidStep);
    if (!data.bidStep || isNaN(step) || step <= 0) {
      showError('bidStep', 'Bid step must be greater than 0');
      markInvalid($('bidStep'));
      valid = false;
    }

    var estimated = Number(data.estimatedValue);
    if (!data.estimatedValue || isNaN(estimated) || estimated <= 0) {
      showError('estimatedValue', 'Estimated value must be greater than 0');
      markInvalid($('estimatedValue'));
      valid = false;
    } else if (!isNaN(price) && estimated < price) {
      showError('estimatedValue', 'Estimated value should be at least the starting price');
      markInvalid($('estimatedValue'));
      valid = false;
    }

    if (!data.auctionEventName) {
      showError('auctionEventName', 'Auction event name is required');
      markInvalid($('auctionEventName'));
      valid = false;
    }

    if (!data.startDate) {
      showError('startDate', 'Start date is required');
      markInvalid($('startDate'));
      valid = false;
    } else {
      var start = new Date(data.startDate);
      if (start < now) {
        showError('startDate', 'Start date cannot be in the past');
        markInvalid($('startDate'));
        valid = false;
      }
    }

    if (!data.endDate) {
      showError('endDate', 'End date is required');
      markInvalid($('endDate'));
      valid = false;
    } else if (data.startDate) {
      var startD = new Date(data.startDate);
      var endD = new Date(data.endDate);
      if (endD <= startD) {
        showError('endDate', 'End date must be greater than start date');
        markInvalid($('endDate'));
        valid = false;
      }
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
      if ($('subtitle') && data.subtitle) $('subtitle').value = data.subtitle;
      if ($('year') && data.year) $('year').value = data.year;
      if ($('setName') && data.setName) $('setName').value = data.setName;
      if ($('language') && data.language) $('language').value = data.language;
      if ($('cardNumber') && data.cardNumber) $('cardNumber').value = data.cardNumber;
      if ($('grade') && data.grade) $('grade').value = data.grade;
      if ($('certificateNumber') && data.certificateNumber) $('certificateNumber').value = data.certificateNumber;
      if ($('gradingCentering') && data.gradingCentering) $('gradingCentering').value = data.gradingCentering;
      if ($('gradingCorners') && data.gradingCorners) $('gradingCorners').value = data.gradingCorners;
      if ($('gradingEdges') && data.gradingEdges) $('gradingEdges').value = data.gradingEdges;
      if ($('gradingSurface') && data.gradingSurface) $('gradingSurface').value = data.gradingSurface;
      if ($('startingPrice') && data.startingPrice) $('startingPrice').value = data.startingPrice;
      if ($('bidStep') && data.bidStep) $('bidStep').value = data.bidStep;
      if ($('estimatedValue') && data.estimatedValue) $('estimatedValue').value = data.estimatedValue;
      if ($('auctionEventName') && data.auctionEventName) $('auctionEventName').value = data.auctionEventName;
      if ($('startDate') && data.startDate) $('startDate').value = data.startDate;
      if ($('endDate') && data.endDate) $('endDate').value = data.endDate;

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
    var banner = $('successBanner');
    var text = $('successMessageText');
    if (text) text.textContent = 'Your auction "' + name + '" has been created successfully!';
    if (banner) {
      banner.classList.remove('hidden');
      banner.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
    try { localStorage.removeItem(DRAFT_KEY); } catch (e) { /* ignore */ }
  }

  function initEditor() {
    var el = $('descriptionEditor');
    if (!el || typeof ClassicEditor === 'undefined') return;

    ClassicEditor.create(el, {
      placeholder: 'Describe your card — highlights, provenance, condition notes...',
      toolbar: ['heading', '|', 'bold', 'italic', 'link', 'bulletedList', 'numberedList', '|', 'undo', 'redo']
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
    var fields = [
      'productName', 'category', 'subtitle', 'year', 'setName', 'language', 'cardNumber',
      'grade', 'certificateNumber', 'gradingCentering', 'gradingCorners', 'gradingEdges', 'gradingSurface',
      'startingPrice', 'bidStep', 'estimatedValue', 'auctionEventName', 'startDate', 'endDate'
    ];

    fields.forEach(function (id) {
      var el = $(id);
      if (!el) return;
      el.addEventListener('input', updatePreview);
      el.addEventListener('change', updatePreview);
    });

    form.querySelectorAll('input[name="Condition"]').forEach(function (el) {
      el.addEventListener('change', updatePreview);
    });

    setupDropZone('imageDropZone', 'imageInput', addImages);
    setupDropZone('documentDropZone', 'documentInput', addDocuments);

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
      showSuccess(data.productName);

      fetch(form.action, {
        method: 'POST',
        body: new FormData(form),
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
      }).catch(function () { /* UI-only fallback */ });
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
