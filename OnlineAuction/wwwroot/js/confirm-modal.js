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

    applyClassFromData(iconWrap, isDanger ? 'data-class-danger' : 'data-class-default');
    applyClassFromData(confirmBtn, isDanger ? 'data-class-danger' : 'data-class-default');

    if (iconInfo) {
      iconInfo.classList.toggle('hidden', isDanger);
    }

    if (iconDanger) {
      iconDanger.classList.toggle('hidden', !isDanger);
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
    document.removeEventListener('keydown', onKeyDown);

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
      closeModal(false);
    }
  }

  function showConfirmModal(options) {
    options = options || {};

    if (!overlay || !titleEl || !messageEl || !confirmBtn || !cancelBtn) {
      return Promise.resolve(window.confirm(options.message || options.title || ''));
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
    document.addEventListener('keydown', onKeyDown);
    confirmBtn.focus();

    return new Promise(function (resolve) {
      pendingResolve = resolve;
    });
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
        closeModal(false);
      }
    });
  }

  if (dialog) {
    dialog.addEventListener('click', function (event) {
      event.stopPropagation();
    });
  }

  window.showConfirmModal = showConfirmModal;
})();
