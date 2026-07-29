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
  var iconSuccess = document.getElementById('confirmModalIconSuccess');
  var cancelBtn = document.getElementById('confirmModalCancelBtn');
  var confirmBtn = document.getElementById('confirmModalConfirmBtn');

  var defaults = (window.confirmModalConfig && window.confirmModalConfig.i18n) || {};
  var pendingResolve = null;
  var lastFocusedElement = null;
  var alertMode = false;

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

  function isModalOpen() {
    return Boolean(overlay && !overlay.hidden);
  }

  function applyClassFromData(element, key) {
    if (!element) {
      return;
    }

    var className = element.getAttribute(key);
    if (className) {
      element.className = className;
    }
  }

  function setVariant(variant) {
    var isDanger = variant === 'danger';
    var isSuccess = variant === 'success';
    var classKey = isDanger
      ? 'data-class-danger'
      : isSuccess
        ? 'data-class-success'
        : 'data-class-default';

    applyClassFromData(iconWrap, classKey);
    applyClassFromData(confirmBtn, classKey);

    if (iconInfo) {
      iconInfo.classList.toggle('hidden', isDanger || isSuccess);
    }

    if (iconDanger) {
      iconDanger.classList.toggle('hidden', !isDanger);
    }

    if (iconSuccess) {
      iconSuccess.classList.toggle('hidden', !isSuccess);
    }
  }

  function setAlertMode(enabled) {
    alertMode = Boolean(enabled);
    if (!cancelBtn) {
      return;
    }

    // Use the HTML hidden attribute — Tailwind `hidden` loses to `inline-flex`.
    cancelBtn.hidden = alertMode;
    cancelBtn.setAttribute('aria-hidden', alertMode ? 'true' : 'false');
    cancelBtn.style.display = alertMode ? 'none' : '';
    cancelBtn.disabled = alertMode;
  }

  function resolveLabel(value, fallback) {
    if (!value || value.indexOf('_') >= 0 && value === value.replace(/[^A-Za-z0-9_]/g, '')) {
      // Missing i18n often returns the raw key (e.g. AlertModal_Ok).
      if (value && /^[A-Za-z][A-Za-z0-9]*(_[A-Za-z0-9]+)+$/.test(value)) {
        return fallback;
      }
    }
    return value || fallback;
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
    document.removeEventListener('keydown', onKeyDown);
    setAlertMode(false);

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
    if (event.key === 'Escape' && isModalOpen()) {
      event.preventDefault();
      closeModal(true);
    }
  }

  function openModal(options) {
    options = options || {};

    if (!overlay || !titleEl || !messageEl || !confirmBtn || !cancelBtn) {
      var fallbackMessage = options.message || options.title || '';
      if (options.alertOnly) {
        window.alert(fallbackMessage);
        return Promise.resolve(true);
      }

      return Promise.resolve(window.confirm(fallbackMessage));
    }

    if (pendingResolve) {
      closeModal(false);
    }

    setAlertMode(Boolean(options.alertOnly));
    setVariant(options.variant || 'default');

    var defaultTitle = options.alertOnly
      ? (options.variant === 'danger'
        ? resolveLabel(defaults.errorTitle, 'Error')
        : options.variant === 'success'
          ? resolveLabel(defaults.successTitle, 'Success')
          : resolveLabel(defaults.title, 'Notice'))
      : resolveLabel(defaults.title, 'Confirm');

    titleEl.textContent = options.title || defaultTitle;
    messageEl.textContent = applyTemplate(
      options.message || defaults.message || '',
      options.messageArgs || []);
    confirmBtn.textContent = options.confirmText
      || (options.alertOnly
        ? resolveLabel(defaults.ok, resolveLabel(defaults.confirm, 'OK'))
        : resolveLabel(defaults.confirm, 'Confirm'));
    cancelBtn.textContent = options.cancelText || resolveLabel(defaults.cancel, 'Cancel');

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
    document.addEventListener('keydown', onKeyDown);
    confirmBtn.focus();

    return new Promise(function (resolve) {
      pendingResolve = resolve;
    });
  }

  function showConfirmModal(options) {
    options = options || {};
    options.alertOnly = false;
    return openModal(options);
  }

  function showAlertModal(options) {
    options = options || {};
    options.alertOnly = true;
    if (!options.variant) {
      options.variant = 'default';
    }
    return openModal(options);
  }

  function readConfirmOptions(element) {
    return {
      title: element.getAttribute('data-confirm-title') || '',
      message: element.getAttribute('data-confirm-message') || '',
      note: element.getAttribute('data-confirm-note') || '',
      confirmText: element.getAttribute('data-confirm-confirm') || '',
      cancelText: element.getAttribute('data-confirm-cancel') || '',
      variant: element.getAttribute('data-confirm-variant') || 'danger'
    };
  }

  function findConfirmSource(form, submitter) {
    if (submitter && submitter.hasAttribute('data-confirm')) {
      return submitter;
    }

    if (form && form.hasAttribute('data-confirm')) {
      return form;
    }

    return null;
  }

  function acceptAndResubmit(form, submitter) {
    form.dataset.confirmAccepted = '1';
    if (typeof form.requestSubmit === 'function') {
      form.requestSubmit(submitter || undefined);
      return;
    }

    HTMLFormElement.prototype.submit.call(form);
  }

  document.addEventListener('submit', function (event) {
    var form = event.target;
    if (!(form instanceof HTMLFormElement)) {
      return;
    }

    if (form.dataset.confirmAccepted === '1') {
      delete form.dataset.confirmAccepted;
      return;
    }

    var source = findConfirmSource(form, event.submitter);
    if (!source) {
      return;
    }

    event.preventDefault();
    event.stopPropagation();

    var submitter = event.submitter;
    showConfirmModal(readConfirmOptions(source)).then(function (confirmed) {
      if (confirmed) {
        acceptAndResubmit(form, submitter);
      }
    });
  }, true);

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
        closeModal(true);
      }
    });
  }

  if (dialog) {
    dialog.addEventListener('click', function (event) {
      event.stopPropagation();
    });
  }

  window.showConfirmModal = showConfirmModal;
  window.showAlertModal = showAlertModal;
})();
