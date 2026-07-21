(function () {
  'use strict';

  var form = document.getElementById('adminListingForm');
  if (!form) return;

  var config = window.adminListingConfig || {};
  var i18n = config.i18n || {};
  var isBuyNow = config.listingType === 'buynow' || form.getAttribute('data-listing-type') === 'buynow';
  var isEdit = config.isEdit === true || form.getAttribute('data-is-edit') === 'true';
  var DEFAULT_LIVE_DURATION_MS = 60 * 60 * 1000;
  var MAX_DOC_SIZE = 5 * 1024 * 1024;
  var DOC_TYPES = ['application/pdf'];

  var DOC_NAME_OPTIONS = [
    'PSA Certificate',
    'BGS Certificate',
    'Product Verification',
    'Warranty'
  ];

  var PHASE_BADGE_CLASSES = [
    'bg-sky-600',
    'bg-amber-500',
    'bg-emerald-600',
    'bg-red-600',
    'bg-slate-600',
    'text-white'
  ];

  var docUploader = document.querySelector('[data-document-uploader]');
  var maxDocuments = docUploader ? Number(docUploader.getAttribute('data-max-documents') || 5) : 5;
  var existingDocumentCount = docUploader ? Number(docUploader.getAttribute('data-existing-count') || 0) : 0;

  var state = {
    documents: [],
    editor: null
  };

  function $(id) {
    return document.getElementById(id);
  }

  function $all(id) {
    return Array.from(document.querySelectorAll('[id="' + id + '"]'));
  }

  function t(key, fallback) {
    return i18n[key] || fallback || '';
  }

  function tf(template) {
    var args = Array.prototype.slice.call(arguments, 1);
    return String(template).replace(/\{(\d+)\}/g, function (_, index) {
      return args[Number(index)] !== undefined ? args[Number(index)] : '';
    });
  }

  function setPreviewText(id, value) {
    $all(id).forEach(function (el) {
      el.textContent = value;
    });
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
    form.querySelectorAll('.field-input.border-error-500, .field-input.border-red-400').forEach(function (el) {
      el.classList.remove('border-error-500', 'border-red-400', 'ring-2', 'ring-red-100');
    });
  }

  function markInvalid(input) {
    if (input) {
      input.classList.add('border-error-500', 'ring-2', 'ring-red-100');
    }
  }

  function formatMoney(value) {
    if (value === '' || value === null || isNaN(value)) return '$ —';
    return '$' + Number(value).toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 2 });
  }

  function formatCardMoney(value) {
    if (value === '' || value === null || isNaN(value)) return '$ —';
    return '$' + Number(value).toLocaleString('en-US', { maximumFractionDigits: 0 });
  }

  function formatTimeRemaining(endDate) {
    if (!endDate) return '—';
    var end = new Date(endDate);
    if (isNaN(end.getTime())) return '—';
    var diff = end.getTime() - Date.now();
    if (diff <= 0) return t('timeEnded', 'Ended');
    var days = Math.floor(diff / (1000 * 60 * 60 * 24));
    var hours = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
    if (days > 0) return tf(t('timeLeftDays', '{0}d {1}h left'), days, hours);
    var mins = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
    return tf(t('timeLeftHours', '{0}h {1}m left'), hours, mins);
  }

  function parseDateMs(value) {
    if (!value) return NaN;
    var date = new Date(value);
    return isNaN(date.getTime()) ? NaN : date.getTime();
  }

  function composeGradeLabel(authenticator, gradeValue) {
    if (!authenticator || authenticator === 'Ungraded') return 'Ungraded';
    if (!gradeValue) return authenticator;
    return authenticator + ' ' + gradeValue;
  }

  function toggleGradeValueField() {
    var authenticator = $('authenticator')?.value || '';
    var gradeField = $('gradeValue');
    if (!gradeField) return;
    var isUngraded = authenticator === 'Ungraded';
    gradeField.disabled = isUngraded;
    gradeField.classList.toggle('opacity-50', isUngraded);
  }

  function composeGrade() {
    toggleGradeValueField();
    var auth = $('authenticator');
    var gradeValue = $('gradeValue');
    var gradeHidden = $('grade');
    var condition = $('condition');
    if (!auth || !gradeValue) return;

    var authenticator = auth.value || 'PSA';
    if (authenticator.toLowerCase() === 'ungraded') {
      if (gradeHidden) gradeHidden.value = 'Ungraded';
      if (condition) condition.value = 'Ungraded';
      return;
    }

    var grade = composeGradeLabel(authenticator, gradeValue.value || '10');
    if (gradeHidden) gradeHidden.value = grade;
    if (condition) condition.value = 'Graded';
  }

  function countRemainingExistingDocuments() {
    var remaining = existingDocumentCount;
    document.querySelectorAll('[data-remove-document]:checked').forEach(function () {
      remaining -= 1;
    });
    return Math.max(remaining, 0);
  }

  function getFormData() {
    composeGrade();
    var category = $('categoryId');
    var selectedCategory = category && category.options[category.selectedIndex];

    return {
      productName: $('productName')?.value.trim() || '',
      categoryId: category?.value || '',
      category: selectedCategory && selectedCategory.value ? selectedCategory.text : '',
      subtitle: $('subtitle')?.value.trim() || '',
      setName: $('setName')?.value.trim() || '',
      year: $('year')?.value || '',
      authenticator: $('authenticator')?.value || '',
      gradeValue: $('gradeValue')?.value || '',
      grade: $('grade')?.value || '',
      sellerId: $('sellerId')?.value || '',
      startingPrice: $('startingPrice')?.value || '',
      bidStep: $('bidStep')?.value || '',
      buyNowPrice: $('buyNowPrice')?.value || '',
      price: $('price')?.value || '',
      registrationStartDate: $('registrationStartDate')?.value || '',
      registrationEndDate: $('registrationEndDate')?.value || '',
      startDate: $('liveStartDate')?.value || '',
      endDate: $('liveEndDate')?.value || ''
    };
  }

  function buildPreviewSubtitle(data) {
    if (data.subtitle) return data.subtitle;
    var parts = [];
    if (data.setName) parts.push(data.setName);
    var grade = data.grade || composeGradeLabel(data.authenticator, data.gradeValue);
    if (grade) parts.push(grade);
    if (data.year) parts.push(data.year);
    return parts.length ? parts.join(' · ') : '\u00a0';
  }

  function resolvePreviewPhase(data) {
    var now = Date.now();
    var regStart = parseDateMs(data.registrationStartDate);
    var regEnd = parseDateMs(data.registrationEndDate);
    var liveStart = parseDateMs(data.startDate);
    var liveEnd = parseDateMs(data.endDate);

    if (!isNaN(liveEnd) && now >= liveEnd) {
      return {
        label: t('timeEnded', 'Ended'),
        badgeClass: 'bg-slate-600 text-white',
        countdownLabel: t('countdownLiveEnd', 'Live ends in'),
        countdownTarget: data.endDate,
        endingSoon: false
      };
    }

    if (!isNaN(liveStart) && now >= liveStart) {
      var endingSoon = !isNaN(liveEnd) && (liveEnd - now) <= 24 * 60 * 60 * 1000;
      return {
        label: endingSoon ? t('phaseLiveEndingSoon', 'Ending Soon') : t('phaseLiveAuction', 'Live Now'),
        badgeClass: endingSoon ? 'bg-red-600 text-white' : 'bg-emerald-600 text-white',
        countdownLabel: t('countdownLiveEnd', 'Live ends in'),
        countdownTarget: data.endDate,
        endingSoon: endingSoon
      };
    }

    if (!isNaN(regEnd) && now >= regEnd && (isNaN(liveStart) || now < liveStart)) {
      return {
        label: t('phaseUpcoming', 'Upcoming'),
        badgeClass: 'bg-slate-600 text-white',
        countdownLabel: t('countdownLiveStart', 'Live starts in'),
        countdownTarget: data.startDate,
        endingSoon: false
      };
    }

    if (!isNaN(regStart) && now >= regStart) {
      return {
        label: t('phaseRegistrationOpen', 'Registration Open'),
        badgeClass: 'bg-sky-600 text-white',
        countdownLabel: t('countdownRegistrationEnd', 'Registration ends in'),
        countdownTarget: data.registrationEndDate,
        endingSoon: false
      };
    }

    return {
      label: t('phaseUpcoming', 'Upcoming'),
      badgeClass: 'bg-slate-600 text-white',
      countdownLabel: t('countdownRegistrationStart', 'Registration opens in'),
      countdownTarget: data.registrationStartDate || data.startDate,
      endingSoon: false
    };
  }

  function setPreviewPhaseBadge(phase) {
    $all('previewPhaseBadge').forEach(function (badge) {
      PHASE_BADGE_CLASSES.forEach(function (cls) {
        badge.classList.remove(cls);
      });
      phase.badgeClass.split(/\s+/).forEach(function (cls) {
        if (cls) badge.classList.add(cls);
      });
      badge.textContent = phase.label;
    });
  }

  function updatePreviewImage() {
    var previewImg = document.querySelector('[data-primary-preview-img]');
    var savedImg = document.querySelector('[data-saved-primary-thumb]');
    var previewImage = $all('previewImage');
    var placeholder = $all('previewImagePlaceholder');

    var src = '';
    if (previewImg && !previewImg.closest('[data-primary-preview-wrap]')?.classList.contains('hidden')) {
      src = previewImg.getAttribute('src') || '';
    } else if (savedImg && !savedImg.closest('[data-saved-primary-image]')?.classList.contains('hidden')) {
      src = savedImg.getAttribute('src') || '';
    }

    previewImage.forEach(function (img) {
      if (src) {
        img.src = src;
        img.classList.remove('hidden');
      } else {
        img.removeAttribute('src');
        img.classList.add('hidden');
      }
    });

    placeholder.forEach(function (el) {
      el.classList.toggle('hidden', !!src);
    });
  }

  function updateAuctionPreview() {
    var data = getFormData();
    var phase = resolvePreviewPhase(data);

    setPreviewText('previewName', data.productName || t('productNameDefault', 'Product name'));
    setPreviewText('previewSubtitle', buildPreviewSubtitle(data));
    setPreviewText('previewCategory', data.category || t('categoryDefault', '—'));
    setPreviewText('previewCurrentBid', formatCardMoney(data.startingPrice));
    setPreviewText('previewCountdownLabel', phase.countdownLabel);
    setPreviewText('previewTimeRemaining', formatTimeRemaining(phase.countdownTarget));

    $all('previewTimeRemaining').forEach(function (el) {
      el.classList.toggle('text-red-600', phase.endingSoon);
      el.classList.toggle('text-stone-700', !phase.endingSoon);
    });

    setPreviewPhaseBadge(phase);
    updatePreviewImage();
  }

  function updateBuyNowPreview() {
    var data = getFormData();

    setPreviewText('previewName', data.productName || t('productNameDefault', 'Product name'));
    setPreviewText('previewSubtitle', buildPreviewSubtitle(data));
    setPreviewText('previewCategory', data.category || t('categoryDefault', '—'));
    setPreviewText('previewPrice', formatMoney(data.price));
    updatePreviewImage();
  }

  function updatePreview() {
    composeGrade();
    if (isBuyNow) {
      updateBuyNowPreview();
    } else {
      updateAuctionPreview();
    }
  }

  function toLocalInputValue(date) {
    var pad = function (n) { return String(n).padStart(2, '0'); };
    return date.getFullYear() + '-' + pad(date.getMonth() + 1) + '-' + pad(date.getDate()) +
      'T' + pad(date.getHours()) + ':' + pad(date.getMinutes());
  }

  function syncLiveStartFromRegistrationEnd() {
    var regEnd = $('registrationEndDate');
    var liveStart = $('liveStartDate');
    if (!regEnd || !liveStart || !regEnd.value) return;
    liveStart.value = regEnd.value;
    syncLiveEndFromStart();
  }

  function syncLiveEndFromStart() {
    var liveStart = $('liveStartDate');
    var liveEnd = $('liveEndDate');
    if (!liveStart || !liveEnd || !liveStart.value) return;
    var start = new Date(liveStart.value);
    if (isNaN(start.getTime())) return;
    liveEnd.value = toLocalInputValue(new Date(start.getTime() + DEFAULT_LIVE_DURATION_MS));
  }

  function renderDocuments() {
    var list = $('documentList');
    if (!list) return;
    list.innerHTML = '';

    state.documents.forEach(function (doc) {
      var li = document.createElement('li');
      li.className = 'flex flex-col gap-3 rounded-lg border border-gray-200 bg-gray-50 px-4 py-3 dark:border-gray-700 dark:bg-gray-900/40 sm:flex-row sm:items-center sm:justify-between';
      var selectHtml = '<select data-doc-name="' + doc.id + '" class="field-input h-9 w-full rounded-lg border border-gray-300 px-3 text-theme-sm dark:border-gray-700 dark:bg-gray-900 sm:w-52">';
      DOC_NAME_OPTIONS.forEach(function (option) {
        selectHtml += '<option value="' + option + '"' + (option === doc.displayName ? ' selected' : '') + '>' + option + '</option>';
      });
      selectHtml += '</select>';
      li.innerHTML =
        '<div class="flex min-w-0 items-center gap-3">' +
        '<span class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-red-100 text-xs font-bold text-red-800">PDF</span>' +
        '<div class="min-w-0"><p class="truncate text-sm font-medium text-gray-800 dark:text-white/90">' + doc.file.name + '</p>' +
        '<p class="text-xs text-gray-500">' + (doc.file.size / 1024).toFixed(1) + ' KB</p></div></div>' +
        '<div class="flex w-full flex-col gap-2 sm:w-auto sm:flex-row sm:items-center">' + selectHtml +
        '<button type="button" data-remove-doc="' + doc.id + '" class="shrink-0 text-xs font-semibold text-error-600 hover:text-error-700">' + t('remove', 'Remove') + '</button></div>';
      list.appendChild(li);
    });

    list.querySelectorAll('[data-doc-name]').forEach(function (select) {
      select.addEventListener('change', function () {
        var id = select.getAttribute('data-doc-name');
        var doc = state.documents.find(function (d) { return d.id === id; });
        if (doc) doc.displayName = select.value;
      });
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
    var remainingSlots = maxDocuments - countRemainingExistingDocuments() - state.documents.length;

    Array.from(files).forEach(function (file) {
      if (remainingSlots <= 0) {
        errors.push(t('errorDocMaxCount', 'You can upload up to 5 documents per product.'));
        return;
      }
      var isPdf = DOC_TYPES.includes(file.type) || file.name.toLowerCase().endsWith('.pdf');
      if (!isPdf) {
        errors.push(tf(t('errorDocInvalidFormat', '{0}: invalid format (PDF only)'), file.name));
        return;
      }
      if (file.size > MAX_DOC_SIZE) {
        errors.push(tf(t('errorDocSizeLimit', '{0}: exceeds 5MB limit'), file.name));
        return;
      }
      state.documents.push({
        id: 'doc_' + Date.now() + '_' + Math.random().toString(36).slice(2),
        file: file,
        displayName: 'PSA Certificate'
      });
      remainingSlots -= 1;
    });

    showError('documents', errors.length ? errors[0] : '');
    renderDocuments();
  }

  function setupDropZone(zoneId, inputId, onFiles) {
    var zone = $(zoneId);
    var input = $(inputId);
    if (!zone || !input) return;

    zone.addEventListener('click', function () { input.click(); });
    zone.addEventListener('dragover', function (e) {
      e.preventDefault();
      zone.classList.add('border-brand-500', 'bg-brand-50/40');
    });
    zone.addEventListener('dragleave', function () {
      zone.classList.remove('border-brand-500', 'bg-brand-50/40');
    });
    zone.addEventListener('drop', function (e) {
      e.preventDefault();
      zone.classList.remove('border-brand-500', 'bg-brand-50/40');
      if (e.dataTransfer.files.length) onFiles(e.dataTransfer.files);
    });
    input.addEventListener('change', function () {
      if (input.files.length) onFiles(input.files);
      input.value = '';
    });
  }

  function hasPrimaryImage() {
    var primaryInput = document.querySelector('[data-primary-input]');
    if (primaryInput && primaryInput.files && primaryInput.files.length > 0) return true;
    var saved = document.querySelector('[data-saved-primary-image]');
    return !!(saved && !saved.classList.contains('hidden'));
  }

  function validateSharedFields(data) {
    var valid = true;

    if (!data.productName) {
      showError('productName', t('errorProductNameRequired', 'Product name is required'));
      markInvalid($('productName'));
      valid = false;
    }

    if (!data.categoryId) {
      showError('categoryId', t('errorCategoryRequired', 'Please select a category'));
      markInvalid($('categoryId'));
      valid = false;
    }

    if (!data.sellerId) {
      showError('sellerId', t('errorSellerRequired', 'Please select a seller.'));
      markInvalid($('sellerId'));
      valid = false;
    }

    if (!isEdit && !hasPrimaryImage()) {
      showError('primaryImage', t('errorPrimaryImageRequired', 'Primary image is required.'));
      markInvalid(document.querySelector('[data-primary-input]'));
      valid = false;
    }

    if (!data.year) {
      showError('year', t('errorYearRequired', 'Year is required'));
      markInvalid($('year'));
      valid = false;
    } else {
      var year = Number(data.year);
      if (isNaN(year) || year < 1800 || year > 2100) {
        showError('year', t('errorYearInvalid', 'Please enter a valid year between 1800 and 2100.'));
        markInvalid($('year'));
        valid = false;
      }
    }

    if (!data.authenticator) {
      showError('authenticator', t('errorAuthenticatorRequired', 'Please select an authenticator'));
      markInvalid($('authenticator'));
      valid = false;
    } else if (data.authenticator !== 'Ungraded' && !data.gradeValue) {
      showError('gradeValue', t('errorGradeRequired', 'Please select a grade'));
      markInvalid($('gradeValue'));
      valid = false;
    }

    return valid;
  }

  function validateAuctionForm(data) {
    var valid = validateSharedFields(data);
    var now = new Date();

    var price = Number(data.startingPrice);
    if (!data.startingPrice || isNaN(price) || price <= 0) {
      showError('startingPrice', t('errorStartingPriceRequired', 'Starting price must be greater than 0'));
      markInvalid($('startingPrice'));
      valid = false;
    }

    var step = Number(data.bidStep);
    if (!data.bidStep || isNaN(step) || step <= 0) {
      showError('bidStep', t('errorBidStepRequired', 'Bid step must be greater than 0'));
      markInvalid($('bidStep'));
      valid = false;
    }

    if (data.buyNowPrice) {
      var buyNow = Number(data.buyNowPrice);
      if (isNaN(buyNow) || buyNow <= 0) {
        showError('buyNowPrice', t('errorBuyNowPriceInvalid', 'Buy now price must be greater than 0'));
        markInvalid($('buyNowPrice'));
        valid = false;
      } else if (!isNaN(price) && buyNow <= price) {
        showError('buyNowPrice', t('errorBuyNowPriceGreater', 'Buy now price must be greater than the starting price'));
        markInvalid($('buyNowPrice'));
        valid = false;
      }
    }

    if (!data.registrationStartDate) {
      showError('registrationStartDate', t('errorRegistrationStartRequired', 'Registration start is required'));
      markInvalid($('registrationStartDate'));
      valid = false;
    } else if (!isEdit) {
      var registrationStart = new Date(data.registrationStartDate);
      if (registrationStart < now) {
        showError('registrationStartDate', t('errorRegistrationStartPast', 'Registration start cannot be in the past'));
        markInvalid($('registrationStartDate'));
        valid = false;
      }
    }

    if (!data.registrationEndDate) {
      showError('registrationEndDate', t('errorRegistrationEndRequired', 'Registration end is required'));
      markInvalid($('registrationEndDate'));
      valid = false;
    } else if (data.registrationStartDate) {
      var regStart = new Date(data.registrationStartDate);
      var regEnd = new Date(data.registrationEndDate);
      if (regEnd <= regStart) {
        showError('registrationEndDate', t('errorRegistrationEndAfterStart', 'Registration end must be after registration start'));
        markInvalid($('registrationEndDate'));
        valid = false;
      }
    }

    if (!data.startDate) {
      showError('liveStartDate', t('errorLiveStartRequired', 'Live start is required'));
      markInvalid($('liveStartDate'));
      valid = false;
    } else if (data.registrationEndDate) {
      var liveStart = new Date(data.startDate);
      var registrationEnd = new Date(data.registrationEndDate);
      if (liveStart < registrationEnd) {
        showError('liveStartDate', t('errorLiveStartAfterRegistration', 'Live start must be after registration ends'));
        markInvalid($('liveStartDate'));
        valid = false;
      }
    }

    if (!data.endDate) {
      showError('liveEndDate', t('errorLiveEndRequired', 'Live end is required'));
      markInvalid($('liveEndDate'));
      valid = false;
    } else if (data.startDate) {
      var liveStartDate = new Date(data.startDate);
      var liveEndDate = new Date(data.endDate);
      if (liveEndDate <= liveStartDate) {
        showError('liveEndDate', t('errorLiveEndAfterStart', 'Live end must be after live start'));
        markInvalid($('liveEndDate'));
        valid = false;
      }
    }

    return valid;
  }

  function validateBuyNowForm(data) {
    var valid = validateSharedFields(data);
    var price = Number(data.price);
    if (!data.price || isNaN(price) || price <= 0) {
      showError('price', t('errorPriceRequired', 'Price must be greater than 0'));
      markInvalid($('price'));
      valid = false;
    }
    return valid;
  }

  function validateForm() {
    clearErrors();
    var data = getFormData();
    if (state.editor) {
      var hidden = $('productDescription');
      if (hidden) hidden.value = state.editor.getData();
    }
    return isBuyNow ? validateBuyNowForm(data) : validateAuctionForm(data);
  }

  function syncDocumentsToForm() {
    var input = $('documentInput');
    if (!input) return;

    form.querySelectorAll('[data-dynamic-doc-name]').forEach(function (el) {
      el.remove();
    });

    if (state.documents.length === 0) {
      input.removeAttribute('name');
      return;
    }

    var dt = new DataTransfer();
    state.documents.forEach(function (doc) {
      dt.items.add(doc.file);
      var hidden = document.createElement('input');
      hidden.type = 'hidden';
      hidden.name = 'DocumentNames';
      hidden.value = doc.displayName || 'PSA Certificate';
      hidden.setAttribute('data-dynamic-doc-name', 'true');
      form.appendChild(hidden);
    });

    input.name = 'DocumentFiles';
    input.files = dt.files;
  }

  function initEditor() {
    var editorHost = $('descriptionEditor');
    var hidden = $('productDescription');
    if (!editorHost || !hidden || typeof ClassicEditor === 'undefined') return;

    ClassicEditor.create(editorHost, {
      placeholder: t('editorPlaceholder', 'Describe your item…')
    }).then(function (editor) {
      state.editor = editor;
      editor.setData(hidden.value || '');
      editor.model.document.on('change:data', function () {
        hidden.value = editor.getData();
      });
    }).catch(function () { /* noop */ });
  }

  function bindPreviewListeners() {
    [
      'productName', 'categoryId', 'subtitle', 'setName', 'year', 'sellerId',
      'startingPrice', 'bidStep', 'buyNowPrice', 'price',
      'authenticator', 'gradeValue',
      'registrationStartDate', 'registrationEndDate', 'liveStartDate', 'liveEndDate'
    ].forEach(function (id) {
      var el = $(id);
      if (el) {
        el.addEventListener('input', updatePreview);
        el.addEventListener('change', updatePreview);
      }
    });

    var regEnd = $('registrationEndDate');
    if (regEnd) {
      regEnd.addEventListener('change', function () {
        syncLiveStartFromRegistrationEnd();
        updatePreview();
      });
    }

    var liveStart = $('liveStartDate');
    if (liveStart) {
      liveStart.addEventListener('change', function () {
        syncLiveEndFromStart();
        updatePreview();
      });
    }

    var primaryInput = document.querySelector('[data-primary-input]');
    if (primaryInput) {
      primaryInput.addEventListener('change', function () {
        showError('primaryImage', '');
        setTimeout(updatePreview, 100);
      });
    }

    document.querySelectorAll('[data-remove-document]').forEach(function (checkbox) {
      checkbox.addEventListener('change', function () {
        showError('documents', '');
      });
    });

    document.addEventListener('product-image-uploader:changed', updatePreview);

    if (!isBuyNow) {
      setInterval(updatePreview, 30000);
    }
  }

  function bindFormSubmit() {
    form.addEventListener('submit', function (e) {
      if (!validateForm()) {
        e.preventDefault();
        var firstError = form.querySelector('.field-error:not(.hidden)');
        if (firstError) {
          firstError.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
        return;
      }

      syncDocumentsToForm();
    });
  }

  setupDropZone('documentDropZone', 'documentInput', addDocuments);
  initEditor();
  bindPreviewListeners();
  bindFormSubmit();
  updatePreview();
})();
