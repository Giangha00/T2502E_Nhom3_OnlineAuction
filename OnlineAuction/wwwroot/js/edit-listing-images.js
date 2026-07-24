(function () {
  'use strict';

  var IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp'];
  var IMAGE_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.webp'];

  function parseLabels(raw) {
    if (!raw) {
      return {};
    }

    try {
      return JSON.parse(raw);
    } catch (_error) {
      return {};
    }
  }

  function label(labels, key, fallback) {
    return labels[key] || fallback;
  }

  function isAllowedImage(file) {
    var extension = (file.name.split('.').pop() || '').toLowerCase();
    var normalizedExtension = extension ? '.' + extension : '';
    return IMAGE_TYPES.indexOf(file.type) >= 0 || IMAGE_EXTENSIONS.indexOf(normalizedExtension) >= 0;
  }

  function syncFileInput(input, files) {
    if (!input) {
      return;
    }

    var dataTransfer = new DataTransfer();
    files.forEach(function (file) {
      dataTransfer.items.add(file);
    });
    input.files = dataTransfer.files;
  }

  function initUploader(root) {
    var labels = parseLabels(root.getAttribute('data-labels'));
    var maxGallery = parseInt(root.getAttribute('data-max-gallery') || '4', 10);
    var maxImageSize = parseInt(root.getAttribute('data-max-image-size') || String(5 * 1024 * 1024), 10);
    var originalCoverUrl = root.getAttribute('data-cover-url') || '';

    var primaryInput = root.querySelector('[data-primary-input]');
    var galleryInput = root.querySelector('[data-gallery-input]');
    var dropZone = root.querySelector('[data-gallery-dropzone]');
    var coverImg = root.querySelector('[data-cover-img]');
    var coverBadge = root.querySelector('[data-cover-pending-badge]');
    var changeCoverBtn = root.querySelector('[data-change-cover]');
    var clearCoverBtn = root.querySelector('[data-clear-cover]');
    var pendingGrid = root.querySelector('[data-pending-gallery-grid]');
    var slotsText = root.querySelector('[data-gallery-slots]');
    var errorEl = root.querySelector('[data-images-error]');
    var form = root.closest('form');
    var previewImg = form ? form.querySelector('[data-edit-preview-card] img') : null;

    var pendingPrimaryUrl = null;
    var pendingGallery = [];

    function setError(message) {
      if (!errorEl) {
        return;
      }

      if (!message) {
        errorEl.textContent = '';
        errorEl.classList.add('hidden');
        return;
      }

      errorEl.textContent = message;
      errorEl.classList.remove('hidden');
    }

    function activeExistingCount() {
      return root.querySelectorAll('[data-existing-gallery-item]:not([data-marked-for-removal])').length;
    }

    function remainingSlots() {
      return Math.max(0, maxGallery - activeExistingCount() - pendingGallery.length);
    }

    function updateSlotsText() {
      if (!slotsText) {
        return;
      }

      slotsText.textContent = label(labels, 'slotsRemaining', '{0} gallery slots left')
        .replace('{0}', String(remainingSlots()));
    }

    function updatePreviewImage(url) {
      if (previewImg && url) {
        previewImg.src = url;
      }
    }

    function setCoverPreview(url, isPending) {
      if (coverImg && url) {
        coverImg.src = url;
      }

      if (coverBadge) {
        coverBadge.classList.toggle('hidden', !isPending);
      }

      if (clearCoverBtn) {
        clearCoverBtn.classList.toggle('hidden', !isPending);
      }

      updatePreviewImage(url);
    }

    function revokePending(entry) {
      if (entry && entry.url) {
        URL.revokeObjectURL(entry.url);
      }
    }

    function syncGalleryInput() {
      syncFileInput(galleryInput, pendingGallery.map(function (entry) {
        return entry.file;
      }));
      if (galleryInput && pendingGallery.length === 0) {
        galleryInput.value = '';
      }
    }

    function trimPendingToCapacity() {
      while (activeExistingCount() + pendingGallery.length > maxGallery) {
        revokePending(pendingGallery.pop());
      }
      syncGalleryInput();
    }

    function renderPendingGallery() {
      if (!pendingGrid) {
        return;
      }

      pendingGrid.innerHTML = '';

      if (pendingGallery.length === 0) {
        pendingGrid.classList.add('hidden');
        updateSlotsText();
        return;
      }

      pendingGrid.classList.remove('hidden');

      pendingGallery.forEach(function (item) {
        var tile = document.createElement('div');
        tile.className = 'group relative aspect-square overflow-hidden rounded-xl border border-slate-200 bg-slate-100';
        tile.innerHTML =
          '<img src="' + item.url + '" alt="" class="h-full w-full object-cover" />' +
          '<span class="absolute left-2 top-2 rounded-md bg-blue-700 px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide text-white">' +
          label(labels, 'new', 'New') +
          '</span>' +
          '<button type="button" data-pending-remove="' + item.id + '" class="absolute right-1.5 top-1.5 cursor-pointer rounded-lg bg-red-600 px-2 py-1 text-[10px] font-bold uppercase text-white shadow-sm">' +
          label(labels, 'remove', 'Remove') +
          '</button>';
        pendingGrid.appendChild(tile);
      });

      pendingGrid.querySelectorAll('[data-pending-remove]').forEach(function (button) {
        button.addEventListener('click', function () {
          var id = button.getAttribute('data-pending-remove');
          var idx = pendingGallery.findIndex(function (entry) {
            return entry.id === id;
          });
          if (idx < 0) {
            return;
          }

          revokePending(pendingGallery[idx]);
          pendingGallery.splice(idx, 1);
          syncGalleryInput();
          renderPendingGallery();
        });
      });

      updateSlotsText();
    }

    function addGalleryFiles(fileList) {
      var files = Array.from(fileList || []);
      if (files.length === 0) {
        return;
      }

      var errors = [];
      var slotsBefore = remainingSlots();
      var accepted = 0;

      files.forEach(function (file) {
        if (remainingSlots() <= 0) {
          return;
        }

        if (!isAllowedImage(file)) {
          errors.push(label(labels, 'invalidFormat', '{0}: invalid format (JPG, PNG, WEBP only)').replace('{0}', file.name));
          return;
        }

        if (file.size > maxImageSize) {
          errors.push(label(labels, 'sizeLimit', '{0}: exceeds 5MB limit').replace('{0}', file.name));
          return;
        }

        pendingGallery.push({
          id: 'gallery_' + Date.now() + '_' + Math.random().toString(36).slice(2),
          file: file,
          url: URL.createObjectURL(file)
        });
        accepted += 1;
      });

      if (slotsBefore === 0 || (accepted < files.length && remainingSlots() === 0 && errors.length === 0)) {
        errors.push(label(labels, 'galleryLimit', 'You can upload up to {0} gallery images.').replace('{0}', String(maxGallery)));
      }

      syncGalleryInput();
      setError(errors[0] || '');
      renderPendingGallery();
    }

    function handlePrimaryChange() {
      if (!primaryInput || !primaryInput.files || primaryInput.files.length === 0) {
        return;
      }

      var file = primaryInput.files[0];
      if (!isAllowedImage(file)) {
        primaryInput.value = '';
        setError(label(labels, 'invalidFormat', '{0}: invalid format (JPG, PNG, WEBP only)').replace('{0}', file.name));
        return;
      }

      if (file.size > maxImageSize) {
        primaryInput.value = '';
        setError(label(labels, 'sizeLimit', '{0}: exceeds 5MB limit').replace('{0}', file.name));
        return;
      }

      if (pendingPrimaryUrl) {
        URL.revokeObjectURL(pendingPrimaryUrl);
      }

      pendingPrimaryUrl = URL.createObjectURL(file);
      setCoverPreview(pendingPrimaryUrl, true);
      setError('');
    }

    function clearPrimarySelection() {
      if (primaryInput) {
        primaryInput.value = '';
      }

      if (pendingPrimaryUrl) {
        URL.revokeObjectURL(pendingPrimaryUrl);
        pendingPrimaryUrl = null;
      }

      setCoverPreview(originalCoverUrl, false);
      setError('');
    }

    function setMarked(item, marked) {
      var checkbox = item.querySelector('[data-remove-checkbox]');
      var markButton = item.querySelector('[data-mark-remove]');
      var undoButton = item.querySelector('[data-undo-remove]');
      var badge = item.querySelector('[data-removal-badge]');

      if (checkbox) {
        checkbox.checked = marked;
      }

      if (marked) {
        item.setAttribute('data-marked-for-removal', 'true');
        item.classList.add('opacity-50', 'ring-2', 'ring-red-300');
      } else {
        item.removeAttribute('data-marked-for-removal');
        item.classList.remove('opacity-50', 'ring-2', 'ring-red-300');
      }

      if (badge) {
        badge.classList.toggle('hidden', !marked);
      }

      if (markButton) {
        markButton.classList.toggle('hidden', marked);
      }

      if (undoButton) {
        undoButton.classList.toggle('hidden', !marked);
      }

      trimPendingToCapacity();
      renderPendingGallery();
    }

    root.querySelectorAll('[data-existing-gallery-item]').forEach(function (item) {
      var markButton = item.querySelector('[data-mark-remove]');
      var undoButton = item.querySelector('[data-undo-remove]');

      if (markButton) {
        markButton.addEventListener('click', function () {
          setMarked(item, true);
        });
      }

      if (undoButton) {
        undoButton.addEventListener('click', function () {
          setMarked(item, false);
        });
      }
    });

    function openPrimaryPicker() {
      if (primaryInput) {
        primaryInput.click();
      }
    }

    if (changeCoverBtn) {
      changeCoverBtn.addEventListener('click', openPrimaryPicker);
    }

    if (coverImg) {
      coverImg.addEventListener('click', openPrimaryPicker);
      coverImg.classList.add('cursor-pointer');
    }

    if (primaryInput) {
      primaryInput.addEventListener('change', handlePrimaryChange);
    }

    if (clearCoverBtn) {
      clearCoverBtn.addEventListener('click', clearPrimarySelection);
    }

    if (dropZone && galleryInput) {
      dropZone.addEventListener('click', function () {
        if (remainingSlots() <= 0) {
          setError(label(labels, 'galleryLimit', 'You can upload up to {0} gallery images.').replace('{0}', String(maxGallery)));
          return;
        }
        galleryInput.click();
      });

      dropZone.addEventListener('dragover', function (event) {
        event.preventDefault();
        dropZone.classList.add('border-blue-400', 'bg-blue-50/40');
      });

      dropZone.addEventListener('dragleave', function () {
        dropZone.classList.remove('border-blue-400', 'bg-blue-50/40');
      });

      dropZone.addEventListener('drop', function (event) {
        event.preventDefault();
        dropZone.classList.remove('border-blue-400', 'bg-blue-50/40');
        if (event.dataTransfer && event.dataTransfer.files) {
          addGalleryFiles(event.dataTransfer.files);
        }
      });
    }

    if (galleryInput) {
      galleryInput.addEventListener('change', function () {
        addGalleryFiles(galleryInput.files);
      });
    }

    updateSlotsText();
    setCoverPreview(originalCoverUrl, false);

    window.addEventListener('pagehide', function () {
      if (pendingPrimaryUrl) {
        URL.revokeObjectURL(pendingPrimaryUrl);
      }
      pendingGallery.forEach(revokePending);
    });
  }

  document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('[data-edit-listing-images]').forEach(initUploader);
  });
})();
