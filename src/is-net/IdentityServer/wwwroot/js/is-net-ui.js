$(function () {

    // Color scheme toggle — AJAX, no page reload
    $(document).on('click', '.navbar-colorscheme-toggle', function (e) {
        e.preventDefault();
        var url = $(this).data('toggle-url');
        $.ajax({
            url: url,
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            success: function (data) {
                var isDark = data.scheme === 'dark';
                $('body')
                    .toggleClass('colorscheme-dark', isDark)
                    .toggleClass('colorscheme-light', !isDark);
            }
        });
    });

    $('.collapsable-tool').each(function (i, collapsable) {
        var $collapsable = $(collapsable);

        $collapsable.children('h4:first-child').click(function (e) {
            console.log(this);
            e.stopPropagation();
            $collapsable.toggleClass('expanded');
        });
    });

});