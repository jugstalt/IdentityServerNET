$(function () {

    // Color scheme toggle — AJAX, no page reload
    $(document).on('click', '.navbar-colorscheme-toggle', function (e) {
        e.preventDefault();
        var $btn = $(this);
        var url = $btn.data('toggle-url');
        $.ajax({
            url: url,
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            success: function (data) {
                var isDark = data.scheme === 'dark';
                $('body')
                    .toggleClass('colorscheme-dark', isDark)
                    .toggleClass('colorscheme-light', !isDark);

                // swap icon class: dark→bulb-shining, light→darkmode
                var newIcon = isDark ? 'bulb-shining' : 'darkmode';
                var oldIcon = isDark ? 'darkmode' : 'bulb-shining';
                var scheme = isDark ? 'dark' : 'light';
                var size = 16;
                $('.navbar-colorscheme-toggle').each(function () {
                    var $icon = $(this).find('div');
                    var cls = $icon.attr('class') || '';
                    cls = cls.replace(oldIcon, newIcon)
                             .replace('bg-dark-', 'bg-' + scheme + '-')
                             .replace('bg-light-', 'bg-' + scheme + '-');
                    $icon.attr('class', cls);
                    $(this).attr('title', isDark ? 'Switch to light mode' : 'Switch to dark mode');
                });
            }
        });
    });

    // Auto-add Cancel button after primary submit buttons in forms
    $('form').each(function () {
        var $form = $(this);
        // skip forms that already have a cancel/back link
        if ($form.find('a.btn, [data-cancel]').length) return;

        $form.find('button[type=submit].btn-primary, input[type=submit].btn-primary').each(function () {
            var $submit = $(this);
            if ($submit.parent('.btn-row').length) return;
            var $cancel = $('<a>')
                .addClass('btn btn-outline-secondary cancel-btn')
                .attr('href', '#')
                .text('Cancel')
                .on('click', function (e) {
                    e.preventDefault();
                    history.back();
                });
            var $row = $('<div>').addClass('btn-row d-flex align-items-center gap-2');
            $submit.replaceWith($row);
            $row.append($submit).append($cancel);
        });
    });

    // Captcha input — auto uppercase
    $(document).on('input', '.captcha-input', function () {
        var pos = this.selectionStart;
        this.value = this.value.toUpperCase();
        this.setSelectionRange(pos, pos);
    });

    // Submit spinner — show loading indicator on primary submit buttons
    $('form').on('submit', function () {
        var $form = $(this);
        var $btn = $form.find('button.btn-primary:not([type=button]):not([type=reset])').not('[data-no-spinner]');
        if ($btn.length && !$btn.data('submitting')) {
            $btn.data('submitting', true);
            // preserve name/value so the server still receives the button's submit signal
            if ($btn.attr('name')) {
                $('<input>').attr({ type: 'hidden', name: $btn.attr('name'), value: $btn.attr('value') || '' })
                            .appendTo($form);
            }
            $btn.prop('disabled', true);
            $btn.html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>');
        }
    });

    $('.collapsable-tool').each(function (i, collapsable) {
        var $collapsable = $(collapsable);

        $collapsable.children('h4:first-child').click(function (e) {
            e.stopPropagation();
            $collapsable.toggleClass('expanded');
        });
    });

});
