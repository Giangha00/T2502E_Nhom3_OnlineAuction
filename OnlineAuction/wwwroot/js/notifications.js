(function () {
	var page = document.querySelector('[data-notification-page]');
	if (!page) return;

	var searchInput = page.querySelector('[data-notification-search]');
	var typeFilter = page.querySelector('[data-notification-type-filter]');
	var readFilter = page.querySelector('[data-notification-read-filter]');
	var markAllButton = page.querySelector('[data-page-mark-all-read]');
	var emptyState = page.querySelector('[data-empty-notification]');
	var unreadCount = document.querySelector('[data-page-unread-count]');

	function cards() {
		return Array.from(page.querySelectorAll('[data-notification-card]'));
	}

	function updateUnreadCount() {
		var count = cards().filter(function (card) {
			return card.dataset.read === 'false';
		}).length;

		if (unreadCount) {
			unreadCount.textContent = String(count);
		}
	}

	function markCardRead(card) {
		card.dataset.read = 'true';
		card.classList.remove('ring-2', 'ring-amber-200');

		var dot = card.querySelector('[data-unread-dot]');
		if (dot) dot.remove();

		var bullet = card.querySelector('[data-unread-bullet]');
		if (bullet) bullet.remove();

		var button = card.querySelector('[data-mark-read]');
		if (button) {
			button.disabled = true;
		}
	}

	function applyFilters() {
		var query = (searchInput && searchInput.value ? searchInput.value : '').trim().toLowerCase();
		var typeValue = typeFilter ? typeFilter.value : 'all';
		var readValue = readFilter ? readFilter.value : 'all';
		var visibleCount = 0;

		cards().forEach(function (card) {
			var matchesSearch = !query || (card.dataset.search || '').indexOf(query) !== -1;
			var matchesType = typeValue === 'all' || card.dataset.type === typeValue;
			var matchesRead = readValue === 'all'
				|| (readValue === 'read' && card.dataset.read === 'true')
				|| (readValue === 'unread' && card.dataset.read === 'false');
			var visible = matchesSearch && matchesType && matchesRead;

			card.classList.toggle('hidden', !visible);
			if (visible) visibleCount += 1;
		});

		if (emptyState) {
			emptyState.classList.toggle('hidden', visibleCount !== 0);
		}

		updateUnreadCount();
	}

	page.addEventListener('click', function (event) {
		var markButton = event.target.closest('[data-mark-read]');
		if (markButton) {
			var card = markButton.closest('[data-notification-card]');
			if (card) {
				markCardRead(card);
				applyFilters();
			}
			return;
		}

		var deleteButton = event.target.closest('[data-delete-notification]');
		if (deleteButton) {
			var deleteCard = deleteButton.closest('[data-notification-card]');
			if (deleteCard) {
				deleteCard.remove();
				applyFilters();
			}
		}
	});

	if (markAllButton) {
		markAllButton.addEventListener('click', function () {
			cards().forEach(markCardRead);
			applyFilters();
		});
	}

	[searchInput, typeFilter, readFilter].forEach(function (control) {
		if (!control) return;
		control.addEventListener('input', applyFilters);
		control.addEventListener('change', applyFilters);
	});

	applyFilters();
})();
