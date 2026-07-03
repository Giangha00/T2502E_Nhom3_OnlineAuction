(function () {
  'use strict';

  var NAVIGATION_KEYS = new Set([
    'Backspace',
    'Delete',
    'Tab',
    'Escape',
    'Enter',
    'ArrowLeft',
    'ArrowRight',
    'ArrowUp',
    'ArrowDown',
    'Home',
    'End'
  ]);

  function sanitizeInteger(value) {
    return String(value || '').replace(/\D/g, '');
  }

  function sanitizeDecimal(value) {
    var raw = String(value || '').replace(/[^\d.]/g, '');
    var dotIndex = raw.indexOf('.');

    if (dotIndex === -1) {
      return raw;
    }

    var whole = raw.slice(0, dotIndex);
    var fraction = raw.slice(dotIndex + 1).replace(/\./g, '');
    return whole + '.' + fraction;
  }

  function isNavigationEvent(event) {
    return event.ctrlKey || event.metaKey || event.altKey || NAVIGATION_KEYS.has(event.key);
  }

  function bindIntegerInput(input) {
    input.setAttribute('inputmode', 'numeric');
    input.setAttribute('autocomplete', 'off');

    input.addEventListener('keydown', function (event) {
      if (isNavigationEvent(event)) {
        return;
      }

      if (event.key.length === 1 && /\d/.test(event.key)) {
        return;
      }

      event.preventDefault();
    });

    input.addEventListener('input', function () {
      var sanitized = sanitizeInteger(input.value);
      if (input.value !== sanitized) {
        input.value = sanitized;
      }
    });
  }

  function bindDecimalInput(input) {
    input.setAttribute('inputmode', 'decimal');
    input.setAttribute('autocomplete', 'off');

    input.addEventListener('keydown', function (event) {
      if (isNavigationEvent(event)) {
        return;
      }

      if (event.key === '.' && !input.value.includes('.')) {
        return;
      }

      if (event.key.length === 1 && /\d/.test(event.key)) {
        return;
      }

      event.preventDefault();
    });

    input.addEventListener('input', function () {
      var sanitized = sanitizeDecimal(input.value);
      if (input.value !== sanitized) {
        input.value = sanitized;
      }
    });
  }

  function initSellNumericInputs(root) {
    (root || document).querySelectorAll('[data-sell-numeric]').forEach(function (input) {
      if (input.dataset.sellNumericBound === 'true') {
        return;
      }

      input.dataset.sellNumericBound = 'true';
      var kind = input.getAttribute('data-sell-numeric');

      if (kind === 'integer') {
        bindIntegerInput(input);
      } else if (kind === 'decimal') {
        bindDecimalInput(input);
      }
    });
  }

  document.addEventListener('DOMContentLoaded', function () {
    initSellNumericInputs();
  });

  window.sellNumericInput = {
    init: initSellNumericInputs
  };
})();
