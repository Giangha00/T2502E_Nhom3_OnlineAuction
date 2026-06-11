(function () {
    const catalogEl = document.getElementById('auctionCatalog');
    const watchingList = document.getElementById('watchingList');
    const watchingEmpty = document.getElementById('watchingEmpty');
    const watchingCountBadge = document.getElementById('watchingCountBadge');
    const summaryWatching = document.getElementById('summaryWatching');
    const summaryTotal = document.getElementById('summaryTotal');

    if (!catalogEl || !watchingList || !window.AuctionCart) return;

    const catalog = JSON.parse(catalogEl.textContent || '[]');
    const wonCount = window.cartPageConfig?.wonCount || 0;

    function findAuction(id) {
        return catalog.find(function (a) { return a.Id === id; });
    }

    function formatPrice(value) {
        return '$' + Number(value).toLocaleString('en-US', { maximumFractionDigits: 0 });
    }

    function statusClass(status) {
        if (status === 'Ending Soon') return 'bg-orange-600';
        if (status === 'Won') return 'bg-green-600';
        return 'bg-amber-700';
    }

    function renderWatchingItem(auction) {
        const row = document.createElement('div');
        row.className = 'flex flex-col gap-4 p-6 sm:flex-row sm:items-center';
        row.dataset.auctionId = String(auction.Id);

        row.innerHTML =
            '<div class="h-24 w-24 shrink-0 overflow-hidden rounded-xl border border-stone-200">' +
                '<img src="' + auction.ImageUrl + '" alt="' + auction.Name + '" class="h-full w-full object-cover"/>' +
            '</div>' +
            '<div class="min-w-0 flex-1">' +
                '<p class="text-[10px] font-semibold uppercase tracking-widest text-stone-400">' + auction.Category + '</p>' +
                '<h3 class="mt-1 font-semibold text-stone-900">' + auction.Name + '</h3>' +
                '<div class="mt-2 flex flex-wrap items-center gap-3 text-sm">' +
                    '<span class="rounded-full px-2.5 py-0.5 text-xs font-semibold text-white ' + statusClass(auction.Status) + '">' + auction.Status + '</span>' +
                    '<span class="text-stone-500">Current bid: <strong class="text-amber-700">' + formatPrice(auction.CurrentPrice) + '</strong></span>' +
                    '<span class="flex items-center gap-1 text-stone-500">' +
                        '<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">' +
                            '<circle cx="12" cy="12" r="10"/><path d="M12 6v6l4 2"/>' +
                        '</svg>' + auction.TimeRemaining +
                    '</span>' +
                '</div>' +
            '</div>' +
            '<div class="flex shrink-0 flex-col gap-2 sm:items-end">' +
                '<a href="/Auction/Index" class="rounded-xl border border-amber-700 px-5 py-2.5 text-center text-sm font-semibold text-amber-700 transition hover:bg-amber-700 hover:text-white">Place Bid</a>' +
                '<button type="button" data-remove-id="' + auction.Id + '" class="text-xs text-stone-400 transition hover:text-red-600">Remove</button>' +
            '</div>';

        return row;
    }

    function updateSummary(watchingCount) {
        if (watchingCountBadge) {
            watchingCountBadge.textContent = watchingCount + (watchingCount === 1 ? ' item' : ' items');
        }
        if (summaryWatching) summaryWatching.textContent = String(watchingCount);
        if (summaryTotal) summaryTotal.textContent = String(watchingCount + wonCount);
        window.AuctionCart.updateBadge(watchingCount);
    }

    function render() {
        const ids = window.AuctionCart.readWatching();
        watchingList.innerHTML = '';

        if (ids.length === 0) {
            watchingEmpty?.classList.remove('hidden');
            updateSummary(0);
            return;
        }

        watchingEmpty?.classList.add('hidden');

        ids.forEach(function (id) {
            const auction = findAuction(id);
            if (!auction) return;
            watchingList.appendChild(renderWatchingItem(auction));
        });

        updateSummary(ids.length);
    }

    watchingList.addEventListener('click', function (event) {
        const btn = event.target.closest('[data-remove-id]');
        if (!btn) return;
        const id = parseInt(btn.getAttribute('data-remove-id'), 10);
        window.AuctionCart.removeFromCart(id);
        render();
    });

    render();
})();
