(function () {
  function getToken() {
    var meta = document.querySelector('meta[name="request-verification-token"]');
    return meta ? meta.getAttribute('content') : '';
  }

  function updateButtonState(button, isWatched) {
    button.classList.toggle('is-watched', isWatched);
    button.classList.toggle('text-red-500', isWatched);
    button.classList.toggle('text-stone-400', !isWatched);
    button.setAttribute('aria-pressed', isWatched ? 'true' : 'false');
    button.setAttribute('aria-label', isWatched ? 'Remove from watchlist' : 'Add to watchlist');
    var icon = button.querySelector('svg path');
    if (icon) {
      icon.setAttribute('fill', isWatched ? 'currentColor' : 'none');
    }
  }

  function updateWatchlistCount(count) {
    document.querySelectorAll('[data-watchlist-count]').forEach(function (el) {
      el.textContent = el.textContent.replace(/\(\d+\)/, '(' + count + ')');
    });
  }

  function toggleWatchlist(auctionId, button) {
    if (!auctionId || button.disabled) {
      return;
    }

    button.disabled = true;

    fetch('/Watchlist/Toggle/' + auctionId, {
      method: 'POST',
      credentials: 'same-origin',
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded',
        'X-Requested-With': 'XMLHttpRequest'
      },
      body: (function () {
        var body = new URLSearchParams();
        var token = getToken();
        if (token) {
          body.append('__RequestVerificationToken', token);
        }
        return body.toString();
      })()
    })
      .then(function (response) {
        if (response.status === 401) {
          window.location.href = '/Auth/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
          return null;
        }
        return response.json();
      })
      .then(function (data) {
        if (!data || !data.success) {
          return;
        }

        updateButtonState(button, data.isWatched);
        updateWatchlistCount(data.count);

        if (!data.isWatched && button.closest('[data-watchlist-grid]')) {
          var card = button.closest('.rarecard-card--grid');
          if (card) {
            card.remove();
          }
          if (!document.querySelector('[data-watchlist-grid] .rarecard-card--grid')) {
            window.location.reload();
          }
        }
      })
      .catch(function () { })
      .finally(function () {
        button.disabled = false;
      });
  }

  function initWatchlistButtons(root) {
    (root || document).querySelectorAll('[data-watchlist-toggle]').forEach(function (button) {
      if (button.dataset.watchlistBound === 'true') {
        return;
      }

      button.dataset.watchlistBound = 'true';
      updateButtonState(button, button.dataset.watched === 'true');

      button.addEventListener('click', function (event) {
        event.preventDefault();
        event.stopPropagation();
        toggleWatchlist(button.dataset.auctionId, button);
      });
    });
  }

  function loadWatchedState() {
    fetch('/Watchlist/Ids', {
      headers: { 'X-Requested-With': 'XMLHttpRequest' }
    })
      .then(function (response) {
        if (!response.ok) {
          return null;
        }
        return response.json();
      })
      .then(function (data) {
        if (!data || !data.success || !Array.isArray(data.auctionIds)) {
          return;
        }

        var watched = new Set(data.auctionIds);
        document.querySelectorAll('[data-watchlist-toggle]').forEach(function (button) {
          var id = parseInt(button.dataset.auctionId, 10);
          updateButtonState(button, watched.has(id));
          button.dataset.watched = watched.has(id) ? 'true' : 'false';
        });

        if (typeof data.auctionIds.length === 'number') {
          updateWatchlistCount(data.auctionIds.length);
        }
      })
      .catch(function () { });
  }

  document.addEventListener('DOMContentLoaded', function () {
    initWatchlistButtons();
    if (document.querySelector('[data-watchlist-toggle]')) {
      loadWatchedState();
    }
  });

  window.watchlist = {
    init: initWatchlistButtons,
    refresh: loadWatchedState
  };
})();
