(function () {
  'use strict';

  var overlay = document.getElementById('confirmModalOverlay');
  var dialog = document.getElementById('confirmModalDialog');
  var titleEl = document.getElementById('confirmModalTitle');
  var messageEl = document.getElementById('confirmModalMessage');
  var noteEl = document.getElementById('confirmModalNote');
  var iconWrap = document.getElementById('confirmModalIconWrap');
  var iconInfo = document.getElementById('confirmModalIconInfo');
  var iconDanger = document.getElementById('confirmModalIconDanger');
  var cancelBtn = document.getElementById('confirmModalCancelBtn');
  var confirmBtn = document.getElementById('confirmModalConfirmBtn');

  var defaults = (window.confirmModalConfig && window.confirmModalConfig.i18n) || {};
  var pendingResolve = null;
  var lastFocusedElement = null;

  function applyTemplate(text, values) {
    if (!text) {
      return '';
    }

    var result = text;
    (values || []).forEach(function (value, index) {
      result = result.replace('{' + index + '}', value);
    });
    return result;
  }

  function setVariant(variant) {
    var isDanger = variant === 'danger';

    if (iconWrap) {
      iconWrap.className = isDanger
        ? 'mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-red-100'
        : 'mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-blue-100';
    }

    if (iconInfo) {
      iconInfo.classList.toggle('hidden', isDanger);
    }

    if (iconDanger) {
      iconDanger.classList.toggle('hidden', !isDanger);
    }

    if (confirmBtn) {
      confirmBtn.className = isDanger
        ? 'inline-flex w-full items-center justify-center rounded-xl bg-red-600 px-4 py-3 text-sm font-semibold text-white transition hover:bg-red-700 sm:w-auto sm:min-w-[7.5rem]'
        : 'inline-flex w-full items-center justify-center rounded-xl bg-slate-900 px-4 py-3 text-sm font-semibold text-white transition hover:bg-slate-800 sm:w-auto sm:min-w-[7.5rem]';
    }
  }

  function closeModal(confirmed) {
    if (!overlay) {
      return;
    }

    overlay.classList.add('hidden');
    overlay.classList.remove('flex');
    overlay.hidden = true;
    overlay.setAttribute('aria-hidden', 'true');
    document.body.classList.remove('confirm-modal-open');

    if (pendingResolve) {
      var resolve = pendingResolve;
      pendingResolve = null;
      resolve(Boolean(confirmed));
    }

    if (lastFocusedElement && typeof lastFocusedElement.focus === 'function') {
      lastFocusedElement.focus();
    }
  }

  function onKeyDown(event) {
    if (event.key === 'Escape') {
      event.preventDefault();
      closeModal(false);
    }
  }

  function showConfirmModal(options) {
    options = options || {};

    if (!overlay || !titleEl || !messageEl || !confirmBtn || !cancelBtn) {
      return Promise.resolve(window.confirm(options.message || ''));
    }

    if (pendingResolve) {
      closeModal(false);
    }

    setVariant(options.variant || 'default');

    titleEl.textContent = options.title || defaults.title || 'Confirm';
    messageEl.textContent = applyTemplate(
      options.message || defaults.message || '',
      options.messageArgs || []);
    confirmBtn.textContent = options.confirmText || defaults.confirm || 'Confirm';
    cancelBtn.textContent = options.cancelText || defaults.cancel || 'Cancel';

    if (noteEl) {
      var note = applyTemplate(options.note || '', options.noteArgs || []);
      if (note) {
        noteEl.textContent = note;
        noteEl.classList.remove('hidden');
      } else {
        noteEl.textContent = '';
        noteEl.classList.add('hidden');
      }
    }

    lastFocusedElement = document.activeElement;
    overlay.hidden = false;
    overlay.classList.remove('hidden');
    overlay.classList.add('flex');
    overlay.setAttribute('aria-hidden', 'false');
    document.body.classList.add('confirm-modal-open');
    confirmBtn.focus();

    return new Promise(function (resolve) {
      pendingResolve = resolve;
    });
  }

  if (cancelBtn) {
    cancelBtn.addEventListener('click', function () {
      closeModal(false);
    });
  }

  if (confirmBtn) {
    confirmBtn.addEventListener('click', function () {
      closeModal(true);
    });
  }

  if (overlay) {
    overlay.addEventListener('click', function (event) {
      if (event.target === overlay) {
        closeModal(false);
      }
    });
  }

  document.addEventListener('keydown', onKeyDown);

  window.showConfirmModal = showConfirmModal;
})();
