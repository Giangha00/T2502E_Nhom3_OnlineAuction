(function () {
  'use strict';

  var i18n = (window.buyNowDetailConfig && window.buyNowDetailConfig.i18n) || {};
  var panel = document.querySelector('.product-buy-panel');
  var addToCartBtn = document.getElementById('addToCartBtn');

  if (!panel || !addToCartBtn || !window.buyNowCart) return;

  addToCartBtn.addEventListener('click', function () {
    var auctionId = panel.getAttribute('data-auction-id');
    var isLoggedIn = panel.getAttribute('data-is-logged-in') === 'true';

    addToCartBtn.disabled = true;
    addToCartBtn.textContent = i18n.adding || 'Adding…';

    window.buyNowCart.add(auctionId, {
      isLoggedIn: isLoggedIn,
      signInMessage: i18n.signInRequired || 'Please sign in to continue.',
      failedMessage: i18n.addFailed || 'Unable to add to cart. Please try again.'
    })
      .then(function (data) {
        if (data.redirectUrl) {
          window.location.href = data.redirectUrl;
        }
      })
      .catch(function () {
        addToCartBtn.disabled = false;
        addToCartBtn.textContent = i18n.addToCart || 'Add to Cart';
      });
  });
})();
