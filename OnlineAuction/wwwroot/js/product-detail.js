(function () {
  'use strict';

  var mainImage = document.getElementById('mainProductImage');
  var thumbs = document.querySelectorAll('.gallery-thumb');

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
          t.classList.remove('border-amber-500', 'ring-2', 'ring-amber-100');
          t.classList.add('border-transparent');
        });
        thumb.classList.remove('border-transparent');
        thumb.classList.add('border-amber-500', 'ring-2', 'ring-amber-100');
      });
    });

    mainImage.style.transition = 'opacity 0.3s ease';
  }
})();
