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

    $('.collapsable-tool').each(function (i, collapsable) {
        var $collapsable = $(collapsable);

        $collapsable.children('h4:first-child').click(function (e) {
            e.stopPropagation();
            $collapsable.toggleClass('expanded');
        });
    });

});
