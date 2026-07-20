(function () {
  'use strict';

  var DRAFT_KEY = 'auctionHouse_createAuction_draft';
  var MAX_IMAGE_SIZE = 5 * 1024 * 1024;
  var MAX_DOC_SIZE = 5 * 1024 * 1024;
  var MAX_IMAGES = 5;
  var MAX_DOCUMENTS = 5;
  var DEFAULT_LIVE_DURATION_MS = 60 * 60 * 1000;
  var IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp'];
  var DOC_TYPES = ['application/pdf'];
  var DOC_NAME_OPTIONS = [
    'PSA Certificate',
    'BGS Certificate',
    'Product Verification',
    'Warranty'
  ];

  var state = {
    images: [],
    documents: [],
    editor: null
  };

  var form = document.getElementById('createAuctionForm');
  if (!form) return;

  var i18n = (window.sellPageConfig && window.sellPageConfig.i18n) || {};

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

  // Create.cshtml renders Auction Preview twice: one for mobile and one for desktop.
  // Update every matching preview element so the visible preview is always in sync.
  function $all(id) {
    return Array.from(document.querySelectorAll('[id="' + id + '"]'));
  }

  function setPreviewText(id, value) {
    $all(id).forEach(function (el) {
      el.textContent = value;
    });
  }

  function stripHtml(value) {
    var temp = document.createElement('div');
    temp.innerHTML = value || '';
    return (temp.textContent || temp.innerText || '').trim();
  }

  function formatMoney(value) {
    if (value === '' || value === null || isNaN(value)) return '$ --';
    return '$' + Number(value).toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 2 });
  }

  function formatDateTime(value) {
    if (!value) return '--';
    var d = new Date(value);
    if (isNaN(d.getTime())) return '--';
    return d.toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: 'numeric',
      minute: '2-digit'
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
    document.querySelectorAll('.field-input.border-red-400').forEach(function (el) {
      el.classList.remove('border-red-400', 'ring-2', 'ring-red-100');
    });
  }

  function markInvalid(input) {
    if (input) {
      input.classList.add('border-red-400', 'ring-2', 'ring-red-100');
    }
  }

  function composeGradeLabel(authenticator, gradeValue) {
    if (!authenticator || authenticator === 'Ungraded') return 'Ungraded';
    if (!gradeValue) return authenticator;
    return authenticator + ' ' + gradeValue;
  }

  function syncGradeHidden() {
    var authenticator = $('authenticator')?.value || '';
    var gradeValue = $('gradeValue')?.value || '';
    var gradeInput = $('grade');
    if (gradeInput) {
      gradeInput.value = composeGradeLabel(authenticator, gradeValue);
    }
  }

  function toggleGradeValueField() {
    var authenticator = $('authenticator')?.value || '';
    var gradeField = $('gradeValue');
    if (!gradeField) return;
    var isUngraded = authenticator === 'Ungraded';
    gradeField.disabled = isUngraded;
    gradeField.classList.toggle('opacity-50', isUngraded);
  }

  function getFormData() {
    syncGradeHidden();
    return {
      productName: $('productName')?.value.trim() || '',
      shortDescription: $('shortDescription')?.value.trim() || '',
      subtitle: $('subtitle')?.value.trim() || '',
      category: $('category')?.value || '',
      productDescription: state.editor ? state.editor.getData() : ($('productDescription')?.value || ''),
      authenticator: $('authenticator')?.value || '',
      gradeValue: $('gradeValue')?.value || '',
      year: $('year')?.value || '',
      setName: $('setName')?.value.trim() || '',
      language: $('language')?.value || '',
      cardNumber: $('cardNumber')?.value.trim() || '',
      grade: $('grade')?.value || '',
      certificateNumber: $('certificateNumber')?.value.trim() || '',
      startingPrice: $('startingPrice')?.value || '',
      bidStep: $('bidStep')?.value || '',
      buyNowPrice: $('buyNowPrice')?.value || '',
      registrationStartDate: $('registrationStartDate')?.value || '',
      registrationEndDate: $('registrationEndDate')?.value || '',
      startDate: $('startDate')?.value || '',
      endDate: $('endDate')?.value || '',
      imageCount: state.images.length,
      documentCount: state.documents.length
    };
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
        label: endingSoon
          ? t('phaseLiveEndingSoon', 'Ending Soon')
          : t('phaseLiveAuction', 'Live Now'),
        badgeClass: endingSoon ? 'bg-red-600 text-white' : 'bg-emerald-600 text-white',
        countdownLabel: t('countdownLiveEnd', 'Live ends in'),
        countdownTarget: data.endDate,
        endingSoon: endingSoon
      };
    }

    if (!isNaN(regEnd) && now >= regEnd) {
      return {
        label: t('phaseRegistrationClosed', 'Awaiting Live'),
        badgeClass: 'bg-amber-500 text-white',
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

  function buildPreviewSubtitle(data) {
    if (data.subtitle) return data.subtitle;
    var parts = [];
    if (data.setName) parts.push(data.setName);
    var grade = data.grade || composeGradeLabel(data.authenticator, data.gradeValue);
    if (grade) parts.push(grade);
    if (data.year) parts.push(data.year);
    return parts.length ? parts.join(' · ') : '\u00a0';
  }

  var PHASE_BADGE_CLASSES = [
    'bg-sky-600',
    'bg-amber-500',
    'bg-emerald-600',
    'bg-red-600',
    'bg-slate-600',
    'text-white'
  ];

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
    var phase = resolvePreviewPhase(data);

    setPreviewText('previewCategory', data.category || t('categoryDefault', '—'));
    setPreviewText('previewName', data.productName || t('productNameDefault', 'Product Name'));
    setPreviewText('previewSubtitle', buildPreviewSubtitle(data));
    setPreviewText('previewCurrentBid', formatCardMoney(data.startingPrice));
    setPreviewText('previewCountdownLabel', phase.countdownLabel);
    setPreviewText('previewTimeRemaining', formatTimeRemaining(phase.countdownTarget));

    $all('previewTimeRemaining').forEach(function (el) {
      el.classList.toggle('text-red-600', phase.endingSoon);
      el.classList.toggle('text-stone-700', !phase.endingSoon);
    });

    setPreviewPhaseBadge(phase);
    setPreviewMainImage(state.images.length > 0 ? state.images[0].url : '');
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
    Array.from(files).forEach(function (file) {
      if (state.images.length >= MAX_IMAGES) {
        return;
      }

      if (!IMAGE_TYPES.includes(file.type)) {
        errors.push(tf(t('errorImageInvalidFormat', '{0}: invalid format (JPG, PNG, WEBP only)'), file.name));
        return;
      }

      if (file.size > MAX_IMAGE_SIZE) {
        errors.push(tf(t('errorImageSizeLimit', '{0}: exceeds 5MB limit'), file.name));
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

  function renderDocuments() {
    var list = $('documentList');
    if (!list) return;
    list.innerHTML = '';

    state.documents.forEach(function (doc) {
      var li = document.createElement('li');
      li.className = 'flex flex-col gap-3 rounded-lg border border-slate-200 bg-slate-50 px-4 py-3 sm:flex-row sm:items-center sm:justify-between';
      var selectHtml = '<select data-doc-name="' + doc.id + '" class="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm text-slate-800 sm:w-52">';
      DOC_NAME_OPTIONS.forEach(function (option) {
        var selected = option === doc.displayName ? ' selected' : '';
        selectHtml += '<option value="' + option + '"' + selected + '>' + option + '</option>';
      });
      selectHtml += '</select>';
      li.innerHTML =
        '<div class="flex min-w-0 items-center gap-3">' +
        '<span class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-red-100 text-xs font-bold text-red-800">PDF</span>' +
        '<div class="min-w-0"><p class="truncate text-sm font-medium text-slate-800">' + doc.file.name + '</p>' +
        '<p class="text-xs text-slate-400">' + (doc.file.size / 1024).toFixed(1) + ' KB</p></div></div>' +
        '<div class="flex w-full flex-col gap-2 sm:w-auto sm:flex-row sm:items-center">' + selectHtml +
        '<button type="button" data-remove-doc="' + doc.id + '" class="shrink-0 cursor-pointer text-xs font-semibold text-red-600 hover:text-red-700">' + t('remove', 'Remove') + '</button></div>';
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

    updatePreview();
  }

  function addDocuments(files) {
    var errors = [];
    Array.from(files).forEach(function (file) {
      if (state.documents.length >= MAX_DOCUMENTS) {
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
    });

    if (errors.length) {
      showError('documents', errors[0]);
    } else {
      showError('documents', '');
    }

    renderDocuments();
  }

  function setupDropZone(zoneId, inputId, onFiles, resetAfterChange) {
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
      if (resetAfterChange) input.value = '';
    });
  }

  function toLocalDateTimeValue(date) {
    var pad = function (n) { return String(n).padStart(2, '0'); };
    return date.getFullYear() + '-' + pad(date.getMonth() + 1) + '-' + pad(date.getDate()) +
      'T' + pad(date.getHours()) + ':' + pad(date.getMinutes());
  }

  function syncLiveEndFromStart() {
    var startInput = $('startDate');
    var endInput = $('endDate');
    if (!startInput || !endInput || !startInput.value) return;
    var start = new Date(startInput.value);
    if (isNaN(start.getTime())) return;
    endInput.value = toLocalDateTimeValue(new Date(start.getTime() + DEFAULT_LIVE_DURATION_MS));
  }

  function syncLiveStartFromRegistrationEnd() {
    var registrationEndInput = $('registrationEndDate');
    var startInput = $('startDate');
    if (!registrationEndInput || !startInput || !registrationEndInput.value) return;
    startInput.value = registrationEndInput.value;
    syncLiveEndFromStart();
  }

  function validateForm() {
    clearErrors();
    var valid = true;
    var data = getFormData();
    var now = new Date();

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
    } else {
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
      showError('startDate', t('errorLiveStartRequired', 'Live start is required'));
      markInvalid($('startDate'));
      valid = false;
    } else if (data.registrationEndDate) {
      var liveStart = new Date(data.startDate);
      var registrationEnd = new Date(data.registrationEndDate);
      if (liveStart < registrationEnd) {
        showError('startDate', t('errorLiveStartAfterRegistration', 'Live start must be after registration ends'));
        markInvalid($('startDate'));
        valid = false;
      }
    }

    if (!data.endDate) {
      showError('endDate', t('errorLiveEndRequired', 'Live end is required'));
      markInvalid($('endDate'));
      valid = false;
    } else if (data.startDate) {
      var liveStartD = new Date(data.startDate);
      var liveEndD = new Date(data.endDate);
      if (liveEndD <= liveStartD) {
        showError('endDate', t('errorLiveEndAfterStart', 'Live end must be after live start'));
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
      if ($('shortDescription') && data.shortDescription) $('shortDescription').value = data.shortDescription;
      if ($('subtitle') && data.subtitle) $('subtitle').value = data.subtitle;
      if ($('category') && data.category) $('category').value = data.category;
      if ($('year') && data.year) $('year').value = data.year;
      if ($('setName') && data.setName) $('setName').value = data.setName;
      if ($('language') && data.language) $('language').value = data.language;
      if ($('cardNumber') && data.cardNumber) $('cardNumber').value = data.cardNumber;
      if ($('authenticator') && data.authenticator) $('authenticator').value = data.authenticator;
      if ($('gradeValue') && data.gradeValue) $('gradeValue').value = data.gradeValue;
      if ($('grade') && data.grade) $('grade').value = data.grade;
      if ($('certificateNumber') && data.certificateNumber) $('certificateNumber').value = data.certificateNumber;
      if ($('startingPrice') && data.startingPrice) $('startingPrice').value = data.startingPrice;
      if ($('bidStep') && data.bidStep) $('bidStep').value = data.bidStep;
      if ($('buyNowPrice') && data.buyNowPrice) $('buyNowPrice').value = data.buyNowPrice;
      if ($('registrationStartDate') && data.registrationStartDate) $('registrationStartDate').value = data.registrationStartDate;
      if ($('registrationEndDate') && data.registrationEndDate) $('registrationEndDate').value = data.registrationEndDate;
      if ($('startDate') && data.startDate) $('startDate').value = data.startDate;
      if ($('endDate') && data.endDate) $('endDate').value = data.endDate;

      toggleGradeValueField();
      syncGradeHidden();

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

  function requestConfirm(options) {
    if (typeof window.showConfirmModal === 'function') {
      return window.showConfirmModal(options);
    }

    var message = options.message || '';
    if (options.note) {
      message += '\n\n' + options.note;
    }

    return Promise.resolve(window.confirm(message));
  }

  function submitAuctionForm(data, formData) {
    return fetch(form.action, {
      method: 'POST',
      body: formData,
      headers: { 'X-Requested-With': 'XMLHttpRequest' }
    })
      .then(function (response) {
        return response.json().then(function (payload) {
          if (!response.ok || payload.success === false) {
            throw new Error(payload.message || t('errorCreateFailed', 'Could not create auction.'));
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
        showSubmitStatus('error', error.message);
      });
  }

  function showSuccess(name) {
    try { localStorage.removeItem(DRAFT_KEY); } catch (e) { /* ignore */ }
  }

  function showTopToast(type, message) {
    // Server pushes FCM / in-app notifications; keep form-level status only.
  }

  function showSubmitStatus(type, message) {
    var status = $('createAuctionStatus');
    if (!status) return;

    status.textContent = message;
    status.className = 'mb-4 rounded-lg border px-4 py-3 text-sm font-medium';

    if (type === 'success') {
      status.classList.add('border-emerald-200', 'bg-emerald-50', 'text-emerald-800');
    } else {
      status.classList.add('border-red-200', 'bg-red-50', 'text-red-700');
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
    var fields = [
      'productName', 'shortDescription', 'subtitle', 'category',
      'year', 'setName', 'language', 'cardNumber', 'authenticator', 'gradeValue', 'grade', 'certificateNumber',
      'startingPrice', 'bidStep', 'buyNowPrice',
      'registrationStartDate', 'registrationEndDate', 'startDate', 'endDate'
    ];

    fields.forEach(function (id) {
      var el = $(id);
      if (!el) return;
      el.addEventListener('input', updatePreview);
      el.addEventListener('change', updatePreview);
    });

    var registrationEndField = $('registrationEndDate');
    if (registrationEndField) {
      registrationEndField.addEventListener('change', function () {
        syncLiveStartFromRegistrationEnd();
        updatePreview();
      });
    }

    var liveStartField = $('startDate');
    if (liveStartField) {
      liveStartField.addEventListener('change', function () {
        syncLiveEndFromStart();
        updatePreview();
      });
    }

    var authenticatorField = $('authenticator');
    if (authenticatorField) {
      authenticatorField.addEventListener('change', function () {
        toggleGradeValueField();
        updatePreview();
      });
      toggleGradeValueField();
    }

    setupDropZone('imageDropZone', 'imageInput', addImages, false);
    setupDropZone('documentDropZone', 'documentInput', addDocuments, true);

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
      for (var i = 1; i < state.images.length; i++) {
        formData.append('GalleryImageFiles', state.images[i].file);
      }
      state.documents.forEach(function (doc) {
        formData.append('DocumentFiles', doc.file);
        formData.append('DocumentNames', doc.displayName || 'PSA Certificate');
      });

      var productName = data.productName || t('productNameDefault', 'Product name');

      requestConfirm({
        title: t('confirmCreateTitle', 'Submit auction listing?'),
        message: t('confirmCreateMessage', 'You are about to submit "{0}" for admin review before it goes live.'),
        messageArgs: [productName],
        note: t('confirmCreateNote', 'The listing will be pending review.'),
        confirmText: t('confirmCreateConfirm', 'Create auction')
      }).then(function (confirmed) {
        if (!confirmed) {
          return;
        }

        submitAuctionForm(data, formData);
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
