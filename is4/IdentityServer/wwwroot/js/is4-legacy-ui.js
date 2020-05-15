$(function () {

    $('.collapsable-tool').each(function (i, collapsable) {
        var $collapsable = $(collapsable);

        $collapsable.children('h4:first-child').click(function (e) {
            console.log(this);
            e.stopPropagation();
            $collapsable.toggleClass('expanded');
        });
    });

});