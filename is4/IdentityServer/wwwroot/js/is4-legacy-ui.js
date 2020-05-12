$(function () {

    $('.collapsable-tool').each(function (i, collapsable) {
        var $collapsable = $(collapsable);

        $collapsable.first('h4').click(function (e) {
            console.log(this);
            e.stopPropagation();
            $collapsable.toggleClass('expanded');
        });
    });

});