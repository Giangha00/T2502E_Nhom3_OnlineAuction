(function () {
  'use strict';

  var i18n = (window.productDetailConfig && window.productDetailConfig.i18n) || {};
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

  var bidPanel = document.querySelector('.product-bid-panel');
  var placeBidBtn = document.getElementById('placeBidBtn');

  if (placeBidBtn && bidPanel) {
    placeBidBtn.addEventListener('click', function () {
      var isLoggedIn = bidPanel.getAttribute('data-is-logged-in') === 'true';

      if (!isLoggedIn) {
        if (typeof window.openAuthModal === 'function') {
          window.openAuthModal('signup');
        }
        return;
      }

      var auctionId = bidPanel.getAttribute('data-auction-id');
      var amount = bidSelect ? bidSelect.value : null;
      if (!auctionId || !amount) return;

      placeBidBtn.disabled = true;
      placeBidBtn.textContent = i18n.placingBid || 'Placing bid…';

      var body = new URLSearchParams();
      body.append('auctionId', auctionId);
      body.append('amount', amount);

      var csrfMeta = document.querySelector('meta[name="request-verification-token"]');
      var csrfToken = csrfMeta ? csrfMeta.getAttribute('content') : '';
      if (csrfToken) {
        body.append('__RequestVerificationToken', csrfToken);
      }

      fetch('/Order/PlaceBid', {
        method: 'POST',
        credentials: 'same-origin',
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded'
        },
        body: body.toString()
      })
        .then(function (response) {
          if (!response.ok) {
            throw new Error('Bid failed');
          }
          return response.json();
        })
        .then(function (data) {
          if (data.success && data.redirectUrl) {
            window.location.href = data.redirectUrl;
            return;
          }
          throw new Error(data.message || 'Bid failed');
        })
        .catch(function () {
          placeBidBtn.disabled = false;
          placeBidBtn.textContent = i18n.bid || 'Bid';
          window.alert(i18n.bidFailed || 'Unable to place bid. Please try again.');
        });
    });
  }
})();
