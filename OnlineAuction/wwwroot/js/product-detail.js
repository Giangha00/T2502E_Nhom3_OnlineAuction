(function () {
  'use strict';

  var mainImage = document.getElementById('mainProductImage');
  var thumbs = document.querySelectorAll('.gallery-thumb');
  var bidSelect = document.getElementById('bidAmount');
  var tabButtons = document.querySelectorAll('.product-detail-tabs__tab');
  var tabPanels = document.querySelectorAll('.product-detail-tabs__panel');

  if (mainImage && thumbs.length) {
    thumbs.forEach(function (thumb) {
      thumb.addEventListener('click', function () {
        var src = thumb.getAttribute('data-image');
        if (!src) return;

        mainImage.style.opacity = '0';
        setTimeout(function () {
          mainImage.src = src;
          mainImage.style.opacity = '1';
        }, 150);

        thumbs.forEach(function (t) {
          t.classList.remove('border-slate-900');
          t.classList.add('border-transparent');
        });
        thumb.classList.remove('border-transparent');
        thumb.classList.add('border-slate-900');
      });
    });

    mainImage.style.transition = 'opacity 0.3s ease';
  }

  tabButtons.forEach(function (button) {
    button.addEventListener('click', function () {
      var target = button.getAttribute('data-tab');
      if (!target) return;

      tabButtons.forEach(function (btn) {
        btn.classList.remove('is-active');
        btn.setAttribute('aria-selected', 'false');
      });
      button.classList.add('is-active');
      button.setAttribute('aria-selected', 'true');

      tabPanels.forEach(function (panel) {
        var isMatch = panel.getAttribute('data-panel') === target;
        panel.classList.toggle('is-active', isMatch);
        panel.hidden = !isMatch;
      });
    });
  });
})();
