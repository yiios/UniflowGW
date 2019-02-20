(function () {
    $('#unbindB').click(function () {
        $.ajax({
            url: abp.appPath + 'Home/Unbind',
            type: 'POST',
            contentType: 'application/json',
            success: function (content) {
                //$('#').html(content);
            },
            error: function (e) { }
        });
    });
})();



function unbind() {

    if (window.confirm('你确定要解绑帐号吗？')) {
        return false;
    }
    $.ajax({
        url: '/home/unBind',
        type: 'POST',
        //contentType: 'application/json',
        success: function (content) {
            if (content.result == 1) {
                window.location("/home/index");
            }
        },
        error: function (e) { }
    });
}