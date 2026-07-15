(function () {
  'use strict';

  var IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp'];
  var IMAGE_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.webp'];
  var TEMPLATE_EXTENSIONS = ['.jpg', '.jpeg', '.png'];

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

  function isAllowedImage(file, mode) {
    var extension = (file.name.split('.').pop() || '').toLowerCase();
    var normalizedExtension = extension ? '.' + extension : '';
    var allowedExtensions = mode === 'template' ? TEMPLATE_EXTENSIONS : IMAGE_EXTENSIONS;

    if (IMAGE_TYPES.indexOf(file.type) >= 0) {
      return true;
    }

    return allowedExtensions.indexOf(normalizedExtension) >= 0;
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

  function createToastContainer(root) {
    var container = document.createElement('div');
    container.className = 'pointer-events-none fixed right-4 top-4 z-999999 flex max-w-sm flex-col gap-2';
    container.setAttribute('data-image-uploader-toast-root', '');
    root.appendChild(container);
    return container;
  }

  function showToast(root, message, type) {
    if (!message) {
      return;
    }

    var toastRoot = root.querySelector('[data-image-uploader-toast-root]') || createToastContainer(root);
    var toast = document.createElement('div');
    var tone = type === 'error'
      ? 'border-error-200 bg-error-50 text-error-700'
      : 'border-warning-200 bg-warning-50 text-warning-700';

    toast.className = 'pointer-events-auto rounded-lg border px-4 py-3 text-theme-sm shadow-theme-sm ' + tone;
    toast.textContent = message;
    toastRoot.appendChild(toast);

    window.setTimeout(function () {
      toast.remove();
    }, 4500);
  }

  function getLightbox() {
    var lightbox = document.querySelector('[data-admin-image-lightbox]');
    if (!lightbox) {
      return null;
    }

    if (lightbox._initialized) {
      return lightbox;
    }

    var image = lightbox.querySelector('[data-lightbox-image]');
    var closeButtons = lightbox.querySelectorAll('[data-lightbox-close]');
    var lastFocused = null;

    function closeLightbox() {
      lightbox.classList.add('hidden');
      lightbox.classList.remove('flex');
      lightbox.setAttribute('aria-hidden', 'true');
      document.body.classList.remove('overflow-hidden');
      if (lastFocused && typeof lastFocused.focus === 'function') {
        lastFocused.focus();
      }
    }

    function openLightbox(src, altText) {
      if (!image || !src) {
        return;
      }

      lastFocused = document.activeElement;
      image.src = src;
      image.alt = altText || '';
      lightbox.classList.remove('hidden');
      lightbox.classList.add('flex');
      lightbox.setAttribute('aria-hidden', 'false');
      document.body.classList.add('overflow-hidden');

      var firstClose = lightbox.querySelector('[data-lightbox-close]');
      if (firstClose) {
        firstClose.focus();
      }
    }

    closeButtons.forEach(function (button) {
      button.addEventListener('click', closeLightbox);
    });

    lightbox.addEventListener('click', function (event) {
      if (event.target === lightbox || event.target.hasAttribute('data-lightbox-backdrop')) {
        closeLightbox();
      }
    });

    document.addEventListener('keydown', function (event) {
      if (event.key === 'Escape' && !lightbox.classList.contains('hidden')) {
        closeLightbox();
      }
    });

    lightbox._open = openLightbox;
    lightbox._close = closeLightbox;
    lightbox._initialized = true;
    return lightbox;
  }

  function bindLightboxTrigger(element, getSrc, getAlt) {
    if (!element) {
      return;
    }

    element.addEventListener('click', function () {
      var lightbox = getLightbox();
      if (!lightbox || !lightbox._open) {
        return;
      }

      lightbox._open(getSrc(), getAlt());
    });

    element.addEventListener('keydown', function (event) {
      if (event.key === 'Enter' || event.key === ' ') {
        event.preventDefault();
        element.click();
      }
    });
  }

  function initProductImageUploader(root) {
    var mode = root.getAttribute('data-mode') || 'product';
    var labels = parseLabels(root.getAttribute('data-labels'));
    var maxGallery = parseInt(root.getAttribute('data-max-gallery') || '4', 10);
    var maxImageSize = parseInt(root.getAttribute('data-max-image-size') || String(5 * 1024 * 1024), 10);

    var primaryInput = root.querySelector('[data-primary-input]');
    var primaryPreviewWrap = root.querySelector('[data-primary-preview-wrap]');
    var primaryPreviewImg = root.querySelector('[data-primary-preview-img]');
    var primaryClearButton = root.querySelector('[data-primary-clear]');
    var savedPrimaryWrap = root.querySelector('[data-saved-primary-image]');
    var savedPrimaryImg = root.querySelector('[data-saved-primary-thumb]');

    var galleryInput = root.querySelector('[data-gallery-input]');
    var galleryGrid = root.querySelector('[data-gallery-preview-grid]');

    var pendingPrimary = null;
    var pendingGallery = [];

    function fileKey(file) {
      return [file.name, file.size, file.lastModified].join(':');
    }

    function isDuplicateGalleryFile(file) {
      return pendingGallery.some(function (entry) {
        return fileKey(entry.file) === fileKey(file);
      });
    }

    function revokePrimaryPreview() {
      if (pendingPrimary && pendingPrimary.url) {
        URL.revokeObjectURL(pendingPrimary.url);
      }
      pendingPrimary = null;
    }

    function revokeGalleryPreview(item) {
      if (item && item.url) {
        URL.revokeObjectURL(item.url);
      }
    }

    function clearGalleryPreviews() {
      pendingGallery.forEach(revokeGalleryPreview);
      pendingGallery = [];
      syncFileInput(galleryInput, []);
      renderGalleryPreviews();
    }

    function countExistingGalleryRemaining() {
      return root.querySelectorAll('[data-existing-gallery-item]:not([data-marked-for-removal])').length;
    }

    function availableGallerySlots() {
      return Math.max(0, maxGallery - countExistingGalleryRemaining() - pendingGallery.length);
    }

    function updatePrimaryPreviewVisibility() {
      if (!primaryPreviewWrap) {
        return;
      }

      if (pendingPrimary) {
        primaryPreviewWrap.classList.remove('hidden');
        primaryPreviewImg.src = pendingPrimary.url;
        primaryPreviewImg.alt = pendingPrimary.file.name;
      } else {
        primaryPreviewWrap.classList.add('hidden');
        if (primaryPreviewImg) {
          primaryPreviewImg.removeAttribute('src');
        }
      }
    }

    function clearPrimarySelection() {
      revokePrimaryPreview();
      if (primaryInput) {
        primaryInput.value = '';
      }
      updatePrimaryPreviewVisibility();
      if (savedPrimaryWrap) {
        savedPrimaryWrap.classList.remove('hidden');
      }
    }

    function validateImageFile(file) {
      if (!isAllowedImage(file, mode)) {
        showToast(
          root,
          label(labels, 'invalidFormat', 'Only JPG, PNG, or WEBP images are allowed.'),
          'error');
        return false;
      }

      if (file.size > maxImageSize) {
        var sizeMb = Math.round(maxImageSize / (1024 * 1024));
        showToast(
          root,
          label(labels, 'sizeLimit', 'Image must not exceed ' + sizeMb + 'MB.'),
          'error');
        return false;
      }

      return true;
    }

    function handlePrimaryChange() {
      if (!primaryInput || !primaryInput.files || primaryInput.files.length === 0) {
        return;
      }

      var file = primaryInput.files[0];
      if (!validateImageFile(file)) {
        primaryInput.value = '';
        return;
      }

      revokePrimaryPreview();
      pendingPrimary = {
        file: file,
        url: URL.createObjectURL(file)
      };

      syncFileInput(primaryInput, [file]);
      updatePrimaryPreviewVisibility();

      if (savedPrimaryWrap) {
        savedPrimaryWrap.classList.remove('hidden');
      }
    }

    function renderGalleryPreviews() {
      if (!galleryGrid) {
        return;
      }

      galleryGrid.innerHTML = '';

      if (pendingGallery.length === 0) {
        galleryGrid.classList.add('hidden');
        return;
      }

      galleryGrid.classList.remove('hidden');

      pendingGallery.forEach(function (item) {
        var tile = document.createElement('div');
        tile.className = 'group relative overflow-hidden rounded-lg border border-gray-200 bg-white dark:border-gray-700 dark:bg-gray-900';

        var openButton = document.createElement('button');
        openButton.type = 'button';
        openButton.className = 'block w-full cursor-zoom-in';
        openButton.setAttribute('data-gallery-preview-open', item.id);

        var image = document.createElement('img');
        image.src = item.url;
        image.alt = '';
        image.className = 'h-32 w-full object-cover';
        openButton.appendChild(image);

        var fileName = document.createElement('p');
        fileName.className = 'truncate px-2 py-1 text-theme-xs text-gray-500 dark:text-gray-400';
        fileName.textContent = item.file.name;
        fileName.title = item.file.name;

        var removeButton = document.createElement('button');
        removeButton.type = 'button';
        removeButton.setAttribute('data-gallery-remove', item.id);
        removeButton.setAttribute('aria-label', label(labels, 'remove', 'Remove'));
        removeButton.className = 'absolute right-2 top-2 inline-flex h-8 w-8 items-center justify-center rounded-full bg-gray-900/75 text-white hover:bg-error-600';
        removeButton.innerHTML = '<span aria-hidden="true">&times;</span>';

        tile.appendChild(openButton);
        tile.appendChild(fileName);
        tile.appendChild(removeButton);
        galleryGrid.appendChild(tile);
      });

      galleryGrid.querySelectorAll('[data-gallery-remove]').forEach(function (button) {
        button.addEventListener('click', function () {
          var id = button.getAttribute('data-gallery-remove');
          var index = pendingGallery.findIndex(function (entry) { return entry.id === id; });
          if (index >= 0) {
            revokeGalleryPreview(pendingGallery[index]);
            pendingGallery.splice(index, 1);
            syncFileInput(galleryInput, pendingGallery.map(function (entry) { return entry.file; }));
            if (galleryInput && pendingGallery.length === 0) {
              galleryInput.value = '';
            }
            renderGalleryPreviews();
          }
        });
      });

      galleryGrid.querySelectorAll('[data-gallery-preview-open]').forEach(function (button) {
        button.addEventListener('click', function () {
          var id = button.getAttribute('data-gallery-preview-open');
          var entry = pendingGallery.find(function (item) { return item.id === id; });
          var lightbox = getLightbox();
          if (entry && lightbox && lightbox._open) {
            lightbox._open(entry.url, entry.file.name);
          }
        });
      });
    }

    function handleGalleryChange() {
      if (!galleryInput || !galleryInput.files) {
        return;
      }

      var incoming = Array.from(galleryInput.files);
      if (incoming.length === 0) {
        return;
      }

      var slots = availableGallerySlots();
      var accepted = [];
      var rejectedCount = 0;

      incoming.forEach(function (file) {
        if (accepted.length >= slots) {
          rejectedCount += 1;
          return;
        }

        if (isDuplicateGalleryFile(file)) {
          return;
        }

        if (!validateImageFile(file)) {
          rejectedCount += 1;
          return;
        }

        accepted.push({
          id: 'gallery_' + Date.now() + '_' + Math.random().toString(36).slice(2),
          file: file,
          url: URL.createObjectURL(file)
        });
      });

      if (rejectedCount > 0 && accepted.length === 0 && slots === 0) {
        showToast(
          root,
          label(labels, 'galleryLimit', 'You can upload up to ' + maxGallery + ' gallery images.'),
          'error');
      } else if (rejectedCount > 0) {
        showToast(
          root,
          label(labels, 'galleryPartial', 'Some files were skipped due to limits or invalid format.'),
          'error');
      }

      pendingGallery = pendingGallery.concat(accepted);
      syncFileInput(galleryInput, pendingGallery.map(function (entry) { return entry.file; }));
      renderGalleryPreviews();
    }

    function enforcePendingGalleryLimit() {
      if (!galleryInput) {
        return;
      }

      var allowedPending = Math.max(0, maxGallery - countExistingGalleryRemaining());
      if (pendingGallery.length <= allowedPending) {
        return;
      }

      while (pendingGallery.length > allowedPending) {
        revokeGalleryPreview(pendingGallery.pop());
      }

      syncFileInput(galleryInput, pendingGallery.map(function (entry) { return entry.file; }));
      if (pendingGallery.length === 0) {
        galleryInput.value = '';
      }
      renderGalleryPreviews();
      showToast(
        root,
        label(labels, 'galleryLimit', 'You can upload up to ' + maxGallery + ' gallery images.'),
        'error');
    }

    function initExistingGallery() {
      root.querySelectorAll('[data-existing-gallery-item]').forEach(function (item) {
        var checkbox = item.querySelector('[data-remove-checkbox]');
        var markButton = item.querySelector('[data-mark-remove]');
        var undoButton = item.querySelector('[data-undo-remove]');
        var badge = item.querySelector('[data-removal-badge]');
        var image = item.querySelector('[data-existing-gallery-thumb]');
        var lightboxTrigger = image ? (image.closest('button') || image) : null;

        function setMarked(marked) {
          if (checkbox) {
            checkbox.checked = marked;
          }

          if (marked) {
            item.setAttribute('data-marked-for-removal', 'true');
            item.classList.add('opacity-60', 'ring-2', 'ring-error-300');
          } else {
            item.removeAttribute('data-marked-for-removal');
            item.classList.remove('opacity-60', 'ring-2', 'ring-error-300');
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

          enforcePendingGalleryLimit();
        }

        if (markButton) {
          markButton.addEventListener('click', function () {
            setMarked(true);
          });
        }

        if (undoButton) {
          undoButton.addEventListener('click', function () {
            setMarked(false);
          });
        }

        bindLightboxTrigger(lightboxTrigger, function () {
          return image ? image.getAttribute('src') : '';
        }, function () {
          return image ? image.getAttribute('alt') : '';
        });
      });
    }

    if (primaryInput) {
      primaryInput.addEventListener('change', handlePrimaryChange);
    }

    if (primaryClearButton) {
      primaryClearButton.addEventListener('click', clearPrimarySelection);
    }

    if (galleryInput) {
      galleryInput.addEventListener('change', handleGalleryChange);
    }

    var savedPrimaryTrigger = savedPrimaryImg ? (savedPrimaryImg.closest('button') || savedPrimaryImg) : null;
    var primaryPreviewTrigger = primaryPreviewImg ? (primaryPreviewImg.closest('button') || primaryPreviewImg) : null;

    bindLightboxTrigger(savedPrimaryTrigger, function () {
      return savedPrimaryImg ? savedPrimaryImg.getAttribute('src') : '';
    }, function () {
      return savedPrimaryImg ? savedPrimaryImg.getAttribute('alt') : '';
    });

    bindLightboxTrigger(primaryPreviewTrigger, function () {
      return pendingPrimary ? pendingPrimary.url : '';
    }, function () {
      return pendingPrimary ? pendingPrimary.file.name : '';
    });

    initExistingGallery();

    window.addEventListener('pagehide', function () {
      revokePrimaryPreview();
      pendingGallery.forEach(revokeGalleryPreview);
    });
  }

  document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('[data-product-image-uploader]').forEach(initProductImageUploader);
  });
})();
