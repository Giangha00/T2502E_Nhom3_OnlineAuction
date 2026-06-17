(function () {
  'use strict';

  var i18n = (window.buyNowDetailConfig && window.buyNowDetailConfig.i18n) || {};
  var panel = document.querySelector('.product-buy-panel');
  var addToCartBtn = document.getElementById('addToCartBtn');
  var messageEl = document.getElementById('addToCartMessage');

  if (!panel || !addToCartBtn) return;

  addToCartBtn.addEventListener('click', function () {
    var isLoggedIn = panel.getAttribute('data-is-logged-in') === 'true';

    if (!isLoggedIn) {
      if (typeof window.openAuthModal === 'function') {
        window.openAuthModal('login');
      }
      return;
    }

    var productId = panel.getAttribute('data-product-id');
    if (!productId) return;

    addToCartBtn.disabled = true;
    addToCartBtn.textContent = i18n.adding || 'Adding…';

    var body = new URLSearchParams();
    body.append('productId', productId);

    var csrfMeta = document.querySelector('meta[name="request-verification-token"]');
    var csrfToken = csrfMeta ? csrfMeta.getAttribute('content') : '';
    if (csrfToken) {
      body.append('__RequestVerificationToken', csrfToken);
    }

    fetch('/BuyNow/AddToCart', {
      method: 'POST',
      credentials: 'same-origin',
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded'
      },
      body: body.toString()
    })
      .then(function (response) {
        if (!response.ok) {
          throw new Error('Add to cart failed');
        }
        return response.json();
      })
      .then(function (data) {
        if (!data.success) {
          throw new Error(data.message || 'Add to cart failed');
        }

        addToCartBtn.disabled = false;
        addToCartBtn.textContent = i18n.addToCart || 'Add to Cart';

        if (messageEl) {
          messageEl.textContent = data.message || i18n.addedToCart || 'Added to cart.';
          messageEl.classList.remove('hidden');
        }
      })
      .catch(function () {
        addToCartBtn.disabled = false;
        addToCartBtn.textContent = i18n.addToCart || 'Add to Cart';
        window.alert(i18n.addFailed || 'Unable to add to cart. Please try again.');
      });
  });
})();
