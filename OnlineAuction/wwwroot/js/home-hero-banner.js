(function () {
  var root = document.querySelector('[data-home-hero-banner]');
  if (!root) {
    return;
  }

  var slides = Array.from(root.querySelectorAll('[data-home-hero-slide]'));
  var backdropImages = Array.from(root.querySelectorAll('[data-home-hero-backdrop]'));
  var total = slides.length;
  var index = 0;
  var timer = null;
  var intervalMs = 5500;

  if (total <= 1) {
    return;
  }

  function setActive(nextIndex) {
    index = (nextIndex + total) % total;

    slides.forEach(function (slide, i) {
      slide.classList.toggle('is-active', i === index);
      slide.setAttribute('aria-hidden', i === index ? 'false' : 'true');
    });

    backdropImages.forEach(function (image, i) {
      image.classList.toggle('is-active', i === index);
    });
  }

  function startAutoplay() {
    if (timer) {
      window.clearInterval(timer);
    }

    timer = window.setInterval(function () {
      setActive(index + 1);
    }, intervalMs);
  }

  setActive(0);
  startAutoplay();
})();
