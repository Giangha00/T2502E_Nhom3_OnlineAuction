(function () {
  var root = document.getElementById('userMenuRoot');
  var btn = document.getElementById('userMenuBtn');
  var panel = document.getElementById('userMenuPanel');
  if (!root || !btn || !panel) return;

  function openMenu() {
    panel.classList.remove('hidden');
    btn.setAttribute('aria-expanded', 'true');
  }

  function closeMenu() {
    panel.classList.add('hidden');
    btn.setAttribute('aria-expanded', 'false');
  }

  function toggleMenu() {
    if (panel.classList.contains('hidden')) {
      openMenu();
    } else {
      closeMenu();
    }
  }

  btn.addEventListener('click', function (e) {
    e.stopPropagation();
    toggleMenu();
  });

  document.addEventListener('click', function (e) {
    if (!root.contains(e.target)) {
      closeMenu();
    }
  });

  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') {
      closeMenu();
    }
  });

  var copyBtn = panel.querySelector('[data-copy-vault]');
  var vaultText = document.getElementById('vaultAddressText');
  if (copyBtn && vaultText) {
    copyBtn.addEventListener('click', function (e) {
      e.preventDefault();
      e.stopPropagation();
      var text = vaultText.innerText.replace(/\s*\n\s*/g, '\n').trim();
      if (navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(text);
      }
    });
  }
})();
