(function () {
  function getItemSelector(section) {
    if (section.hasAttribute('data-product-related-carousel')) {
      return '.product-detail-related-row__item';
    }

    return section.hasAttribute('data-home-carousel-header-nav')
      ? '.rarecard-recommended-row__item'
      : '.rarecard-home-row__item';
  }

  function getVisibleRange(track, itemSelector) {
    var items = Array.from(track.querySelectorAll(itemSelector));
    var total = items.length;
    if (total === 0) {
      return { start: 0, end: 0, total: 0 };
    }

    var trackRect = track.getBoundingClientRect();
    var visibleIndexes = [];

    items.forEach(function (item, index) {
      var rect = item.getBoundingClientRect();
      if (rect.right > trackRect.left + 4 && rect.left < trackRect.right - 4) {
        visibleIndexes.push(index);
      }
    });

    if (visibleIndexes.length === 0) {
      return { start: 1, end: Math.min(4, total), total: total };
    }

    return {
      start: visibleIndexes[0] + 1,
      end: visibleIndexes[visibleIndexes.length - 1] + 1,
      total: total
    };
  }

  function updateCarouselRange(section) {
    var track = section.querySelector('[data-home-carousel-track]');
    var counter = section.querySelector('[data-home-carousel-range]');
    if (!track || !counter) {
      return;
    }

    var range = getVisibleRange(track, getItemSelector(section));
    var i18n = window.homeCarouselI18n || {};
    if (range.total === 0) {
      counter.textContent = i18n.showingZero || 'Showing 0 items';
      return;
    }

    var template = i18n.showingRange || 'Showing {0} – {1} of {2}';
    counter.textContent = template
      .replace('{0}', String(range.start))
      .replace('{1}', String(range.end))
      .replace('{2}', String(range.total));
  }

  function updateCarouselButtons(section) {
    var shell = section.querySelector('.home-carousel-shell');
    var track = section.querySelector('[data-home-carousel-track]');
    var prev = section.querySelector('[data-home-carousel-prev]');
    var next = section.querySelector('[data-home-carousel-next]');
    if (!track || !prev || !next) {
      return;
    }

    var maxScroll = track.scrollWidth - track.clientWidth;
    var canScrollLeft = track.scrollLeft > 4;
    var canScrollRight = track.scrollLeft < maxScroll - 4;

    prev.disabled = !canScrollLeft;
    next.disabled = !canScrollRight;

    if (shell) {
      shell.classList.toggle('can-scroll-left', canScrollLeft);
      shell.classList.toggle('can-scroll-right', canScrollRight);
    }

    updateCarouselRange(section);
  }

  function triggerNudge(track, direction) {
    track.classList.remove('is-nudge-left', 'is-nudge-right', 'is-scrolling');
    void track.offsetWidth;
    track.classList.add(direction < 0 ? 'is-nudge-left' : 'is-nudge-right', 'is-scrolling');

    window.setTimeout(function () {
      track.classList.remove('is-nudge-left', 'is-nudge-right', 'is-scrolling');
    }, 420);
  }

  function pulseButton(button) {
    if (!button || button.disabled) {
      return;
    }

    button.classList.add('is-pressed');
    window.setTimeout(function () {
      button.classList.remove('is-pressed');
    }, 180);
  }

  function scrollCarousel(section, direction) {
    var track = section.querySelector('[data-home-carousel-track]');
    var prev = section.querySelector('[data-home-carousel-prev]');
    var next = section.querySelector('[data-home-carousel-next]');
    if (!track) {
      return;
    }

    var item = track.querySelector(getItemSelector(section));
    var gap = 16;
    var amount = item ? item.getBoundingClientRect().width + gap : track.clientWidth * 0.75;
    var scrollMultiplier = section.hasAttribute('data-home-carousel-header-nav')
      ? 2
      : section.hasAttribute('data-product-related-carousel')
        ? 4
        : 3;

    triggerNudge(track, direction);
    pulseButton(direction < 0 ? prev : next);
    track.scrollBy({ left: direction * amount * scrollMultiplier, behavior: 'smooth' });
  }

  document.querySelectorAll('[data-home-carousel]').forEach(function (section) {
    var track = section.querySelector('[data-home-carousel-track]');
    var prev = section.querySelector('[data-home-carousel-prev]');
    var next = section.querySelector('[data-home-carousel-next]');

    if (!track) {
      return;
    }

    prev?.addEventListener('click', function () {
      scrollCarousel(section, -1);
    });

    next?.addEventListener('click', function () {
      scrollCarousel(section, 1);
    });

    track.addEventListener('scroll', function () {
      track.classList.add('is-scrolling');
      updateCarouselButtons(section);
      window.clearTimeout(track._scrollTimer);
      track._scrollTimer = window.setTimeout(function () {
        track.classList.remove('is-scrolling');
      }, 180);
    }, { passive: true });

    window.addEventListener('resize', function () {
      updateCarouselButtons(section);
    });

    updateCarouselButtons(section);
  });
})();
